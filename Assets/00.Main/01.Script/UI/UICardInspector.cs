using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 작은 카드(증강 카드, 카드팩 그림 등)를 눌렀을 때 크게 들여다보는 연출.
/// 누른 카드의 모습을 복사해서, 그 자리에서 화면 가운데로 날아오며 커지고 샤라락 한 바퀴 돈 뒤
/// 둥실둥실 떠 있게 한다. 뒤의 화면은 그 순간을 찍어서 흐리게 깐다(UI까지 전부 흐려진다).
/// 뒤로가기 버튼 / Esc / 빈 곳 클릭으로 닫으면 다시 돌면서 원래 자리로 돌아간다.
/// 여러 장을 묶어서 열면(InspectGallery) ←/→(A/D) 키나 양옆 화살표로 옆 카드로 샤라락 넘어간다.
///
/// [씬 설정]
///   - 패널 맨 위(가장 나중에 그려지는 자리)에 전체 화면 오브젝트를 두고 이 스크립트를 붙인다.
///   - blurImage: 흐려진 화면을 깔 전체 화면 RawImage(누르면 닫히도록 raycast를 켜 둔다).
///   - cardHolder: 복사된 카드가 들어갈 자리(화면 가운데 기준).
///   - blurMaterial: Hidden/UI/KawaseBlur 머티리얼.
/// </summary>
public class UICardInspector : MonoBehaviour
{
    [SerializeField] private RawImage blurImage;
    [SerializeField] private Image dim;
    [Tooltip("카드 뒤에 은은하게 깔리는 스포트라이트 빛(선택 사항).")]
    [SerializeField] private Image spotGlow;
    [Range(0f, 1f)] [SerializeField] private float spotGlowAlpha = 0.35f;
    [SerializeField] private RectTransform cardHolder;
    [SerializeField] private Button backButton;
    [Tooltip("여러 장을 묶어 열었을 때만 보이는 이전/다음 버튼.")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [Tooltip("여러 장을 묶어 열었을 때 \"3 / 13\"처럼 지금 위치를 보여줄 글자(선택 사항).")]
    [SerializeField] private TMPro.TMP_Text counterText;
    [SerializeField] private CanvasGroup controls;
    [SerializeField] private Material blurMaterial;
    [Tooltip("복사본에서 끌 자식 이름들. 원래 카드의 호버 빛처럼, 누르는 순간의 상태가 크게 보일 때 같이 따라오면 안 되는 것들.")]
    [SerializeField] private string[] hideInClone = { "Hover Glow" };

    [Header("연출")]
    [Tooltip("가운데로 왔을 때 원래 크기의 몇 배로 키울지. 원래 카드가 큰 경우(카드팩 그림)는 화면 높이 기준으로 줄인다.")]
    [SerializeField] private float targetScale = 2.3f;
    [Tooltip("가운데로 왔을 때 카드 높이가 화면 높이의 이 비율을 넘지 않게 한다.")]
    [Range(0.2f, 1f)] [SerializeField] private float maxScreenHeightRatio = 0.62f;
    [SerializeField] private Vector2 centerOffset = new Vector2(0f, 30f);
    [Min(0.05f)] [SerializeField] private float openDuration = 0.55f;
    [Min(0.05f)] [SerializeField] private float closeDuration = 0.45f;
    [Tooltip("날아오면서 도는 바퀴 수.")]
    [SerializeField] private float spins = 1f;
    [Range(0f, 1f)] [SerializeField] private float dimAlpha = 0.45f;
    [Range(1, 8)] [SerializeField] private int blurIterations = 5;
    [SerializeField] private float blurSpread = 1.1f;
    [Tooltip("옆 카드로 넘길 때 한쪽으로 빠지고 반대쪽에서 들어오는 거리.")]
    [SerializeField] private float swapDistance = 520f;
    [Min(0.05f)] [SerializeField] private float swapDuration = 0.22f;

    [Header("사운드")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.55f;

    public bool IsOpen { get; private set; }

    private RectTransform source;
    private CanvasGroup sourceGroup;
    private RectTransform clone;
    private Vector3 cloneBaseScale;
    private float shownScale;
    private Coroutine routine;
    private RenderTexture screenRT;
    private RenderTexture blurA;
    private RenderTexture blurB;
    private float floatTime;
    private bool floating;

    // 여러 장을 묶어 열었을 때(좌우로 넘기기)
    private readonly System.Collections.Generic.List<RectTransform> galleryVisuals = new System.Collections.Generic.List<RectTransform>();
    private readonly System.Collections.Generic.List<CanvasGroup> galleryHides = new System.Collections.Generic.List<CanvasGroup>();
    private int galleryIndex;

    private static readonly int OffsetId = Shader.PropertyToID("_Offset");

    private void Awake()
    {
        if (backButton != null)
            backButton.onClick.AddListener(Close);

        // 흐린 배경(빈 곳)을 눌러도 닫힌다.
        if (blurImage != null)
        {
            Button bgButton = blurImage.GetComponent<Button>();
            if (bgButton == null)
                bgButton = blurImage.gameObject.AddComponent<Button>();
            bgButton.transition = Selectable.Transition.None;
            bgButton.onClick.AddListener(Close);
        }

        if (prevButton != null)
            prevButton.onClick.AddListener(() => Step(-1));
        if (nextButton != null)
            nextButton.onClick.AddListener(() => Step(1));

        // 여기서 SetActive(false)를 하면 안 된다 — 이 오브젝트는 씬에서 꺼진 채로 시작하고, 처음
        // Inspect()가 켜는 순간 Awake가 그 호출 안에서 돌기 때문에 바로 다시 꺼져 버린다.
        // 꺼진 상태는 씬에서 설정해 두고, 닫힘은 CloseRoutine 끝에서 끈다.
        SetOverlayAlpha(0f);
    }

    private void OnDisable()
    {
        // 창째로 닫혀 버린 경우에도 원래 카드는 다시 보이게, 복사본은 지운다.
        RestoreSource();
        if (clone != null)
            Destroy(clone.gameObject);
        clone = null;
        IsOpen = false;
        floating = false;
        routine = null;
    }

    private void OnDestroy()
    {
        ReleaseTextures();
    }

    /// <summary>sourceVisual(카드 그림 묶음)을 복사해서 크게 보여준다.
    /// hideGroup: 크게 보는 동안 원래 자리의 카드를 숨길 CanvasGroup. 비우면 sourceVisual에 붙여서 쓴다
    /// (sourceVisual의 알파를 다른 연출이 매 프레임 덮어쓰는 경우엔 바깥쪽 CanvasGroup을 넘겨야 한다).</summary>
    public void Inspect(RectTransform sourceVisual, CanvasGroup hideGroup = null)
    {
        if (sourceVisual == null || IsOpen)
            return;

        galleryVisuals.Clear();
        galleryHides.Clear();
        Begin(sourceVisual, hideGroup);
    }

    /// <summary>여러 장 중 index번째를 크게 열고, 좌우로 나머지를 넘겨볼 수 있게 한다.
    /// hideGroups는 visuals와 같은 순서(없으면 null로 채워도 된다).</summary>
    public void InspectGallery(System.Collections.Generic.IList<RectTransform> visuals,
        System.Collections.Generic.IList<CanvasGroup> hideGroups, int index)
    {
        if (visuals == null || visuals.Count == 0 || IsOpen)
            return;

        galleryVisuals.Clear();
        galleryHides.Clear();
        for (int i = 0; i < visuals.Count; i++)
        {
            galleryVisuals.Add(visuals[i]);
            galleryHides.Add(hideGroups != null && i < hideGroups.Count ? hideGroups[i] : null);
        }
        galleryIndex = Mathf.Clamp(index, 0, visuals.Count - 1);
        Begin(galleryVisuals[galleryIndex], galleryHides[galleryIndex]);
    }

    private void Begin(RectTransform sourceVisual, CanvasGroup hideGroup)
    {
        bool hasGallery = galleryVisuals.Count > 1;
        if (prevButton != null)
            prevButton.gameObject.SetActive(hasGallery);
        if (nextButton != null)
            nextButton.gameObject.SetActive(hasGallery);
        if (counterText != null)
            counterText.gameObject.SetActive(hasGallery);
        UpdateCounter();

        IsOpen = true;
        source = sourceVisual;
        sourceGroup = hideGroup;
        gameObject.SetActive(true);
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(OpenRoutine());
    }

    public void Close()
    {
        if (!IsOpen || clone == null)
            return;

        IsOpen = false;
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(CloseRoutine());
    }

    /// <summary>옆 카드로 넘긴다(끝에서는 반대쪽 끝으로 돈다). 연출 중이면 무시한다.</summary>
    public void Step(int direction)
    {
        if (!IsOpen || !floating || galleryVisuals.Count < 2 || clone == null)
            return;

        int next = (galleryIndex + direction + galleryVisuals.Count) % galleryVisuals.Count;
        if (galleryVisuals[next] == null)
            return;

        galleryIndex = next;
        UpdateCounter();
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(SwapRoutine(direction, galleryVisuals[next], galleryHides[next]));
    }

    private void Update()
    {
        if (IsOpen && floating)
        {
            UnityEngine.InputSystem.Keyboard kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
                    Step(-1);
                else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
                    Step(1);
            }
        }

        if (!floating || clone == null)
            return;

        // 가운데에 떠 있는 동안 둥실둥실 + 살짝 흔들흔들
        floatTime += Time.unscaledDeltaTime;
        clone.anchoredPosition = centerOffset + new Vector2(0f, Mathf.Sin(floatTime * 1.6f) * 9f);
        clone.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(floatTime * 1.1f) * 2.5f);
    }

