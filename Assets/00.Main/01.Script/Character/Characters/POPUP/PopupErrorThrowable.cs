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
        [SerializeField] private float damage;

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            IDamageable damageable = FindFirstDamageableInCircle(transform.position, attackRadius, targetLayer);
            if (damageable == null) return;
            if (damageable == OwnerCharacter) return;

            DealDamage(damageable, damage);
            RequestDestroy(OwnedEntityDestroyReason.Manual); // Manual?
        }
    }
}