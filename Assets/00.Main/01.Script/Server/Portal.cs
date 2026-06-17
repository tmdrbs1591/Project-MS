using Photon.Pun;
using UnityEngine;

/// <summary>
/// 매칭 포탈. 플레이어가 앞에 와서 F 를 누르면 매칭을 시작한다.
///
/// 범위 판정은 이 오브젝트의 "트리거 콜라이더"로 한다.
///   - Collider2D 를 하나 달고 Is Trigger 를 켠다 (상호작용 범위만큼 크게)
///   - 플레이어(PortalInteractor)가 이 트리거에 들어오면 Key UI 가 켜지고,
///     그 상태에서 F 를 누르면 PortalInteractor 가 이 포탈의 Interact() 를 호출한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Portal : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("매칭을 담당하는 MatchmakingManager. 비워두면 씬에서 자동으로 찾는다.")]
    [SerializeField] private MatchmakingManager matchmaking;

    private void Awake()
    {
        // 트리거 콜라이더 보장 (상호작용 범위)
        var col = GetComponent<Collider2D>();
        if (!col.isTrigger)
            col.isTrigger = true;

        if (matchmaking == null)
            matchmaking = FindObjectOfType<MatchmakingManager>();
    }

    /// <summary>플레이어가 범위 안에서 F 를 눌렀을 때 호출된다.</summary>
    public void Interact()
    {
        if (matchmaking == null)
        {
            Debug.LogWarning("[Portal] MatchmakingManager 를 찾지 못해 매칭을 시작할 수 없습니다.");
            return;
        }

        Debug.Log("[Portal] 매칭 시작 (F)");
        matchmaking.StartMatching();
    }
}
