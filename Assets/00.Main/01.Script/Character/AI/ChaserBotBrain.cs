using ProjectMS.CharacterSystem.Examples;
using UnityEngine;

namespace ProjectMS.CharacterSystem.AI
{
    /// <summary>
    /// 체이서 AI. 평소에는 가까이서 싸우다가 궁극기가 충전되었을 때 멀리 떨어져서 궁극기를 사용한다.
    ///
    /// [기본공격 - 4발 탄창 + 재장전]
    ///   가능한 가까이 붙어 공격한다. 재장전 중엔 거리를 벌리고, 안전한 거리에서 조금씩 왕복한다.
    ///
    /// [Q - 듀얼 리볼버]
    ///   빠르게 직선 상으로 날아가는 것만 체크한다.
    ///
    /// [E - 전술 도약(도주기)]
    ///   상대와 거리를 둬야 할 때 사용한다.
    ///   재장전 중 / 궁극기 충전 시 이 스킬을 사용해 거리를 두려고 시도한다.
    ///
    /// [궁극기 - 데드아이]
    ///   궁극기 사용 중 무방비 상태가 되기 때문에 상대와 거리가 멀 때만 사용한다.
    ///   충전 시 거리를 두려는 시도는 하지만 맞으면서까지 도망가지는 않는다.
    ///
    /// [패시브 - 비열한 거리]
    ///   상대의 뒤에서 가하는 모든 공격이 치명타가 되므로, 웬만해선 상대의 뒤로 갈 수 있도록 시도한다.
    /// </summary>
    public class ChaserBotBrain : CharacterBotBrain
    {
        // 고정값
        private const float InactiveRetreatStartTime = -1f;
        private const float InstantProjectileSpeed = 0f;

        [Header("스킬 관련")]
        [SerializeField][Min(0f)] private float basicAttackRange = 7f;

        [SerializeField][Min(0f)] private float dualRevolverRange = 16f;

        [SerializeField][Min(0f)] private float ultimateMinRange = 8f;
        [SerializeField][Min(0f)] private float ultimateMaxRange = 22f;
        [SerializeField][Min(0f)] private float ultimateMaxVerticalSpeed = 0.5f;
        [SerializeField][Min(0f)] private float ultimateApproachPredictionTime = 0.5f;
        [SerializeField][Min(0f)] private float ultimateSafeDistanceMargin = 1f;

        [SerializeField][Min(0f)] private float maxRetreatTime = 2.5f;
        [SerializeField][Min(0f)] private float retreatRetryDelay = 3f;

        [SerializeField][Range(0f, 1f)] private float lowAmmoThresholdRatio = 0.5f;

        [SerializeField][Min(0f)] private float defaultAimErrorMultiplier = 1f;
        [SerializeField][Min(0f)] private float snipingAimErrorMultiplier = 0.35f;

        [Header("이동 관련")]
        [SerializeField][Min(0f)] private float closeRange = 1.25f;
        [SerializeField][Min(0f)] private float reloadRange = 4f;
        
        [SerializeField][Min(0f)] private float preferredDistanceTolerance = 1f;
        [SerializeField][Min(0f)] private float backPositionTolerance = 0.3f;
        
        [SerializeField][Min(0f)] private float climbMinHeight = 1.5f;
        [SerializeField][Min(0f)] private float climbMaxHorizontalDistance = 4f;
        [SerializeField][Min(0f)] private float climbJumpHoldDuration = 0.35f;
        
        [SerializeField][Min(0f)] private float crossMaxHorizontalDistance = 2.5f;
        [SerializeField][Min(0f)] private float crossMaxHeightDifference = 1.5f;
        [SerializeField][Min(0f)] private float crossJumpHoldDuration = 0.25f;
        
        [SerializeField][Min(0f)] private float grenadeEscapeJumpHoldDuration = 0.3f;
        

        [Header("감지 및 판단 관련")]
        [SerializeField][Min(0f)] private float escapeLookAhead = 3f;
        [SerializeField][Min(0f)] private float airborneGroundCheckDepth = 3.5f;
        [SerializeField][Min(1f)] private float thinkPauseToleranceMultiplier = 1.5f;
        
        [SerializeField][Min(0f)] private float minSkillDecisionInterval = 0.15f;
        [SerializeField][Min(0f)] private float maxSkillDecisionInterval = 0.35f;

        private ChaserCharacter chaser;

        private float lastThinkTime;
        private float lastHealth; // 피해 받았는지 비교용

