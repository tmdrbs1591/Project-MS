using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 매치 시작 직후(MatchPhase.PackSelect) 뜨는 팩 선택 UI. 해금된 팩 중 최대 5개를 골라
/// 확인 버튼을 누르면 그 선택으로 로컬 캐릭터의 팩 선택이 확정된다(CharacterBase.FinalizePackSelection).
/// 제한시간(기본 45초) 안에 확정하지 않으면 MatchManager 가 무작위로 자동 확정한다.
///
/// [씬 설정]
///   - 게임 씬의 Canvas 하위에 패널을 만들고 이 스크립트를 붙인다.
///   - panel: 평소엔 꺼져 있다가 PackSelect 구간에만 켜지는 패널.
///   - timerText: 남은 시간(초) 표시.
///   - rowContainer / rowPrefab: 해금된 팩마다 하나씩 생성되는 선택 행.
///   - confirmButton: 선택을 확정하는 버튼.
///   - 씬에는 하나만 존재해야 한다(AugmentSelectUI와 마찬가지로 static 불필요).
/// </summary>
public class PackSelectUI : MonoBehaviour
{
    private const int MaxChoices = 5;

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private PackSelectRow rowPrefab;
    [SerializeField] private Button confirmButton;

    private readonly List<AugmentPackData> chosenPacks = new List<AugmentPackData>();
    private readonly List<PackSelectRow> spawnedRows = new List<PackSelectRow>();
    private bool built;
    private bool finalized;

    private void Awake()
    {
        SetPanelActive(false);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);
    }

    private void Update()
    {
        MatchManager match = MatchManager.Instance;
        bool inSelectPhase = match != null && match.Phase == MatchPhase.PackSelect;

        if (!inSelectPhase)
        {
            ResetForNextMatch();
            SetPanelActive(false);
            return;
        }

        if (!built)
            BuildRows();

        SetPanelActive(!finalized);

        if (timerText != null)
            timerText.text = Mathf.CeilToInt(Mathf.Max(0f, match.GetPhaseTimeRemaining())) + "초";
    }

    private void BuildRows()
    {
        built = true;
        chosenPacks.Clear();

        foreach (PackSelectRow row in spawnedRows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }
        spawnedRows.Clear();

        if (rowPrefab == null || rowContainer == null)
            return;

        foreach (AugmentPackData pack in AugmentPackManager.AllPacks)
        {
            if (!AugmentPackManager.IsUnlocked(pack))
                continue;

            PackSelectRow row = Instantiate(rowPrefab, rowContainer);
            row.Bind(pack, OnRowToggled);
            spawnedRows.Add(row);
        }
    }

    private void OnRowToggled(AugmentPackData pack, bool isOn)
    {
        if (isOn)
        {
            if (chosenPacks.Count >= MaxChoices)
            {
                RevertRow(pack);
                return;
            }

            if (!chosenPacks.Contains(pack))
                chosenPacks.Add(pack);
        }
        else
        {
            chosenPacks.Remove(pack);
        }

        UpdateRowInteractability();
    }

    private void RevertRow(AugmentPackData pack)
    {
        foreach (PackSelectRow row in spawnedRows)
        {
            if (row.Pack == pack)
            {
                row.SetOnWithoutNotify(false);
                break;
            }
        }
    }

    private void UpdateRowInteractability()
    {
        bool atMax = chosenPacks.Count >= MaxChoices;
        foreach (PackSelectRow row in spawnedRows)
        {
            bool isChosen = chosenPacks.Contains(row.Pack);
            row.SetInteractable(isChosen || !atMax);
        }
    }

    private void OnConfirm()
    {
        if (finalized)
            return;

        CharacterBase local = CharacterBase.LocalPlayer;
        if (local == null)
            return;

        local.FinalizePackSelection(chosenPacks);
        finalized = true;
    }

    private void ResetForNextMatch()
    {
        finalized = false;
        built = false;
    }

    private void SetPanelActive(bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}
