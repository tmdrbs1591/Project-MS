using System.Collections;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// CaracterBase에서 상속받는 MonoBehaviour, IPunObservable는 매 프레임 연속적으로 상태를 동기화함.
/// 힐팩처럼 단순 이벤트성 호출의 경우 매 프레임 동기화 하는 것 보단 MonoBehaviourPun에 적합함.
/// </summary>

namespace Map
{
    public abstract class ItemBase : MonoBehaviourPun
    {
        [SerializeField] private float respawnDelay = 5f;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Collider2D itemCollider;

        protected abstract void OnPickup(Collider2D player);

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            PhotonView playerView = other.GetComponent<PhotonView>();
            if (playerView == null || !playerView.IsMine) return;

            OnPickup(other);
            photonView.RPC(nameof(PickupRPC), RpcTarget.All);
        }

        [PunRPC]
        private void PickupRPC()
        {
            visual.enabled = false;
            itemCollider.enabled = false;

            if (PhotonNetwork.IsMasterClient)
                StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);
            photonView.RPC(nameof(RespawnRPC), RpcTarget.All);
        }

        [PunRPC]
        private void RespawnRPC()
        {
            visual.enabled = true;
            itemCollider.enabled = true;
        }
    }
}
