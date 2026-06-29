using Fusion;
using UnityEngine;

/// <summary>
/// 총알에 맞으면 부서지는 구조물이다. (Fusion 2 / Shared 모드)
///
/// [동작]
///   - 박스 권한이 물리/충돌 횟수를 관리한다. 충돌 횟수는 [Networked] 로 동기화.
///   - 플레이어가 밀면 "미는 플레이어 소유자"가 박스 권한에 Rpc_AddForce 를 보낸다.
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
        if (collision.collider.CompareTag("Player"))
        {
            NetworkObject playerObj = collision.collider.GetComponentInParent<NetworkObject>();
            if (playerObj != null && playerObj.HasStateAuthority)
                RequestPushFromPlayer(collision);
        }
        else if (collision.collider.CompareTag("Bullet") && Object != null && Object.HasStateAuthority)
        {
            CurrentHits--;
            if (CurrentHits <= 0)
                Runner.Despawn(Object);
        }
    }

    private void RequestPushFromPlayer(Collision2D collision)
    {
        Rigidbody2D playerRb = collision.collider.attachedRigidbody;
        float speed = playerRb != null ? Mathf.Max(playerRb.linearVelocity.magnitude, minPushSpeed) : minPushSpeed;
        Vector2 pushDir = ((Vector2)transform.position - (Vector2)collision.collider.bounds.center).normalized;
        Vector2 force = pushDir * speed * playerPushMultiplier;

        if (Object != null && Object.HasStateAuthority)
            rb.AddForce(force, ForceMode2D.Impulse);
        else
            Rpc_AddForce(force);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_AddForce(Vector2 force)
    {
        rb.AddForce(force, ForceMode2D.Impulse);
    }
}