        private float nextShotTime;
        private float nextSkillDecisionTime;
        private int shotsInBurst;
        private int burstSize;

        private float retreatStartedAt = InactiveRetreatStartTime;
        private float nextUltimateAttemptTime;

        private bool reloadStrafeAway = true;

        private float lastGroundedFeetY;
        private CharacterBase crossTarget;
        private float crossMoveDirection;
        private float crossDestinationX;

        protected override void Initialize(CharacterBase self, BotDifficulty difficulty)
        {
            base.Initialize(self, difficulty);

            chaser = (ChaserCharacter)self;
            ResetDecisions();
        }

        protected override void Think(ref CharacterInputSnapshot input, float deltaTime)
        {
            // 모종의 이유로 판단이 많이 늦게 실행됐다면 상태 다시 지정
            if (Clock - lastThinkTime > deltaTime * thinkPauseToleranceMultiplier)
                ResetDecisions();

            lastThinkTime = Clock;

            if (Self.IsGrounded)
                lastGroundedFeetY = SelfFeetY;

            // 맞았으면 궁극기 준비 미루기
            if (Self.CurrentHealth < lastHealth || Self.IsHitstunned)
                DelayUltimate();

            lastHealth = Self.CurrentHealth;

            Vector2 attackOrigin = chaser.AttackOriginPosition;
            Vector2 projectileOrigin = chaser.ProjectileOriginPosition;

            float distance = Vector2.Distance(SelfCenter, PerceivedTargetPosition);
            float horizontalDistance = Mathf.Abs(PerceivedTargetPosition.x - SelfCenter.x);
            bool hasLineOfSight = HasLineOfSight(projectileOrigin, PerceivedTargetPosition);

            input.AimWorldPosition = PerceivedTargetPosition;

            if (Self.IsHitstunned)
            {
                crossTarget = null;
                return;
            }

            // 궁극기 사용 중엔 기본 공격으로만 공격
            if (chaser.IsSniping)
            {
                crossTarget = null;

                TryBasicAttack(ref input, attackOrigin, PerceivedTargetPosition, PerceivedTargetVelocity,
                    distance, hasLineOfSight, sniping: true);

                return;
            }

            // 회피 판단은 한 틱에 한 번만 호출해야 해서 저장해두기
            bool shouldDodgeProjectile = ShouldDodgeProjectile();
            float grenadeEscapeDirection = EnemyGrenadeEscapeDirection();

            // 궁극기 체크
            bool shouldPrepareUltimate = ShouldPrepareUltimate(horizontalDistance, hasLineOfSight);
            bool canTryUltimate = shouldPrepareUltimate && !shouldDodgeProjectile && Mathf.Approximately(grenadeEscapeDirection, 0f);
            if (canTryUltimate)
            {
                bool isUltimateUsed = TryUltimate(ref input, SelfCenter, PerceivedTargetPosition, PerceivedTargetVelocity, distance, horizontalDistance);
                if (isUltimateUsed)
                    return;
            }

            // 회피 체크
            if (shouldDodgeProjectile)
                RequestJump();

            // 재장전 체크 (상대와 멀리 떨어져 있고 남은 탄 수가 적으면 재장전)
            bool hasLowAmmo = Self.TryGetAmmo(out int ammo, out int capacity) && ammo <= capacity * lowAmmoThresholdRatio;

            bool hasBreakInCombat = !hasLineOfSight || distance > basicAttackRange;
            bool shouldReload = !Self.IsReloading && hasLowAmmo && hasBreakInCombat;
            if (shouldReload)
                input.ReloadPressed = true;

            bool isReloadingOrRequested = Self.IsReloading || input.ReloadPressed;
            if (!isReloadingOrRequested)
                reloadStrafeAway = true;

            // 이동
            input.MoveDirection = DecideMovement(
                SelfCenter, 
                PerceivedTargetPosition, 
                horizontalDistance, 
                hasLineOfSight,
                isReloadingOrRequested, 
                shouldPrepareUltimate, 
                grenadeEscapeDirection, 
                out bool isRetreating);

            // 전술 도약은 조준 방향 반대로 도약이라 쓰고 나서 조준 필요한 공격은 X
            if (isRetreating && TryTacticalJump(ref input, attackOrigin))
                return;

            // 듀얼 리볼버 체크
            bool isDualRevolverUsed = TryDualRevolver(ref input, projectileOrigin, PerceivedTargetPosition, PerceivedTargetVelocity, distance, hasLineOfSight);
            if (isDualRevolverUsed)
                return;

            // 기본 공격 체크
            if (!input.ReloadPressed)
            {
                TryBasicAttack(ref input, projectileOrigin, PerceivedTargetPosition, PerceivedTargetVelocity, distance, hasLineOfSight);
            }
        }

