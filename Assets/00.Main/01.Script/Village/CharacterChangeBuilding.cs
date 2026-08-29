using UnityEngine;

/// <summary>
/// 캐릭터 변경 건물. F 상호작용 시 캐릭터 선택 UI(CharacterSelectUI)를 열고,
/// 카메라가 이 건물 쪽으로 줌인하도록 한다(LobbyCameraFocusController, CharacterSelectUI 참고).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CharacterChangeBuilding : MonoBehaviour, IInteractable
{
    [Tooltip("캐릭터 선택 UI. 비워두면 씬에서 자동으로 찾는다.")]
    [SerializeField] private CharacterSelectUI selectUI;

    [Tooltip("UI가 열려있는 동안 카메라가 줌인할 기준점. 비워두면 이 건물의 위치를 사용한다.")]
    [SerializeField] private Transform cameraFocusPoint;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (!col.isTrigger)
            col.isTrigger = true;

        if (selectUI == null)
            selectUI = FindObjectOfType<CharacterSelectUI>(true);
    }

    public void Interact()
    {
        if (selectUI == null)
        {
            Debug.LogWarning("[CharacterChangeBuilding] CharacterSelectUI 를 찾지 못해 패널을 열 수 없습니다.");
            return;
        }

        selectUI.Open(cameraFocusPoint != null ? cameraFocusPoint : transform);
    }
}
