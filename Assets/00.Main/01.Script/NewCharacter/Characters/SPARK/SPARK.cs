using ProjectMS.CharacterSystem;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>
    /// 신규 캐릭터 시작 템플릿.
    /// 필요한 스킬과 패시브 훅만 override하고 Fusion API는 직접 사용하지 않는다.
    /// </summary>
    public class SPARK : CharacterBase
    {
        [Header("Basic Attack")]
        [SerializeField] private CharacterProjectile projectilePrefab;
        [Min(0f)][SerializeField] private float projectileSpeed = 14f;
        [SerializeField] private LayerMask targetLayer;

        [Header("Skill Q - Piercing Beam")]
        [Min(0f)][SerializeField] private float beamRange = 10f;
        [Min(0f)][SerializeField] private float beamWidth = 0.8f;

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

        protected override bool OnSkillE(CharacterActionContext context)
        {
            return false;
        }

        protected override bool OnUltimate(CharacterActionContext context)
        {
            return false;
        }

        protected override void OnPassiveTick(float deltaTime)
        {
            // 매 네트워크 Simulation 틱에 필요한 패시브만 구현한다.
        }
    }
}