        /// <summary>
        /// 상황에 따라 바뀔 수 있는 상태들을 리셋시킨다.
        /// </summary>
        private void ResetDecisions()
        {
            lastThinkTime = Clock;
            lastHealth = Self.CurrentHealth;

            nextShotTime = Clock;
            nextSkillDecisionTime = Clock;
            shotsInBurst = 0;
            burstSize = Random.Range(BurstSizeRange.x, BurstSizeRange.y + 1);

            nextUltimateAttemptTime = Clock;
            retreatStartedAt = InactiveRetreatStartTime;

            reloadStrafeAway = true;

            lastGroundedFeetY = SelfFeetY;
            crossTarget = null;
        }

        private void DelayUltimate()
        {
            retreatStartedAt = InactiveRetreatStartTime;
            nextUltimateAttemptTime = Clock + retreatRetryDelay;
        }

        /// <summary>
        /// 데드아이 사용을 위해 조금 떨어져야 하는지 체크한다.
        /// 체크 후 그에 따른 실제 이동은 <see cref="DecideMovement(Vector2, Vector2, float, bool, bool, bool, float, out bool)">DecideMovement</see>에서 담당한다.
        /// </summary>
        private bool ShouldPrepareUltimate(float horizontalDistance, bool hasLineOfSight)
        {
            bool meetsGaugeRequirement = !Self.IsUltimateGaugeMode || Self.UltimateGaugeCurrent >= Self.UltimateGaugeMax;

            bool canPrepareUltimate = IsReady(CharacterActionType.Ultimate) && meetsGaugeRequirement && hasLineOfSight && Clock >= nextUltimateAttemptTime;
            if (!canPrepareUltimate)
            {
                retreatStartedAt = InactiveRetreatStartTime;

                return false;
            }

            if (horizontalDistance >= ultimateMinRange)
            {
                retreatStartedAt = InactiveRetreatStartTime;

                return true;
            }

            if (retreatStartedAt == InactiveRetreatStartTime)
                retreatStartedAt = Clock;

            // 아직 도망치는 중
            if (Clock - retreatStartedAt < maxRetreatTime)
                return true;

            // Max에 도달했는데도 못도망쳤으면 그냥 일반 싸움
            DelayUltimate();

            return false;
        }