    private IEnumerator OpenRoutine()
    {
        // 지금 화면(보관함 UI 포함)을 찍어서 흐리게 깐다. 오버레이를 켜기 전 프레임을 찍어야 한다.
        // 화면 캡처는 프레임 끝에서만 되는데, 에디터 창이 포커스를 잃는 등 화면이 안 그려지면
        // 그 신호가 안 와서 영영 멈출 수 있다 — 잠깐만 기다리고 안 오면 블러 없이(어둡게만) 진행한다.
        SetOverlayAlpha(0f);
        captured = false;
        StartCoroutine(CaptureAtEndOfFrame());
        float waited = 0f;
        while (!captured && waited < 0.15f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!captured && blurImage != null)
            blurImage.texture = null;

        CreateClone();
        SoundManager.Instance?.PlaySfx(openSound, sfxVolume);

        Vector2 startPos = SourcePositionInHolder();
        Vector2 endPos = centerOffset;
        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            float move = EaseOutCubic(t);
            float grow = EaseOutBack(t);

            clone.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, move);
            ApplySpin(Mathf.LerpUnclamped(1f, shownScale, grow), spins * 360f * move);
            clone.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI) * -8f);
            SetOverlayAlpha(move);
            yield return null;
        }

        ApplySpin(shownScale, 0f);
        SetOverlayAlpha(1f);
        floatTime = 0f;
        floating = true;
        routine = null;
    }

    private IEnumerator CloseRoutine()
    {
        floating = false;
        SoundManager.Instance?.PlaySfx(closeSound, sfxVolume);

        Vector2 startPos = clone.anchoredPosition;
        Quaternion startRot = clone.localRotation;
        float elapsed = 0f;
        while (elapsed < closeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / closeDuration);
            float move = EaseInOutCubic(t);

            // 원래 카드가 스크롤 등으로 움직였을 수도 있으니 매 프레임 다시 잰다.
            clone.anchoredPosition = Vector2.LerpUnclamped(startPos, SourcePositionInHolder(), move);
            ApplySpin(Mathf.Lerp(shownScale, 1f, move), -spins * 360f * move);
            clone.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, move);
            SetOverlayAlpha(1f - move);
            yield return null;
        }

        RestoreSource();
        Destroy(clone.gameObject);
        clone = null;
        routine = null;
        gameObject.SetActive(false);
    }

    /// <summary>지금 카드는 넘기는 방향 반대쪽으로 반 바퀴 돌며 빠지고, 다음 카드가 반대편에서
    /// 반 바퀴 돌며 들어와 가운데에 선다.</summary>
    private IEnumerator SwapRoutine(int direction, RectTransform nextSource, CanvasGroup nextHide)
    {
        floating = false;
        SoundManager.Instance?.PlaySfx(openSound, sfxVolume * 0.7f, 0.08f);

        // 1) 지금 카드가 빠진다
        RectTransform outgoing = clone;
        CanvasGroup outGroup = outgoing.GetComponent<CanvasGroup>();
        Vector2 outStart = outgoing.anchoredPosition;
        Vector2 outEnd = centerOffset + new Vector2(-direction * swapDistance, 0f);
        float elapsed = 0f;
        while (elapsed < swapDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / swapDuration);
            float e = t * t;
            clone = outgoing;
            outgoing.anchoredPosition = Vector2.Lerp(outStart, outEnd, e);
            ApplySpin(shownScale * Mathf.Lerp(1f, 0.85f, e), 90f * e);
            if (outGroup != null)
                outGroup.alpha = 1f - e;
            yield return null;
        }

        Destroy(outgoing.gameObject);
        RestoreSource();

        // 2) 다음 카드가 들어온다
        source = nextSource;
        sourceGroup = nextHide;
        CreateClone();
        Vector2 inStart = centerOffset + new Vector2(direction * swapDistance, 0f);
        CanvasGroup inGroup = clone.GetComponent<CanvasGroup>();
        elapsed = 0f;
        while (elapsed < swapDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / swapDuration);
            float e = EaseOutCubic(t);
            clone.anchoredPosition = Vector2.Lerp(inStart, centerOffset, e);
            ApplySpin(shownScale * Mathf.Lerp(0.85f, 1f, e), 90f * (1f - e));
            clone.localRotation = Quaternion.identity;
            if (inGroup != null)
                inGroup.alpha = e;
            yield return null;
        }

        ApplySpin(shownScale, 0f);
        if (inGroup != null)
            inGroup.alpha = 1f;
        floatTime = 0f;
        floating = true;
        routine = null;
    }

    private bool captured;

    private IEnumerator CaptureAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        // 이미 블러 없이 넘어가서 카드가 떠 있는 뒤라면, 그 화면을 찍으면 카드가 배경에 같이 찍힌다.
        if (clone == null)
        {
            CaptureAndBlur();
            captured = true;
        }
    }

    private void UpdateCounter()
    {
        if (counterText != null && galleryVisuals.Count > 1)
            counterText.text = (galleryIndex + 1) + " / " + galleryVisuals.Count;
    }

    private void CreateClone()
    {
        clone = Instantiate(source, cardHolder);
        clone.name = "Inspected Card";
        clone.anchorMin = clone.anchorMax = clone.pivot = new Vector2(0.5f, 0.5f);
        clone.sizeDelta = source.rect.size;
        clone.localRotation = Quaternion.identity;
        cloneBaseScale = Vector3.one;

        // 복사본이 원래 카드의 연출(호버 빛 등) 상태를 그대로 들고 오지 않게 정리한다.
        CanvasGroup group = clone.GetComponent<CanvasGroup>();
        if (group != null)
            group.alpha = 1f;
        foreach (Graphic g in clone.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;
        foreach (Button b in clone.GetComponentsInChildren<Button>(true))
            b.enabled = false;
        if (hideInClone != null)
        {
            foreach (Transform t in clone.GetComponentsInChildren<Transform>(true))
            {
                if (System.Array.IndexOf(hideInClone, t.name) >= 0)
                    t.gameObject.SetActive(false);
            }
        }

        float screenHeight = ((RectTransform)cardHolder.parent).rect.height;
        float maxScale = screenHeight * maxScreenHeightRatio / Mathf.Max(1f, source.rect.height);
        shownScale = Mathf.Min(targetScale, maxScale);

        // 원래 자리에서는 카드가 빠져나간 것처럼 숨긴다.
        if (sourceGroup == null)
            sourceGroup = source.GetComponent<CanvasGroup>();
        if (sourceGroup == null)
            sourceGroup = source.gameObject.AddComponent<CanvasGroup>();
        sourceGroup.alpha = 0f;

        clone.anchoredPosition = SourcePositionInHolder();
        ApplySpin(1f, 0f);
    }

    private void RestoreSource()
    {
        if (sourceGroup != null)
            sourceGroup.alpha = 1f;
        sourceGroup = null;
    }

    /// <summary>scale 크기로, angle만큼 세로축으로 돈 것처럼(가로 폭이 cos로 줄었다 늘었다) 보이게 한다.
    /// 뒷면 대신 글자가 뒤집혀 보이지 않도록 절댓값을 쓴다.</summary>
    private void ApplySpin(float scale, float angle)
    {
        float width = Mathf.Abs(Mathf.Cos(angle * Mathf.Deg2Rad));
        clone.localScale = new Vector3(cloneBaseScale.x * scale * Mathf.Max(width, 0.02f), cloneBaseScale.y * scale, 1f);
    }

    private Vector2 SourcePositionInHolder()
    {
        if (source == null)
            return centerOffset;

        Vector3 world = source.TransformPoint(source.rect.center);
        Vector3 local = cardHolder.InverseTransformPoint(world);
        return new Vector2(local.x, local.y);
    }

    private void SetOverlayAlpha(float a)
    {
        if (blurImage != null)
        {
            // 캡처를 못 했으면(텍스처 없음) 흰 화면 대신 어두운 막으로 대신한다.
            blurImage.color = blurImage.texture != null
                ? new Color(1f, 1f, 1f, a)
                : new Color(0.04f, 0.06f, 0.14f, a * 0.8f);
        }
        if (dim != null)
        {
            Color c = dim.color;
            c.a = dimAlpha * a;
            dim.color = c;
        }
        if (spotGlow != null)
        {
            Color c = spotGlow.color;
            c.a = spotGlowAlpha * a;
            spotGlow.color = c;
        }
        if (controls != null)
        {
            controls.alpha = Mathf.Clamp01(a * 2f - 1f);
            controls.blocksRaycasts = a > 0.5f;
        }
    }

    private void CaptureAndBlur()
    {
        if (blurImage == null || blurMaterial == null)
            return;

        int w = Screen.width, h = Screen.height;
        if (screenRT == null || screenRT.width != w || screenRT.height != h)
        {
            ReleaseTextures();
            screenRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32) { name = "Inspector Screen" };
            // 1/4 해상도에서 흐리게 해야 격자 무늬 없이 부드럽게 퍼진다(화면에 띄울 때 다시 늘어나며 더 뭉개짐).
            blurA = new RenderTexture(w / 4, h / 4, 0, RenderTextureFormat.ARGB32) { name = "Inspector Blur A" };
            blurB = new RenderTexture(w / 4, h / 4, 0, RenderTextureFormat.ARGB32) { name = "Inspector Blur B" };
        }

        ScreenCapture.CaptureScreenshotIntoRenderTexture(screenRT);

        // 먼저 그대로 줄여 담은 뒤 그 해상도에서 반복해서 흐린다.
        Graphics.Blit(screenRT, blurB);
        RenderTexture src = blurB;
        RenderTexture dst = blurA;
        for (int i = 0; i < blurIterations; i++)
        {
            blurMaterial.SetFloat(OffsetId, (i + 0.5f) * blurSpread);
            Graphics.Blit(src, dst, blurMaterial);
            src = dst;
            dst = dst == blurA ? blurB : blurA;
        }

        blurImage.texture = src;
        // 일부 그래픽 API(DirectX 등)에서는 화면 캡처가 위아래 뒤집혀 들어온다.
        blurImage.uvRect = SystemInfo.graphicsUVStartsAtTop ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f);
    }

    private void ReleaseTextures()
    {
        foreach (RenderTexture rt in new[] { screenRT, blurA, blurB })
        {
            if (rt != null)
            {
                rt.Release();
                Destroy(rt);
            }
        }
        screenRT = blurA = blurB = null;
    }

    private static float EaseOutCubic(float t)
    {
        float x = 1f - t;
        return 1f - x * x * x;
    }

    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.5f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}
