using ProjectMS.CharacterSystem;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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
        [Min(0.01f)] [SerializeField] private float errorThrowableMinFlightTime = 0.1f;
        [Min(0.01f)] [SerializeField] private float errorThrowableMaxFlightTime = 1.25f;
        [Min(0.01f)] [SerializeField] private float errorThrowableMaxDistance = 10f;
        [Min(0)] [SerializeField] private int errorThrowableMaxCount = 5;
        [Min(0f)][SerializeField] private float errorReloadingDuration = 1.2f;

        [Header("Skill Q - Occur Aiming Bug")]
        [SerializeField] private CharacterProjectile aimingBugHackingCDProjectile;
        [SerializeField] private float aimingBugHackingCDSpeed = 1.5f;

        [Header("Skill E - Occur Moving Bug")]
        [Min(0f)][SerializeField] private float movingBugArcAngle = 60f;
        [Min(0f)][SerializeField] private float movingBugArcRadius = 6f;
        [Min(0f)][SerializeField] private float movingBugSlowRatio = 0.3f;
        [Min(0f)][SerializeField] private float movingBugSlowDuration = 0.75f;

        [Header("Skill R - System Error Popup Appeared")]
        [SerializeField] private CharacterProjectile popupAppearGlitchProjectile;
        [Min(0f)][SerializeField] private float popupAppearGlitchSpeed = 4.5f;
        [SerializeField] private PopupErrorPopupDeployable popupAppearErrorPopupDeployable;
        [Min(0f)][SerializeField] private float popupAppearErrorPopupDuration = 3f;
        [Min(0)][SerializeField] private int popupAppearErrorPopupDamageTimes = 3;

        [Header("Passive - Glitch Occured")]
        [Min(0f)][SerializeField] private float glitchDuration = 3f;
        [Min(0f)][SerializeField] private float glitchDamageTimes = 3f;
        [Min(0f)][SerializeField] private float glitchDamageRatio = 0.3f;

        private CharacterProjectile currentHackingCD;
        private CharacterProjectile currentPopupAppearGlitch;

        private bool isFirstThrowError = true;

        private float errorThrowableGravityScale = 1f;
        private bool isThrowableInitialized = false;

        private int currentGlitchDealedCount;
        private CharacterBase currentGlitchTarget;
        private bool isGlitchOccuring = false;
        private bool isGlitchDamage = false;

        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            if (isFirstThrowError)
            {
                SetActionCharges(CharacterActionType.BasicAttack, errorThrowableFireCount);
                isFirstThrowError = false;
            }

            if (!isThrowableInitialized)
            {
                errorThrowableGravityScale = errorThrowablePrefab.GetComponent<Rigidbody2D>().gravityScale;
                isThrowableInitialized = true;
            }

            Vector2 errorVelocity = CalculateThrowVelocity(
                ProjectileOrigin.position,
                context.AimWorldPosition,
                errorThrowableMaxDistance,
                errorThrowableMinFlightTime,
                errorThrowableMaxFlightTime,
                errorThrowableGravityScale);

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
            }
            else
                ResetCooldownDuration(CharacterActionType.BasicAttack);

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

            return true;
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
                // 이동 방해 로직

                return;
            }

            if (projectile == currentPopupAppearGlitch)
            {
                PopupErrorPopupDeployable popupDeployable = SpawnOwnedEntity(
                    popupAppearErrorPopupDeployable,
                    CharacterActionType.Ultimate,
                    hitTarget.transform.position,
                    maxCount: 1,
                    initialize: (popup) => popup.Initialize(
                        hitTarget, 
                        popupAppearErrorPopupDuration, 
                        Definition.GetDamage(CharacterActionType.Ultimate), 
                        popupAppearErrorPopupDamageTimes));

                return;
            }
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

        private Vector2 CalculateThrowVelocity(Vector2 startPosition, Vector2 targetPosition, float maxThrowDistance, float minFlightTime, float maxFlightTime, float projectileGravityScale)
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
                Mathf.Clamp01(offset.sqrMagnitude / maxThrowDistance * maxThrowDistance));

            float gravity = Physics2D.gravity.y * projectileGravityScale;

            Vector2 delta = realTargetPosition - startPosition;

            float velocityX = delta.x / flightTime;
            float velocityY = (delta.y - 0.5f * gravity * flightTime * flightTime) / flightTime;

            return new Vector2(velocityX, velocityY);
        }

        private void DealGlitchDamage()
        {
            DealDamage(currentGlitchTarget, baseAttackPower * glitchDamageRatio / glitchDamageTimes);
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