        private float DecideMovement(
            Vector2 selfCenter,
            Vector2 targetPosition,
            float horizontalDistance,
            bool hasLineOfSight,
            bool isReloading,
            bool shouldPrepareUltimate,
            float grenadeEscapeDirection,
            out bool isRetreating)
        {
            isRetreating = false;

            bool shouldRetreatFromGrenade = !Mathf.Approximately(grenadeEscapeDirection, 0f);
            if (shouldRetreatFromGrenade)
            {
                crossTarget = null;

                float escapeDirection = GetSafeMoveDirection(grenadeEscapeDirection);
                isRetreating = !Mathf.Approximately(escapeDirection, 0f);
                if (!isRetreating)
                    RequestJump(grenadeEscapeJumpHoldDuration);

                return escapeDirection;
            }

            if (crossTarget != null)
            {
                bool canContinueCrossing = crossTarget == Target && !Self.IsGrounded;
                if (canContinueCrossing)
                {
                    // 점프 중 상대가 돌아서도 목적지는 유지하고, 도착하면 착지할 때까지 멈춘다.
                    float remainingDistance = (crossDestinationX - selfCenter.x) * crossMoveDirection;
                    if (remainingDistance <= backPositionTolerance)
                        return 0f;

                    return GetSafeMoveDirection(crossMoveDirection);
                }

                crossTarget = null;
            }

            float directionToTarget = Mathf.Sign(targetPosition.x - selfCenter.x);
            float heightDifference = targetPosition.y - selfCenter.y;

            bool shouldClimbToTarget = heightDifference > climbMinHeight && horizontalDistance < climbMaxHorizontalDistance;
            if (shouldClimbToTarget)
                RequestJump(climbJumpHoldDuration);

            if (!hasLineOfSight && !isReloading)
                return GetSafeMoveDirection(directionToTarget);

            float preferredDistance = 0f;

            if (shouldPrepareUltimate)
                preferredDistance = ultimateMinRange;
            else if (isReloading)
                preferredDistance = reloadRange;

            if (preferredDistance > 0f)
            {
                if (isReloading)
                {
                    if (horizontalDistance <= preferredDistance)
                        reloadStrafeAway = true;
                    else if (horizontalDistance >= preferredDistance + preferredDistanceTolerance)
                        reloadStrafeAway = false;
                }

                if (horizontalDistance < preferredDistance)
                {
                    float retreatDirection = GetSafeMoveDirection(-directionToTarget);
                    bool canRetreat = !Mathf.Approximately(retreatDirection, 0f) && !IsWallAhead(retreatDirection);
                    if (canRetreat)
                    {
                        isRetreating = true;
                        return retreatDirection;
                    }

                    if (shouldPrepareUltimate)
                        DelayUltimate();

                    // 후퇴가 막히면 그냥 일반 이동 로직 적용
                }
                else
                {
                    // 좀 많이 떨어졌으면 다시 살짝 붙기
                    if (horizontalDistance > preferredDistance + preferredDistanceTolerance)
                        return GetSafeMoveDirection(directionToTarget);

                    // 장전 중에는 거리 내에서 무빙
                    return isReloading ? GetReloadStrafeMovement(directionToTarget) : 0f;
                }
            }

            float targetForward = Target.AimDirection.x >= 0f ? 1f : -1f;
            float backPositionX = targetPosition.x - targetForward * closeRange;
            float backOffset = backPositionX - selfCenter.x;

            if (Mathf.Abs(backOffset) < backPositionTolerance)
                return 0f;

            float moveDirection = GetSafeMoveDirection(Mathf.Sign(backOffset));
            
            // 상대 정면에 있고 근처에 있으면서 이동 가능하면
            bool shouldJumpBehindTarget = Self.IsGrounded &&
                (selfCenter.x - targetPosition.x) * targetForward >= 0f &&
                horizontalDistance < crossMaxHorizontalDistance &&
                Mathf.Abs(heightDifference) < crossMaxHeightDifference &&
                !Mathf.Approximately(moveDirection, 0f);

            if (shouldJumpBehindTarget)
            {
                crossTarget = Target;
                crossMoveDirection = moveDirection;
                crossDestinationX = backPositionX;

                RequestJump(crossJumpHoldDuration);
            }

            return moveDirection;
        }

        /// <summary>
        /// 재장전 중 무빙을 어떻게 칠 지 가져오는 메서드
        /// </summary>
        private float GetReloadStrafeMovement(float directionToTarget)
        {
            float moveDirection = reloadStrafeAway ? -directionToTarget : directionToTarget;
            bool isPathBlocked = IsWallAhead(moveDirection) ||
                Mathf.Approximately(GetSafeMoveDirection(moveDirection), 0f);

            if (isPathBlocked)
            {
                reloadStrafeAway = !reloadStrafeAway;
                moveDirection = -moveDirection;
            }

            return IsWallAhead(moveDirection)
                ? 0f
                : GetSafeMoveDirection(moveDirection);
        }

        private float GetSafeMoveDirection(float direction)
        {
            if (Self.IsGrounded)
                return SafeMove(direction);

            // 바닥 체크가 점프 하면 사실상 false만 되서 이동이 0으로 바뀜 > 점프 할 때 바닥 높이 기준으로 바닥 있는지 체크
            float heightAboveGround = Mathf.Max(0f, SelfFeetY - lastGroundedFeetY);
            float groundCheckDepth = airborneGroundCheckDepth + heightAboveGround;

            return IsGroundAhead(direction, maxDrop: groundCheckDepth) ? direction : 0f;
        }

        private bool TryTacticalJump(ref CharacterInputSnapshot input, Vector2 attackOrigin)
        {
            float escapeDirection = input.MoveDirection;

            bool canStartTacticalJump = Self.IsGrounded && !Mathf.Approximately(escapeDirection, 0f) && IsReady(CharacterActionType.SkillE);
            if (!canStartTacticalJump)
                return false;

            bool hasSafeEscapePath = !IsWallAhead(escapeDirection, escapeLookAhead) &&
                IsGroundAhead(escapeDirection, escapeLookAhead);
            if (!hasSafeEscapePath)
                return false;

            if (!SkillDecisionPasses())
                return false;

            // 가려는 곳 반대편 겨냥
            input.AimWorldPosition = attackOrigin - new Vector2(escapeDirection, 0f);
            input.SkillEPressed = true;

            return true;
        }

