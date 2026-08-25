using System.Collections;
using System.Collections.Generic;
using ProjectMS.CharacterSystem;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    public class ZipperCharacter : CharacterBase
    {
        [Header("Basic Attack Settings")]
        [SerializeField] private float slashDistance = 0f;
        [SerializeField] private float slashAngle = 0f;
        [SerializeField] private int reloadCount = 3;

        [Header("Q Skill Custom Settings")]
        [SerializeField] private float qDashPower = 14f;
        [SerializeField] private float qDashDuration = 0.16f;
        [SerializeField] private float qDashWidth = 4f;
        [SerializeField] private float qRechargeTime = 10f;

        [Header("Ultimate (위상 돌파) Settings")]
        [SerializeField] private float ultimateTeleportDelay = 0.5f;
        [SerializeField] private float ultimateBlastRadius = 3f; // 시트에 값이 없어 임의 지정, 실제 값으로 조정 필요

        private float qRechargeTimer = 0f;
        private bool isDashing = false;
        private float currentQDamage = 0f;
        private HashSet<IDamageable> dashedHitTargets = new HashSet<IDamageable>();
        private CharacterTimerHandle ultimateTimer;

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
            var targets = FindEnemiesInArc(
                AttackOrigin.position,
                context.AimDirection,
                slashDistance,
                slashAngle,
                LayerMask.GetMask("Player")
            );

            foreach (IDamageable target in targets)
            {
                DealDamage(target, context.Damage, CharacterDamageSource.Direct);
            }

            PlayActionEffect(context.Action, AttackOrigin.position, context.AimAngle);

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
            return false;
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
                }

                SetMovementEnabled(true);
            });

            // 궁극기 게이지 미사용(n) -> Definition의 Ultimate Cooldown을 그대로 사용
            return true;
        }

        protected override void OnPassiveTick(float deltaTime)
        {
            
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
                }
            }
        }

        protected override float ModifyOutgoingDamage(CharacterBase target, float damage, CharacterDamageSource source)
        {
            return damage;
        }

        protected override void OnProjectileDespawned(CharacterProjectile projectile, ProjectileDespawnReason reason, CharacterBase hitTarget)
        {
        }
    }
}