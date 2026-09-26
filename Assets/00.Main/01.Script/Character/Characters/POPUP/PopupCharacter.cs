using System.Collections.Generic;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>
    /// 팝업 캐릭터의 스킬 로직을 담당하는 클래스.
    /// </summary>
    public class PopupCharacter : CharacterBase
    {
        [Header("Common")]
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private int baseAttackPower;

        [Header("Basic Attack - Throw Error")]
        [SerializeField] private PopupErrorThrowable errorThrowablePrefab;
        [Min(1)][SerializeField] private int errorThrowableFireCount = 5;
        [Min(0f)][SerializeField] private float errorReloadingDuration = 1.2f;
        [Min(0f)][SerializeField] private float errorThrowableMaxVelocity = 3f;
        [Min(0)] [SerializeField] private int errorThrowableMaxCount = 5;

        [Header("Another Throw Error Settings")]
        [SerializeField] private bool useAnotherErrorThrow = true;
        [SerializeField] private float anotherErrorThrowableSpeed = 1f;

        [Header("Old Throw Error Settings")]
        [SerializeField] private bool useOldErrorThorw = false;
        [Min(0.01f)] [SerializeField] private float errorThrowableMinFlightTime = 0.1f;
        [Min(0.01f)] [SerializeField] private float errorThrowableMaxFlightTime = 1.25f;
        [Min(0.01f)] [SerializeField] private float errorThrowableMaxDistance = 10f;

        [Header("Skill Q - Occur Aiming Bug")]
        [SerializeField] private CharacterProjectile aimingBugHackingCDProjectile;
        [SerializeField] private float aimingBugHackingCDSpeed = 7f;
        [SerializeField] private float aimingBugDuration = 1f;

        [Header("Skill E - Occur Moving Bug")]
        [Min(0f)][SerializeField] private float movingBugArcAngle = 60f;
        [Min(0f)][SerializeField] private float movingBugArcRadius = 6f;
        [Min(0f)][SerializeField] private float movingBugSlowRatio = 0.3f;
        [Min(0f)][SerializeField] private float movingBugSlowDuration = 0.75f;
        [Tooltip("E 를 쓸 때 스킬이 나가는 방향(조준 방향)으로 회전해서 생성되는 이펙트")]
        [SerializeField] private GameObject movingBugVfxPrefab;
        [Tooltip("X 회전에 더할 보정각. 조준 방향과 어긋나 보이면 여기서 맞춘다.")]
        [SerializeField] private float movingBugVfxAngleOffset = 0f;
        [Tooltip("켜면 X 회전 방향을 반대로 돌린다(위로 조준했는데 이펙트가 아래로 가면 켠다).")]
        [SerializeField] private bool movingBugVfxInvertAngle = false;
        [Tooltip("고정 회전값 Y / Z. X 회전만 조준 방향에 맞춰 바뀐다.")]
        [SerializeField] private float movingBugVfxFixedY = 90f;
        [SerializeField] private float movingBugVfxFixedZ = -90f;
        [Tooltip("이펙트 재생 시간(초). 지나면 남은 파티클이 다 사라진 뒤 지운다.")]
        [Min(0f)][SerializeField] private float movingBugVfxLifetime = 1f;
        [Tooltip("켜면 이펙트가 캐릭터를 따라다닌다(캐릭터 자식으로 붙음).")]
        [SerializeField] private bool movingBugVfxFollowCharacter = false;

        [Header("Skill R - System Error Popup Appeared")]
        [SerializeField] private CharacterProjectile popupAppearGlitchProjectile;
        [Min(0f)][SerializeField] private float popupAppearGlitchSpeed = 6f;
        [SerializeField] private PopupErrorPopupDeployable popupAppearErrorPopupDeployable;
        [Min(0f)][SerializeField] private float popupAppearErrorPopupDuration = 3f;
        [Min(0)][SerializeField] private int popupAppearErrorPopupDamageTimes = 3;
        [Min(0)][SerializeField] private int popupAppearErrorPopupBreakAttemptsRequire = 10;

        [Header("Passive - Glitch Occured")]
        [Min(0f)][SerializeField] private float glitchDuration = 3f;
        [Min(0f)][SerializeField] private float glitchDamageTimes = 3f;
        [Min(0f)][SerializeField] private float glitchDamageRatio = 0.3f;

        private CharacterProjectile currentHackingCD;
        private CharacterProjectile currentPopupAppearGlitch;

        private bool isFirstThrowError = true;

        private float errorThrowableGravityScale = 1f;

        private int currentGlitchDealedCount;
        private CharacterBase currentGlitchTarget;
        private bool isGlitchOccuring = false;
        private bool isGlitchDamage = false;

        private bool isErrorPopupEnabled = false;
        private int errorPopupBreakAttempts = 0;
        private CharacterBase errorPopupTarget;
        private PopupErrorPopupDeployable errorPopupDeployable;

        protected override bool OnBasicAttack(CharacterActionContext context)
        { 
            if (isFirstThrowError)
            {
                SetActionCharges(CharacterActionType.BasicAttack, errorThrowableFireCount);
                errorThrowableGravityScale = errorThrowablePrefab.GetComponent<Rigidbody2D>().gravityScale;
                isFirstThrowError = false;
            }

            Vector2 errorVelocity = default;
            
            if (useOldErrorThorw)
            {
                errorVelocity = CalculateThrowVelocityOldVersion(
                    ProjectileOrigin.position,
                    context.AimWorldPosition,
                    errorThrowableMaxDistance,
                    errorThrowableMinFlightTime,
                    errorThrowableMaxFlightTime,
                    errorThrowableGravityScale);
            }
            else if (useAnotherErrorThrow)
            {
                Vector2 throwDirection = Quaternion.Euler(0, 0, AimAngle) * Vector2.right;
                errorVelocity = throwDirection * anotherErrorThrowableSpeed;
            }
            else
            {
                errorVelocity = CalculateThrowVelocity(
                    ProjectileOrigin.position,
                    context.AimWorldPosition,
                    errorThrowableMaxVelocity,
                    errorThrowableGravityScale);
            }

            OwnedEntitySpawnRequest request = new OwnedEntitySpawnRequest(
                ProjectileOrigin.position,
                Quaternion.identity,
                new OwnedEntityGroupId((int)context.Action),
                maxCount: errorThrowableMaxCount,
                overflowPolicy: OwnedEntityOverflowPolicy.DestroyOldest,
                initialVelocity: errorVelocity);

            OwnedEntitySpawnResult<PopupErrorThrowable> result = SpawnThrowable(
                errorThrowablePrefab,
                in request,
                initialize: (errorThrowable) => errorThrowable.Initialize(context.Damage));

            if (!result.Success)
            {
                Debug.LogWarning("[PopupCharacter] 에러 투럭!(기본 공격) 발사체를 소환하는데 실패했습니다!");
                return false;
            }

            bool shouldReload = GetActionCharges(CharacterActionType.BasicAttack) - 1 == 0;
            if (shouldReload)
            {
                SetCooldownDuration(CharacterActionType.BasicAttack, errorReloadingDuration);
                SetActionCharges(CharacterActionType.BasicAttack, errorThrowableFireCount + 1);
                NotifyReloadStarted(errorReloadingDuration);
            }
            else
                ResetCooldownDuration(CharacterActionType.BasicAttack);

            return true;
        }

        // 휠 수동 재장전.
        protected override bool TryGetReloadInfo(out int magazine, out float duration)
        {
            magazine = errorThrowableFireCount;
            duration = errorReloadingDuration;
            return true;
        }

        protected override bool OnSkillQ(CharacterActionContext context)
        {
            currentHackingCD = SpawnProjectile(
                aimingBugHackingCDProjectile,
                ProjectileOrigin.position,
                context.AimDirection,
                aimingBugHackingCDSpeed,
                context.Damage,
                targetLayer);

            return true;
        }

        protected override bool OnSkillE(CharacterActionContext context)
        {
            List<CharacterBase> enimies = FindEnemiesInArc(
                ProjectileOrigin.position,
                context.AimDirection,
                movingBugArcRadius,
                movingBugArcAngle,
                targetLayer);

            foreach (CharacterBase enemy in enimies)
            {
                DealDamage(enemy, context.Damage);
                ApplySlow(enemy, movingBugSlowRatio, movingBugSlowDuration);
            }

            if (!Runner.IsResimulation)
                Rpc_PlayMovingBugVfx(ProjectileOrigin.position, context.AimAngle);

            return true;
        }

        // E 이펙트: 모든 클라에서 스킬 방향(조준 각도)으로 회전해서 생성한다.
        // 3D 파티클 프리팹이라 Y/Z 는 고정(90, -90)하고 X 회전만 바꾼다. 이 고정값에서 파티클의 +Z(뿜는 방향)는
        // 화면상 (cos x, -sin x) 를 향하므로, 조준 각도 θ 를 향하려면 X = -θ 다.
        [Fusion.Rpc(Fusion.RpcSources.StateAuthority, Fusion.RpcTargets.All)]
        private void Rpc_PlayMovingBugVfx(Vector2 position, float angle)
        {
            if (movingBugVfxPrefab == null)
                return;

            float x = (movingBugVfxInvertAngle ? angle : -angle) + movingBugVfxAngleOffset;
            Quaternion rotation = Quaternion.Euler(x, movingBugVfxFixedY, movingBugVfxFixedZ);
            GameObject vfx = movingBugVfxFollowCharacter
                ? Instantiate(movingBugVfxPrefab, position, rotation, transform)
                : Instantiate(movingBugVfxPrefab, position, rotation);
            EffectAutoDestroy.Schedule(vfx, movingBugVfxLifetime);
        }

        protected override bool OnUltimate(CharacterActionContext context)
        {
            currentPopupAppearGlitch = SpawnProjectile(
                popupAppearGlitchProjectile,
                ProjectileOrigin.position,
                context.AimDirection,
                popupAppearGlitchSpeed,
                context.Damage,
                targetLayer);

            return true;
        }

        protected override void OnProjectileDespawned(CharacterProjectile projectile, ProjectileDespawnReason reason, CharacterBase hitTarget)
        {
            if (reason != ProjectileDespawnReason.HitCharacter)
                return;

            if (projectile == currentHackingCD)
            {
                ApplyControlSeal(hitTarget, CharacterControlType.BasicAttack, aimingBugDuration);
                return;
            }

            if (projectile == currentPopupAppearGlitch)
            {
                errorPopupTarget = hitTarget;
                ChangeErrorPopupStatus(true);

                errorPopupDeployable = SpawnOwnedEntity(
                    popupAppearErrorPopupDeployable,
                    CharacterActionType.Ultimate,
                    errorPopupTarget.transform.position,
                    maxCount: 1,
                    initialize: (popup) => popup.Initialize(
                        errorPopupTarget,
                        popupAppearErrorPopupDuration,
                        Definition.GetDamage(CharacterActionType.Ultimate),
                        popupAppearErrorPopupDamageTimes,
                        popupAppearErrorPopupBreakAttemptsRequire,
                        () => ChangeErrorPopupStatus(false)));

                return;
            }
        }

        // 패시브 틱이지만 시스템 오류 팝업 등장!(궁극기)의 팝업창 입력 감지에 사용
        protected override void OnPassiveTick(float deltaTime)
        {
            if (!isErrorPopupEnabled)
                return;

            if (errorPopupTarget == null)
                return;

            // 점프(스페이스)와 기본공격(좌클릭)만 "팝업 깨기 시도"로 센다. 한 틱에 여러 번 눌려도
            // 누락 없이 더하도록 WasInputPressed(bool) 대신 새로 눌린 횟수를 받아온다.
            int newAttempts =
                ConsumeNewInputCount(errorPopupTarget, CharacterInputType.BasicAttack) +
                ConsumeNewInputCount(errorPopupTarget, CharacterInputType.Jump);

            if (newAttempts > 0)
            {
                errorPopupBreakAttempts += newAttempts;
                if (errorPopupDeployable != null)
                    errorPopupDeployable.SetBreakProgress(errorPopupBreakAttempts);
            }

            if (errorPopupBreakAttempts >= popupAppearErrorPopupBreakAttemptsRequire)
                BreakErrorPopup();
        }

        // 라운드 도중 리셋되면(상대 사망 등) 남아있는 팝업/봉인/도트딜 상태를 정리한다.
        protected override void OnResetCharacter()
        {
            if (isErrorPopupEnabled)
                BreakErrorPopup();
        }

        // 상대가 연타로 팝업을 깨거나 리셋으로 강제 종료될 때 쓴다. 시간이 다 돼서 끝나는 경우는
        // 봉인이 스스로 만료되므로 여기를 거치지 않는다.
        private void BreakErrorPopup()
        {
            // ChangeErrorPopupStatus(false)가 target/deployable 참조를 null로 지우므로, 그 전에
            // 봉인 해제와 팝업창 파괴에 쓸 값을 먼저 빼둔다(예전엔 지운 뒤에 null을 넘겨서
            // 팝업창이 안 부서지고 도트딜이 계속 들어갔다).
            CharacterBase target = errorPopupTarget;
            PopupErrorPopupDeployable deployable = errorPopupDeployable;

            ReleaseControlSeal(target, CharacterControlType.All);
            ChangeErrorPopupStatus(false);
            DestroyOwnedEntity(deployable, OwnedEntityDestroyReason.Manual);
        }

        protected override void OnDamageDealt(CharacterBase target, float requestedDamage)
        {
            if (isGlitchDamage)
            {
                isGlitchDamage = false;;
                return;
            }

            currentGlitchTarget = target;
            currentGlitchDealedCount = 0;

            if (isGlitchOccuring) return;

            SetContinuousGlitch();
        }

        private Vector2 CalculateThrowVelocityOldVersion(Vector2 startPosition, Vector2 targetPosition, float maxThrowDistance, float minFlightTime, float maxFlightTime, float projectileGravityScale)
        {
            Vector2 offsetBeforeCheck = targetPosition - startPosition;
            Vector2 realTargetPosition = targetPosition;

            if (offsetBeforeCheck.sqrMagnitude > (maxThrowDistance * maxThrowDistance))
                realTargetPosition = startPosition + offsetBeforeCheck.normalized * maxThrowDistance;

            Vector2 offset = realTargetPosition - startPosition;

            // clamp01 결과에 따라 min ~ max
            float flightTime = Mathf.Lerp(
                minFlightTime,
                maxFlightTime,
                Mathf.Clamp01(offset.sqrMagnitude / (maxThrowDistance * maxThrowDistance)));

            float gravity = Physics2D.gravity.y * projectileGravityScale;

            Vector2 delta = realTargetPosition - startPosition;

            float velocityX = delta.x / flightTime;
            float velocityY = (delta.y - 0.5f * gravity * flightTime * flightTime) / flightTime;

            return new Vector2(velocityX, velocityY);
        }


        private Vector2 CalculateThrowVelocity(Vector2 startPosition, Vector2 targetPosition, float maxVelocity, float projectileGravityScale)
        {
            Vector2 delta = targetPosition - startPosition;

            float gravity = Mathf.Abs(Physics2D.gravity.y * projectileGravityScale);

            float x = Mathf.Abs(delta.x);
            float y = delta.y;

            float speedSquared = maxVelocity * maxVelocity;

            // 판별식
            float discriminant = speedSquared * speedSquared - gravity * (gravity * x * x + 2f * y * speedSquared);

            float directionX = Mathf.Sign(delta.x);

            bool canReachInMaxVelocity = discriminant >= 0f && x > 0.001f;
            if (canReachInMaxVelocity)
            {
                float tanTheta =
                    (speedSquared - Mathf.Sqrt(discriminant))
                    / (gravity * x);

                float cosTheta = 1f / Mathf.Sqrt(1f + tanTheta * tanTheta);
                float sinTheta = tanTheta * cosTheta;

                return new Vector2(
                    directionX * cosTheta,
                    sinTheta
                ) * maxVelocity;
            }

            Vector2 fallbackDirection =
                new Vector2(directionX, 1f).normalized;

            return fallbackDirection * maxVelocity;
        }

        private void ChangeErrorPopupStatus(bool isEnable)
        {
            errorPopupBreakAttempts = 0;
            isErrorPopupEnabled = isEnable;
            if (!isEnable)
            {
                errorPopupDeployable = null;
                errorPopupTarget = null;
            }

            Collider2D collider = GetComponent<Collider2D>();
            collider.enabled = !isEnable;

            Rigidbody.bodyType = isEnable ? RigidbodyType2D.Static : RigidbodyType2D.Dynamic;

            if (isEnable)
            {
                // 이전에 눌렀던 입력이 "깨기 시도"로 세어지지 않게 관찰 기준을 지금으로 맞춘다.
                ResetInputObservation(errorPopupTarget, CharacterInputType.BasicAttack);
                ResetInputObservation(errorPopupTarget, CharacterInputType.Jump);

                ApplyControlSeal(errorPopupTarget, CharacterControlType.All, popupAppearErrorPopupDuration);
            }
            // 봉인 해제: 연타로 깬 경우는 BreakErrorPopup()이 ReleaseControlSeal로 처리하고,
            // 시간이 다 된 경우는 봉인 타이머가 스스로 만료된다.
        }

        private void DealGlitchDamage()
        {
            DealDamage(currentGlitchTarget, baseAttackPower * glitchDamageRatio / glitchDamageTimes, CharacterDamageSource.Periodic);
            currentGlitchDealedCount++;
            isGlitchDamage = true;
        }

        private void SetContinuousGlitch()
        {
            if (currentGlitchDealedCount >= glitchDamageTimes)
            {
                isGlitchOccuring = false;
                return;
            }

            isGlitchOccuring = true;

            ScheduleTimer(glitchDuration / glitchDamageTimes, () =>
            {
                DealGlitchDamage();
                SetContinuousGlitch();
            });
        }
    }
}
