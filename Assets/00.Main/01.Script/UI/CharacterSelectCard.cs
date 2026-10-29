using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 선택 패널(CharacterSelectUI)의 카드 한 장의 연출 담당.
/// 선택되면 금색 테두리 + 뒤쪽 글로우 + 살짝 떠오르며 커지고, 카드 위로 빛줄기가 한 번 훑고 지나간다.
/// 선택 안 된 카드는 배경을 어둡게 눌러서 선택 카드가 확 튀게 한다. 마우스를 올리면 살짝 커지고,
/// 마우스 위치에 따라 배경과 캐릭터가 서로 반대로 움직이는 시차 + 반사광으로 입체감이 난다.
/// 패널이 열릴 때는 CharacterSelectUI가 PlayDealIn을 불러 카드가 아래에서 부채꼴로 촤라락 펼쳐진다.
/// 클릭해서 고르면 PlayConfirm으로 번쩍 + 외곽선 퍼짐 + 불꽃 연출과 캐릭터 고유 소리가 난다.
///
/// [씬 설정]
///   - 캐릭터 버튼(Button, CharacterChangeUi가 붙은 오브젝트)에 붙인다. 버튼 자체 위치는 건드리지 않고
///     visual(자식) 하나만 움직이므로 레이아웃/클릭 영역은 그대로다.
///   - visual: 카드 그래픽 전체를 묶은 자식. 스케일/회전/띄우기는 전부 여기에 들어간다.
///   - artBackground: 카드 배경 그림(CharacterSelectUI가 선택 시 스프라이트를 갈아끼우는 이미지).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CharacterSelectCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform visual;
    [SerializeField] private Image artBackground;
    [SerializeField] private Image frame;
    [SerializeField] private Image glow;
    [SerializeField] private Image gem;
    [Tooltip("선택될 때 카드 위를 한 번 대각선으로 훑고 지나가는 빛줄기(카드 마스크 안쪽에 둔다).")]
    [SerializeField] private RectTransform shine;

    [Header("색")]
    [SerializeField] private Color normalFrameColor = new Color(0.33f, 0.5f, 0.86f, 1f);
    [SerializeField] private Color selectedFrameColor = new Color(1f, 0.83f, 0.36f, 1f);
    [Tooltip("선택 안 된 카드 배경에 곱할 색(어둡게 눌러서 선택 카드를 돋보이게 한다).")]
    [SerializeField] private Color normalArtTint = new Color(0.42f, 0.47f, 0.78f, 1f);
    [SerializeField] private Color hoverArtTint = new Color(0.62f, 0.67f, 0.95f, 1f);

    [Header("움직임")]
    [SerializeField] private float selectedScale = 1.06f;
    [SerializeField] private float hoverScale = 1.03f;
    [SerializeField] private float selectedLift = 14f;
    [Tooltip("선택/호버 상태로 따라가는 속도(클수록 빠름).")]
    [Min(1f)] [SerializeField] private float followSpeed = 14f;
    [Min(0.01f)] [SerializeField] private float shineDuration = 0.45f;

    [Header("사운드")]
    [SerializeField] private AudioClip hoverSound;
    [Range(0f, 1f)] [SerializeField] private float hoverVolume = 0.35f;
    [Tooltip("등장 연출에서 이 카드가 날아오기 시작할 때 나는 소리.")]
    [SerializeField] private AudioClip dealSound;
    [Range(0f, 1f)] [SerializeField] private float dealVolume = 0.5f;
    [Tooltip("이 캐릭터를 클릭해서 골랐을 때 나는 캐릭터 고유 소리(거너=철컥 탕, 스파크=찌리릿 등).")]
    [SerializeField] private AudioClip pickSound;
    [Range(0f, 1f)] [SerializeField] private float pickVolume = 0.7f;

    [Header("입체감(시차) / 반사광")]
    [Tooltip("마우스 쪽으로 카드가 살짝 기우는 최대 각도.")]
    [SerializeField] private float maxTilt = 2.5f;
    [Tooltip("카드 배경 그림. 마우스 방향으로 움직인다(뒤쪽 레이어).")]
    [SerializeField] private RectTransform parallaxBack;
    [Tooltip("캐릭터 그림. 배경과 반대 방향으로 움직여서 앞으로 튀어나와 보이게 한다(앞쪽 레이어).")]
    [SerializeField] private RectTransform parallaxFront;
    [SerializeField] private float parallaxBackDistance = 10f;
    [SerializeField] private float parallaxFrontDistance = 9f;
    [Tooltip("카드 표면에서 마우스를 따라다니는 반사광(카드 마스크 안쪽에 둔다).")]
    [SerializeField] private Image glare;
    [Range(0f, 1f)] [SerializeField] private float glareAlpha = 0.1f;

    [Header("선택 확정 연출")]
    [SerializeField] private Image confirmFlash;
    [Range(0f, 1f)] [SerializeField] private float confirmFlashAlpha = 0.45f;
    [Tooltip("확정 순간 카드 테두리를 따라 바깥으로 살짝 퍼지며 사라지는 카드 모양 외곽선.")]
    [UnityEngine.Serialization.FormerlySerializedAs("confirmRing")]
    [SerializeField] private Image confirmOutline;
    [Tooltip("확정 순간 카드 중심에서 사방으로 튀어나가는 불꽃 줄기들.")]
    [SerializeField] private Image[] confirmSparks;
    [Min(0.1f)] [SerializeField] private float confirmDuration = 0.6f;

    private CanvasGroup canvasGroup;
    private Coroutine confirmRoutine;
    private bool selected;
    private bool hovered;

    /// <summary>선택됐거나 마우스가 올라가 있는 동안 true. 캐릭터별 연출(CharacterCardAnimator)이 이걸 보고 움직인다.</summary>
    public bool IsHighlighted => selected || hovered;

    // 상태(선택/호버)로 인한 값 — Update에서 목표값을 부드럽게 따라간다.
    private float stateScale = 1f;
    private float stateLift;
    private float glowAlpha;
    private Color artTint;

    // 등장 연출로 인한 값 — 상태 값 위에 곱/더해진다.
    private Vector2 dealOffset;
    private float dealRotation;
    private float dealScale = 1f;

    private Coroutine dealRoutine;
    private Coroutine shineRoutine;

    // 기울기(마우스 위치, -1~1)와 반사광 세기 — Update에서 부드럽게 따라간다.
    private Vector2 tiltPoint;
    private float currentGlareAlpha;
    private Vector2 parallaxBackBase;
    private Vector2 parallaxFrontBase;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        artTint = normalArtTint;
        if (parallaxBack != null)
            parallaxBackBase = parallaxBack.anchoredPosition;
        if (parallaxFront != null)
            parallaxFrontBase = parallaxFront.anchoredPosition;
        if (shine != null)
            shine.gameObject.SetActive(false);
        HideConfirmFx();
        ApplyImmediate();
    }

    private void OnDisable()
    {
        // 등장 도중에 패널이 닫혀도 다음에 열 때 이상한 자세로 남지 않게 정리한다.
        dealRoutine = null;
        shineRoutine = null;
        confirmRoutine = null;
        HideConfirmFx();
        dealOffset = Vector2.zero;
        dealRotation = 0f;
        dealScale = 1f;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        if (shine != null)
            shine.gameObject.SetActive(false);
    }

    private void Update()
    {
        float k = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);

        float targetScale = selected ? selectedScale : hovered ? hoverScale : 1f;
        float targetLift = selected ? selectedLift : hovered ? selectedLift * 0.35f : 0f;
        Color targetTint = selected ? Color.white : hovered ? hoverArtTint : normalArtTint;

        stateScale = Mathf.Lerp(stateScale, targetScale, k);
        stateLift = Mathf.Lerp(stateLift, targetLift, k);
        artTint = Color.Lerp(artTint, targetTint, k);

        // 선택된 카드의 글로우는 은은하게 숨쉬듯 깜빡인다.
        float targetGlow = selected ? 0.78f + 0.22f * Mathf.Sin(Time.unscaledTime * 3.2f) : 0f;
        glowAlpha = Mathf.Lerp(glowAlpha, targetGlow, k);

        UpdateTilt(k);
        Apply();
    }

    /// <summary>마우스가 카드 위 어디에 있는지(-1~1)를 구해서, 그쪽이 눌린 듯 카드를 기울이고
    /// 반사광을 마우스 위치로 옮긴다. 마우스가 벗어나면 평평하게 돌아온다.</summary>
    private void UpdateTilt(float k)
    {
        Vector2 target = Vector2.zero;
        UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;
        RectTransform rect = (RectTransform)transform;
        if (hovered && mouse != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, mouse.position.ReadValue(), null, out Vector2 local))
        {
            Vector2 half = rect.rect.size * 0.5f;
            target = new Vector2(Mathf.Clamp(local.x / half.x, -1f, 1f), Mathf.Clamp(local.y / half.y, -1f, 1f));
        }

        tiltPoint = Vector2.Lerp(tiltPoint, target, k);
        float glareTarget = hovered ? glareAlpha : 0f;
        currentGlareAlpha = Mathf.Lerp(currentGlareAlpha, glareTarget, k);

        if (parallaxBack != null)
            parallaxBack.anchoredPosition = parallaxBackBase + tiltPoint * parallaxBackDistance;
        if (parallaxFront != null)
            parallaxFront.anchoredPosition = parallaxFrontBase - tiltPoint * parallaxFrontDistance;

        if (glare != null)
        {
            RectTransform gr = glare.rectTransform;
            RectTransform parent = gr.parent as RectTransform;
            Vector2 half = parent != null ? parent.rect.size * 0.5f : Vector2.zero;
            gr.anchoredPosition = Vector2.Scale(tiltPoint, half);
            Color c = glare.color;
            c.a = currentGlareAlpha;
            glare.color = c;
        }
    }

    public void SetSelected(bool value, bool instant = false)
    {
        bool changed = value != selected;
        selected = value;

        if (frame != null)
            frame.color = selected ? selectedFrameColor : normalFrameColor;
        if (gem != null)
            gem.color = selected ? selectedFrameColor : normalFrameColor;

        if (instant)
        {
            stateScale = selected ? selectedScale : 1f;
            stateLift = selected ? selectedLift : 0f;
            artTint = selected ? Color.white : normalArtTint;
            glowAlpha = selected ? 1f : 0f;
            Apply();
        }
        else if (changed && selected)
        {
            PlayShine();
        }
    }

    /// <summary>카드를 아래쪽(fromOffset)에서 기울어진 채(fromRotation) 투명하게 시작해서,
    /// delay 뒤에 제자리로 튕기듯 날아와 펼쳐지게 한다.</summary>
    public void PlayDealIn(float delay, float duration, Vector2 fromOffset, float fromRotation)
    {
        if (!isActiveAndEnabled)
            return;

        if (dealRoutine != null)
            StopCoroutine(dealRoutine);
        dealRoutine = StartCoroutine(DealInRoutine(delay, duration, fromOffset, fromRotation));
    }

    /// <summary>클릭으로 이 캐릭터를 골랐을 때의 확정 연출: 카드가 살짝 번쩍 → 카드 외곽선이 바깥으로 퍼지고
    /// 불꽃 줄기가 사방으로 튐. 캐릭터 고유 소리도 같이 난다.</summary>
    public void PlayConfirm()
    {
        SoundManager.Instance?.PlaySfx(pickSound, pickVolume);

        if (!isActiveAndEnabled)
            return;

        if (confirmRoutine != null)
            StopCoroutine(confirmRoutine);
        confirmRoutine = StartCoroutine(ConfirmRoutine());
        PlayShine();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        if (!selected)
            SoundManager.Instance?.PlaySfx(hoverSound, hoverVolume, 0.05f);
    }

    public void OnPointerExit(PointerEventData eventData) => hovered = false;

    private IEnumerator DealInRoutine(float delay, float duration, Vector2 fromOffset, float fromRotation)
    {
        dealOffset = fromOffset;
        dealRotation = fromRotation;
        dealScale = 0.75f;
        canvasGroup.alpha = 0f;
        Apply();

        float wait = 0f;
        while (wait < delay)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        SoundManager.Instance?.PlaySfx(dealSound, dealVolume, 0.08f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float move = EaseOutBack(t, 1.4f);
            float fade = Mathf.Clamp01(t * 3f);

            dealOffset = Vector2.LerpUnclamped(fromOffset, Vector2.zero, move);
            dealRotation = Mathf.LerpUnclamped(fromRotation, 0f, EaseOutCubic(t));
            dealScale = Mathf.LerpUnclamped(0.75f, 1f, move);
            canvasGroup.alpha = fade;
            Apply();
            yield return null;
        }

        dealOffset = Vector2.zero;
        dealRotation = 0f;
        dealScale = 1f;
        canvasGroup.alpha = 1f;
        Apply();
        dealRoutine = null;

        if (selected)
            PlayShine();
    }

    private void PlayShine()
    {
        if (shine == null || !isActiveAndEnabled)
            return;

        if (shineRoutine != null)
            StopCoroutine(shineRoutine);
        shineRoutine = StartCoroutine(ShineRoutine());
    }

    /// <summary>빛줄기가 카드 왼쪽 밖에서 오른쪽 밖으로 한 번 쓸고 지나간다.</summary>
    private IEnumerator ShineRoutine()
    {
        RectTransform parent = shine.parent as RectTransform;
        float width = parent != null ? parent.rect.width : 300f;
        float startX = -width * 0.5f - shine.rect.width;
        float endX = width * 0.5f + shine.rect.width;

        shine.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < shineDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / shineDuration));
            Vector2 pos = shine.anchoredPosition;
            pos.x = Mathf.Lerp(startX, endX, t);
            shine.anchoredPosition = pos;
            yield return null;
        }

        shine.gameObject.SetActive(false);
        shineRoutine = null;
    }

    private IEnumerator ConfirmRoutine()
    {
        int sparkCount = confirmSparks != null ? confirmSparks.Length : 0;
        float[] sparkAngles = new float[sparkCount];
        float[] sparkDistances = new float[sparkCount];
        for (int i = 0; i < sparkCount; i++)
        {
            // 균등하게 퍼지되 약간씩 흐트러뜨려 기계적으로 안 보이게 한다.
            sparkAngles[i] = (360f / sparkCount) * i + Random.Range(-12f, 12f);
            sparkDistances[i] = Random.Range(150f, 230f);
            confirmSparks[i].gameObject.SetActive(true);
        }

        SetActive(confirmFlash, true);
        SetActive(confirmOutline, true);

        float elapsed = 0f;
        while (elapsed < confirmDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed;

            // 1) 카드가 살짝 번쩍
            if (confirmFlash != null)
                confirmFlash.color = new Color(1f, 1f, 1f, confirmFlashAlpha * (1f - Mathf.Clamp01(t / 0.2f)));

            // 2) 카드 모양 외곽선이 테두리에서 바깥으로 살짝 퍼지며 사라짐
            if (confirmOutline != null)
            {
                float rt = Mathf.Clamp01(t / 0.45f);
                float s = Mathf.Lerp(1f, 1.14f, EaseOutCubic(rt));
                confirmOutline.rectTransform.localScale = new Vector3(s, s, 1f);
                Color c = selectedFrameColor;
                c.a = 1f - rt;
                confirmOutline.color = c;
            }

            // 3) 불꽃 줄기가 중심에서 사방으로 튀어나감
            float st = Mathf.Clamp01(t / 0.5f);
            for (int i = 0; i < sparkCount; i++)
            {
                RectTransform r = confirmSparks[i].rectTransform;
                float a = sparkAngles[i] * Mathf.Deg2Rad;
                float dist = Mathf.Lerp(40f, sparkDistances[i], EaseOutCubic(st));
                r.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * dist;
                r.localRotation = Quaternion.Euler(0f, 0f, sparkAngles[i] - 90f);
                r.localScale = new Vector3(1f, Mathf.Lerp(1.4f, 0.3f, st), 1f);
                Color c = Color.Lerp(Color.white, selectedFrameColor, st);
                c.a = 1f - st;
                confirmSparks[i].color = c;
            }

            yield return null;
        }

        HideConfirmFx();
        confirmRoutine = null;
    }

    private void HideConfirmFx()
    {
        SetActive(confirmFlash, false);
        SetActive(confirmOutline, false);
        if (confirmSparks != null)
        {
            foreach (Image spark in confirmSparks)
                SetActive(spark, false);
        }
    }

    private static void SetActive(Graphic graphic, bool active)
    {
        if (graphic != null)
            graphic.gameObject.SetActive(active);
    }

    private void ApplyImmediate()
    {
        stateScale = 1f;
        stateLift = 0f;
        glowAlpha = 0f;
        Apply();
    }

    private void Apply()
    {
        if (visual != null)
        {
            visual.anchoredPosition = dealOffset + new Vector2(0f, stateLift);
            // Overlay 캔버스는 원근이 없어서 X/Y 회전은 거의 안 보인다 — 살짝 Z로만 기울이고
            // 입체감은 배경/캐릭터 시차(parallax)로 낸다.
            visual.localRotation = Quaternion.Euler(0f, 0f, dealRotation - tiltPoint.x * maxTilt);
            float s = stateScale * dealScale;
            visual.localScale = new Vector3(s, s, 1f);
        }

        if (artBackground != null)
            artBackground.color = artTint;

        if (glow != null)
        {
            Color c = selectedFrameColor;
            c.a = glowAlpha;
            glow.color = c;
        }
    }

    private static float EaseOutCubic(float t)
    {
        float x = 1f - t;
        return 1f - x * x * x;
    }

    private static float EaseOutBack(float t, float overshoot)
    {
        float c3 = overshoot + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + overshoot * x * x;
    }
}
