using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 설명 패널(CharacterInfoPanel)의 스킬 한 줄: 아이콘 + 이름 + 설명.
/// isPassive를 켜면 actionType 대신 패시브 정보를 보여준다(패시브는 액션 종류가 아니라서 따로 둠).
/// 아이콘이 없으면 아이콘 Image를 끄고, 이름과 설명이 둘 다 비어 있으면 줄 전체를 숨긴다.
/// </summary>
public class CharacterSkillInfoRow : MonoBehaviour
{
    [SerializeField] private bool isPassive;
    [SerializeField] private CharacterActionType actionType = CharacterActionType.SkillQ;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;

    public void Show(CharacterDefinition definition)
    {
        Sprite icon = null;
        CharacterSkillInfo info = default;

        if (definition != null)
        {
            icon = isPassive ? definition.PassiveIcon : definition.GetIcon(actionType);
            info = isPassive ? definition.PassiveInfo : definition.GetSkillInfo(actionType);
        }

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (nameText != null)
            nameText.text = info.name ?? string.Empty;
        if (descriptionText != null)
            descriptionText.text = info.description ?? string.Empty;

        bool hasText = !string.IsNullOrEmpty(info.name) || !string.IsNullOrEmpty(info.description);
        gameObject.SetActive(definition != null && hasText);
    }
}
