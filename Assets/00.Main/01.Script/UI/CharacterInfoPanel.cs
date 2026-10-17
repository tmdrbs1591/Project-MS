using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 선택 패널(CharacterSelectUI) 안의 설명 영역. Show(definition) 한 번으로 이름/역할/소개/
/// 초상화/스킬 줄을 전부 채운다. 내용은 전부 CharacterDefinition의 "Character Info" 항목에서 온다.
///
/// [씬 설정]
///   - CharacterSelectUI 패널 안에 설명 영역 오브젝트를 만들고 이 스크립트를 붙인다.
///   - 텍스트/이미지를 연결하고, 스킬 줄마다 CharacterSkillInfoRow를 붙여 skillRows에 넣는다
///     (Q/E/대시/궁은 actionType으로, 패시브는 isPassive를 켜서 구분).
///   - CharacterSelectUI의 infoPanel 필드에 이 컴포넌트를 연결한다.
/// </summary>
public class CharacterInfoPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private CharacterSkillInfoRow[] skillRows;

    public void Show(CharacterDefinition definition)
    {
        SetText(nameText, definition != null ? definition.DisplayName : null);
        SetText(roleText, definition != null ? definition.Role : null);
        SetText(descriptionText, definition != null ? definition.Description : null);

        if (portraitImage != null)
        {
            Sprite portrait = definition != null ? definition.Portrait : null;
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }

        if (skillRows == null)
            return;

        foreach (CharacterSkillInfoRow row in skillRows)
        {
            if (row != null)
                row.Show(definition);
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? string.Empty;
    }
}
