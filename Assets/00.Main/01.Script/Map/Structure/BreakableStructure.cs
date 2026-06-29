using Map;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// 총알에 맞으면 부서지는 구조물이다.
///
/// [역할]
///   - MasterClient: 물리엔진이 자연스럽게 밀고, 총알 충돌 횟수를 관리한다.
///   - 비방장: 로컬 충돌을 감지해 MasterClient에 AddNetworkForce RPC를 보낸다.
///   - maxHits에 도달하면 모든 클라이언트에 RPC로 파괴를 알린다.
///   - 위치 동기화는 NetworkRigidbody2D가 담당한다.
///
/// [필요한 것]
///   - Collider2D + Rigidbody2D (Dynamic)
///   - PhotonView (Ownership Transfer: Fixed) + NetworkRigidbody2D
///   - 총알 태그: "Bullet"
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkRigidbody2D))]
public class BreakableStructure : StructureBase
{
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float playerPushMultiplier = 1.5f;
    [SerializeField] private float minPushSpeed = 2f;

    private int currentHits;
    private NetworkRigidbody2D netRb;

    private void Awake()
    {
        currentHits = maxHits;
        netRb = GetComponent<NetworkRigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            PhotonView playerView = collision.collider.GetComponent<PhotonView>();
            if (playerView == null || !playerView.IsMine) return;

            if (!PhotonNetwork.IsMasterClient)
                PushFromPlayer(collision);
        }
        else if (collision.collider.CompareTag("Bullet") && PhotonNetwork.IsMasterClient)
        {
            currentHits--;
            if (currentHits <= 0)
                photonView.RPC(nameof(BreakRPC), RpcTarget.All);
        }
    }

    private void PushFromPlayer(Collision2D collision)
    {
        Rigidbody2D playerRb = collision.collider.attachedRigidbody;
        float speed = playerRb != null ? Mathf.Max(playerRb.linearVelocity.magnitude, minPushSpeed) : minPushSpeed;
        Vector2 pushDir = ((Vector2)transform.position - (Vector2)collision.collider.bounds.center).normalized;
        netRb.AddNetworkForce(pushDir * speed * playerPushMultiplier);
    }

    [PunRPC]
    private void BreakRPC()
    {
        // 프로토타입: 즉시 비활성화. 이후 파괴 이펙트 재생 후 PhotonNetwork.Destroy로 교체
        gameObject.SetActive(false);
    }
}
