using System;
using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 설명 패널(CharacterInfoPanel) 왼쪽의 스킬 탭 한 줄: 아이콘 + 이름.
/// 누르면 CharacterInfoPanel이 오른쪽 상세 영역에 이 스킬의 종류/키/쿨타임/설명을 띄운다.
/// isPassive를 켜면 actionType 대신 패시브 정보를 보여준다(패시브는 액션 종류가 아니라서 따로 둠).
/// 아이콘이 없으면 아이콘 대신 키 글자(Q/E/R/P)를 보여주고, 이름과 설명이 둘 다 비어 있으면 줄 전체를 숨긴다.
/// </summary>
public class CharacterSkillInfoRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private bool isPassive;
    [SerializeField] private CharacterActionType actionType = CharacterActionType.SkillQ;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [Tooltip("아이콘이 없을 때 대신 보여줄 글자(Q/E/R/P).")]
    [SerializeField] private TMP_Text fallbackLetterText;

    [Header("탭 연출")]
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Image frame;
    [SerializeField] private Image iconFrame;
    [SerializeField] private Color normalBackground = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color hoverBackground = new Color(0.25f, 0.45f, 0.9f, 0.18f);
    [SerializeField] private Color selectedBackground = new Color(0.16f, 0.4f, 0.9f, 0.55f);
    [SerializeField] private Color normalFrame = new Color(0.45f, 0.62f, 1f, 0f);
    [SerializeField] private Color selectedFrame = new Color(0.45f, 0.75f, 1f, 1f);
    [SerializeField] private Color normalName = new Color(0.78f, 0.84f, 0.95f, 1f);
    [SerializeField] private Color selectedName = Color.white;

    [Header("사운드")]
    [SerializeField] private AudioClip clickSound;
    [Range(0f, 1f)] [SerializeField] private float clickVolume = 0.5f;
    [SerializeField] private AudioClip hoverSound;
    [Range(0f, 1f)] [SerializeField] private float hoverVolume = 0.25f;

    public event Action<CharacterSkillInfoRow> Clicked;

    public bool IsPassive => isPassive;
    public CharacterActionType ActionType => actionType;
    public CharacterSkillInfo Info { get; private set; }
    public Sprite Icon { get; private set; }
    public bool HasContent { get; private set; }

    private bool selected;
    private bool hovered;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(() =>
            {
                if (!selected)
                    PlayClickSound();
                Clicked?.Invoke(this);
            });
        RefreshVisual();
    }

    public void Show(CharacterDefinition definition)
    {
        Sprite icon = null;
        CharacterSkillInfo info = default;

        if (definition != null)
        {
            icon = isPassive ? definition.PassiveIcon : definition.GetIcon(actionType);
            info = isPassive ? definition.PassiveInfo : definition.GetSkillInfo(actionType);
        }

        Icon = icon;
        Info = info;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (fallbackLetterText != null)
        {
            fallbackLetterText.text = FallbackLetter;
            fallbackLetterText.gameObject.SetActive(icon == null);
        }

        if (nameText != null)
            nameText.text = info.name ?? string.Empty;

        bool hasText = !string.IsNullOrEmpty(info.name) || !string.IsNullOrEmpty(info.description);
        HasContent = definition != null && hasText;
        gameObject.SetActive(HasContent);
    }

    public void PlayClickSound()
    {
        SoundManager.Instance?.PlaySfx(clickSound, clickVolume, 0.04f);
    }

    public void SetSelected(bool value)
    {
        selected = value;
        RefreshVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        if (!selected)
            SoundManager.Instance?.PlaySfx(hoverSound, hoverVolume, 0.05f);
        RefreshVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        RefreshVisual();
    }

    private string FallbackLetter
    {
        get
        {
            if (isPassive)
                return "P";

            switch (actionType)
            {
                case CharacterActionType.SkillQ: return KeyBindings.GetKeyLabel(BindableAction.SkillQ);
                case CharacterActionType.SkillE: return KeyBindings.GetKeyLabel(BindableAction.SkillE);
                case CharacterActionType.Ultimate: return KeyBindings.GetKeyLabel(BindableAction.Ultimate);
                case CharacterActionType.Dash: return KeyBindings.GetKeyLabel(BindableAction.Dash);
                default: return string.Empty;
            }
        }
    }

    private void RefreshVisual()
    {
        if (background != null)
            background.color = selected ? selectedBackground : hovered ? hoverBackground : normalBackground;
        if (frame != null)
            frame.color = selected ? selectedFrame : normalFrame;
        if (iconFrame != null)
            iconFrame.color = selected ? selectedFrame : new Color(selectedFrame.r, selectedFrame.g, selectedFrame.b, 0.35f);
        if (nameText != null)
            nameText.color = selected ? selectedName : normalName;
    }
}
