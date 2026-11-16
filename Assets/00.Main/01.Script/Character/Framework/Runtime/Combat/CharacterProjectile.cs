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
    public sealed class CharacterProjectile : NetworkBehaviour
    {
        [Min(0.1f)] [SerializeField] private float lifetime = 5f;

        [Header("Pierce")]
        [Tooltip("켜면 캐릭터에 맞아도 소멸하지 않고 계속 날아간다(벽에는 그대로 막힘). 관통형 투사체(예: 루시안식 Q)에 사용. 같은 대상은 한 번만 맞는다.")]
        [SerializeField] private bool pierceCharacters;

        [Header("Collision")]
        [Tooltip("데미지 판정에 사용할 콜라이더. 비워두면 기본 Collider2D를 사용한다.")]
        [SerializeField] private Collider2D damageCollider;
        [Tooltip("벽/지형 충돌 판정에 사용할 콜라이더. 비워두면 기본 Collider2D를 사용한다.")]
        [SerializeField] private Collider2D wallCollider;
        [Tooltip("캐릭터 레이어와 별개로, 벽/지형 등 투사체가 부딪혀 소멸할 레이어.")]
        [SerializeField] private LayerMask wallLayer;
        [Tooltip("한 틱 이동 거리보다 살짝 더 검사해서 경계면 누락을 줄인다.")]
        [Min(0f)] [SerializeField] private float collisionSkinWidth = 0.03f;

        [Header("Visual")]
        [Tooltip("투사체 프리팹의 0도 방향 보정각. 스프라이트가 +X를 바라보면 0, +Y를 바라보면 -90.")]
        [SerializeField] private float projectileAngleOffset;
        [Tooltip("강화 상태(ConfigureEmpowered) 스프라이트 교체에 쓸 렌더러. 비워두면 자식에서 자동으로 찾는다.")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("강화된 채로 발사됐을 때 바꿔 낄 스프라이트. 비워두면 원본 스프라이트를 그대로 쓴다" +
            "(예: SPARK 패시브로 충전된 기본공격 총알을 다른 색으로 보여줄 때 사용).")]
        [SerializeField] private Sprite empoweredSprite;
        [SerializeField] private GameObject hitVfxPrefab;
        [Min(0.01f)] [SerializeField] private float hitVfxScale = 1f;
        [Tooltip("생겨나는 순간 그 자리에 한 번 재생할 이펙트(예: 저격탄 탄착). 모든 클라에서 재생된다.")]
        [SerializeField] private GameObject spawnVfxPrefab;
        [Min(0.01f)] [SerializeField] private float spawnVfxScale = 1f;
        [Tooltip("아무것도 못 맞히고 수명이 다해 사라질 때 그 자리에서 살짝 터지는 이펙트. 비우면 그냥 사라진다.")]
        [SerializeField] private GameObject expireVfxPrefab;
        [Min(0.01f)] [SerializeField] private float expireVfxScale = 1f;
        [Tooltip("이펙트 프리팹의 0도 방향 보정각. 프리팹이 +X를 향하면 0, +Y를 향하면 -90.")]
        [SerializeField] private float hitVfxAngleOffset = -90f;
        [Tooltip("이펙트를 표면 노말 방향으로 살짝 밀어 벽 안쪽에 묻히는 것을 방지한다.")]
        [Min(0f)] [SerializeField] private float hitVfxSurfaceOffset = 0.02f;

        [Header("Remote Despawn Sync")]
        [Tooltip("맞은 뒤 오브젝트를 이만큼(초) 더 남겨 둔다. 상대 화면의 투사체는 위치 동기화 때문에 조금 늦게 따라오므로, " +
            "맞은 자리까지 날아온 뒤에 사라지고 이펙트가 터지도록 시간을 준다. 쏜 쪽 화면에서는 맞는 즉시 사라진다.")]
        [Min(0f)] [SerializeField] private float remoteDespawnDelay = 0.3f;

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
        [Networked] private NetworkBool NetEmpowered { get; set; }
        // 소멸 정보: 상대 화면이 투사체가 이 지점에 도착했을 때 사라지고 이펙트를 띄우는 데 쓴다.
        [Networked] private NetworkBool NetConsumed { get; set; }
        [Networked] private Vector2 NetEndPoint { get; set; }
        [Networked] private Vector2 NetEndNormal { get; set; }
        [Networked] private byte NetEndVfx { get; set; }
        // 캐릭터에 맞았으면 그 캐릭터. 상대 화면에서는 투사체가 그 캐릭터의 "지금 몸"까지 날아가 닿고 터진다.
        [Networked] private NetworkId NetEndTargetId { get; set; }

        private const byte EndVfxNone = 0;
        private const byte EndVfxHit = 1;
        private const byte EndVfxExpire = 2;

        /// <summary>이 투사체를 쏜 플레이어. 다른 투사체/오브젝트가 "내가 쏜 게 맞는지" 확인할 때 쓴다
        /// (예: 거너 수류탄의 "본인 총알에 맞으면 조기 폭발" 판정).</summary>
        public PlayerRef Owner => NetOwner;
        public int SkillId => NetSkillId;
        /// <summary>소멸한 지점(맞은 지점 / 벽·땅 충돌 지점 / 수명 만료 위치). OnProjectileDespawned 에서 이펙트 위치로 쓴다.</summary>
        public Vector2 LastHitPoint { get; private set; }

        private const int MaxHitCount = 32;

        private readonly RaycastHit2D[] castHits = new RaycastHit2D[MaxHitCount];
        private readonly HashSet<IDamageable> pierceHitTargets = new HashSet<IDamageable>();
        private bool consumed;
        private bool endShown;          // 이 클라 화면에서 사라짐(숨김 + 이펙트)을 처리했는지
        private bool followingEnd;      // 상대 화면: 소멸 지점까지 따라가는 중
        private Vector2 visualPos;      // 상대 화면: 따라가는 동안 그리는 위치
        private float catchSpeed;       // 상대 화면: 따라가는 속도(남은 거리를 EndCatchTime 안에 가도록)
        private Vector2 endDir;         // 상대 화면: 소멸 지점까지 날아가는 방향(쏜 방향 그대로)
        private float endRemaining;     // 상대 화면: 소멸 지점까지 남은 거리
        private static readonly RaycastHit2D[] endBodyHits = new RaycastHit2D[8];
        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();

        /// <summary>상대 화면에서 늦게 따라오던 투사체가 소멸 지점까지 따라잡는 최대 시간(초). remoteDespawnDelay보다 짧아야
        /// 지워지기 전에 반드시 도착한다.</summary>
        private const float EndCatchTime = 0.12f;

        /// <summary>아직 날아가는 중인지. 맞거나 수명이 다하면 false(오브젝트는 잠시 남아 있다 — remoteDespawnDelay).
        /// 투사체를 찾아 쓰는 쪽(AI 회피, 외곽선 등)은 이 값이 false인 투사체를 무시한다.</summary>
        public bool IsFlying => Object != null && Object.IsValid && !NetConsumed;

        private void Awake()
        {
            Collider2D defaultCollider = GetComponent<Collider2D>();
            if (defaultCollider == null)
                defaultCollider = GetComponentInChildren<Collider2D>();
            if (damageCollider == null)
                damageCollider = defaultCollider != null ? defaultCollider : wallCollider;
            if (wallCollider == null)
                wallCollider = defaultCollider != null ? defaultCollider : damageCollider;
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
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

        /// <summary>이 발이 "강화된" 상태로 발사됐는지 설정한다. true면 empoweredSprite로 바꿔
        /// 그려서(설정돼 있을 때) 양쪽 클라이언트 모두에게 이 발이 특별하다는 걸 보여준다.
        /// ConfigureAugmentBehavior와 마찬가지로 SpawnProjectile이 반환한 인스턴스에 스폰 직후
        /// (같은 프레임, 아직 FixedUpdateNetwork가 안 돈 시점) 호출한다.</summary>
        public void ConfigureEmpowered(bool empowered)
        {
            NetEmpowered = empowered;
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
            endShown = false;
            followingEnd = false;
            pierceHitTargets.Clear();
            SetVisualsVisible(true);
            SetCollidersEnabled(true);

            if (Object.HasStateAuthority)
                LifeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);

            ApplyEmpoweredVisual();
            AlignToDirection();

            // Spawned()는 모든 클라에서 불리므로 RPC 없이 각자 재생한다.
            if (spawnVfxPrefab != null)
                FxSpawner.Spawn(spawnVfxPrefab, transform.position, Quaternion.identity, spawnVfxScale);
        }

        // 스프라이트는 비행 중 안 바뀌므로 매 프레임이 아니라 Spawned() 시점에 한 번만 적용한다.
        // NetEmpowered는 스폰 콜백(ConfigureEmpowered)에서 초기 네트워크 상태로 설정되므로,
        // 관전/상대 클라이언트도 자기 Spawned()가 불릴 때 이미 최종값을 읽을 수 있다.
        private void ApplyEmpoweredVisual()
        {
            if (spriteRenderer != null && NetEmpowered && empoweredSprite != null)
                spriteRenderer.sprite = empoweredSprite;
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
            bool hasWallHit = TryCastWallAlongDelta(delta, out RaycastHit2D wallHit);
            int hitCount = CastTargetsAlongDelta(delta);
            if (hitCount > 1)
                Array.Sort(castHits, 0, hitCount, RaycastHitDistanceComparer.Instance);
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = castHits[i];
                if (hasWallHit && wallHit.distance < hit.distance)
                    break;
                if (!TryResolveTargetHit(hit, out IDamageable target))
                    continue;

                DealDamage(target);
                pierceHitTargets.Add(target);

                bool piercesHit = pierceCharacters && target is CharacterBase;
                if (!piercesHit)
                {
                    Complete(
                        ResolveHitReason(target),
                        hit.point,
                        hit.normal,
                        target as CharacterBase,
                        EndVfxHit);
                    return;
                }

                // 캐릭터 관통: 같은 틱의 다음 충돌도 거리순으로 계속 처리한다.
            }

            if (hasWallHit)
            {
                if (TryHandleWallHit(wallHit.point, wallHit.normal))
                {
                    // 바운스: 이번 틱은 여기서 이동을 멈추고, 다음 틱부터 반사된 방향으로 날아간다.
                    return;
                }

                Complete(
                    ProjectileDespawnReason.HitWall,
                    wallHit.point,
                    wallHit.normal,
                    null,
                    EndVfxHit);
                return;
            }

            transform.position += (Vector3)delta;
            if (LifeTimer.Expired(Runner))
            {
                Complete(ProjectileDespawnReason.LifetimeExpired, transform.position, -NetDirection, null,
                    expireVfxPrefab != null ? EndVfxExpire : EndVfxNone);
            }
        }

        public override void Render()
        {
            AlignToDirection();

            // 쏜 쪽은 맞는 즉시 사라진다. 상대 쪽은 LateUpdate에서 소멸 지점까지 날아간 뒤 사라진다.
            if (NetConsumed && !endShown && Object.HasStateAuthority)
                ShowEnd(NetEndPoint, NetEndNormal);
        }

        // 상대 화면: 소멸 정보가 오면 늦게 따라오던 투사체를 쏜 방향 그대로 직선으로 날려 보내고,
        //   - 가는 길에 맞은 캐릭터의 지금 몸에 닿으면 거기서,
        //   - 아니면 쏜 쪽에서 맞은 지점(NetEndPoint)까지 가서 터뜨린다.
        // 캐릭터를 쫓아가지 않으므로 궤적이 휘지 않는다(유도탄처럼 보이던 문제). 공중에서 그냥 사라지지도 않는다.
        // (위치 동기화가 Render에서 위치를 덮어쓰므로, 그 뒤인 LateUpdate에서 그린다. 회전은 Render의 AlignToDirection 그대로.)
        private void LateUpdate()
        {
            if (endShown || Object == null || !Object.IsValid || Object.HasStateAuthority || !NetConsumed)
                return;

            if (!followingEnd)
            {
                followingEnd = true;
                visualPos = transform.position;
                endDir = NetDirection.sqrMagnitude > 0.0001f
                    ? NetDirection.normalized
                    : (NetEndPoint - visualPos).normalized;
                endRemaining = Mathf.Max(0f, Vector2.Dot(NetEndPoint - visualPos, endDir));
                catchSpeed = Mathf.Max(NetSpeed, endRemaining / EndCatchTime);
            }

            Vector2 before = visualPos;
            float step = Mathf.Min(catchSpeed * Time.deltaTime, endRemaining);
            visualPos = before + endDir * step;
            endRemaining -= step;
            transform.position = visualPos;

            if (TryHitEndTargetBody(before, step, out Vector2 bodyPoint, out Vector2 bodyNormal))
            {
                transform.position = bodyPoint;
                ShowEnd(bodyPoint, bodyNormal);
                return;
            }

            if (endRemaining <= 0.0001f)
                ShowEnd(visualPos, NetEndTargetId.IsValid ? -endDir : NetEndNormal);
        }

        // 이번 프레임 이동 구간(before → before + endDir * step)이 맞은 캐릭터의 지금 몸에 닿았는지.
        private bool TryHitEndTargetBody(Vector2 before, float step, out Vector2 point, out Vector2 normal)
        {
            point = default;
            normal = -endDir;
            if (!NetEndTargetId.IsValid || Runner == null ||
                !Runner.TryFindObject(NetEndTargetId, out NetworkObject targetObject) || targetObject == null)
                return false;

            Collider2D body = targetObject.GetComponent<Collider2D>();
            if (body == null || !body.enabled)
                return false;

            if (body.OverlapPoint(before))
            {
                point = before;
                return true;
            }

            ContactFilter2D filter = new ContactFilter2D { useLayerMask = true, layerMask = 1 << body.gameObject.layer, useTriggers = true };
            int count = Physics2D.Raycast(before, endDir, filter, endBodyHits, step);
            for (int i = 0; i < count; i++)
            {
                if (endBodyHits[i].collider != body)
                    continue;
                point = endBodyHits[i].point;
                if (endBodyHits[i].normal.sqrMagnitude > 0.001f)
                    normal = endBodyHits[i].normal;
                return true;
            }
            return false;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            // 따라가는 도중 지워졌으면(드묾) 지금 자리에서 터뜨린다.
            if (!endShown && hasState && NetConsumed)
                ShowEnd(followingEnd ? visualPos : NetEndPoint, NetEndNormal);
        }

        // 이 클라 화면에서 투사체를 숨기고 소멸 이펙트를 띄운다(클라마다 한 번).
        private void ShowEnd(Vector2 point, Vector2 normal)
        {
            endShown = true;
            SetVisualsVisible(false);
            SetCollidersEnabled(false);

            if (NetEndVfx == EndVfxHit)
                PlayHitVfx(point, normal);
            else if (NetEndVfx == EndVfxExpire && expireVfxPrefab != null)
                FxSpawner.Spawn(expireVfxPrefab, point, Quaternion.identity, expireVfxScale);
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (damageCollider != null)
                damageCollider.enabled = enabled;
            if (wallCollider != null)
                wallCollider.enabled = enabled;
        }

        // 숨길 때는 그 순간 붙어 있는 그림을 모두 끈다(외곽선처럼 나중에 붙은 것 포함). 보일 때는 끈 것만 되돌린다.
        private void SetVisualsVisible(bool visible)
        {
            if (visible)
            {
                foreach (Renderer r in hiddenRenderers)
                    if (r != null)
                        r.enabled = true;
                hiddenRenderers.Clear();
                return;
            }

            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled)
                    continue;
                r.enabled = false;
                hiddenRenderers.Add(r);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (consumed || Object == null || !Object.HasStateAuthority || IsOwnCollider(other))
                return;

            int otherLayerBit = 1 << other.gameObject.layer;
            bool isTarget = (NetTargetLayerMask & otherLayerBit) != 0 && IsColliderTouching(damageCollider, other);
            bool isWall = (wallLayer.value & otherLayerBit) != 0 && IsColliderTouching(wallCollider, other);

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
                    Complete(ResolveHitReason(target), hitPosition, hitNormal, target as CharacterBase, EndVfxHit);
                    return;
                }
            }

            if (ResolveDamageable(other) != null)
                return;

            {
                Vector2 hitPosition = other.ClosestPoint(transform.position);
                Vector2 hitNormal = ((Vector2)transform.position - hitPosition).normalized;
                if (TryHandleWallHit(hitPosition, hitNormal))
                    return;
                Complete(ProjectileDespawnReason.HitWall, hitPosition, hitNormal, null, EndVfxHit);
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

            EffectAutoDestroy.Schedule(Instantiate(explodeVfxPrefab, position, Quaternion.identity), 2f);
        }

        private bool TryCastWallAlongDelta(Vector2 delta, out RaycastHit2D closestWallHit)
        {
            closestWallHit = default;
            if (wallCollider == null || wallLayer.value == 0 || delta.sqrMagnitude < 0.000001f)
                return false;

            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = wallLayer,
                useTriggers = true
            };
            int hitCount = wallCollider.Cast(
                delta.normalized,
                filter,
                castHits,
                delta.magnitude + collisionSkinWidth);

            bool found = false;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D candidate = castHits[i];
                Collider2D candidateCollider = candidate.collider;
                if (IsOwnCollider(candidateCollider) || ResolveDamageable(candidateCollider) != null)
                    continue;

                if (!found || candidate.distance < closestWallHit.distance)
                {
                    closestWallHit = candidate;
                    found = true;
                }
            }

            return found;
        }

        private int CastTargetsAlongDelta(Vector2 delta)
        {
            if (damageCollider == null || NetTargetLayerMask == 0 || delta.sqrMagnitude < 0.000001f)
                return 0;

            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = NetTargetLayerMask,
                useTriggers = true
            };
            return damageCollider.Cast(
                delta.normalized,
                filter,
                castHits,
                delta.magnitude + collisionSkinWidth);
        }

        private bool TryResolveTargetHit(RaycastHit2D candidate, out IDamageable target)
        {
            target = null;
            Collider2D candidateCollider = candidate.collider;
            if (IsOwnCollider(candidateCollider))
                return false;

            target = ResolveDamageable(candidateCollider);
            if (target == null || IsSourceCharacter(target))
                return false;
            if (pierceCharacters && target is CharacterBase && pierceHitTargets.Contains(target))
                return false;

            return true;
        }

        private bool IsOwnCollider(Collider2D candidateCollider)
        {
            return candidateCollider == null ||
                   candidateCollider == damageCollider ||
                   candidateCollider == wallCollider;
        }

        private static bool IsColliderTouching(Collider2D selfCollider, Collider2D other)
        {
            return selfCollider != null && other != null &&
                   (selfCollider.IsTouching(other) || selfCollider.Distance(other).isOverlapped);
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

            Complete(ProjectileDespawnReason.Manual, transform.position, -NetDirection, null, EndVfxNone);
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
            byte endVfx)
        {
            if (consumed)
                return;

            consumed = true;
            if (surfaceNormal.sqrMagnitude < 0.001f)
                surfaceNormal = -NetDirection;
            LastHitPoint = hitPoint;

            // 바로 지우지 않고 소멸 정보를 남긴다. 쏜 쪽은 Render에서 즉시 숨기고, 상대 쪽은 늦게 따라오던
            // 투사체가 이 지점에 도착했을 때 숨기며 이펙트를 띄운다(ShowEnd). 오브젝트는 그 시간만큼 뒤에 지운다.
            NetConsumed = true;
            NetEndPoint = hitPoint;
            NetEndNormal = surfaceNormal.normalized;
            NetEndVfx = endVfx;
            NetEndTargetId = hitTarget != null && hitTarget.Object != null ? hitTarget.Object.Id : default;
            transform.position = hitPoint;
            SetCollidersEnabled(false);
            DespawnTimer = remoteDespawnDelay > 0f
                ? TickTimer.CreateFromSeconds(Runner, remoteDespawnDelay)
                : TickTimer.CreateFromTicks(Runner, 1);

            CharacterBase source = ResolveSource();
            if (source != null)
                source.NotifyProjectileDespawned(this, reason, hitTarget);

        }

        private void PlayHitVfx(Vector2 position, Vector2 surfaceNormal)
        {
            if (hitVfxPrefab == null)
                return;

            Vector2 normal = surfaceNormal.sqrMagnitude > 0.001f ? surfaceNormal.normalized : Vector2.left;
            Vector2 spawnPosition = position + normal * hitVfxSurfaceOffset;
            float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg + hitVfxAngleOffset;
            FxSpawner.Spawn(hitVfxPrefab, spawnPosition, Quaternion.Euler(0f, 0f, angle), hitVfxScale);
        }

        private void AlignToDirection()
        {
            Vector2 direction = NetDirection.sqrMagnitude > 0.0001f ? NetDirection.normalized : Vector2.right;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + projectileAngleOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
