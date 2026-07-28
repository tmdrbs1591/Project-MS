using Fusion;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    /// <summary>
    /// NetworkObject와 NetworkTransform이 붙은 투사체 프리팹에 사용한다.
    /// 충돌과 데미지는 투사체 StateAuthority에서 한 번만 처리한다.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class CharacterProjectile : NetworkBehaviour
    {
        [Min(0.1f)] [SerializeField] private float lifetime = 5f;

        [Networked] private Vector2 NetDirection { get; set; }
        [Networked] private float NetSpeed { get; set; }
        [Networked] private float NetDamage { get; set; }
        [Networked] private int NetTargetLayerMask { get; set; }
        [Networked] private PlayerRef NetOwner { get; set; }
        [Networked] private TickTimer LifeTimer { get; set; }

        private bool consumed;

        public void Initialize(
            Vector2 direction,
            float speed,
            float damage,
            LayerMask targetLayer,
            PlayerRef owner)
        {
            NetDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            NetSpeed = Mathf.Max(0f, speed);
            NetDamage = Mathf.Max(0f, damage);
            NetTargetLayerMask = targetLayer.value;
            NetOwner = owner;
        }

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
                LifeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || consumed)
                return;

            transform.position += (Vector3)(NetDirection * NetSpeed * Runner.DeltaTime);
            if (LifeTimer.Expired(Runner))
                Runner.Despawn(Object);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || Object == null || !Object.HasStateAuthority)
                return;

            int otherLayerBit = 1 << other.gameObject.layer;
            if ((NetTargetLayerMask & otherLayerBit) == 0)
                return;

            CharacterBase target = other.GetComponentInParent<CharacterBase>();
            if (target == null || target.Object == null || target.Object.InputAuthority == NetOwner)
                return;

            consumed = true;
            target.RequestDamage(NetDamage, NetOwner);
            Runner.Despawn(Object);
        }
    }
}
