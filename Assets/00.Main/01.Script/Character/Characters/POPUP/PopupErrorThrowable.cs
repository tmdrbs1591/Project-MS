using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>
    /// 팝업의 오류 투척(기본 공격) 스킬의 투사체를 담당하는 클래스.
    /// </summary>
    public class PopupErrorThrowable : CharacterThrowable
    {
        [SerializeField] private float attackRadius = 0.5f;
        [SerializeField] private LayerMask targetLayer;
        
        private float damage;

        public void Initialize(float _damage)
        {
            damage = _damage;
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            IDamageable target = FindFirstDamageableInCircle(transform.position, attackRadius, targetLayer);
            if (target == null) return;
            if (target == OwnerCharacter) return;

            DealDamage(target, damage);
            RequestDestroy(OwnedEntityDestroyReason.Manual); // Manual?
        }
    }
}