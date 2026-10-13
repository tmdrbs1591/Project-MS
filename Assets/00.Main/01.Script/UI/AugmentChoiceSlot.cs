using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 라운드 종료 후 증강 선택 UI(AugmentSelectUI)의 선택지 슬롯 하나.
/// 증강 아이콘/제목/설명과 출처 팩 이름을 보여주고, 선택 버튼과 슬롯당 1회 제한인 리롤 버튼을 가진다.
///
/// [연출]
///   - 첫 등장은 패널의 애니메이터(AugmentSelect.controller)가 맡고, 끝나면 AugmentSelectUI가
///     TakeOver()를 불러 이 슬롯이 직접 움직이기 시작한다(애니메이터가 계속 값을 덮어쓰지 않도록).
///   - 호버: 살짝 떠오르고 커지며 팩 색 테두리가 빛난다.
///   - 공개(PlayReveal)/리롤(PlayFlipSwap): 카드가 옆으로 접혔다 펴지며 나타나거나 내용이 바뀐다.
///   - 선택(PlayPicked)/탈락(PlayDismissed): 고른 카드는 튀어 오르며 사라지고, 나머지는 작아지며 사라진다.
///   - 모든 연출은 실제 시간(unscaled) 기준이라 슬로우모션과 겹쳐도 속도가 일정하다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class AugmentChoiceSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text packNameText;
    [SerializeField] private Button pickButton;
    [SerializeField] private Button rerollButton;

    [Header("호버")]
    [SerializeField] private float hoverLift = 18f;
    [SerializeField] private float hoverScale = 0.05f;
    [SerializeField] private float hoverSpeed = 14f;
    [SerializeField] private Vector2 glowDistance = new Vector2(5f, 5f);

    public AugmentPoolEntry CurrentEntry { get; private set; }
    public AugmentType CurrentType => CurrentEntry.Data.type;
    public bool IsAnimating => routine != null;

    /// <summary>마우스가 카드 위로 들어왔을 때(효과음 재생용).</summary>
    public event Action<AugmentChoiceSlot> HoverEntered;

    private bool hasRerolled;
    private Action<AugmentChoiceSlot> onPick;
    private Action<AugmentChoiceSlot> onReroll;

    private CanvasGroup canvasGroup;
    private RectTransform rect;
    private Outline glow;
    private Color glowColor = Color.white;
    private bool tookOver;
    private bool interactable = true;
    private bool hovered;
    private float hoverT;
    private Vector3 baseScale = Vector3.one;
    private Vector2 basePosition;
    private Vector3 animScale = Vector3.one; // 공개/선택 연출이 곱하는 배율
    private Coroutine routine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rect = (RectTransform)transform;

        if (pickButton != null)
            pickButton.onClick.AddListener(() => { if (interactable && routine == null) onPick?.Invoke(this); });
        if (rerollButton != null)
            rerollButton.onClick.AddListener(() => { if (interactable && routine == null) onReroll?.Invoke(this); });

        Graphic target = pickButton != null ? pickButton.targetGraphic : GetComponent<Graphic>();
        if (target != null)
        {
            glow = target.GetComponent<Outline>();
            if (glow == null)
                glow = target.gameObject.AddComponent<Outline>();
            glow.effectDistance = glowDistance;
            glow.enabled = false;
        }
    }

    private void OnDisable()
    {
        routine = null;
        hovered = false;
        hoverT = 0f;
        if (glow != null)
            glow.enabled = false;
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

        glowColor = entry.SourcePack != null ? entry.SourcePack.themeColor : Color.white;
        if (packNameText != null)
        {
            packNameText.text = entry.SourcePack != null ? entry.SourcePack.displayName : string.Empty;
            // 팩 이름은 띠 위에 올라가므로 팩 색을 흰색 쪽으로 밝혀서 읽기 좋게 쓴다.
            packNameText.color = Color.Lerp(glowColor, Color.white, 0.55f);
        }
    }

    /// <summary>새 라운드 시작 시 리롤 기회를 다시 채운다.</summary>
    public void ResetRerollState()
    {
        hasRerolled = false;
        RefreshRerollButton();
    }

    /// <summary>리롤을 아직 안 썼으면 소비하고 true, 이미 썼으면 false(무시).</summary>
    public bool TryConsumeReroll()
    {
        if (hasRerolled)
            return false;

        hasRerolled = true;
        RefreshRerollButton();
        return true;
    }

    public void SetSlotActive(bool active) => gameObject.SetActive(active);

    /// <summary>클릭/리롤 가능 여부. 연출 중에는 꺼서 실수로 고르는 것을 막는다.</summary>
    public void SetInteractable(bool value)
    {
        interactable = value;
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.interactable = value;
        canvasGroup.blocksRaycasts = value;
        if (!value)
            hovered = false;
        RefreshRerollButton();
    }

    /// <summary>패널 애니메이터의 첫 등장 연출이 끝난 뒤 호출. 지금 모습을 기준값으로 잡고 직접 움직이기 시작한다.</summary>
    public void TakeOver()
    {
        if (rect == null)
            rect = (RectTransform)transform;

        tookOver = true;
        baseScale = rect.localScale;
        basePosition = rect.anchoredPosition;
        animScale = Vector3.one;
        canvasGroup.alpha = 1f;
    }

    /// <summary>패널 애니메이터가 다시 재생되기 전에 호출. 이 슬롯이 위치/크기를 건드리지 않게 손을 뗀다.</summary>
    public void ReleaseToAnimator()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (tookOver && rect != null)
        {
            // 넘겨받기 전 기준값으로 되돌려 두면 애니메이터가 이어서 정상적으로 그린다.
            rect.anchoredPosition = basePosition;
            rect.localScale = baseScale;
        }

        tookOver = false;
        hovered = false;
        hoverT = 0f;
        animScale = Vector3.one;
        if (glow != null)
            glow.enabled = false;
    }

    /// <summary>카드가 옆으로 펴지며 나타난다(두 번째 픽의 새 카드 등).</summary>
    public void PlayReveal(float delay)
    {
        Restart(RevealRoutine(delay));
    }

    /// <summary>카드가 접혔다가 swap으로 내용을 바꾸고 다시 펴진다(리롤).</summary>
    public void PlayFlipSwap(Action swap)
    {
        Restart(FlipSwapRoutine(swap));
    }

    /// <summary>고른 카드: 튀어 오르며 빛나다가 사라진다.</summary>
    public void PlayPicked()
    {
        Restart(PickedRoutine());
    }

    /// <summary>고르지 않은 카드: 작아지며 사라진다.</summary>
    public void PlayDismissed()
    {
        Restart(DismissedRoutine());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!interactable || routine != null)
            return;
        hovered = true;
        HoverEntered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData) => hovered = false;

    private void Update()
    {
        if (!tookOver)
            return;

        float target = hovered && interactable && routine == null ? 1f : 0f;
        hoverT = Mathf.MoveTowards(hoverT, target, hoverSpeed * Time.unscaledDeltaTime);
        float eased = hoverT * hoverT * (3f - 2f * hoverT);

        rect.anchoredPosition = basePosition + Vector2.up * (hoverLift * eased);
        rect.localScale = Vector3.Scale(baseScale * (1f + hoverScale * eased), animScale);

        if (glow != null)
        {
            bool showGlow = eased > 0.01f;
            if (glow.enabled != showGlow)
                glow.enabled = showGlow;
            Color c = glowColor;
            c.a = 0.85f * eased;
            glow.effectColor = c;
        }
    }

    private void Restart(IEnumerator next)
    {
        if (!gameObject.activeInHierarchy)
            return;
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(Wrap(next));
    }

    private IEnumerator Wrap(IEnumerator inner)
    {
        yield return inner;
        routine = null;
    }

    private IEnumerator RevealRoutine(float delay)
    {
        canvasGroup.alpha = 0f;
        animScale = new Vector3(0f, 1f, 1f);
        float wait = 0f;
        while (wait < delay)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        canvasGroup.alpha = 1f;
        yield return ScaleX(0f, 1.12f, 0.16f);
        yield return ScaleX(1.12f, 1f, 0.08f);
    }

    private IEnumerator FlipSwapRoutine(Action swap)
    {
        yield return ScaleX(1f, 0f, 0.12f);
        swap?.Invoke();
        yield return ScaleX(0f, 1.12f, 0.14f);
        yield return ScaleX(1.12f, 1f, 0.08f);
    }

    private IEnumerator PickedRoutine()
    {
        if (glow != null)
        {
            glow.enabled = true;
            Color c = glowColor;
            c.a = 1f;
            glow.effectColor = c;
        }

        yield return Tween(0.12f, t => animScale = Vector3.one * Mathf.Lerp(1f, 1.18f, t));
        yield return Tween(0.3f, t =>
        {
            animScale = Vector3.one * Mathf.Lerp(1.18f, 1.3f, t);
            canvasGroup.alpha = 1f - t;
        });
    }

    private IEnumerator DismissedRoutine()
    {
        yield return Tween(0.25f, t =>
        {
            animScale = Vector3.one * Mathf.Lerp(1f, 0.82f, t);
            canvasGroup.alpha = 1f - t;
        });
    }

    private IEnumerator ScaleX(float from, float to, float duration)
    {
        yield return Tween(duration, t => animScale = new Vector3(Mathf.Lerp(from, to, t), 1f, 1f));
    }

    private static IEnumerator Tween(float duration, Action<float> apply)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            apply(1f - (1f - t) * (1f - t)); // ease-out
            yield return null;
        }
        apply(1f);
    }

    private void RefreshRerollButton()
    {
        if (rerollButton != null)
            rerollButton.interactable = !hasRerolled && interactable;
    }
}
