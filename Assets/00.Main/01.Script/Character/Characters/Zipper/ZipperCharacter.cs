using System.Collections;
using System.Collections.Generic;
using ProjectMS.CharacterSystem;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    public class ZipperCharacter : CharacterBase
    {
        [Header("Basic Attack Settings")]
        [SerializeField] private CharacterProjectile projectilePrefab;
        [SerializeField] private float slashSpeed = 20f;
        [SerializeField] private int reloadCount = 3;

        [Header("Q Skill Custom Settings")]
        [SerializeField] private float qDashPower = 14f;
        [SerializeField] private float qDashDuration = 0.16f;
        [SerializeField] private float qDashWidth = 4f;
        [SerializeField] private float qRechargeTime = 10f;

        [Header("E Skill Custom Settings")]
        [SerializeField] private CharacterDeployable E_SkillPrefab;

        [Header("Ultimate (위상 돌파) Settings")]
        [SerializeField] private float ultimateTeleportDelay = 0.5f;
        [SerializeField] private float ultimateBlastRadius = 3f; // 시트에 값이 없어 임의 지정, 실제 값으로 조정 필요

        [Header("Passive (차원 표식) Settings")]
        [Tooltip("표식 유지 시간 (초)")]
        [SerializeField] private float markDuration = 4f;
        [Tooltip("기본기로 표식을 소모했을 때 다이브(Q) 남은 재충전 시간 감소 비율")]
        [SerializeField] private float diveRechargeReductionRatio = 0.6f;

        private float qRechargeTimer = 0f;
        private bool isDashing = false;
        private float currentQDamage = 0f;
        private HashSet<IDamageable> dashedHitTargets = new HashSet<IDamageable>();
        private CharacterTimerHandle ultimateTimer;

        // 패시브: 차원 표식 - 대상별 남은 유지 시간 보관 (OnPassiveTick에서 매 프레임 감소)
        private Dictionary<IDamageable, float> dimensionalMarks = new Dictionary<IDamageable, float>();
        private List<IDamageable> expiredMarksBuffer = new List<IDamageable>();

        private Coroutine dashCoroutine;
        private Coroutine qRechargeCoroutine;

        protected override void OnCharacterSpawned()
        {
            base.OnCharacterSpawned();
            InitQCharges();
        }
        protected override void OnResetCharacter()
        {
            base.OnResetCharacter();

            // 실행 중인 대시 및 재충전 코루틴 정지 후 초기화
            if (dashCoroutine != null) StopCoroutine(dashCoroutine);
            if (qRechargeCoroutine != null) StopCoroutine(qRechargeCoroutine);

            InitQCharges();

            // 라운드 리셋 시 진행 중이던 궁극기 이동 예약 취소
            CancelTimer(ultimateTimer);
            SetMovementEnabled(true);

            // 패시브: 라운드 리셋 시 남아있는 표식 전부 제거
            ClearAllDimensionalMarks();
        }

        private void InitQCharges()
        {
            SetActionCharges(CharacterActionType.SkillQ, 2);
            SetAutoCooldown(CharacterActionType.SkillQ, false);
            qRechargeTimer = 0f;
            isDashing = false;

            // 스폰/리셋 시 재충전 코루틴 시작
            if (qRechargeCoroutine != null) StopCoroutine(qRechargeCoroutine);
            qRechargeCoroutine = StartCoroutine(ProcessQRecharge());
        }

        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            CharacterProjectile projectile = SpawnProjectile(
                 projectilePrefab,
                 AttackOrigin.position,
                 context.AimDirection,
                 slashSpeed,
                 context.Damage,
                 LayerMask.GetMask("Player")
            );

            // CharacterProjectile의 LifeTime으로 Distance구현

            if (reloadCount <= 0)
            {
                reloadCount = 3;
                return true;
            }
            reloadCount--;

            return false;
        }

        protected override bool OnSkillQ(CharacterActionContext context)
        {
            // 1. 프레임워크 쿨다운 상태와 관계없이 잔탄이 1개 이상이면 발동
            int currentCharges = GetActionCharges(CharacterActionType.SkillQ);
            if (currentCharges <= 0)
            {
                return false;
            }

            // 2. 대시 데이터 세팅
            currentQDamage = context.Damage;
            dashedHitTargets.Clear();

            // 3. 물리 대시 실행
            StartDash(context.AimDirection, qDashPower, qDashDuration);

            // 4. 이펙트 재생
            PlayActionEffect(context.Action, transform.position, context.AimAngle);

            // 5. 잔탄 1개 차감
            AddActionCharges(CharacterActionType.SkillQ, -1);

            // 6. 대시 실시간 충돌 체크 코루틴 실행
            if (dashCoroutine != null) StopCoroutine(dashCoroutine);
            dashCoroutine = StartCoroutine(ProcessDashCollision(qDashDuration));

            return false;
        }

        protected override bool OnSkillE(CharacterActionContext context)
        {
            if (E_SkillPrefab == null) return false;

            Vector2 spawnPosition = (Vector2)transform.position + (context.AimDirection * 1.5f);

            CharacterDeployable ownedEntity = SpawnOwnedEntity(
                E_SkillPrefab,
                context.Action,
                spawnPosition,
                maxCount: 1,
                replaceOldest: true,
                initialize: entity =>
                {
                    entity.transform.rotation = Quaternion.Euler(0f, 0f, context.AimAngle);
                }
            );

            return true;
        }

        protected override bool OnUltimate(CharacterActionContext context)
        {
            // 위상 돌파: 마우스 위치에 이동 위치 표시 -> 0.5초 후 해당 장소로 이동하며 주위에 피해
            Vector3 targetPosition = context.AimWorldPosition;
            float ultimateDamage = context.Damage; // Definition에서 ATK 300%로 설정, 여기서 재계산하지 않음

            // 1. 사용 시 행동 불가 상태
            SetMovementEnabled(false);

            // 2. 이동할 위치 표시 이펙트 (즉시 재생)
            PlayActionEffect(context.Action, targetPosition, context.AimAngle);

            // 3. 0.5초 후 해당 장소로 이동하며 주위에 피해
            ultimateTimer = ScheduleTimer(ultimateTeleportDelay, () =>
            {
                transform.position = targetPosition;

                var targets = FindDamageablesInCircle(
                    targetPosition,
                    ultimateBlastRadius,
                    LayerMask.GetMask("Player")
                );

                foreach (IDamageable target in targets)
                {
                    DealDamage(target, ultimateDamage, CharacterDamageSource.Direct);

                    // 패시브: 궁극기로 피해를 입힌 대상에게 차원 표식 부여
                    ApplyDimensionalMark(target);
                }

                SetMovementEnabled(true);
            });

            // 궁극기 게이지 미사용(n) -> Definition의 Ultimate Cooldown을 그대로 사용
            return true;
        }

        protected override void OnPassiveTick(float deltaTime)
        {
            // 패시브: 차원 표식 - 남은 유지 시간을 매 틱 감소시키고, 만료된 표식을 제거한다.
            if (dimensionalMarks.Count == 0) return;

            expiredMarksBuffer.Clear();

            List<IDamageable> keys = new List<IDamageable>(dimensionalMarks.Keys);
            foreach (IDamageable target in keys)
            {
                float remaining = dimensionalMarks[target] - deltaTime;
                if (remaining <= 0f)
                {
                    expiredMarksBuffer.Add(target);
                }
                else
                {
                    dimensionalMarks[target] = remaining;
                }
            }

            for (int i = 0; i < expiredMarksBuffer.Count; i++)
            {
                dimensionalMarks.Remove(expiredMarksBuffer[i]);
            }
        }

        private IEnumerator ProcessDashCollision(float duration)
        {
            isDashing = true;
            float timer = duration;

            // 시작 즉시 1차 체크
            CheckDashCollision();

            while (timer > 0f)
            {
                yield return null;
                timer -= Time.deltaTime;
                CheckDashCollision();
            }

            isDashing = false;
        }

        private IEnumerator ProcessQRecharge()
        {
            while (true)
            {
                yield return null;

                int currentCharges = GetActionCharges(CharacterActionType.SkillQ);
                if (currentCharges < 2)
                {
                    qRechargeTimer += Time.deltaTime;
                    if (qRechargeTimer >= qRechargeTime)
                    {
                        AddActionCharges(CharacterActionType.SkillQ, 1);
                        qRechargeTimer = 0f;
                    }
                }
                else
                {
                    qRechargeTimer = 0f;
                }
            }
        }

        private void CheckDashCollision()
        {
            float radius = qDashWidth * 0.5f;

            var targets = FindDamageablesInCircle(
                transform.position,
                radius,
                LayerMask.GetMask("Player")
            );

            foreach (IDamageable target in targets)
            {
                if (dashedHitTargets.Add(target))
                {
                    DealDamage(target, currentQDamage, CharacterDamageSource.Direct);

                    // 패시브: Q(다이브)로 피해를 입힌 대상에게 차원 표식 부여
                    ApplyDimensionalMark(target);
                }
            }
        }

        protected override float ModifyOutgoingDamage(CharacterBase target, float damage, CharacterDamageSource source)
        {
            return damage;
        }

        protected override void OnProjectileDespawned(CharacterProjectile projectile, ProjectileDespawnReason reason, CharacterBase hitTarget)
        {
            // 기본 공격 투사체가 대상을 맞혔을 때, 표식이 있으면 소모하고 다이브 재충전을 단축
            if (reason == ProjectileDespawnReason.HitCharacter && hitTarget != null)
            {
                TryConsumeDimensionalMark(hitTarget);
            }
        }

        private void ApplyDimensionalMark(IDamageable target)
        {
            if (target == null) return;

            // 이미 표식이 있어도 없어도 동일하게 남은 시간을 4초로 (재)설정한다 (표식 갱신)
            dimensionalMarks[target] = markDuration;
        }

        private void TryConsumeDimensionalMark(IDamageable target)
        {
            if (target == null) return;
            if (!dimensionalMarks.ContainsKey(target)) return;

            // 표식 소모
            dimensionalMarks.Remove(target);

            // 다이브(Q) 남은 재충전 시간 60% 감소
            ReduceDiveRemainingRecharge(diveRechargeReductionRatio);
        }

        private void ReduceDiveRemainingRecharge(float ratio)
        {
            int currentCharges = GetActionCharges(CharacterActionType.SkillQ);

            // 이미 잔탄이 가득 차 있으면(재충전 중이 아니면) 줄일 시간이 없음
            if (currentCharges >= 2) return;

            float remaining = qRechargeTime - qRechargeTimer;
            if (remaining <= 0f) return;

            qRechargeTimer += remaining * ratio;
            if (qRechargeTimer > qRechargeTime)
            {
                qRechargeTimer = qRechargeTime;
            }
        }

        private void ClearAllDimensionalMarks()
        {
            dimensionalMarks.Clear();
        }
    }
}
