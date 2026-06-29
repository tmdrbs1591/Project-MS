using Map;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// 플레이어가 밀 수 있는 구조물이다.
///
/// [역할]
///   - MasterClient: 물리엔진이 자연스럽게 밀고, 낙하 데미지 판정도 담당한다.
///   - 비방장: 로컬 충돌을 감지해 MasterClient에 AddNetworkForce RPC를 보낸다.
///   - 총알에 맞으면 MasterClient가 반대 방향으로 힘을 가한다.
///   - 위치 동기화는 NetworkRigidbody2D가 담당한다.
///
/// [필요한 것]
///   - Collider2D + Rigidbody2D (Dynamic)
///   - PhotonView (Ownership Transfer: Fixed) + NetworkRigidbody2D
///   - 플레이어 태그: "Player" / 총알 태그: "Bullet"
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkRigidbody2D))]
public class PushableStructure : StructureBase
{
    [SerializeField] private float bulletPushForce = 8f;
    [SerializeField] private float fallDamage = 20f;
    [SerializeField] private float fallSpeedThreshold = 5f;
    [SerializeField] private float playerPushMultiplier = 1.5f;
    [SerializeField] private float minPushSpeed = 2f;

    private Rigidbody2D rb;
    private NetworkRigidbody2D netRb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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

            if (PhotonNetwork.IsMasterClient)
                TryApplyFallDamage(collision);
        }
        else if (collision.collider.CompareTag("Bullet") && PhotonNetwork.IsMasterClient)
        {
            ApplyBulletPush(collision);
        }
    }

    private void PushFromPlayer(Collision2D collision)
    {
        Rigidbody2D playerRb = collision.collider.attachedRigidbody;
        float speed = playerRb != null ? Mathf.Max(playerRb.linearVelocity.magnitude, minPushSpeed) : minPushSpeed;
        Vector2 pushDir = ((Vector2)transform.position - (Vector2)collision.collider.bounds.center).normalized;
        netRb.AddNetworkForce(pushDir * speed * playerPushMultiplier);
    }

    private void TryApplyFallDamage(Collision2D collision)
    {
        if (rb.linearVelocity.y >= -fallSpeedThreshold) return;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            // normal.y > 0 : 플레이어가 구조물 아래에 있음 (위에서 낙하 중)
            if (contact.normal.y < 0.5f) continue;

            PhotonView playerView = collision.collider.GetComponent<PhotonView>();
            playerView?.RPC("TakeDamageRPC", playerView.Owner, fallDamage);
            break;
        }
    }

    private void ApplyBulletPush(Collision2D collision)
    {
        Vector2 pushDir = -collision.relativeVelocity.normalized;
        rb.AddForce(pushDir * bulletPushForce, ForceMode2D.Impulse);
    }
}