        private bool TryUltimate(
            ref CharacterInputSnapshot input,
            Vector2 selfCenter,
            Vector2 targetPosition,
            Vector2 targetVelocity,
            float distance,
            float horizontalDistance)
        {
            bool canEnterSnipingMode = Self.IsGrounded && !Self.IsReloading &&
                Mathf.Abs(Self.Velocity.y) <= ultimateMaxVerticalSpeed;

            bool isTargetAtSafeRange = horizontalDistance >= ultimateMinRange && distance <= ultimateMaxRange;
            if (!canEnterSnipingMode || !isTargetAtSafeRange)
                return false;

            // 상대가 빠르게 다가오고 있으면 궁극기 사용 X
            float directionToTarget = Mathf.Sign(targetPosition.x - selfCenter.x);
            float closingSpeed = Mathf.Max(0f, -targetVelocity.x * directionToTarget);
            float predictedHorizontalDistance = horizontalDistance - closingSpeed * (ReactionTime + ultimateApproachPredictionTime);

            if (predictedHorizontalDistance < ultimateMinRange - ultimateSafeDistanceMargin)
                return false;

            if (!SkillDecisionPasses())
                return false;

            input.UltimatePressed = true;

            nextShotTime = Clock;
            shotsInBurst = 0;
            retreatStartedAt = InactiveRetreatStartTime;

            return true;
        }

        private bool TryDualRevolver(
            ref CharacterInputSnapshot input,
            Vector2 projectileOrigin,
            Vector2 targetPosition,
            Vector2 targetVelocity,
            float distance,
            bool hasLineOfSight)
        {
            bool canUseDualRevolver = hasLineOfSight && distance <= dualRevolverRange && IsReady(CharacterActionType.SkillQ);
            if (!canUseDualRevolver)
                return false;

            if (!SkillDecisionPasses())
                return false;

            AimAtTarget(ref input, projectileOrigin, targetPosition, targetVelocity, chaser.DualRevolverSpeed, defaultAimErrorMultiplier);
            input.SkillQPressed = true;

            return true;
        }

        private bool TryBasicAttack(
            ref CharacterInputSnapshot input,
            Vector2 origin,
            Vector2 targetPosition,
            Vector2 targetVelocity,
            float distance,
            bool hasLineOfSight,
            bool sniping = false)
        {
            bool canReachTarget = hasLineOfSight && (sniping || distance <= basicAttackRange);
            bool canFire = Clock >= nextShotTime && !Self.IsReloading && IsReady(CharacterActionType.BasicAttack);

            if (!canReachTarget || !canFire)
                return false;

            float projectileSpeed = sniping ? InstantProjectileSpeed : chaser.BurstBulletSpeed;
            float aimErrorMultiplier = sniping ? snipingAimErrorMultiplier : defaultAimErrorMultiplier;
            
            AimAtTarget(ref input, origin, targetPosition, targetVelocity, projectileSpeed, aimErrorMultiplier);
            input.BasicAttackPressed = true;

            Vector2 shotDelay = ShotDelayRange;
            nextShotTime = Clock + Random.Range(shotDelay.x, shotDelay.y);

            if (sniping)
                return true;
            
            // 저격 중 아닐 때만 기본 공격(점사) 연사랑 딜레이 주기
            shotsInBurst++;

            if (shotsInBurst >= burstSize)
            {
                shotsInBurst = 0;
                burstSize = Random.Range(BurstSizeRange.x, BurstSizeRange.y + 1);
                shotDelay = BurstPauseRange;
            }

            return true;
        }

        private void AimAtTarget(
            ref CharacterInputSnapshot input,
            Vector2 origin,
            Vector2 targetPosition,
            Vector2 targetVelocity,
            float projectileSpeed,
            float errorMultiplier)
        {
            Vector2 aim = PredictIntercept(origin, targetPosition, targetVelocity, projectileSpeed);
            input.AimWorldPosition = ApplyAimError(origin, aim, errorMultiplier);
        }

        private bool SkillDecisionPasses()
        {
            if (Clock < nextSkillDecisionTime)
                return false;

            nextSkillDecisionTime = Clock + Random.Range(minSkillDecisionInterval, maxSkillDecisionInterval);

            return !Roll(SkillHesitation);
        }
    }
}
