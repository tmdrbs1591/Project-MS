using UnityEngine;
using ProjectMS.CharacterSystem;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>부채꼴 근접 판정과 돌진 공격을 사용하는 검 캐릭터 예제.</summary>
    public sealed class SwordCharacterExample : CharacterBase
    {
        [Header("Basic Attack")]
        [Min(0f)] [SerializeField] private float swordRange = 1.6f;
        [Range(0f, 360f)] [SerializeField] private float swordAngle = 100f;
        [SerializeField] private LayerMask targetLayer;

        [Header("Skill Q - Lunge")]
        [Min(0f)] [SerializeField] private float lungePower = 18f;
        [Min(0.01f)] [SerializeField] private float lungeDuration = 0.12f;
        [Min(0f)] [SerializeField] private float lungeHitRadius = 1.2f;

        protected override bool OnBasicAttack(CharacterActionContext context)
        {
            foreach (CharacterBase target in FindEnemiesInArc(
                         context.Origin,
                         context.AimDirection,
                         swordRange,
                         swordAngle,
                         targetLayer))
            {
                DealDamage(target, context.Damage);
            }

            PlayActionEffect(context.Action, EffectOrigin.position, context.AimAngle);
            return true;
        }

        protected override bool OnSkillQ(CharacterActionContext context)
        {
            StartDash(context.AimDirection, lungePower, lungeDuration);

            foreach (CharacterBase target in FindEnemiesInCircle(
                         context.Origin,
                         lungeHitRadius,
                         targetLayer))
            {
                DealDamage(target, context.Damage);
            }

            PlayActionEffect(context.Action, EffectOrigin.position, context.AimAngle);
            return true;
        }
    }
}
