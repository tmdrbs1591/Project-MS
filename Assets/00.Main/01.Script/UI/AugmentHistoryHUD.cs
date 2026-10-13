using System.Collections.Generic;
using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 체력바 아래에 그 플레이어가 이번 매치에서 고른 증강을 고른 순서대로 아이콘으로 쌓아 보여준다.
/// PlayerCornerHUD처럼 스폰 기준 왼쪽(P1)/오른쪽(P2) 중 한쪽을 맡고, 네트워크 값을 매 프레임 폴링한다.
/// 새로 추가된 아이콘은 통통 튀며 나타난다.
///
/// [씬 설정]
///   - 각 체력바 아래에 가로 레이아웃(HorizontalLayoutGroup) 컨테이너를 만들고 이 스크립트를 붙인다.
///     오른쪽 HUD는 childAlignment를 오른쪽으로 두면 체력바 바깥쪽부터 쌓인다.
///   - iconTemplate: 꺼둔 아이콘 하나(Image). 자식에 TMP_Text가 있으면 아이콘이 없는 증강의
///     이름 첫 글자를 대신 보여준다.
/// </summary>
public class AugmentHistoryHUD : MonoBehaviour
{
    [SerializeField] private bool isLeftSide = true;
    [SerializeField] private RectTransform container;
    [SerializeField] private Image iconTemplate;
    [Min(0f)] [SerializeField] private float popDuration = 0.35f;
    [Min(1f)] [SerializeField] private float popScale = 1.6f;

    private readonly List<Image> icons = new List<Image>();
    private readonly List<float> popTimers = new List<float>();
    private CharacterBase character;

    private void Awake()
    {
        if (container == null)
            container = (RectTransform)transform;
        if (iconTemplate != null)
            iconTemplate.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        character = FindCharacter();
        int count = character != null ? character.AugmentHistoryCount : 0;

        // 재대결 등으로 기록이 줄면 처음부터 다시 그린다.
        if (count < icons.Count)
            Clear();

        for (int i = icons.Count; i < count; i++)
            AddIcon(character.GetAugmentHistoryData(i));

        for (int i = 0; i < icons.Count; i++)
        {
            if (popTimers[i] <= 0f)
                continue;
            popTimers[i] -= Time.unscaledDeltaTime;
            float t = popDuration > 0f ? Mathf.Clamp01(popTimers[i] / popDuration) : 0f;
            icons[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, popScale, t * t);
        }
    }

    private CharacterBase FindCharacter()
    {
        MatchManager match = MatchManager.Instance;
        if (match == null)
            return null;

        foreach (CharacterBase candidate in CharacterBase.All)
        {
            if (candidate != null && candidate.Object != null && match.IsPlayer1(candidate.MatchPlayer) == isLeftSide)
                return candidate;
        }
        return null;
    }

    private void AddIcon(AugmentData data)
    {
        if (iconTemplate == null || container == null)
            return;

        Image icon = Instantiate(iconTemplate, container);
        icon.gameObject.SetActive(true);
        icon.sprite = data != null ? data.icon : null;
        icon.enabled = true;

        TMP_Text fallback = icon.GetComponentInChildren<TMP_Text>(true);
        if (fallback != null)
        {
            bool needsText = icon.sprite == null && data != null && !string.IsNullOrEmpty(data.title);
            fallback.gameObject.SetActive(needsText);
            if (needsText)
                fallback.text = data.title.Substring(0, 1);
        }

        icons.Add(icon);
        popTimers.Add(popDuration);
    }

    private void Clear()
    {
        foreach (Image icon in icons)
        {
            if (icon != null)
                Destroy(icon.gameObject);
        }
        icons.Clear();
        popTimers.Clear();
    }
}
