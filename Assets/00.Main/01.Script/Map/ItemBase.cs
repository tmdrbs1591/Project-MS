using Fusion;
using UnityEngine;

/// <summary>
/// 맵에 놓이는 네트워크 아이템의 공통 부모. (Fusion 2 / Shared 모드)
///
/// [동작]
///   - 사용 가능 여부(Available)는 [Networked] 로 모두에게 동기화된다.
///   - "내 플레이어"가 사용 가능한 아이템에 닿으면 효과(OnPickup)를 적용하고,
///     아이템 권한에 소비를 알린다(Rpc_Consume) → 모두에게서 사라짐.
///   - 아이템 권한이 respawnDelay 후 다시 사용 가능 상태로 되돌린다.
///
/// [필요 컴포넌트 (프리팹/씬오브젝트)]
///   - NetworkObject + Collider2D(Is Trigger)
///   - 플레이어 태그: "Player"
/// </summary>
namespace Map
{
    public abstract class ItemBase : NetworkBehaviour
    {
        [SerializeField] private float respawnDelay = 5f;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Collider2D itemCollider;

        [Networked] private NetworkBool Available { get; set; }
        [Networked] private TickTimer RespawnTimer { get; set; }

        private bool lastAppliedAvailable;

        protected abstract void OnPickup(Collider2D player);

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
                Available = true;

            ApplyVisual(Available);
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority)
                return;

            // 재생성 타이머가 끝나면 다시 사용 가능.
            if (!Available && RespawnTimer.Expired(Runner))
                Available = true;
        }

        public override void Render()
        {
            // 모든 클라가 동기화된 Available 상태를 시각에 반영.
            if (lastAppliedAvailable != (bool)Available)
                ApplyVisual(Available);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (!Available) return;

            // "내 플레이어"가 먹었을 때만 처리(중복 방지).
            NetworkObject playerObj = other.GetComponentInParent<NetworkObject>();
            if (playerObj == null || !playerObj.HasStateAuthority) return;

            OnPickup(other);     // 효과는 내 플레이어(권한)에 적용
            Rpc_Consume();       // 아이템 권한에 소비 통보
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void Rpc_Consume()
        {
            if (!Available) return; // 이미 누가 먹었으면 무시
            Available = false;
            RespawnTimer = TickTimer.CreateFromSeconds(Runner, respawnDelay);
        }

        private void ApplyVisual(bool available)
        {
            lastAppliedAvailable = available;
            if (visual != null) visual.enabled = available;
            if (itemCollider != null) itemCollider.enabled = available;
        }
    }
}
