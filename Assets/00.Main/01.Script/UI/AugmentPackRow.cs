using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 로비 카드팩 보관함(AugmentPackManageUI) 왼쪽 목록의 팩 한 줄.
/// 작은 팩 그림 + 팩 이름 + 증강 개수 + 해금 상태 칩("해금됨"/"잠김")을 보여준다.
/// 해금 여부는 읽기만 하며(이 UI에서 바꾸는 액션 없음), 잠긴 팩도 눌러서 어떤 증강이 들었는지 미리 볼 수 있다.
/// 선택된 줄은 팩 테마 색으로 테두리/배경이 물들고, 마우스를 올리면 살짝 밝아지며 옆으로 밀린다.
/// 보관함이 열릴 때 PlayReveal로 위에서부터 차례로 밀려 들어온다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class AugmentPackRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text unlockStatusText;
    [SerializeField] private Button detailButton;

    [Header("꾸밈")]
    [SerializeField] private RectTransform visual;
    [SerializeField] private Image packArt;
    [Tooltip("팩에 아이콘이 없을 때 대신 쓸 기본 카드팩 그림.")]
    [SerializeField] private Sprite fallbackPackArt;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Image statusChip;
    [SerializeField] private Image background;
    [SerializeField] private Image frame;
    [SerializeField] private Color unlockedColor = new Color(0.3f, 0.85f, 0.5f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.45f, 0.5f, 0.6f, 1f);

    [Header("사운드")]
    [SerializeField] private AudioClip hoverSound;
    [Range(0f, 1f)] [SerializeField] private float hoverVolume = 0.3f;

    private AugmentPackData pack;
    private CanvasGroup canvasGroup;
    private bool selected;
    private bool hovered;
    private float hoverT;
    private float selectT;
    private float revealStart = -1f;
    private float revealDelay;
    private Color theme = Color.white;

    public AugmentPackData Pack => pack;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Bind(AugmentPackData pack, Action<AugmentPackData> onDetailRequested)
    {
        this.pack = pack;
        theme = pack.themeColor;

        if (nameText != null)
            nameText.text = pack.displayName;

        if (countText != null)
            countText.text = "증강 " + (pack.augments != null ? pack.augments.Length : 0) + "종";

        if (packArt != null)
            packArt.sprite = pack.icon != null ? pack.icon : fallbackPackArt;

        bool isUnlocked = AugmentPackManager.IsUnlocked(pack);
        if (unlockStatusText != null)
            unlockStatusText.text = isUnlocked ? "해금됨" : "잠김";
        if (statusChip != null)
            statusChip.color = isUnlocked ? unlockedColor : lockedColor;

        if (detailButton != null)
        {
            detailButton.onClick.RemoveAllListeners();
            detailButton.onClick.AddListener(() => onDetailRequested?.Invoke(this.pack));
        }
    }

    public void SetSelected(bool value)
    {
        selected = value;
    }

    /// <summary>delay 뒤에 왼쪽에서 밀려 들어오며 나타난다.</summary>
    public void PlayReveal(float delay)
    {
        revealStart = Time.unscaledTime;
        revealDelay = delay;
        ApplyVisual(0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        if (!selected)
            SoundManager.Instance?.PlaySfx(hoverSound, hoverVolume, 0.05f);
    }

    public void OnPointerExit(PointerEventData eventData) => hovered = false;

    private void Update()
    {
        float k = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
        hoverT = Mathf.Lerp(hoverT, hovered ? 1f : 0f, k);
        selectT = Mathf.Lerp(selectT, selected ? 1f : 0f, k);

        float reveal = 1f;
        if (revealStart >= 0f)
        {
            reveal = Mathf.Clamp01((Time.unscaledTime - revealStart - revealDelay) / 0.3f);
            if (reveal >= 1f)
                revealStart = -1f;
        }

        ApplyVisual(reveal);
    }

    private void ApplyVisual(float reveal)
    {
        float ease = 1f - Mathf.Pow(1f - reveal, 3f);
        if (canvasGroup != null)
            canvasGroup.alpha = ease;
        if (visual != null)
            visual.anchoredPosition = new Vector2((1f - ease) * -40f + hoverT * 6f + selectT * 4f, 0f);

        if (background != null)
        {
            Color baseColor = new Color(0.08f, 0.12f, 0.25f, 0.9f);
            Color c = Color.Lerp(baseColor, new Color(theme.r * 0.45f, theme.g * 0.45f, theme.b * 0.6f, 0.95f), Mathf.Max(selectT, hoverT * 0.4f));
            background.color = c;
        }

        if (frame != null)
        {
            Color c = theme;
            c.a = Mathf.Max(selectT, hoverT * 0.45f, 0.15f);
            frame.color = c;
        }
    }
}
