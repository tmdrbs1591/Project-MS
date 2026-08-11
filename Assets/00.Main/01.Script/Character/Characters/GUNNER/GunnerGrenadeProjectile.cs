using Fusion;
using ProjectMS.CharacterSystem;
using UnityEngine;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>
    /// 거너 E스킬(수류탄) 전용 투사체. 일반 CharacterProjectile과 별개 클래스인 이유는
    /// "던진 사람 본인의 총알에 맞으면 조기 폭발"이라는, 캐릭터 대 캐릭터 데미지와는
    /// 다른 충돌 규칙이 필요해서다(CharacterProjectile은 sealed라 상속도 안 됨).
    ///
    /// [움직임]
    ///   던지는 순간의 초기 속도만 권한자가 Rigidbody2D에 적용하고, 그 이후는 Unity 물리
    ///   (중력/충돌/마찰)가 자연스럽게 포물선으로 날아가 착지하고 굴러가는 움직임을 만든다
    ///   (Assets/00.Main/01.Script/Grenade.cs와 동일한 접근). 위치/회전은 Fusion의
    ///   NetworkRigidbody2D(Physics 애드온)가 동기화한다.
    ///
    /// [폭발 조건 — 벽/바닥/캐릭터에 부딪혀도 터지지 않는다, 실제 수류탄처럼 굴러다닌다]
    ///   1. 던진 사람 본인의 CharacterProjectile(기본공격/Q 총알)이 스쳐 지나감 — "쏘면 폭발".
    ///      (다른 플레이어의 총알에는 반응하지 않는다 — CharacterProjectile.Owner로 구분)
    ///   2. fuseSeconds가 지나도록 아무것도 안 맞음 — 자동 폭발.
    ///
    /// [데미지 위임]
    ///   실제 범위 데미지/이펙트 계산은 이 오브젝트가 아니라 던진 GunnerCharacter가 한다
    ///   (FindEnemiesInCircle 등 전투 쿼리 헬퍼가 CharacterBase 하위에서만 protected로 열려있어서).
    ///   그래서 폭발 시 ResolveOwner()로 GunnerCharacter를 찾아 DetonateGrenade()를 호출만 한다.
    ///
    /// [필요 컴포넌트 (프리팹)]
    ///   - NetworkObject + Fusion의 NetworkRigidbody2D(Physics 애드온)
    ///   - Rigidbody2D (Body Type: Dynamic)
    ///   - CircleCollider2D 권장(구르는 모양이 자연스러움). Is Trigger는 끈다(꺼둔 채로 둔다) —
    ///     총알 쪽 콜라이더가 Trigger라서, 이 콜라이더가 Trigger가 아니어도 총알에 맞으면
    ///     OnTriggerEnter2D가 정상적으로 발생하고, 동시에 바닥/캐릭터와는 평범한 물리 충돌로
    ///     반응해 착지/굴러가는 게 가능해진다.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class GunnerGrenadeProjectile : NetworkBehaviour
    {
        [Min(0.1f)] [SerializeField] private float fuseSeconds = 2f;

        [Header("Explosion VFX")]
        [Tooltip("터질 때 재생할 이펙트. GunnerCharacter의 SkillE 액션 이펙트(던지는 연출)와는\n별개 슬롯이라 서로 안 겹친다. 비워두면 이펙트 없이 조용히 넘어간다.")]
        [SerializeField] private GameObject explosionVfxPrefab;
        [Min(0f)] [SerializeField] private float explosionVfxLifetime = 1f;

        [Networked] private Vector2 NetInitialVelocity { get; set; }
        [Networked] private float NetDamage { get; set; }
        [Networked] private PlayerRef NetOwner { get; set; }
        [Networked] private NetworkId NetSourceObjectId { get; set; }
        [Networked] private TickTimer FuseTimer { get; set; }

        private Rigidbody2D rb;
        private bool exploded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        /// <summary>스폰 직전에 던진 사람이 호출한다.</summary>
        public void Initialize(Vector2 throwVelocity, float damage, PlayerRef owner, NetworkId sourceObjectId)
        {
            NetInitialVelocity = throwVelocity;
            NetDamage = Mathf.Max(0f, damage);
            NetOwner = owner;
            NetSourceObjectId = sourceObjectId;
        }

        public override void Spawned()
        {
            exploded = false;

            if (!Object.HasStateAuthority)
                return;

            FuseTimer = TickTimer.CreateFromSeconds(Runner, fuseSeconds);
            // 던진 순간의 초기 속도만 부여한다. 이후 움직임(포물선/바운스/구름)은 물리 시뮬레이션이 맡는다.
            rb.linearVelocity = NetInitialVelocity;
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || exploded)
                return;

            if (FuseTimer.Expired(Runner))
                Explode(transform.position);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (exploded || Object == null || !Object.HasStateAuthority)
                return;

            // 본인이 던진 수류탄이 자기 총알에 맞았는지("쏘면 폭발"). 상대 총알에는 반응하지 않는다.
            CharacterProjectile bullet = other.GetComponentInParent<CharacterProjectile>();
            if (bullet != null && bullet.Owner == NetOwner)
                Explode(transform.position);
        }

        private void Explode(Vector2 position)
        {
            if (exploded)
                return;

            exploded = true;

            GunnerCharacter owner = ResolveOwner();
            owner?.DetonateGrenade(position, NetDamage);
            Rpc_PlayExplosionVfx(position);

            if (Runner != null && Object != null)
                Runner.Despawn(Object);
        }

        private GunnerCharacter ResolveOwner()
        {
            if (Runner == null || !NetSourceObjectId.IsValid ||
                !Runner.TryFindObject(NetSourceObjectId, out NetworkObject sourceObject))
                return null;

            return sourceObject.GetComponent<GunnerCharacter>();
        }

        /// <summary>폭발 순간 모든 클라에서 재생되는 시각 이펙트. GunnerCharacter의
        /// PlayActionEffect(SkillE)와 완전히 별개 경로라 던지기 연출과 안 겹친다.</summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_PlayExplosionVfx(Vector2 position)
        {
            if (explosionVfxPrefab == null)
                return;

            GameObject vfx = Instantiate(explosionVfxPrefab, position, Quaternion.identity);
            if (explosionVfxLifetime > 0f)
                Destroy(vfx, explosionVfxLifetime);
        }
    }
}
