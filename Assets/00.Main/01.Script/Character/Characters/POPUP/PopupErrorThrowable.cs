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

        [Header("Destroy VFX")]
        [Tooltip("사라질 때(적 명중, 수명 끝, 개수 초과로 밀려남 등) 그 자리에서 터지는 이펙트")]
        [SerializeField] private GameObject destroyVfxPrefab;
        [Tooltip("이펙트 재생 시간(초). 이 시간이 지나면 남은 파티클이 다 사라진 뒤 지운다.")]
        [Min(0f)] [SerializeField] private float destroyVfxLifetime = 1f;

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

        // 사라지는 순간 모든 클라에서 한 번 불린다. 라운드 리셋/주인 퇴장처럼 연출이 필요 없는 정리는 건너뛴다.
        protected override void OnOwnedEntityDestroyedRendered(OwnedEntityDestroyReason reason)
        {
            base.OnOwnedEntityDestroyedRendered(reason);

            if (destroyVfxPrefab == null || reason == OwnedEntityDestroyReason.OwnerDespawned ||
                reason == OwnedEntityDestroyReason.OwnerDisconnected)
                return;

            GameObject vfx = Instantiate(destroyVfxPrefab, transform.position, Quaternion.identity);
            EffectAutoDestroy.Schedule(vfx, destroyVfxLifetime);
        }
    }
}