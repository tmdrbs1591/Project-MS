using UnityEngine;
using ProjectMS.CharacterSystem;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>투사체와 직선 관통 스킬을 사용하는 원거리 캐릭터 예제.</summary>
    public sealed class GunCharacterExample : CharacterBase
    {
        [Header("Basic Attack")]
        [SerializeField] private CharacterProjectile projectilePrefab;
        [Min(0f)] [SerializeField] private float projectileSpeed = 14f;
        [SerializeField] private LayerMask targetLayer;

        [Header("Skill Q - Piercing Beam")]
        [Min(0f)] [SerializeField] private float beamRange = 10f;
        [Min(0f)] [SerializeField] private float beamWidth = 0.8f;

        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            if (projectilePrefab == null)
                return false;

            SpawnProjectile(
                projectilePrefab,
                ProjectileOrigin.position,
                context.AimDirection,
                projectileSpeed,
                context.Damage,
                targetLayer);

            PlayActionEffect(context.Action, ProjectileOrigin.position, context.AimAngle);
            return true;
        }

        protected override bool OnSkillQ(CharacterActionContext context)
        {
            foreach (CharacterBase target in FindEnemiesInLine(
                         context.Origin,
                         context.AimDirection,
                         beamRange,
                         beamWidth,
                         targetLayer))
            {
                DealDamage(target, context.Damage);
            }

            PlayActionEffect(context.Action, context.Origin, context.AimAngle);
            return true;
        }
    }
}
