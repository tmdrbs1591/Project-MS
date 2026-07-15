using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 라운드 종료 후 증강 선택 UI(AugmentSelectUI)의 선택지 슬롯 하나.
/// 증강 아이콘/제목/설명과 출처 팩 이름을 보여주고, 선택 버튼과 슬롯당 1회 제한인 리롤 버튼을 가진다.
/// </summary>
public class AugmentChoiceSlot : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text packNameText;
    [SerializeField] private Button pickButton;
    [SerializeField] private Button rerollButton;

    public AugmentPoolEntry CurrentEntry { get; private set; }
    public AugmentType CurrentType => CurrentEntry.Data.type;

    private bool hasRerolled;
    private Action<AugmentChoiceSlot> onPick;
    private Action<AugmentChoiceSlot> onReroll;

    private void Awake()
    {
        if (pickButton != null)
            pickButton.onClick.AddListener(() => onPick?.Invoke(this));

        if (rerollButton != null)
            rerollButton.onClick.AddListener(() => onReroll?.Invoke(this));
    }

    public void Bind(AugmentPoolEntry entry, Action<AugmentChoiceSlot> onPick, Action<AugmentChoiceSlot> onReroll)
    {
        CurrentEntry = entry;
        this.onPick = onPick;
        this.onReroll = onReroll;

        AugmentData data = entry.Data;

        if (iconImage != null)
            iconImage.sprite = data != null ? data.icon : null;

        if (titleText != null)
            titleText.text = data != null ? data.title : string.Empty;

        if (descriptionText != null)
            descriptionText.text = data != null ? data.description : string.Empty;

        if (packNameText != null)
            packNameText.text = entry.SourcePack != null ? entry.SourcePack.displayName : string.Empty;
    }

    /// <summary>새 라운드 시작 시 리롤 기회를 다시 채운다.</summary>
    public void ResetRerollState()
    {
        hasRerolled = false;
        SetRerollInteractable(true);
    }

    /// <summary>리롤을 아직 안 썼으면 소비하고 true, 이미 썼으면 false(무시).</summary>
    public bool TryConsumeReroll()
    {
        if (hasRerolled)
            return false;

        hasRerolled = true;
        SetRerollInteractable(false);
        return true;
    }

    public void SetSlotActive(bool active) => gameObject.SetActive(active);

    private void SetRerollInteractable(bool interactable)
    {
        if (rerollButton != null)
            rerollButton.interactable = interactable;
    }
}
