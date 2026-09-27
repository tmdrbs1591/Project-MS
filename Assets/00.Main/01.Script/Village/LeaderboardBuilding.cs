using UnityEngine;

/// <summary>
/// 랭킹 리더보드 건물 (스텁). 랭킹 백엔드가 아직 없어 F 상호작용 시 로그만 출력한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LeaderboardBuilding : MonoBehaviour, IInteractable
{
    [Tooltip("[F] 옆에 표시할 문구.")]
    [SerializeField] private string interactionPrompt = "선택";

    public string InteractionPrompt => interactionPrompt;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (!col.isTrigger)
            col.isTrigger = true;
    }

    public void Interact()
    {
        Debug.Log("[LeaderboardBuilding] 랭킹 리더보드 기능은 준비중입니다.");
    }
}
