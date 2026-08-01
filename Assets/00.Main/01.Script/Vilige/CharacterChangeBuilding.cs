using UnityEngine;

/// <summary>
/// 캐릭터 변경 건물 (스텁). 캐릭터 목록/선택 UI가 아직 없어 F 상호작용 시 로그만 출력한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CharacterChangeBuilding : MonoBehaviour, IInteractable
{
    [SerializeField] private int selectedCharacterIndex = 0;


    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (!col.isTrigger)
            col.isTrigger = true;
    }

    public void Interact()
    {
        // SelectedCharacterIndex에 플레이어 프리팹 인덱스 정보 저장
        PlayerPrefs.SetInt("SelectedCharacterIndex", selectedCharacterIndex);
        PlayerPrefs.Save();

        Debug.Log($"캐릭터가 변경되었습니다. (인덱스: {selectedCharacterIndex})");
    }
}
