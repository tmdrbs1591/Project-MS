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

        [Header("Augment - 바운스/폭발")]
        [Tooltip("바운스 마법/폭발 마법 증강 적용 시 쓰는 폭발 반경/이펙트. 두 증강 다 안 쓰면 무시된다.")]
        [Min(0f)] [SerializeField] private float explodeRadius = 2f;
        [SerializeField] private GameObject explodeVfxPrefab;

        [Networked] private Vector2 NetDirection { get; set; }
        [Networked] private float NetSpeed { get; set; }
        [Networked] private float NetDamage { get; set; }
        [Networked] private int NetTargetLayerMask { get; set; }
        [Networked] private PlayerRef NetOwner { get; set; }
        [Networked] private NetworkId NetSourceObjectId { get; set; }
        [Networked] private int NetOwnerTeamId { get; set; }
        [Networked] private int NetSkillId { get; set; }
        [Networked] private TickTimer LifeTimer { get; set; }
        [Networked] private TickTimer DespawnTimer { get; set; }
        [Networked] private int NetBounceCount { get; set; }
        [Networked] private NetworkBool NetExplodeOnWall { get; set; }
        [Networked] private float NetExplodeDamageMultiplier { get; set; }

        /// <summary>이 투사체를 쏜 플레이어. 다른 투사체/오브젝트가 "내가 쏜 게 맞는지" 확인할 때 쓴다
        /// (예: 거너 수류탄의 "본인 총알에 맞으면 조기 폭발" 판정).</summary>
        public PlayerRef Owner => NetOwner;
        public int SkillId => NetSkillId;

        private const int MaxHitCount = 32;

        private readonly RaycastHit2D[] castHits = new RaycastHit2D[MaxHitCount];
        private readonly HashSet<IDamageable> pierceHitTargets = new HashSet<IDamageable>();
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
            NetworkId sourceObjectId,
            int ownerTeamId,
            int skillId = 0)
        {
            NetDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            NetSpeed = Mathf.Max(0f, speed);
            NetDamage = Mathf.Max(0f, damage);
            NetTargetLayerMask = targetLayer.value;
            NetOwner = owner;
            NetSourceObjectId = sourceObjectId;
            NetOwnerTeamId = ownerTeamId;
            NetSkillId = skillId;
        }

        /// <summary>바운스 마법/폭발 마법 증강을 적용한다. SpawnProjectile이 반환한 인스턴스에
        /// 대고 스폰 직후(같은 프레임, 아직 FixedUpdateNetwork가 안 돈 시점) 호출한다.
        /// 두 효과는 동시에 켤 수 없다 — 폭발이 우선(켜져 있으면 바운스 횟수는 무시된다).</summary>
        public void ConfigureAugmentBehavior(int bounceCount, bool explodeOnWall, float explodeDamageMultiplier)
        {
            NetBounceCount = Mathf.Max(0, bounceCount);
            NetExplodeOnWall = explodeOnWall;
            NetExplodeDamageMultiplier = Mathf.Max(0f, explodeDamageMultiplier);
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
            if (!Object.HasStateAuthority)
                return;
            if (consumed)
            {
                if (DespawnTimer.Expired(Runner) && Runner != null && Object != null && Object.IsValid)
                    Runner.Despawn(Object);
                return;
            }

            Vector2 delta = NetDirection * NetSpeed * Runner.DeltaTime;
            int hitCount = CastAlongDelta(delta);
            if (hitCount > 1)
                Array.Sort(castHits, 0, hitCount, RaycastHitDistanceComparer.Instance);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = castHits[i];
                if (!TryResolveHit(hit, out IDamageable target, out bool hitTarget))
                    continue;

                if (hitTarget && target != null)
                {
                    DealDamage(target);
                    pierceHitTargets.Add(target);
                }

                bool piercesHit = hitTarget && pierceCharacters && target is CharacterBase;
                if (!piercesHit)
                {
                    if (!hitTarget && TryHandleWallHit(hit.point, hit.normal))
                    {
                        // 바운스: 이번 틱은 여기서 이동을 멈추고, 다음 틱부터 반사된 방향으로 날아간다.
                        return;
                    }

                    Complete(
                        hitTarget ? ResolveHitReason(target) : ProjectileDespawnReason.HitWall,
                        hit.point,
                        hit.normal,
                        target as CharacterBase,
                        true);
                    return;
                }

                // 캐릭터 관통: 같은 틱의 다음 충돌도 거리순으로 계속 처리한다.
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
                IDamageable target = ResolveDamageable(other);
                if (IsSourceCharacter(target))
                    return;
                if (target == null)
                {
                    if (!isWall)
                        return;
                }
                else if (pierceCharacters && target is CharacterBase && pierceHitTargets.Contains(target))
                {
                    return;
                }
                else
                {
                    DealDamage(target);
                    pierceHitTargets.Add(target);

                    if (pierceCharacters && target is CharacterBase)
                        return; // 캐릭터만 관통한다. 소유 오브젝트는 맞으면 소멸한다.

                    Vector2 hitPosition = other.ClosestPoint(transform.position);
                    Vector2 hitNormal = ((Vector2)transform.position - hitPosition).normalized;
                    Complete(ResolveHitReason(target), hitPosition, hitNormal, target as CharacterBase, true);
                    return;
                }
            }

            {
                Vector2 hitPosition = other.ClosestPoint(transform.position);
                Vector2 hitNormal = ((Vector2)transform.position - hitPosition).normalized;
                if (TryHandleWallHit(hitPosition, hitNormal))
                    return;
                Complete(ProjectileDespawnReason.HitWall, hitPosition, hitNormal, null, true);
            }
        }

        /// <summary>벽에 맞았을 때 증강(바운스 마법/폭발 마법)을 처리한다. true를 반환하면 이번엔
        /// 소멸하지 않고 계속 날아간다(바운스). 폭발은 항상 그대로 소멸한다(false 반환).</summary>
        private bool TryHandleWallHit(Vector2 hitPoint, Vector2 hitNormal)
        {
            if (NetExplodeOnWall)
            {
                ExplodeAtPoint(hitPoint);
                return false;
            }

            if (NetBounceCount > 0)
            {
                NetBounceCount--;
                Vector2 normal = hitNormal.sqrMagnitude > 0.001f ? hitNormal.normalized : -NetDirection;
                NetDirection = Vector2.Reflect(NetDirection, normal).normalized;
                return true;
            }

            return false;
        }

        private void ExplodeAtPoint(Vector2 position)
        {
            CharacterBase source = ResolveSource();
            source?.DetonateProjectileExplosion(position, NetDamage * NetExplodeDamageMultiplier, explodeRadius, NetTargetLayerMask);

            if (explodeVfxPrefab != null)
                Rpc_PlayExplodeVfx(position);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_PlayExplodeVfx(Vector2 position)
        {
            if (explodeVfxPrefab == null)
                return;

            Destroy(Instantiate(explodeVfxPrefab, position, Quaternion.identity), 2f);
        }

        private int CastAlongDelta(Vector2 delta)
        {
            if (projectileCollider == null || delta.sqrMagnitude < 0.000001f)
                return 0;

            int collisionMask = NetTargetLayerMask | wallLayer.value;
            if (collisionMask == 0)
                return 0;

            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = collisionMask,
                useTriggers = true
            };
            return projectileCollider.Cast(
                delta.normalized,
                filter,
                castHits,
                delta.magnitude + collisionSkinWidth);
        }

        private bool TryResolveHit(
            RaycastHit2D candidate,
            out IDamageable target,
            out bool hitTarget)
        {
            target = null;
            hitTarget = false;
            Collider2D candidateCollider = candidate.collider;
            if (candidateCollider == null || candidateCollider == projectileCollider)
                return false;

            int otherLayerBit = 1 << candidateCollider.gameObject.layer;
            bool candidateIsTarget = (NetTargetLayerMask & otherLayerBit) != 0;
            bool candidateIsWall = (wallLayer.value & otherLayerBit) != 0;
            if (!candidateIsTarget && !candidateIsWall)
                return false;

            if (candidateIsTarget)
            {
                target = ResolveDamageable(candidateCollider);
                if (IsSourceCharacter(target))
                    return false;
                if (target == null && !candidateIsWall)
                    return false;
                if (pierceCharacters && target is CharacterBase && pierceHitTargets.Contains(target))
                    return false;
            }

            hitTarget = target != null;
            return true;
        }

        private sealed class RaycastHitDistanceComparer : IComparer<RaycastHit2D>
        {
            public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

            public int Compare(RaycastHit2D left, RaycastHit2D right)
            {
                return left.distance.CompareTo(right.distance);
            }
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

        private void DealDamage(IDamageable target)
        {
            // Fusion이 예측이 빗나가서 과거 틱을 리시뮬레이션할 때, 이 가드가 없으면 같은
            // 타격의 데미지 적용 코드가 여러 번 재실행돼서 데미지/사운드/히트/넉백이 전부
            // 중복된다(SPARK.cs의 UpdateElectricLine/UpdateTeslaFieldLogic과 동일한 이유로
            // 같은 패턴 적용).
            if (Runner != null && Runner.IsResimulation)
                return;

            CharacterBase source = ResolveSource();
            if (source != null)
                source.DealProjectileDamage(target, NetDamage);
            else
            {
                DamageRequest request = new DamageRequest(
                    NetDamage,
                    NetOwner,
                    NetSourceObjectId,
                    NetOwnerTeamId,
                    CharacterDamageSource.Projectile,
                    NetSkillId,
                    hitPosition: transform.position,
                    hitDirection: NetDirection);
                target.RequestDamage(request);
            }
        }

        private static IDamageable ResolveDamageable(Collider2D collider)
        {
            if (collider == null)
                return null;

            CharacterBase character = collider.GetComponentInParent<CharacterBase>();
            if (character != null)
                return character;
            return collider.GetComponentInParent<CharacterOwnedEntity>();
        }

        private bool IsSourceCharacter(IDamageable target)
        {
            CharacterBase character = target as CharacterBase;
            return character != null && character.Object != null &&
                   NetSourceObjectId.IsValid && character.Object.Id == NetSourceObjectId;
        }

        private static ProjectileDespawnReason ResolveHitReason(IDamageable target)
        {
            return target is CharacterOwnedEntity
                ? ProjectileDespawnReason.HitOwnedEntity
                : ProjectileDespawnReason.HitCharacter;
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
            DespawnTimer = TickTimer.CreateFromTicks(Runner, 1);
            if (surfaceNormal.sqrMagnitude < 0.001f)
                surfaceNormal = -NetDirection;

            if (playVfx)
                Rpc_PlayHitVfx(hitPoint, surfaceNormal.normalized);

            CharacterBase source = ResolveSource();
            if (source != null)
                source.NotifyProjectileDespawned(this, reason, hitTarget);

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
