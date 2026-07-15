using UnityEngine;

/// <summary>
/// 증강 팩 선택 건물. F 상호작용 시 팩 관리 패널(AugmentPackManageUI)을 연다.
/// 실제 매치용 팩 선택(최대 5개)은 매치 시작 시 PackSelectUI 에서 이루어지고,
/// 이 건물에서는 팩 해금 여부만 관리한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AugmentPackBuilding : MonoBehaviour, IInteractable
{
    [Tooltip("팩 관리 UI. 비워두면 씬에서 자동으로 찾는다.")]
    [SerializeField] private AugmentPackManageUI manageUI;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (!col.isTrigger)
            col.isTrigger = true;

        if (manageUI == null)
            manageUI = FindObjectOfType<AugmentPackManageUI>(true);
    }

    public void Interact()
    {
        if (manageUI == null)
        {
            Debug.LogWarning("[AugmentPackBuilding] AugmentPackManageUI 를 찾지 못해 패널을 열 수 없습니다.");
            return;
        }

        manageUI.Open();
    }
}
