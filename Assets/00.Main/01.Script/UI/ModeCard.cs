using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 모드 선택 창(ModeSelectPanel)의 모드 카드 하나.
/// 위쪽엔 모드를 보여주는 그림(캐릭터 구성), 아래엔 모드 이름/설명/태그를 두고, 모드별 테마 색으로
/// 테두리·빛·태그를 물들인다. 창이 열릴 때 아래에서 차례로 떠오르며 나타나고(PlayReveal),
/// 마우스를 올리면 떠오르며 빛나고 그림이 살짝 커진다. 준비 중인 모드는 어둡게 잠겨서 누를 수 없다.
/// </summary>
public class ModeCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform visual;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text tagText;
    [Tooltip("모드 그림이 들어갈 자리(카드 마스크 안쪽).")]
    [SerializeField] private RectTransform artSlot;
    [SerializeField] private Image artBackground;

    [Header("테마 색이 들어가는 곳")]
    [SerializeField] private Image frame;
    [SerializeField] private Image glow;
    [SerializeField] private Image tagChip;
    [SerializeField] private Image accentLine;

    [Header("잠김(준비 중)")]
    [SerializeField] private GameObject lockedOverlay;

    [Header("움직임")]
    [SerializeField] private float hoverLift = 18f;
    [SerializeField] private float hoverScale = 1.04f;
    [SerializeField] private float artHoverScale = 1.08f;
    [Min(1f)] [SerializeField] private float followSpeed = 12f;
    [Min(0.01f)] [SerializeField] private float revealDuration = 0.5f;

    [Header("사운드")]
    [SerializeField] private AudioClip hoverSound;
    [Range(0f, 1f)] [SerializeField] private float hoverVolume = 0.35f;

    public event Action<ModeCard> Clicked;
    public bool Available { get; private set; } = true;

    private Color theme = Color.white;
    private bool hovered;
    private float hoverT;
    private float revealStart = -1f;
    private float revealDelay;
    private float confirmTime = -10f;
    private bool dimmed;
    private float dimT;
    private CanvasGroup group;

    private void Awake()
    {
        group = visual != null ? visual.GetComponent<CanvasGroup>() : null;
        if (button != null)
            button.onClick.AddListener(() => { if (Available) Clicked?.Invoke(this); });
    }

    public void Bind(string title, string description, string tag, Color themeColor, Sprite background, Color backgroundTint, GameObject artTemplate, bool available)
    {
        theme = themeColor;
        Available = available;

        if (titleText != null)
            titleText.text = title;
        if (descriptionText != null)
            descriptionText.text = description;
        if (tagText != null)
            tagText.text = tag;

        if (artBackground != null)
        {
            if (background != null)
                artBackground.sprite = background;
            artBackground.color = backgroundTint;
        }

        if (artSlot != null && artTemplate != null)
        {
            GameObject art = Instantiate(artTemplate, artSlot);
            art.SetActive(true);
            RectTransform rt = (RectTransform)art.transform;
            rt.anchoredPosition = Vector2.zero;
        }

        Tint(frame, available ? 1f : 0.45f);
        Tint(tagChip, available ? 1f : 0.6f);
        Tint(accentLine, available ? 0.9f : 0.4f);
        Tint(glow, 0f);

        if (lockedOverlay != null)
            lockedOverlay.SetActive(!available);
        if (button != null)
            button.interactable = available;
    }

    /// <summary>delay 뒤에 아래에서 떠오르며 나타난다.</summary>
    public void PlayReveal(float delay)
    {
        revealStart = Time.unscaledTime;
        revealDelay = delay;
        Apply(0f);
    }

    /// <summary>골랐을 때 카드가 꾹 눌렸다가 크게 튀며 번쩍인다.</summary>
    public void PlayConfirm()
    {
        confirmTime = Time.unscaledTime;
    }

    /// <summary>다른 카드가 골라졌을 때 뒤로 물러나 흐려진다.</summary>
    public void SetDimmed(bool value)
    {
        dimmed = value;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        if (Available)
            SoundManager.Instance?.PlaySfx(hoverSound, hoverVolume, 0.05f);
    }

    public void OnPointerExit(PointerEventData eventData) => hovered = false;

    private void OnDisable()
    {
        hovered = false;
        hoverT = 0f;
        revealStart = -1f;
        Apply(1f);
    }

    private void Update()
    {
        float k = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
        hoverT = Mathf.Lerp(hoverT, hovered && Available && !dimmed ? 1f : 0f, k);
        dimT = Mathf.Lerp(dimT, dimmed ? 1f : 0f, k);

        float reveal = 1f;
        if (revealStart >= 0f)
        {
            reveal = Mathf.Clamp01((Time.unscaledTime - revealStart - revealDelay) / revealDuration);
            if (reveal >= 1f)
                revealStart = -1f;
        }

        Apply(reveal);
    }

    private void Apply(float reveal)
    {
        if (visual == null)
            return;

        float ease = EaseOutBack(reveal);
        // 눌림(0.07초) → 크게 튀었다가(1.12배) 제자리. 번쩍임은 그보다 조금 길게 남는다.
        float since = Time.unscaledTime - confirmTime;
        float confirm = Mathf.Clamp01(1f - since / 0.45f);
        float punch;
        if (since < 0f || since > 0.4f)
            punch = 0f;
        else if (since < 0.07f)
            punch = -0.06f * (since / 0.07f);
        else
        {
            float t = (since - 0.07f) / 0.33f;
            punch = Mathf.Lerp(-0.06f, 0f, t) + Mathf.Sin(t * Mathf.PI) * 0.14f * (1f - t * 0.4f);
        }

        float s = Mathf.Lerp(1f, hoverScale, hoverT) * Mathf.Lerp(0.9f, 1f, ease) * Mathf.Lerp(1f, 0.94f, dimT) + punch;
        visual.localScale = new Vector3(s, s, 1f);
        visual.anchoredPosition = new Vector2(0f, hoverLift * hoverT + (1f - ease) * -90f - 14f * dimT);
        if (group != null)
            group.alpha = Mathf.Clamp01(reveal * 2f) * Mathf.Lerp(1f, 0.35f, dimT);

        if (artSlot != null)
        {
            float a = Mathf.Lerp(1f, artHoverScale, hoverT);
            artSlot.localScale = new Vector3(a, a, 1f);
        }

        if (glow != null)
        {
            Color c = theme;
            c.a = Mathf.Max(0.85f * hoverT, confirm);
            glow.color = c;
        }
    }

    private void Tint(Image image, float alpha)
    {
        if (image == null)
            return;
        Color c = theme;
        c.a = alpha;
        image.color = c;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.4f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}
