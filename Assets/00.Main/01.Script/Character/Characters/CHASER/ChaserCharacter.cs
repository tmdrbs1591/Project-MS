using ProjectMS.CharacterSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>
    /// 체이서 캐릭터의 스킬 로직을 담당하는 클래스.
    /// </summary>
    public class ChaserCharacter : CharacterBase
    {
        [Header("Common")]
        [SerializeField] private LayerMask targetLayer;

        [Header("Basic Attack - Burst")]
        [SerializeField] private CharacterProjectile burstBulletProjectilePrefab;
        [Min(0f)] [SerializeField] private float burstBulletProjectileSpeed = 15f;
        [Min(1)] [SerializeField] private int burstBulletProjectileFireCount = 4;
        [Min(0f)] [SerializeField] private float burstArcAngle = 45f;
        [Min(0f)] [SerializeField] private float burstReloadingDuration = 2.5f;
        [Min(1)] [SerializeField] private int burstCharges = 4;

        [Header("Skill Q - Dual Revolver")]
        [Tooltip("비어있을 경우 Burst Bullet Projectile Prefab 사용")]
        [SerializeField] private CharacterProjectile dualRevolverProjectilePrefab; //없으면 기본 burstBulletProjectilePrefab사용
        [Min(0)] [SerializeField] private float dualRevolverProjectileSpeed = 45f;

        [Header("Skill E - Technical Jump")]
        [Tooltip("바닥 기준, 0 ~ 180 사이로 입력")]
        [Min(0f)] [SerializeField] private float techJumpAngle = 60f;
        [Min(0f)] [SerializeField] private float techJumpPower = 30f;
        [Min(0f)] [SerializeField] private float techJumpDuration = 0.08f;
        [SerializeField] private CharacterProjectile techJumpFlashBangPrefab;
        [Min(0f)] [SerializeField] private float techJumpFlashBangSpeed = 8f;
        [Min(0f)] [SerializeField] private float techJumpFlashBangRange = 0.8f;
        [Min(0f)] [SerializeField] private float techJumpFlashBangSlowDuration = 0.5f;
        [Min(0f)] [SerializeField] private float techJumpFlashBangSlowRatio = 0.4f;

        [Header("Skill R - Dead Eye")]
        [SerializeField] private CharacterProjectile deadEyeProjectilePrefab;
        [Min(0f)] [SerializeField] private float deadEyeProjectileFireCooltime = 0.75f;
        [Min(0)] [SerializeField] private int deadEyeProjectileCharges = 3;
        [Min(0f)] [SerializeField] private float deadEyeSnipingTimeLimit = 7.5f;
        [Min(0f)] [SerializeField] private float deadEyeReuseCooltime = 0.25f;

        [Header("Passive - Dirty Carnival")]
        [Tooltip("캐릭터 등 기준, 0 ~ 360 사이로 입력")]
        [Min(0f)] [SerializeField] private float carnivalBackAttackCriterionAngle = 90f;
        [Min(1f)] [SerializeField] private float carnivalBackAttackAdditionalDamageMultiplier = 2f; // 추후 CharacterBase에 들어가면 좋을 듯함

        private bool isSniping = false;
        private CharacterTimerHandle deadEyeSnipingTimeTimer;

        private bool isFirstBurst = true;

        private bool isFlashBangFired = false;

        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            bool shouldReload = GetActionCharges(CharacterActionType.BasicAttack) - 1 == 0;

            if (isSniping)
            {
                SpawnProjectile(
                    deadEyeProjectilePrefab,
                    context.AimWorldPosition,
                    context.AimDirection,
                    0f,
                    context.Damage,
                    targetLayer);

                PlayActionEffect(CharacterActionType.Ultimate, EffectOrigin.position, context.AimAngle);

                if (shouldReload)
                {
                    TurnOffSnipingMode(isTurnedOffByNoAmmo: true);
                }

                return true;
            }

            if (isFirstBurst)
            {
                SetActionCharges(CharacterActionType.BasicAttack, burstCharges);
                isFirstBurst = false;
            }

            if (burstBulletProjectilePrefab == null) return false;
            Vector2 aim = context.AimDirection;
            
            for (int i = 0; i < burstBulletProjectileFireCount; i++)
            {
                // 총알 발사 개수가 1개 일수도 있으므로 방어코드. 개수가 1개면 0.5, 아니면 나누기
                float angleRatio = (burstBulletProjectileFireCount == 1) ? 0.5f : (float)i / (burstBulletProjectileFireCount - 1);
                float offset = -burstArcAngle * 0.5f + burstArcAngle * angleRatio;
                
                Vector2 dir = Rotate(aim, offset);
                
                SpawnProjectile(
                    burstBulletProjectilePrefab,
                    ProjectileOrigin.position,
                    dir,
                    burstBulletProjectileSpeed,
                    context.Damage,
                    targetLayer);
            }
            
            PlayActionEffect(CharacterActionType.BasicAttack, EffectOrigin.position, context.AimAngle);
            
            if (shouldReload)
            {
                SetCooldownDuration(CharacterActionType.BasicAttack, burstReloadingDuration);
                SetActionCharges(CharacterActionType.BasicAttack, burstCharges + 1);
            }
            else
                ResetCooldownDuration(CharacterActionType.BasicAttack);
            
            return true;
        }

        protected override bool OnSkillQ(CharacterActionContext context)
        {
            CharacterProjectile prefab = ResolveDualRevolverPrefab(burstBulletProjectilePrefab);
            
            if (prefab == null) return false;

            SpawnProjectile(
                prefab,
                ProjectileOrigin.position,
                context.AimDirection,
                dualRevolverProjectileSpeed,
                context.Damage,
                targetLayer);

            PlayActionEffect(CharacterActionType.SkillQ, EffectOrigin.position, context.AimAngle);
            
            return true;
        }

        protected override bool OnSkillE(CharacterActionContext context)
        {
            SpawnProjectile(
                techJumpFlashBangPrefab,
                AttackOrigin.position,
                Vector2.down,
                techJumpFlashBangSpeed,
                context.Damage,
                targetLayer);

            float techJumpAngleRad = Mathf.Min(180, techJumpAngle) * Mathf.Deg2Rad;

            // 빗변 길이 1 기준 연산
            float jumpDirectionX = Mathf.Cos(techJumpAngleRad);
            if (context.AimDirection.x > 0) 
                jumpDirectionX = -1 * Mathf.Cos(techJumpAngleRad);
            
            float jumpDirectionY = Mathf.Sin(techJumpAngleRad);
            
            Vector2 jumpDirection = new Vector2(jumpDirectionX, jumpDirectionY).normalized;

            Movement.StartDash(jumpDirection, techJumpPower, techJumpDuration);

            isFlashBangFired = true;

            PlayActionEffect(CharacterActionType.SkillE, AttackOrigin.position, AimAngle);
            return true;
        }

        protected override bool OnUltimate(CharacterActionContext context)
        {
            ChangeSnipingMode();

            return true;
        }

        protected override float ModifyOutgoingDamage(CharacterBase target, float damage, CharacterDamageSource source)
        {
            bool isBackOfTarget = IsBehindTarget(target, Mathf.Min(360, carnivalBackAttackCriterionAngle));
            
            return isBackOfTarget ? damage * carnivalBackAttackAdditionalDamageMultiplier : damage;
        }

        // 후에 섬광탄인지 검사할 수 있어야 함 (프리팹으로 검사 불가)
        protected override void OnProjectileDespawned(CharacterProjectile projectile, ProjectileDespawnReason reason, CharacterBase hitTarget)
        {
            if (!isFlashBangFired) return;
            isFlashBangFired = false;

            OnFlashBangBoomed(projectile);
        }

        private void OnFlashBangBoomed(CharacterProjectile flashBang)
        {
            List<CharacterBase> foundEnemies = FindEnemiesInCircle(flashBang.transform.position, techJumpFlashBangRange, targetLayer);

            foreach (CharacterBase foundEnemy in foundEnemies)
            {
                DealDamage(foundEnemy, Definition.GetDamage(CharacterActionType.SkillE));
                ApplySlow(foundEnemy, techJumpFlashBangSlowRatio, techJumpFlashBangSlowDuration);
            }
        }

        private void ChangeSnipingMode()
        {
            if (isSniping) TurnOffSnipingMode();
            else TurnOnSnipingMode();
        }

        private void TurnOnSnipingMode()
        {
            isSniping = true;
            
            SetMoveAndSkillExceptUltimateCanUse(false);

            SetActionCharges(CharacterActionType.BasicAttack, deadEyeProjectileCharges);
            SetCooldownDuration(CharacterActionType.BasicAttack, deadEyeProjectileFireCooltime);

            SetCooldownDuration(CharacterActionType.Ultimate, deadEyeReuseCooltime);

            deadEyeSnipingTimeTimer = ScheduleTimer(deadEyeSnipingTimeLimit, () => TurnOffSnipingMode());
        }

        private void TurnOffSnipingMode(bool isTurnedOffByNoAmmo = false)
        {
            isSniping = false;
            CancelTimer(deadEyeSnipingTimeTimer);

            SetMoveAndSkillExceptUltimateCanUse(true);

            SetActionCharges(
                CharacterActionType.BasicAttack, 
                isTurnedOffByNoAmmo ? burstCharges + 1 : burstCharges);
            ResetCooldownDuration(CharacterActionType.BasicAttack);

            ResetCooldownDuration(CharacterActionType.Ultimate);
            StartCooldown(CharacterActionType.Ultimate);
        }

        private void SetMoveAndSkillExceptUltimateCanUse(bool canUse)
        {
            SetMovementEnabled(canUse);

            SetActionEnabled(CharacterActionType.SkillQ, canUse);
            SetActionEnabled(CharacterActionType.SkillE, canUse);
            SetActionEnabled(CharacterActionType.Dash, canUse);
        }

        private CharacterProjectile ResolveDualRevolverPrefab(CharacterProjectile fallback)
        {
            return dualRevolverProjectilePrefab != null ? dualRevolverProjectilePrefab : fallback;
        }

        private Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
