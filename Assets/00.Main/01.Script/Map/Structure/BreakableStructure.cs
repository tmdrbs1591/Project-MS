using Fusion;
using UnityEngine;

/// <summary>
/// 총알에 맞으면 부서지는 구조물이다. (Fusion 2 / Shared 모드)
///
/// [동작]
///   - 박스 권한이 물리/충돌 횟수를 관리한다. 충돌 횟수는 [Networked] 로 동기화.
///   - 물리는 모든 클라가 로컬로 동일하게 시뮬레이션하므로, 권한을 가진 클라도 다른
///     플레이어가 미는 순간의 OnCollisionEnter2D를 그대로 받는다. 그래서 "누가 밀었는지"와
///     상관없이 권한을 가진 클라가 그 콜백에서 바로 힘을 적용한다(RPC 불필요).
///   - 남은 횟수가 0 이 되면 박스 권한이 Runner.Despawn 으로 모두에게서 제거한다.
///   - 위치 동기화는 NetworkRigidbody2D(Physics 애드온)가 담당한다.
///
/// [필요 컴포넌트 (프리팹)]
///   - Collider2D + Rigidbody2D (Dynamic)
///   - NetworkObject + Fusion 의 NetworkRigidbody2D (Physics 애드온)
///   - 총알 태그: "Bullet"
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BreakableStructure : NetworkBehaviour
{
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float playerPushMultiplier = 1.5f;
    [SerializeField] private float minPushSpeed = 2f;

    [Networked] private int CurrentHits { get; set; }

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            CurrentHits = maxHits;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player") || Object == null || !Object.HasStateAuthority)
            return;

        Rigidbody2D playerRb = collision.collider.attachedRigidbody;
        float speed = playerRb != null ? Mathf.Max(playerRb.linearVelocity.magnitude, minPushSpeed) : minPushSpeed;
        Vector2 pushDir = ((Vector2)transform.position - (Vector2)collision.collider.bounds.center).normalized;
        Vector2 force = pushDir * speed * playerPushMultiplier;

        rb.AddForce(force, ForceMode2D.Impulse);
    }

    // 총알 콜라이더는 Trigger라서 OnCollisionEnter2D가 아니라 여기서 받는다.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Bullet") || Object == null || !Object.HasStateAuthority)
            return;

        CurrentHits--;
        if (CurrentHits <= 0)
            Runner.Despawn(Object);
    }
}
