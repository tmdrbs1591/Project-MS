using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    /// <summary>
    /// NetworkObject와 Collider2D가 붙은 투사체 프리팹에 사용한다.
    /// 이동, 충돌, 데미지는 투사체의 StateAuthority에서 한 번만 처리한다.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class CharacterProjectile : NetworkBehaviour
    {
        [Min(0.1f)] [SerializeField] private float lifetime = 5f;

        [Header("Pierce")]
        [Tooltip("켜면 캐릭터에 맞아도 소멸하지 않고 계속 날아간다(벽에는 그대로 막힘). 관통형 투사체(예: 루시안식 Q)에 사용. 같은 대상은 한 번만 맞는다.")]
        [SerializeField] private bool pierceCharacters;

        [Header("Collision")]
        [Tooltip("캐릭터 레이어와 별개로, 벽/지형 등 투사체가 부딪혀 소멸할 레이어.")]
        [SerializeField] private LayerMask wallLayer;
        [Tooltip("한 틱 이동 거리보다 살짝 더 검사해서 경계면 누락을 줄인다.")]
        [Min(0f)] [SerializeField] private float collisionSkinWidth = 0.03f;

        [Header("Visual")]
        [Tooltip("투사체 프리팹의 0도 방향 보정각. 스프라이트가 +X를 바라보면 0, +Y를 바라보면 -90.")]
        [SerializeField] private float projectileAngleOffset;
        [SerializeField] private GameObject hitVfxPrefab;
        [Tooltip("이펙트 프리팹의 0도 방향 보정각. 프리팹이 +X를 향하면 0, +Y를 향하면 -90.")]
        [SerializeField] private float hitVfxAngleOffset = -90f;
        [Tooltip("이펙트를 표면 노말 방향으로 살짝 밀어 벽 안쪽에 묻히는 것을 방지한다.")]
        [Min(0f)] [SerializeField] private float hitVfxSurfaceOffset = 0.02f;

        [Networked] private Vector2 NetDirection { get; set; }
        [Networked] private float NetSpeed { get; set; }
        [Networked] private float NetDamage { get; set; }
        [Networked] private int NetTargetLayerMask { get; set; }
        [Networked] private PlayerRef NetOwner { get; set; }
        [Networked] private NetworkId NetSourceObjectId { get; set; }
        [Networked] private TickTimer LifeTimer { get; set; }

        /// <summary>이 투사체를 쏜 플레이어. 다른 투사체/오브젝트가 "내가 쏜 게 맞는지" 확인할 때 쓴다
        /// (예: 거너 수류탄의 "본인 총알에 맞으면 조기 폭발" 판정).</summary>
        public PlayerRef Owner => NetOwner;

        private const int MaxHitCount = 8;

        private readonly RaycastHit2D[] castHits = new RaycastHit2D[MaxHitCount];
        private readonly HashSet<CharacterBase> pierceHitTargets = new HashSet<CharacterBase>();
        private Collider2D projectileCollider;
        private bool consumed;

        private void Awake()
        {
            projectileCollider = GetComponent<Collider2D>();
        }

        public void Initialize(
            Vector2 direction,
            float speed,
            float damage,
            LayerMask targetLayer,
            PlayerRef owner,
            NetworkId sourceObjectId)
        {
            NetDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            NetSpeed = Mathf.Max(0f, speed);
            NetDamage = Mathf.Max(0f, damage);
            NetTargetLayerMask = targetLayer.value;
            NetOwner = owner;
            NetSourceObjectId = sourceObjectId;
        }

        [Obsolete("Use CharacterBase.SpawnProjectile or Initialize with a source NetworkId; legacy initialization cannot preserve damage callbacks.", true)]
        public void Initialize(
            Vector2 direction,
            float speed,
            float damage,
            LayerMask targetLayer,
            PlayerRef owner)
        {
            throw new InvalidOperationException(
                "Legacy projectile initialization cannot preserve the source damage callback contract.");
        }

        public override void Spawned()
        {
            consumed = false;
            pierceHitTargets.Clear();

            if (Object.HasStateAuthority)
                LifeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);

            AlignToDirection();
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || consumed)
                return;

            Vector2 delta = NetDirection * NetSpeed * Runner.DeltaTime;
            if (TryHitAlongDelta(delta, out RaycastHit2D hit, out CharacterBase target, out bool hitTarget))
            {
                if (hitTarget && target != null)
                {
                    DealDamage(target);
                    pierceHitTargets.Add(target);
                }

                if (!(hitTarget && pierceCharacters))
                {
                    Complete(
                        hitTarget ? ProjectileDespawnReason.HitCharacter : ProjectileDespawnReason.HitWall,
                        hit.point,
                        hit.normal,
                        hitTarget ? target : null,
                        true);
                    return;
                }

                // 관통: 소멸하지 않고 이번 틱도 그대로 이동을 마저 진행한다.
            }

            transform.position += (Vector3)delta;
            if (LifeTimer.Expired(Runner))
                Complete(ProjectileDespawnReason.LifetimeExpired, transform.position, -NetDirection, null, false);
        }

        public override void Render()
        {
            AlignToDirection();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || Object == null || !Object.HasStateAuthority)
                return;

            int otherLayerBit = 1 << other.gameObject.layer;
            bool isTarget = (NetTargetLayerMask & otherLayerBit) != 0;
            bool isWall = (wallLayer.value & otherLayerBit) != 0;

            if (!isTarget && !isWall)
                return;

            if (isTarget)
            {
                CharacterBase target = other.GetComponentInParent<CharacterBase>();
                if (target == null || target.Object == null || target.Object.InputAuthority == NetOwner)
                    return;
                if (pierceCharacters && pierceHitTargets.Contains(target))
                    return;

                DealDamage(target);
                pierceHitTargets.Add(target);

                if (pierceCharacters)
                    return; // 관통: 소멸하지 않고 계속 날아간다.

                Vector2 hitPosition = other.ClosestPoint(transform.position);
                Vector2 hitNormal = ((Vector2)transform.position - hitPosition).normalized;
                Complete(ProjectileDespawnReason.HitCharacter, hitPosition, hitNormal, target, true);
                return;
            }

            {
                Vector2 hitPosition = other.ClosestPoint(transform.position);
                Vector2 hitNormal = ((Vector2)transform.position - hitPosition).normalized;
                Complete(ProjectileDespawnReason.HitWall, hitPosition, hitNormal, null, true);
            }
        }

        private bool TryHitAlongDelta(
            Vector2 delta,
            out RaycastHit2D bestHit,
            out CharacterBase bestTarget,
            out bool hitTarget)
        {
            bestHit = default;
            bestTarget = null;
            hitTarget = false;

            if (projectileCollider == null || delta.sqrMagnitude < 0.000001f)
                return false;

            int collisionMask = NetTargetLayerMask | wallLayer.value;
            if (collisionMask == 0)
                return false;

            Vector2 direction = delta.normalized;
            float distance = delta.magnitude + collisionSkinWidth;
            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = collisionMask,
                useTriggers = true
            };

            int hitCount = projectileCollider.Cast(direction, filter, castHits, distance);
            float bestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D candidate = castHits[i];
                Collider2D candidateCollider = candidate.collider;
                if (candidateCollider == null || candidateCollider == projectileCollider)
                    continue;

                int otherLayerBit = 1 << candidateCollider.gameObject.layer;
                bool candidateIsTarget = (NetTargetLayerMask & otherLayerBit) != 0;
                bool candidateIsWall = (wallLayer.value & otherLayerBit) != 0;
                if (!candidateIsTarget && !candidateIsWall)
                    continue;

                CharacterBase candidateTarget = null;
                if (candidateIsTarget)
                {
                    candidateTarget = candidateCollider.GetComponentInParent<CharacterBase>();
                    if (candidateTarget == null ||
                        candidateTarget.Object == null ||
                        candidateTarget.Object.InputAuthority == NetOwner ||
                        (pierceCharacters && pierceHitTargets.Contains(candidateTarget)))
                    {
                        continue;
                    }
                }

                if (candidate.distance >= bestDistance)
                    continue;

                bestDistance = candidate.distance;
                bestHit = candidate;
                bestTarget = candidateTarget;
                hitTarget = candidateIsTarget;
            }

            if (bestDistance == float.PositiveInfinity)
                return false;

            return true;
        }

        internal void CompleteManually()
        {
            if (Object == null || !Object.HasStateAuthority)
                return;

            Complete(ProjectileDespawnReason.Manual, transform.position, -NetDirection, null, false);
        }

        private CharacterBase ResolveSource()
        {
            if (Runner == null || !NetSourceObjectId.IsValid ||
                !Runner.TryFindObject(NetSourceObjectId, out NetworkObject sourceObject))
                return null;

            return sourceObject.GetComponent<CharacterBase>();
        }

        private void DealDamage(CharacterBase target)
        {
            CharacterBase source = ResolveSource();
            if (source != null)
                source.DealProjectileDamage(target, NetDamage);
            else
                target.RequestDamage(NetDamage, NetOwner);
        }

        private void Complete(
            ProjectileDespawnReason reason,
            Vector2 hitPoint,
            Vector2 surfaceNormal,
            CharacterBase hitTarget,
            bool playVfx)
        {
            if (consumed)
                return;

            consumed = true;
            if (surfaceNormal.sqrMagnitude < 0.001f)
                surfaceNormal = -NetDirection;

            if (playVfx)
                Rpc_PlayHitVfx(hitPoint, surfaceNormal.normalized);

            CharacterBase source = ResolveSource();
            if (source != null)
                source.NotifyProjectileDespawned(this, reason, hitTarget);

            if (Runner != null && Object != null)
                Runner.Despawn(Object);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_PlayHitVfx(Vector2 position, Vector2 surfaceNormal)
        {
            if (hitVfxPrefab == null)
                return;

            Vector2 normal = surfaceNormal.sqrMagnitude > 0.001f ? surfaceNormal.normalized : Vector2.left;
            Vector2 spawnPosition = position + normal * hitVfxSurfaceOffset;
            float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg + hitVfxAngleOffset;
            Destroy(Instantiate(hitVfxPrefab, spawnPosition, Quaternion.Euler(0f, 0f, angle)), 2f);
        }

        private void AlignToDirection()
        {
            Vector2 direction = NetDirection.sqrMagnitude > 0.0001f ? NetDirection.normalized : Vector2.right;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + projectileAngleOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
