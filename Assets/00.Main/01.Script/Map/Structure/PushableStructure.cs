using Map;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// 플레이어가 밀 수 있는 구조물이다.
///
/// [역할]
///   - 플레이어와 물리 충돌로 자연스럽게 밀린다 (Rigidbody2D 질량으로 저항감 조절).
///   - 낙하 중 플레이어와 충돌하면 MasterClient가 해당 플레이어에게 데미지 RPC를 전송한다.
///   - 총알에 맞으면 MasterClient가 총알 반대 방향으로 힘을 가한다.
///   - 위치 동기화는 NetworkRigidbody2D가 담당한다.
///
/// [필요한 것]
///   - Collider2D + Rigidbody2D (Dynamic)
///   - PhotonView + NetworkRigidbody2D (위치 동기화용)
///   - 플레이어 태그: "Player" / 총알 태그: "Bullet"
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PushableStructure : StructureBase
{
    [SerializeField] private float bulletPushForce = 8f;
    [SerializeField] private float fallDamage = 20f;
    [SerializeField] private float fallSpeedThreshold = 5f;

    private Rigidbody2D rb;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (collision.collider.CompareTag("Player"))
            TryApplyFallDamage(collision);
        else if (collision.collider.CompareTag("Bullet"))
            ApplyBulletPush(collision);
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
