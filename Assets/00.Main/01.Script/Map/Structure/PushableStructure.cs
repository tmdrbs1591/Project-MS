using Fusion;
using UnityEngine;

/// <summary>
/// 플레이어가 밀 수 있는 구조물이다. (Fusion 2 / Shared 모드)
///
/// [동작]
///   - 이 박스의 StateAuthority(기본: Shared 모드 마스터)가 물리를 시뮬레이션한다.
///   - 다른 클라가 박스를 밀면, "미는 플레이어의 소유자"가 박스 권한에 Rpc_AddForce 를 보낸다.
///   - 박스 권한이 힘을 적용하고, 위치는 NetworkRigidbody2D(Fusion 물리 애드온)가 동기화한다.
///   - 낙하 데미지 판정은 박스 권한에서만 수행한다.
///
/// [필요 컴포넌트 (프리팹)]
///   - Collider2D + Rigidbody2D (Dynamic)
///   - NetworkObject + Fusion 의 NetworkRigidbody2D (Physics 애드온, 에디터에서 추가)
///   - 플레이어 태그: "Player" / 총알 태그: "Bullet"
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PushableStructure : NetworkBehaviour, IStateAuthorityChanged
{
    [SerializeField] private float bulletPushForce = 8f;
    [SerializeField] private float fallDamage = 20f;
    [SerializeField] private float fallSpeedThreshold = 5f;
    [SerializeField] private float playerPushMultiplier = 1.5f;
    [SerializeField] private float minPushSpeed = 2f;

    private Rigidbody2D rb;

    // Authority Transfer 요청 후 적용할 힘을 임시 보관한다.
    private Vector2? pendingForce;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            // "미는 플레이어의 소유자"만 푸시를 시작한다(중복 방지).
            NetworkObject playerObj = collision.collider.GetComponentInParent<NetworkObject>();
            bool pushedByMyPlayer = playerObj != null && playerObj.HasStateAuthority;

            if (pushedByMyPlayer)
                RequestPushFromPlayer(collision);

            // 낙하 데미지는 박스 권한에서만 판정.
            if (Object != null && Object.HasStateAuthority)
                TryApplyFallDamage(collision);
        }
        else if (collision.collider.CompareTag("Bullet") && Object != null && Object.HasStateAuthority)
        {
            ApplyBulletPush(collision);
        }
    }

    private void RequestPushFromPlayer(Collision2D collision)
    {
        Rigidbody2D playerRb = collision.collider.attachedRigidbody;
        float speed = playerRb != null ? Mathf.Max(playerRb.linearVelocity.magnitude, minPushSpeed) : minPushSpeed;
        Vector2 pushDir = ((Vector2)transform.position - (Vector2)collision.collider.bounds.center).normalized;
        Vector2 force = pushDir * speed * playerPushMultiplier;

        if (Object != null && Object.HasStateAuthority)
        {
            rb.AddForce(force, ForceMode2D.Impulse);
        }
        else
        {
            // RPC(왕복 RTT) 대신 Authority Transfer(편도 RTT/2)로 지연을 줄인다.
            pendingForce = force;
            Object.RequestStateAuthority();
        }
    }

    public void StateAuthorityChanged()
    {
        if (Object.HasStateAuthority && pendingForce.HasValue)
        {
            rb.AddForce(pendingForce.Value, ForceMode2D.Impulse);
            pendingForce = null;
        }
    }

    private void TryApplyFallDamage(Collision2D collision)
    {
        if (rb.linearVelocity.y >= -fallSpeedThreshold) return;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            // normal.y > 0 : 플레이어가 구조물 아래에 있음 (위에서 낙하 중)
            if (contact.normal.y < 0.5f) continue;

            CharacterBase player = collision.collider.GetComponentInParent<CharacterBase>();
            if (player != null)
                player.TakeDamage(fallDamage); // 피격자 권한에서 적용됨
            break;
        }
    }

    private void ApplyBulletPush(Collision2D collision)
    {
        Vector2 pushDir = -collision.relativeVelocity.normalized;
        rb.AddForce(pushDir * bulletPushForce, ForceMode2D.Impulse);
    }
}
