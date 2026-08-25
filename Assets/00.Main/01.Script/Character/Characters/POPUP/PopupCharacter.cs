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
        // 궁극기용 변수

        [Header("Passive - Glitch Occured")]
        [SerializeField] private float glitchDuration = 3f;
        [SerializeField] private float glitchDamageTimes = 3f;
        [SerializeField] private float glitchDamageRatio = 0.3f;

        private CharacterProjectile currentHackingCD;
        private int currentGlitchDealedCount;
        private CharacterBase currentGlitchTarget;
        private bool isGlitchOccuring = false;

        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            float errorAngleRad = Mathf.Min(180, errorThrowAngle) * Mathf.Deg2Rad;

            float errorDirectionX = default;

            // 빗변 길이 1 기준 연산
            if (AimDirection.x <= 0)
                errorDirectionX = Mathf.Cos(errorAngleRad);
            else
                errorDirectionX = -1 * Mathf.Cos(errorAngleRad);

            float errorDirectionY = Mathf.Sin(errorAngleRad);

            Vector2 errorDirection = new Vector2(errorDirectionX, errorDirectionY).normalized;

            SpawnThrowable(
                errorThrowablePrefab,
                context.Action,
                ProjectileOrigin.position,
                errorDirection,
                errorProjectileSpeed);

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
            return false;
        }

        protected override void OnProjectileDespawned(CharacterProjectile projectile, ProjectileDespawnReason reason, CharacterBase hitTarget)
        {
            if (projectile != currentHackingCD || reason != ProjectileDespawnReason.HitCharacter)
                return;

            // 이동 방해 로직
        }

        protected override void OnDamageDealt(CharacterBase target, float requestedDamage)
        {
            currentGlitchTarget = target;
            currentGlitchDealedCount = 0;

            if (isGlitchOccuring) return;

            SetContinuousGlitch();
        }

        private void DealGlitchDamage()
        {
            DealDamage(currentGlitchTarget, baseAttackPower * glitchDamageRatio / glitchDamageTimes);
            currentGlitchDealedCount++;
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
