using UnityEngine;

/// <summary>
/// 캐릭터 변경 건물 (스텁). 캐릭터 목록/선택 UI가 아직 없어 F 상호작용 시 로그만 출력한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CharacterChangeBuilding : MonoBehaviour, IInteractable
{
    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (!col.isTrigger)
            col.isTrigger = true;
    }

    public void Interact()
    {
        Debug.Log("[CharacterChangeBuilding] 캐릭터 변경 기능은 준비중입니다.");
    }
}
