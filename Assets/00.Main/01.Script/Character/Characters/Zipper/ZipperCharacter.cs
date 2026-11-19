using System.Collections;
using System.Collections.Generic;
using Fusion;
using ProjectMS.CharacterSystem;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    public class ZipperCharacter : CharacterBase
    {
        [Header("Basic Attack Settings")]
        [SerializeField] private CharacterProjectile projectilePrefab;
        [SerializeField] private float slashSpeed = 20f;
        [Min(0f)][SerializeField] private float forkAngleStep = 12f;
        [Tooltip("탄창(연속으로 벨 수 있는 횟수). 다 쓰면 재장전.")]
        [Min(1)] [SerializeField] private int magazineSize = 4;
        [Tooltip("재장전 시간(초). 0 이하면 캐릭터 정의의 기본공격 쿨타임을 쓴다(예전 '4번째 공격 뒤 쿨타임'과 같은 값).")]
        [SerializeField] private float reloadDuration = -1f;
        [Tooltip("탄창 안에서 한 번 벤 뒤 다음 공격까지 간격(초).")]
        [Min(0f)] [SerializeField] private float shotInterval = 0.1f;

        private bool isFirstBasicAttack = true;

        private float EffectiveReloadDuration =>
            (reloadDuration > 0f ? reloadDuration : (Definition != null ? Definition.GetCooldown(CharacterActionType.BasicAttack) : 1f))
            * ReloadSpeedMultiplier;
        private int EffectiveMagazineSize => Mathf.Max(1, Mathf.RoundToInt(magazineSize * MaxAmmoMultiplier));

        [Header("Q Skill Custom Settings")]
        [SerializeField] private float qDashPower = 14f;
        [SerializeField] private float qDashDuration = 0.16f;
        [SerializeField] private float qDashWidth = 4f;
        [SerializeField] private float qRechargeTime = 10f;
        [Min(1)] [SerializeField] private int qMaxCharges = 2;

        [Header("E Skill Custom Settings")]
        [SerializeField] private CharacterDeployable E_SkillPrefab;

        [Header("Ultimate (위상 돌파) Settings")]
        [SerializeField] private float ultimateTeleportDelay = 0.5f;
        [Min(1f)] [SerializeField] private float ultimateGroundCheckDistance = 1000f;
        [SerializeField] private float ultimateBlastRadius = 3f; // 시트에 값이 없어 임의 지정, 실제 값으로 조정 필요
        [SerializeField] private LayerMask outOfBoundsZoneLayer;

        [Header("Passive (차원 표식) Settings")]
        [Tooltip("표식 유지 시간 (초)")]
        [SerializeField] private float markDuration = 4f;
        [Tooltip("기본기로 표식을 소모했을 때 다이브(Q) 남은 재충전 시간 감소 비율")]
        [SerializeField] private float diveRechargeReductionRatio = 0.6f;

        private bool isDashing = false;
        private float currentQDamage = 0f;
        private HashSet<IDamageable> dashedHitTargets = new HashSet<IDamageable>();

        private const int UltimateMaxTargetSearchSteps = 64;
        private const float UltimateTargetClearance = 0.02f;
        
        private readonly List<Collider2D> ultimateGroundOverlaps = new List<Collider2D>(8);
        private readonly RaycastHit2D[] ultimateGroundBelowHits = new RaycastHit2D[1];
        
        private CharacterTimerHandle ultimateTimer;

        
        // 판정과 양쪽 화면의 표식이 같은 네트워크 만료 틱을 사용한다.
        [Networked, Capacity(8)] internal NetworkDictionary<NetworkId, TickTimer> DimensionalMarks => default;
        private readonly List<NetworkId> expiredMarksBuffer = new List<NetworkId>();

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
            isFirstBasicAttack = true;

            // 라운드 리셋 시 진행 중이던 궁극기 이동 예약 취소
            CancelTimer(ultimateTimer);
            SetMovementEnabled(true);

            // 패시브: 라운드 리셋 시 남아있는 표식 전부 제거
            DimensionalMarks.Clear();
        }

        protected override void OnCharacterDespawned()
        {
            if (qRechargeCoroutine != null) StopCoroutine(qRechargeCoroutine);
        }

        private void InitQCharges()
        {
            SetActionCharges(CharacterActionType.SkillQ, qMaxCharges);
            SetAutoCooldown(CharacterActionType.SkillQ, false);
            // 쿨타임 슬롯 UI가 잔탄 숫자와 "다음 충전까지" 오버레이를 그릴 수 있게 충전형으로 등록한다.
            SetActionChargeCapacity(CharacterActionType.SkillQ, qMaxCharges, qRechargeTime);
            ClearChargeRecharge(CharacterActionType.SkillQ);
            isDashing = false;

            // 스폰/리셋 시 재충전 코루틴 시작
            if (qRechargeCoroutine != null) StopCoroutine(qRechargeCoroutine);
            qRechargeCoroutine = StartCoroutine(ProcessQRecharge());
        }

        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            if (projectilePrefab == null)
                return false;

            Vector2 aimDirection = context.AimDirection.sqrMagnitude > 0.0001f
                ? context.AimDirection.normalized
                : new Vector2(FacingDirection, 0f);
            FireSlash(aimDirection, context.Damage);
            if (HasStateAuthority && !Runner.IsResimulation)
                Rpc_FxSlash(AttackOrigin.position, Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg);

            // 갈래 마법
            int forkCount = ForkedProjectileCount;
            if (forkCount > 0)
            {
                float forkDamage = context.Damage * ForkedProjectileDamageMultiplier;
                for (int i = 0; i < forkCount; i++)
                {
                    float angleOffset = forkAngleStep * (i / 2 + 1) * (i % 2 == 0 ? 1f : -1f);
                    FireSlash(Rotate(aimDirection, angleOffset), forkDamage);
                }
            }

            // 탄창: 다른 캐릭터와 같은 방식(충전 수 = 남은 탄, 마지막 발이면 쿨타임을 재장전 시간으로).
            if (isFirstBasicAttack)
            {
                SetActionCharges(CharacterActionType.BasicAttack, EffectiveMagazineSize);
                isFirstBasicAttack = false;
            }

            bool shouldReload = GetActionCharges(CharacterActionType.BasicAttack) - 1 == 0;
            if (shouldReload)
            {
                SetCooldownDuration(CharacterActionType.BasicAttack, EffectiveReloadDuration);
                SetActionCharges(CharacterActionType.BasicAttack, EffectiveMagazineSize + 1);
                NotifyReloadStarted(EffectiveReloadDuration);
            }
            else
            {
                SetCooldownDuration(CharacterActionType.BasicAttack, shotInterval);
            }

            return true;
        }

        private void FireSlash(Vector2 direction, float damage)
        {
            CharacterProjectile projectile = SpawnProjectile(
                projectilePrefab,
                AttackOrigin.position,
                direction,
                slashSpeed,
                damage,
                LayerMask.GetMask("Player"));

            if (projectile == null)
                return;

            // 바운스 마법 / 폭발 마법
            bool explosive = HasExplosiveProjectile;
            int bounces = explosive ? 0 : ProjectileBounceCount;
            projectile.ConfigureAugmentBehavior(bounces, explosive, ExplosiveProjectileDamageMultiplier);
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        // 휠 수동 재장전.
        protected override bool TryGetReloadInfo(out int magazine, out float duration)
        {
            magazine = EffectiveMagazineSize;
            duration = EffectiveReloadDuration;
            return true;
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
            if (HasStateAuthority && !Runner.IsResimulation)
                Rpc_FxDiveStart(transform.position, context.AimAngle, qDashDuration);

            // 5. 잔탄 1개 차감
            AddActionCharges(CharacterActionType.SkillQ, -1);

            // 재충전 중이 아니었다면(가득 차 있다가 처음 쓴 경우) 바로 타이머를 시작해서
            // UI 오버레이가 다음 프레임 대기 없이 곧장 돌기 시작하게 한다.
            if (!IsChargeRechargeActive(CharacterActionType.SkillQ))
                StartChargeRecharge(CharacterActionType.SkillQ);

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
            if (!TryResolveUltimateTarget(context.AimWorldPosition, out Vector2 targetPosition))
                return false;
            float ultimateDamage = context.Damage; // Definition에서 ATK 300%로 설정, 여기서 재계산하지 않음

            // 1. 사용 시 행동 불가 상태
            SetMovementEnabled(false);

            // 2. 이동할 위치 표시 이펙트 (즉시 재생)
            PlayActionEffect(context.Action, targetPosition, context.AimAngle);
            if (HasStateAuthority && !Runner.IsResimulation)
                Rpc_FxPhaseMarker(targetPosition, ultimateTeleportDelay);

            // 3. 0.5초 후 해당 장소로 이동하며 주위에 피해
            ultimateTimer = ScheduleTimer(ultimateTeleportDelay, () =>
            {
                // 시전 대기 중 지형이 움직일 수 있으므로 표시한 목적지를 다시 검사한다.
                if (!TryResolveUltimateTarget(targetPosition, out _, maxSearchSteps: 0))
                {
                    SetMovementEnabled(true);
                    return;
                }

                Vector2 departFrom = transform.position;
                Vector3 teleportPosition = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
                if (TryGetComponent(out Fusion.Addons.Physics.NetworkRigidbody2D networkBody))
                    networkBody.Teleport(teleportPosition);
                else
                    Rigidbody.position = targetPosition;
                if (HasStateAuthority)
                    Rpc_FxPhaseArrive(departFrom, targetPosition);

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

        private bool TryResolveUltimateTarget(
            Vector2 requestedPosition,
            out Vector2 targetPosition,
            int maxSearchSteps = UltimateMaxTargetSearchSteps)
        {
            targetPosition = requestedPosition;

            Collider2D bodyCollider = GetComponent<Collider2D>();

            if (bodyCollider == null || !bodyCollider.enabled ||
                !float.IsFinite(requestedPosition.x) || !float.IsFinite(requestedPosition.y))
                return false;

            Bounds bounds = bodyCollider.bounds;
            
            Vector2 centerOffset = (Vector2)bounds.center - (Vector2)transform.position; // position과 콜라이더 중심과의 차이 체크
            float bodyRadius = Mathf.Max(bounds.extents.x, bounds.extents.y); // 가로 반지름, 세로 반지름 중에 더 긴 거를 몸체의 반지름으로 사용
            
            if (bodyRadius <= 0f)
                return false;

            float checkRadius = bodyRadius + UltimateTargetClearance; // 이동 후보를 찾을 때 체크하는 범위 (반지름)
            float searchStep = Mathf.Max(bodyRadius * 0.5f, UltimateTargetClearance); // 이동 후보를 찾을 때 체크하는 간격

            Vector2 candidateCenter = requestedPosition + centerOffset;
            Vector2 searchDirection = Vector2.zero;

            ContactFilter2D groundFilter = new ContactFilter2D { layerMask = Definition.GroundLayer, useLayerMask = true, useTriggers = true };

            for (int step = 0; step <= maxSearchSteps; step++)
            {
                int overlapCount = Physics2D.OverlapCircle(
                    candidateCenter, checkRadius, groundFilter, ultimateGroundOverlaps);

                if (overlapCount == 0) // 찾은 공간이 비어있을 때
                {
                    ContactFilter2D landingFilter = new ContactFilter2D { layerMask = Definition.GroundLayer | outOfBoundsZoneLayer, useLayerMask = true, useTriggers = true };

                    // 매우 길게 아래로 바닥 혹은 OutOfBoundsZone이 있는지 체크해서 찾은 위치가 아래 방향으로 맵을 탈출했는지 체크
                    if (Physics2D.Raycast(
                            candidateCenter, 
                            Vector2.down, 
                            landingFilter,
                            ultimateGroundBelowHits, 
                            ultimateGroundCheckDistance) == 0)
                        return false;

                    targetPosition = candidateCenter - centerOffset;
                    return true;
                }

                if (step == 0) // 바로 못찾았으면 방향을 바꿔서 후보를 찾기 위해 탐색 방향을 정함
                {
                    Vector2 totalSeparation = Vector2.zero;
                    Vector2 largestSeparation = Vector2.zero;

                    for (int i = 0; i < overlapCount; i++)
                    {
                        Vector2 separation = GetUltimateGroundSeparation(ultimateGroundOverlaps[i], candidateCenter, checkRadius);
                        
                        totalSeparation += separation;

                        if (separation.sqrMagnitude > largestSeparation.sqrMagnitude)
                            largestSeparation = separation;
                    }

                    searchDirection = totalSeparation.sqrMagnitude > 0.0001f
                        ? totalSeparation.normalized
                        : largestSeparation.normalized;

                    if (searchDirection.sqrMagnitude < 0.0001f)
                        return false;
                }

                candidateCenter += searchDirection * searchStep;
            }

            // 막힌 공간이나 지나치게 큰 장애물에서 너무 오랫동안 탐색하고 있으면 실패 처리
            return false;
        }

        private static Vector2 GetUltimateGroundSeparation(Collider2D ground, Vector2 center, float radius)
        {
            Vector2 awayFromGround = center - ground.ClosestPoint(center);

            float distance = awayFromGround.magnitude;
            if (distance > 0.0001f)
                return awayFromGround / distance * (radius - distance + UltimateTargetClearance); // 방향 * 거리

            // else: distance가 너무 짧으면
            // 검사 위치가 지형 내부에 있으면 ClosestPoint는 입력 위치를 그대로 반환함 > distance가 매우 짧아짐

            // 지형의 경계 상자 바깥 네 방향에서 표면 위치를 구해 탈출 방향을 찾음
            float probeDistance = ground.bounds.size.magnitude + radius;
            Vector2 nearestExit = Vector2.zero;
            float nearestExitDistance = float.PositiveInfinity;
            for (int side = 0; side < 4; side++)
            {
                Vector2 direction = side switch
                {
                    0 => Vector2.up,
                    1 => Vector2.down,
                    2 => Vector2.left,
                    _ => Vector2.right // 3
                };
                Vector2 exit = ground.ClosestPoint(center + direction * probeDistance) - center;
                float exitDistance = exit.magnitude;

                if (exitDistance < nearestExitDistance)
                {
                    nearestExitDistance = exitDistance;
                    nearestExit = exitDistance > 0.0001f ? exit / exitDistance : direction;
                }
            }

            return nearestExit * (nearestExitDistance + radius + UltimateTargetClearance);
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            if (!HasStateAuthority || DimensionalMarks.Count == 0) return;

            expiredMarksBuffer.Clear();
            foreach (var mark in DimensionalMarks)
            {
                if (mark.Value.ExpiredOrNotRunning(Runner))
                    expiredMarksBuffer.Add(mark.Key);
            }
            foreach (NetworkId targetId in expiredMarksBuffer)
                DimensionalMarks.Remove(targetId);
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

                // 음수 = 잔탄 시스템이 아직 초기화되지 않은 상태(사망 직후 등). 다음 InitQCharges까지 대기.
                if (currentCharges < 0)
                    continue;

                if (currentCharges >= qMaxCharges)
                {
                    if (IsChargeRechargeActive(CharacterActionType.SkillQ))
                        ClearChargeRecharge(CharacterActionType.SkillQ);
                    continue;
                }

                // 충전 타이머(네트워크 동기화되는 표시용 값)가 다 돌았으면 잔탄 1개를 회복한다.
                // 충전은 한 번에 하나씩 순차로 회복되므로, 아직 모자라면 다음 충전 타이머를 이어서 시작한다.
                if (HasChargeRechargeElapsed(CharacterActionType.SkillQ))
                {
                    AddActionCharges(CharacterActionType.SkillQ, 1);
                    currentCharges++;

                    if (currentCharges >= qMaxCharges)
                        ClearChargeRecharge(CharacterActionType.SkillQ);
                    else
                        StartChargeRecharge(CharacterActionType.SkillQ);
                    continue;
                }

                if (!IsChargeRechargeActive(CharacterActionType.SkillQ))
                    StartChargeRecharge(CharacterActionType.SkillQ);
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
            if (!HasStateAuthority || target is not CharacterBase character || character.Object == null) return;

            DimensionalMarks.Set(character.Object.Id, TickTimer.CreateFromSeconds(Runner, markDuration));
        }

        private void TryConsumeDimensionalMark(CharacterBase target)
        {
            NetworkId targetId = target.Object.Id;
            if (!DimensionalMarks.TryGet(targetId, out TickTimer expiry) || expiry.ExpiredOrNotRunning(Runner))
                return;

            // 표식 소모
            DimensionalMarks.Remove(targetId);
            Rpc_FxMarkConsumed(targetId);

            // 다이브(Q) 남은 재충전 시간 60% 감소
            ReduceDiveRemainingRecharge(diveRechargeReductionRatio);
        }

        private void ReduceDiveRemainingRecharge(float ratio)
        {
            int currentCharges = GetActionCharges(CharacterActionType.SkillQ);

            // 이미 잔탄이 가득 차 있으면(재충전 중이 아니면) 줄일 시간이 없음
            if (currentCharges >= qMaxCharges) return;

            float remaining = GetChargeRechargeRemaining(CharacterActionType.SkillQ);
            if (remaining <= 0f) return;

            // 남은 시간을 ratio만큼 줄여서 다시 시작한다. 총 시간(UI 기준)은 그대로라
            // 오버레이가 그만큼 줄어든 채로 보인다.
            StartChargeRecharge(CharacterActionType.SkillQ, remaining * (1f - ratio));
        }

        // ───────── 연출 신호(모든 클라에서 ZipperSkillVisuals가 그린다) ─────────

        private ZipperSkillVisuals Visuals => GetComponent<ZipperSkillVisuals>();

        [Fusion.Rpc(Fusion.RpcSources.StateAuthority, Fusion.RpcTargets.All)]
        private void Rpc_FxSlash(Vector2 origin, float angle) => Visuals?.PlaySlash(origin, angle);

        [Fusion.Rpc(Fusion.RpcSources.StateAuthority, Fusion.RpcTargets.All)]
        private void Rpc_FxDiveStart(Vector2 origin, float angle, float duration) => Visuals?.PlayDiveStart(origin, angle, duration);

        [Fusion.Rpc(Fusion.RpcSources.StateAuthority, Fusion.RpcTargets.All)]
        private void Rpc_FxPhaseMarker(Vector2 target, float delay) => Visuals?.PlayPhaseMarker(target, delay);

        [Fusion.Rpc(Fusion.RpcSources.StateAuthority, Fusion.RpcTargets.All)]
        private void Rpc_FxPhaseArrive(Vector2 from, Vector2 to) => Visuals?.PlayPhaseArrive(from, to);

        [Fusion.Rpc(Fusion.RpcSources.StateAuthority, Fusion.RpcTargets.All)]
        private void Rpc_FxMarkConsumed(Fusion.NetworkId target) => Visuals?.PlayMarkConsumed(target);
    }
}
