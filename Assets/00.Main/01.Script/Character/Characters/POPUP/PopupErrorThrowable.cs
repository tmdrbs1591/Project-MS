using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>
    /// 팝업의 오류 투척(기본 공격) 스킬의 투사체를 담당하는 클래스.
    /// </summary>
    public class PopupErrorThrowable : CharacterThrowable
    {
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private Rigidbody2D rb;

        [Header("Augment - 폭발 마법")]
        [Tooltip("폭발 마법 증강으로 벽/바닥에 명중했을 때 피해를 주는 반경")]
        [Min(0f)] [SerializeField] private float explodeRadius = 2f;
        [SerializeField] private GameObject explodeVfxPrefab;

        [Header("Destroy VFX")]
        [Tooltip("사라질 때(적 명중, 수명 끝, 개수 초과로 밀려남 등) 그 자리에서 터지는 이펙트")]
        [SerializeField] private GameObject destroyVfxPrefab;
        [Tooltip("이펙트 재생 시간(초). 이 시간이 지나면 남은 파티클이 다 사라진 뒤 지운다.")]
        [Min(0f)] [SerializeField] private float destroyVfxLifetime = 1f;
        [SerializeField] private LayerMask _groundLayer;

        private const float AttackRadius = 0.5f;

        private float damage;
        private int bounceTimes;
        private float bounceForce;
        private bool explodeOnWall;
        private float explodeDamageMultiplier;

        private int currentBounceTimes = 0;

        public void Initialize(
            float _damage,
            int _bounceTimes,
            float _bounceForce,
            bool _explodeOnWall = false,
            float _explodeDamageMultiplier = 0f)
        {
            damage = _damage;
            bounceTimes = _bounceTimes;
            bounceForce = Mathf.Max(0f, _bounceForce);
            explodeOnWall = _explodeOnWall;
            explodeDamageMultiplier = Mathf.Max(0f, _explodeDamageMultiplier);
            currentBounceTimes = 0;

            if (rb == null)
                rb = GetComponent<Rigidbody2D>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (Object == null || !Object.HasStateAuthority || IsDestroying ||
                collision == null || collision.collider == null)
                return;

            CheckGroundToDestroy(collision);

            // 위에서 Destroy 처리됐으면 데미지 체크 X
            if (!IsDestroying)
                CheckTargetToDealDamage();
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

        private void CheckGroundToDestroy(Collision2D collision)
        {
            int layerBit = 1 << collision.collider.gameObject.layer;
            if ((_groundLayer.value & layerBit) == 0)
                return;

            // CharacterProjectile 코드랑 똑같이 바운스, 폭발 둘 다 있어도 폭발 마법만 적용
            if (explodeOnWall)
            {
                Vector2 hitPoint = collision.contactCount > 0
                    ? collision.GetContact(0).point
                    : (Vector2)transform.position;
                ExplodeAtPoint(hitPoint);
                RequestDestroy(OwnedEntityDestroyReason.FuseExpired);
                return;
            }

            currentBounceTimes++;
            if (currentBounceTimes > bounceTimes)
            {
                RequestDestroy(OwnedEntityDestroyReason.FuseExpired);
                return;
            }

            // 충돌 당시 속도 사용
            Vector2 impactVelocity = collision.relativeVelocity;
            Vector2 bounceDirection = -impactVelocity.normalized;

            if (collision.contactCount > 0)
            {
                Vector2 normal = collision.GetContact(0).normal;
                bounceDirection = Vector2.Reflect(impactVelocity, normal).normalized;

                bool isBounceDirectionToContact = Vector2.Dot(bounceDirection, normal) < 0f;

                if (bounceDirection.sqrMagnitude < 0.001f)
                    bounceDirection = normal;
                else if (isBounceDirectionToContact)
                    bounceDirection = -bounceDirection;
            }

            // 기존 속도 없애고 속도 지정
            rb.linearVelocity = bounceDirection * (impactVelocity.magnitude * bounceForce);
        }

        private void ExplodeAtPoint(Vector2 position)
        {
            OwnerCharacter?.DetonateProjectileExplosion(
                position,
                damage * explodeDamageMultiplier,
                explodeRadius,
                targetLayer);

            if (explodeVfxPrefab != null)
                Rpc_PlayExplodeVfx(position);
        }

        [Fusion.Rpc(Fusion.RpcSources.StateAuthority, Fusion.RpcTargets.All)]
        private void Rpc_PlayExplodeVfx(Vector2 position)
        {
            if (explodeVfxPrefab == null)
                return;

            EffectAutoDestroy.Schedule(Instantiate(explodeVfxPrefab, position, Quaternion.identity), 2f);
        }

        private void CheckTargetToDealDamage()
        {
            IDamageable target = FindFirstDamageableInCircle(transform.position, AttackRadius, targetLayer);
            if (target == null) return;
            if (target == OwnerCharacter) return;

            DealDamage(target, damage);
            RequestDestroy(OwnedEntityDestroyReason.Manual); // Manual?
        }
    }
}
