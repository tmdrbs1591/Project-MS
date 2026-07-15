using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비의 증강 팩 선택 건물(AugmentPackBuilding)과 상호작용하면 뜨는 관리 패널.
/// 전체 팩 목록을 보여준다. 해금 여부는 각 행(AugmentPackRow)이 텍스트("해금됨"/"해금 필요")로
/// 표시하고, 잠긴 팩도 포함해 행을 누르면 상세 패널(AugmentPackDetailUI)의 내용이 갱신된다.
/// 상세 패널은 상시 켜져 있는 패널이라 자연스럽게 보이도록 열릴 때 첫 번째 팩을 기본 선택해둔다.
/// (재화/구매는 아직 없음)
///
/// [씬 설정]
///   - 로비 씬 Canvas 하위에 패널을 만들고 이 스크립트를 붙인다.
///   - panel: 평소엔 꺼져 있다가 Open() 호출 시 켜지는 패널.
///   - rowContainer: 행(AugmentPackRow)들이 생성될 부모 Transform.
///   - rowPrefab: 팩 하나를 표시하는 행 프리팹.
///   - detailUI: 행 클릭 시(및 처음 열릴 때 기본으로) 팩 상세 정보를 보여줄 상시 패널.
///   - closeButton: 누르면 패널을 닫는 X 버튼.
/// </summary>
public class AugmentPackManageUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private AugmentPackRow rowPrefab;
    [SerializeField] private AugmentPackDetailUI detailUI;
    [SerializeField] private Button closeButton;

    private readonly List<AugmentPackRow> spawnedRows = new List<AugmentPackRow>();

    private void Awake()
    {
        SetPanelActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        BuildRows();
        SetPanelActive(true);
        SelectFirstPackByDefault();
    }

    public void Close()
    {
        SetPanelActive(false);
    }

    private void BuildRows()
    {
        foreach (AugmentPackRow row in spawnedRows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }
        spawnedRows.Clear();

        if (rowPrefab == null || rowContainer == null)
            return;

        foreach (AugmentPackData pack in AugmentPackManager.AllPacks)
        {
            AugmentPackRow row = Instantiate(rowPrefab, rowContainer);
            row.Bind(pack, OnPackDetailRequested);
            spawnedRows.Add(row);
        }
    }

    private void OnPackDetailRequested(AugmentPackData pack)
    {
        detailUI?.Show(pack);
    }

    /// <summary>상시 켜져 있는 상세 패널이 열리자마자 빈 상태로 보이지 않도록 첫 번째 팩을 기본 선택한다.</summary>
    private void SelectFirstPackByDefault()
    {
        IReadOnlyList<AugmentPackData> packs = AugmentPackManager.AllPacks;
        if (packs.Count > 0)
            OnPackDetailRequested(packs[0]);
    }

    private void SetPanelActive(bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}
