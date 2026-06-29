using Map;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// 총알에 맞으면 부서지는 구조물이다.
///
/// [역할]
///   - 플레이어와 물리 충돌로 자연스럽게 밀린다 (데미지 없음).
///   - MasterClient가 총알 충돌 횟수를 관리한다.
///   - maxHits에 도달하면 모든 클라이언트에 RPC로 파괴를 알린다.
///   - 위치 동기화는 NetworkRigidbody2D가 담당한다.
///
/// [필요한 것]
///   - Collider2D + Rigidbody2D (Dynamic) + PhotonView + NetworkRigidbody2D
///   - 총알 태그: "Bullet"
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BreakableStructure : StructureBase
{
    [SerializeField] private int maxHits = 3;

    private int currentHits;

    private void Awake() => currentHits = maxHits;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!collision.collider.CompareTag("Bullet")) return;

        currentHits--;
        if (currentHits <= 0)
            photonView.RPC(nameof(BreakRPC), RpcTarget.All);
    }

    [PunRPC]
    private void BreakRPC()
    {
        // 프로토타입: 즉시 비활성화. 이후 파괴 이펙트 재생 후 PhotonNetwork.Destroy로 교체
        gameObject.SetActive(false);
    }
}
