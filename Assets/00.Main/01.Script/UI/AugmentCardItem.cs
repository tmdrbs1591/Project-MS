using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 로비 카드팩 보관함(AugmentPackDetailUI)에서 팩에 담긴 증강 하나를 보여주는 카드.
/// 아이콘/이름/설명과 중첩 가능 횟수를 표시하고, 팩 테마 색으로 포인트(아이콘 뒤 빛, 구분선, 호버 테두리)를 준다.
/// 팩을 열거나 바꿀 때 PlayReveal로 카드가 한 장씩 뒤집히듯(가로로 펼쳐지며) 나타나고,
/// 마우스를 올리면 살짝 떠오르며 테마 색으로 빛난다.
///
/// [씬 설정]
///   - 카드 루트(GridLayoutGroup이 위치를 잡는 오브젝트)에 붙인다. 연출은 visual(자식)에만 걸어서
///     레이아웃과 충돌하지 않는다. 루트에는 마우스 판정용 Image(투명이어도 됨)가 있어야 한다.
/// </summary>
public class AugmentCardItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    /// <summary>카드를 눌렀을 때(크게 보기 등).</summary>
    public event System.Action<AugmentCardItem> Clicked;

    public RectTransform Visual => visual;

    /// <summary>크게 보는 동안 원래 자리의 카드를 숨길 때 쓰는 CanvasGroup(카드 루트).
    /// visual의 알파는 등장 연출이 매 프레임 덮어써서 루트 쪽을 쓴다.</summary>
    public CanvasGroup HideGroup
    {
        get
        {
            if (hideGroup == null)
                hideGroup = GetComponent<CanvasGroup>();
            if (hideGroup == null)
                hideGroup = gameObject.AddComponent<CanvasGroup>();
            return hideGroup;
        }
    }

    private CanvasGroup hideGroup;

    [SerializeField] private RectTransform visual;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [Tooltip("최대 중첩 횟수 표시(\"최대 3중첩\"). 중첩이 안 되는 증강이면 숨긴다.")]
    [SerializeField] private GameObject stackChip;
    [SerializeField] private TMP_Text stackText;

    [Header("테마 색이 들어가는 곳")]
    [SerializeField] private Image iconGlow;
    [SerializeField] private Image divider;
    [SerializeField] private Image stackChipBackground;
    [SerializeField] private Image hoverGlow;

    [Header("움직임")]
    [SerializeField] private float hoverLift = 10f;
    [SerializeField] private float hoverScale = 1.05f;
    [Min(1f)] [SerializeField] private float followSpeed = 14f;
    [Min(0.01f)] [SerializeField] private float revealDuration = 0.35f;

    [Header("사운드")]
    [SerializeField] private AudioClip hoverSound;
    [Range(0f, 1f)] [SerializeField] private float hoverVolume = 0.25f;
    [SerializeField] private AudioClip revealSound;
    [Range(0f, 1f)] [SerializeField] private float revealVolume = 0.2f;

    private bool hovered;
    private float hoverT;
    private float revealStart = -1f;
    private float revealDelay;
    private bool revealSoundPlayed = true;
    private Color theme = Color.white;
    private CanvasGroup visualGroup;

    public void Bind(AugmentData augment, Color themeColor)
    {
        theme = themeColor;
        if (augment == null)
            return;

        if (iconImage != null)
        {
            iconImage.sprite = augment.icon;
            iconImage.enabled = augment.icon != null;
        }
        if (titleText != null)
            titleText.text = augment.title;
        if (descriptionText != null)
            descriptionText.text = augment.description;

        bool stacks = augment.maxStack > 1;
        if (stackChip != null)
            stackChip.SetActive(stacks);
        if (stackText != null)
            stackText.text = stacks ? "최대 " + augment.maxStack + "중첩" : string.Empty;

        Tint(iconGlow, 0.55f);
        Tint(divider, 0.9f);
        Tint(stackChipBackground, 0.9f);
        Tint(hoverGlow, 0f);
    }

    /// <summary>delay 뒤에 카드가 가로로 펼쳐지며(뒤집히듯) 튕겨 나타난다.</summary>
    public void PlayReveal(float delay, bool withSound)
    {
        revealStart = Time.unscaledTime;
        revealDelay = delay;
        revealSoundPlayed = !withSound;
        ApplyReveal(0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        SoundManager.Instance?.PlaySfx(hoverSound, hoverVolume, 0.06f);
    }

    public void OnPointerExit(PointerEventData eventData) => hovered = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        hovered = false;
        Clicked?.Invoke(this);
    }

    private void OnDisable()
    {
        hovered = false;
        hoverT = 0f;
        revealStart = -1f;
        ApplyReveal(1f);
    }

    private void Update()
    {
        float k = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
        hoverT = Mathf.Lerp(hoverT, hovered ? 1f : 0f, k);

        float reveal = 1f;
        if (revealStart >= 0f)
        {
            float t = (Time.unscaledTime - revealStart - revealDelay) / revealDuration;
            if (t >= 0f && !revealSoundPlayed)
            {
                revealSoundPlayed = true;
                SoundManager.Instance?.PlaySfx(revealSound, revealVolume, 0.1f);
            }
            reveal = Mathf.Clamp01(t);
            if (reveal >= 1f)
                revealStart = -1f;
        }

        ApplyReveal(reveal);
    }

    private void ApplyReveal(float reveal)
    {
        if (visual == null)
            return;

        // 가로 폭이 0 → 넘쳤다 → 1로 펼쳐지는 "카드 뒤집기" 느낌. 세로는 살짝만 커진다.
        float flip = EaseOutBack(reveal);
        float s = Mathf.Lerp(1f, hoverScale, hoverT);
        visual.localScale = new Vector3(Mathf.Max(0f, flip) * s, Mathf.Lerp(0.85f, 1f, reveal) * s, 1f);
        visual.anchoredPosition = new Vector2(0f, hoverLift * hoverT + (1f - reveal) * -16f);

        if (hoverGlow != null)
        {
            Color c = theme;
            c.a = 0.85f * hoverT;
            hoverGlow.color = c;
        }

        if (visualGroup == null)
            visualGroup = visual.GetComponent<CanvasGroup>();
        if (visualGroup != null)
            visualGroup.alpha = Mathf.Clamp01(reveal * 2.5f);
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
        const float c1 = 1.6f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}
