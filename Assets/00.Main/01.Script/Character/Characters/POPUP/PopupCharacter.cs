using System;
using ProjectMS.CharacterSystem;
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
        [Min(0f)] [SerializeField] private float errorProjectileSpeed = 5;
        [Min(0f)] [SerializeField] private float errorThrowAngle = 50f;

        [Header("Skill Q - Occur Aiming Bug")]
        [SerializeField] private CharacterProjectile aimingBugHackingCDProjectile;
        [SerializeField] private float aimingBugHackingCDSpeed = 1.5f;

        [Header("Skill E - Occur Moving Bug")]
        [SerializeField] private float movingBugArcAngle = 60f;
        [SerializeField] private float movingBugArcRadius = 6f;
        [SerializeField] private float movingBugSlowRatio = 0.3f;
        [SerializeField] private float movingBugSlowDuration = 0.75f;

        [Header("Skill R - System Error Popup Appeared")]
        [SerializeField] private CharacterProjectile popupAppearGlitchProjectile;
        [SerializeField] private float popupAppearGlitchSpeed = 4.5f;
        [SerializeField] private PopupErrorPopupDeployable popupAppearErrorPopupDeployable;
        [SerializeField] private float popupAppearErrorPopupDuration = 3f;
        [SerializeField] private int popupAppearErrorPopupDamageTimes = 3;

        [Header("Passive - Glitch Occured")]
        [SerializeField] private float glitchDuration = 3f;
        [SerializeField] private float glitchDamageTimes = 3f;
        [SerializeField] private float glitchDamageRatio = 0.3f;

        private CharacterProjectile currentHackingCD;
        private CharacterProjectile currentPopupAppearGlitch;

        private bool isFirstThrowError = true;
        private int currentGlitchDealedCount;
        private CharacterBase currentGlitchTarget;
        private bool isGlitchOccuring = false;
        private bool isGlitchDamage = false;

        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            float errorAngleRad = Mathf.Min(180, errorThrowAngle) * Mathf.Deg2Rad;

            float errorDirectionX = default;
            if (isFirstThrowError)
            {
                SetActionCharges(CharacterActionType.BasicAttack, errorThrowableFireCount);
                isFirstThrowError = false;
            }

            // 빗변 길이 1 기준 연산
            if (AimDirection.x <= 0)
                errorDirectionX = -1 * Mathf.Cos(errorAngleRad);
            else
                errorDirectionX = Mathf.Cos(errorAngleRad);

            float errorDirectionY = Mathf.Sin(errorAngleRad);

            Vector2 errorDirection = new Vector2(errorDirectionX, errorDirectionY).normalized;

            PopupErrorThrowable errorThrowable = SpawnThrowable(
                errorThrowablePrefab,
                context.Action,
                ProjectileOrigin.position,
                errorDirection,
                errorProjectileSpeed,
                maxCount: 5,
                initialize: (errorThrowable) => errorThrowable.Initialize(context.Damage));

            if (errorThrowable == null)
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
