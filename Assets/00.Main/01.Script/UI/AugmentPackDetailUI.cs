using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 카드팩 보관함(AugmentPackManageUI) 오른쪽의 팩 상세 패널.
/// 위쪽에는 큰 팩 그림(둥실 떠 있고 뒤에서 팩 테마 색 빛줄기가 돈다) + 팩 이름/설명/해금·개수 칩,
/// 아래쪽에는 팩에 담긴 증강 카드(AugmentCardItem)들이 격자로 깔린다.
/// 팩을 바꿀 때마다 팩 그림이 튕기듯 다시 나타나고 증강 카드가 한 장씩 뒤집히며 펼쳐진다.
///
/// [씬 설정]
///   - augmentListContainer: 증강 카드가 생성될 부모(GridLayoutGroup).
///   - augmentCardPrefab: 증강 하나를 보여주는 카드 프리팹(AugmentCardItem).
/// </summary>
public class AugmentPackDetailUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;
    [Tooltip("팩에 아이콘이 없을 때 대신 쓸 기본 카드팩 그림.")]
    [SerializeField] private Sprite fallbackPackArt;
    [SerializeField] private Transform augmentListContainer;
    [SerializeField] private AugmentCardItem augmentCardPrefab;

    [Tooltip("증강 카드나 팩 그림을 눌렀을 때 가운데로 크게 띄워 보여주는 연출.")]
    [SerializeField] private UICardInspector inspector;

    [Header("꾸밈")]
    [Tooltip("팩 그림 뒤에서 천천히 도는 빛줄기. 팩 테마 색으로 물든다.")]
    [SerializeField] private Image packRays;
    [SerializeField] private Image packHalo;
    [SerializeField] private TMP_Text unlockChipText;
    [SerializeField] private Image unlockChip;
    [SerializeField] private TMP_Text countChipText;
    [SerializeField] private TMP_Text sectionCountText;
    [SerializeField] private Image sectionAccent;
    [SerializeField] private Color unlockedColor = new Color(0.3f, 0.85f, 0.5f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.45f, 0.5f, 0.6f, 1f);

    [Header("연출")]
    [Tooltip("증강 카드가 한 장씩 뒤집히는 간격(초).")]
    [Min(0f)] [SerializeField] private float cardStagger = 0.04f;
    [Tooltip("팩을 처음 보여줄 때 카드 연출을 이만큼(초) 늦춘다(보관함 창이 열리는 동안 기다리기).")]
    [Min(0f)] [SerializeField] private float firstShowDelay = 0.15f;

    private readonly List<AugmentCardItem> spawnedCards = new List<AugmentCardItem>();
    private AugmentPackData current;
    private float popTime = -10f;
    private Vector2 iconBasePos;
    private bool iconBaseCached;

    private void Awake()
    {
        // 큰 팩 그림도 눌러서 크게 볼 수 있다.
        if (iconImage != null && inspector != null)
        {
            Button artButton = iconImage.GetComponent<Button>();
            if (artButton == null)
                artButton = iconImage.gameObject.AddComponent<Button>();
            artButton.transition = Selectable.Transition.None;
            iconImage.raycastTarget = true;
            artButton.onClick.AddListener(() => inspector.Inspect(iconImage.rectTransform));
        }
    }

    private void OnDisable()
    {
        current = null;
    }

    /// <summary>크게 보기 중이면 닫고 true를 돌려준다(Esc가 보관함 창보다 크게 보기를 먼저 닫게).</summary>
    public bool TryCloseInspector()
    {
        if (inspector == null || !inspector.IsOpen)
            return false;

        inspector.Close();
        return true;
    }

    public void Show(AugmentPackData pack)
    {
        if (pack == null)
            return;

        bool firstShow = current == null;
        current = pack;
        Color theme = pack.themeColor;

        if (titleText != null)
            titleText.text = pack.displayName;
        if (descriptionText != null)
            descriptionText.text = pack.description;
        if (iconImage != null)
            iconImage.sprite = pack.icon != null ? pack.icon : fallbackPackArt;

        bool isUnlocked = AugmentPackManager.IsUnlocked(pack);
        if (unlockChipText != null)
            unlockChipText.text = isUnlocked ? "해금됨" : "잠김";
        if (unlockChip != null)
            unlockChip.color = isUnlocked ? unlockedColor : lockedColor;

        int count = pack.augments != null ? pack.augments.Length : 0;
        if (countChipText != null)
            countChipText.text = "증강 " + count + "종";
        if (sectionCountText != null)
            sectionCountText.text = count.ToString();

        Tint(packRays, theme, packRays != null ? packRays.color.a : 1f);
        Tint(packHalo, theme, packHalo != null ? packHalo.color.a : 1f);
        Tint(sectionAccent, theme, 1f);

        popTime = Time.unscaledTime;
        BuildAugmentCards(pack, firstShow ? firstShowDelay : 0f);
    }

    private void Update()
    {
        if (iconImage == null)
            return;

        RectTransform icon = iconImage.rectTransform;
        if (!iconBaseCached)
        {
            iconBasePos = icon.anchoredPosition;
            iconBaseCached = true;
        }

        float now = Time.unscaledTime;
        // 팩이 바뀐 직후: 작게 기울어진 채로 튕기듯 커짐 → 이후 둥실둥실
        float pop = Mathf.Clamp01((now - popTime) / 0.45f);
        const float c1 = 1.8f;
        float x = pop - 1f;
        float popEase = 1f + (c1 + 1f) * x * x * x + c1 * x * x;
        float scale = Mathf.LerpUnclamped(0.6f, 1f, popEase);

        icon.localScale = new Vector3(scale, scale, 1f);
        icon.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-14f, 0f, popEase) + Mathf.Sin(now * 1.3f) * 2f);
        icon.anchoredPosition = iconBasePos + new Vector2(0f, Mathf.Sin(now * 1.8f) * 6f);

        if (packRays != null)
            packRays.rectTransform.localRotation = Quaternion.Euler(0f, 0f, now * 18f);
        if (packHalo != null)
        {
            float pulse = 1f + Mathf.Sin(now * 2.2f) * 0.05f;
            packHalo.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
        }
    }

    private void BuildAugmentCards(AugmentPackData pack, float delay)
    {
        foreach (AugmentCardItem card in spawnedCards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }
        spawnedCards.Clear();

        if (augmentCardPrefab == null || augmentListContainer == null || pack.augments == null)
            return;

        int index = 0;
        foreach (AugmentData augment in pack.augments)
        {
            if (augment == null)
                continue;

            AugmentCardItem card = Instantiate(augmentCardPrefab, augmentListContainer);
            card.Bind(augment, pack.themeColor);
            if (inspector != null)
                card.Clicked += InspectCard;
            // 카드마다 다 소리를 내면 너무 시끄러워서 두 장에 한 번만 "촥" 소리를 낸다.
            card.PlayReveal(delay + index * cardStagger, index % 2 == 0);
            spawnedCards.Add(card);
            index++;
        }
    }

    /// <summary>누른 카드를 크게 띄우되, 팩의 다른 증강들도 좌우로 넘겨볼 수 있게 묶어서 연다.</summary>
    private void InspectCard(AugmentCardItem clicked)
    {
        var visuals = new List<RectTransform>(spawnedCards.Count);
        var hides = new List<CanvasGroup>(spawnedCards.Count);
        foreach (AugmentCardItem card in spawnedCards)
        {
            visuals.Add(card.Visual);
            hides.Add(card.HideGroup);
        }
        inspector.InspectGallery(visuals, hides, spawnedCards.IndexOf(clicked));
    }

    private static void Tint(Image image, Color color, float alpha)
    {
        if (image == null)
            return;
        color.a = alpha;
        image.color = color;
    }
}
