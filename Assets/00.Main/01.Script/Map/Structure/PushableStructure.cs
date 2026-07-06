using Fusion;
using UnityEngine;

/// <summary>
/// 플레이어가 밀 수 있는 구조물이다. (Fusion 2 / Shared 모드)
///
/// [동작]
///   - 이 박스의 StateAuthority(기본: Shared 모드 마스터)가 물리를 시뮬레이션한다.
///   - 물리는 모든 클라가 로컬로 동일하게 시뮬레이션하므로, 권한을 가진 클라도 다른
///     플레이어와의 충돌 콜백을 그대로 받는다. 그래서 "누가 밀었는지"와 상관없이
///     권한을 가진 클라가 그 콜백에서 바로 힘을 적용하면 되고, RPC나 권한 이전 같은
///     우회가 필요 없다.
///   - 위치는 NetworkRigidbody2D(Fusion 물리 애드온)가 동기화한다.
///   - 낙하 데미지 판정도 박스 권한에서만 수행한다.
///
/// [필요 컴포넌트 (프리팹)]
///   - Collider2D + Rigidbody2D (Dynamic)
///   - NetworkObject + Fusion 의 NetworkRigidbody2D (Physics 애드온, 에디터에서 추가)
///   - 플레이어 태그: "Player" / 총알 태그: "Bullet"
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PushableStructure : NetworkBehaviour
{
    [SerializeField] private float bulletPushForce = 8f;
    [SerializeField] private float fallDamage = 20f;
    [SerializeField] private float fallSpeedThreshold = 5f;
    [SerializeField] private float playerPushMultiplier = 1.5f;
    [SerializeField] private float minPushSpeed = 2f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player") || Object == null || !Object.HasStateAuthority)
            return;

        ApplyPlayerPush(collision);
        TryApplyFallDamage(collision);
    }

    // 총알 콜라이더는 Trigger라서 OnCollisionEnter2D가 아니라 여기서 받는다.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Bullet") || Object == null || !Object.HasStateAuthority)
            return;

        Projectile bullet = other.GetComponent<Projectile>();
        if (bullet != null)
            ApplyBulletPush(bullet.TravelDirection);
    }

    private void ApplyPlayerPush(Collision2D collision)
    {
        Rigidbody2D playerRb = collision.collider.attachedRigidbody;
        float speed = playerRb != null ? Mathf.Max(playerRb.linearVelocity.magnitude, minPushSpeed) : minPushSpeed;
        Vector2 pushDir = ((Vector2)transform.position - (Vector2)collision.collider.bounds.center).normalized;
        Vector2 force = pushDir * speed * playerPushMultiplier;

        rb.AddForce(force, ForceMode2D.Impulse);
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

    private void ApplyBulletPush(Vector2 bulletDirection)
    {
        rb.AddForce(bulletDirection * bulletPushForce, ForceMode2D.Impulse);
    }
}
