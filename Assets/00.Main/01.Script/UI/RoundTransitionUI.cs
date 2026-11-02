using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 라운드와 라운드 사이 화면 전환: 증강 선택이 끝나면 비스듬한 띠들이 오른쪽에서 촤라락 밀려와 화면을 덮고,
/// 그 뒤에서 맵 교체/캐릭터 순간이동이 일어난 다음 왼쪽으로 쓸려 나가며 새 라운드가 드러난다.
///   - MatchManager가 실제로 넘어가기 RoundTransitionCoverTime 초 전에 RPC로 Play를 부른다(모든 클라).
///   - 다 덮은 뒤에는 다음 라운드가 시작(Phase=Fighting, RoundNumber 증가)된 걸 확인하고 걷어낸다.
/// [씬 설정] 게임 씬의 빈 오브젝트에 붙이고 폰트/효과음을 넣는다. UI는 코드로 만든다.
/// </summary>
public class RoundTransitionUI : MonoBehaviour
{
    public static RoundTransitionUI Instance { get; private set; }

    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Material fontMaterial;
    [SerializeField] private AudioClip whooshInClip;
    [SerializeField] private AudioClip whooshOutClip;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.8f;
    [SerializeField] private Color bandColorA = new Color(0.07f, 0.04f, 0.14f, 1f);
    [SerializeField] private Color bandColorB = new Color(0.12f, 0.06f, 0.22f, 1f);
    [SerializeField] private Color edgeColor = new Color(1f, 0.78f, 0.25f, 1f);
    [SerializeField] private Color edgeColor2 = new Color(0.75f, 0.4f, 1f, 1f);
    [SerializeField] private int sortingOrder = 600;
    [Tooltip("다음 라운드가 시작된 걸 못 받아도 이 시간(초) 뒤엔 걷어낸다.")]
    [SerializeField] private float maxCoverTime = 2f;

    private const int BandCount = 6;
    private const float Angle = 16f;
    private RectTransform root;
    private Canvas canvas;
    private readonly List<RectTransform> bands = new List<RectTransform>();
    private RectTransform centerGroup;
    private TMP_Text title;
    private Image emblem;
    private Coroutine routine;

    private void Awake() => Instance = this;
    private void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>다음 라운드로 넘어가기 직전에 부른다. coverTime 안에 화면을 다 덮는다.</summary>
    public static void Play(int nextRound, float coverTime)
    {
        if (Instance == null)
            return;
        Instance.Begin(nextRound, coverTime);
    }

    private void Begin(int nextRound, float coverTime)
    {
        Build();
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(Run(nextRound, Mathf.Max(0.15f, coverTime - 0.08f)));
    }

    private IEnumerator Run(int nextRound, float coverTime)
    {
        canvas.enabled = true;
        float w = root.rect.width, h = root.rect.height;
        float span = w + h * Mathf.Tan(Angle * Mathf.Deg2Rad) + 400f;   // 띠가 화면 밖에서 밖까지 지나가는 거리
        title.text = "ROUND " + nextRound;
        SoundManager.Instance?.PlaySfx(whooshInClip, volume, 0f);

        // 1) 덮기: 띠들이 오른쪽에서 조금씩 엇갈려 밀려온다
        float stagger = coverTime * 0.35f / bands.Count;
        float each = coverTime - stagger * (bands.Count - 1);
        for (float t = 0f; t < coverTime + 0.001f; t += Time.unscaledDeltaTime)
        {
            for (int i = 0; i < bands.Count; i++)
            {
                float k = Mathf.Clamp01((t - i * stagger) / each);
                float e = 1f - Mathf.Pow(1f - k, 3f);
                bands[i].anchoredPosition = new Vector2(Mathf.Lerp(span, 0f, e), bands[i].anchoredPosition.y);
            }
            SetCenter(Mathf.Clamp01((t - coverTime * 0.55f) / (coverTime * 0.45f)), 0f);
            yield return null;
        }
        foreach (RectTransform b in bands) b.anchoredPosition = new Vector2(0f, b.anchoredPosition.y);

        // 2) 덮인 채로: 다음 라운드가 실제로 시작될 때까지(맵 교체/순간이동이 이 뒤에서 일어난다)
        float waited = 0f;
        while (waited < maxCoverTime)
        {
            MatchManager mm = MatchManager.Instance;
            if (mm != null && mm.Phase == MatchPhase.Fighting && mm.RoundNumber >= nextRound)
                break;
            waited += Time.unscaledDeltaTime;
            SetCenter(1f, waited);
            yield return null;
        }
        // 라운드 시작 글자(RoundIntroUI)가 같은 자리에 뜨므로, 내 글자는 먼저 빠르게 사라진다(겹쳐 보이지 않게).
        for (float t = 0f; t < 0.18f; t += Time.unscaledDeltaTime)
        {
            SetCenter(1f - t / 0.12f, waited + t);
            yield return null;
        }
        SetCenter(0f, 0f);

        // 3) 걷기: 같은 방향(왼쪽)으로 쓸려 나간다
        SoundManager.Instance?.PlaySfx(whooshOutClip, volume, 0f);
        const float outTime = 0.42f;
        for (float t = 0f; t < outTime + 0.001f; t += Time.unscaledDeltaTime)
        {
            for (int i = 0; i < bands.Count; i++)
            {
                float k = Mathf.Clamp01((t - (bands.Count - 1 - i) * 0.035f) / (outTime - 0.035f * (bands.Count - 1)));
                float e = k * k * k;
                bands[i].anchoredPosition = new Vector2(Mathf.Lerp(0f, -span, e), bands[i].anchoredPosition.y);
            }
            yield return null;
        }
        canvas.enabled = false;
        routine = null;
    }

    // 가운데 문양 + 다음 라운드 글자: 덮이는 끝 무렵 쾅 나타나고, 기다리는 동안 반짝인다.
    private void SetCenter(float show, float time)
    {
        show = Mathf.Clamp01(show);
        centerGroup.localScale = Vector3.one * Mathf.LerpUnclamped(1.6f, 1f, 1f - Mathf.Pow(1f - show, 3f));
        float a = show;
        title.alpha = a;
        emblem.color = new Color(edgeColor.r, edgeColor.g, edgeColor.b, a * (0.85f + 0.15f * Mathf.Sin(time * 12f)));
        emblem.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f + time * 90f);
    }

    private void Build()
    {
        if (canvas != null)
            return;
        var go = new GameObject("Round Transition Canvas");
        go.transform.SetParent(transform, false);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root = go.GetComponent<RectTransform>();
        Canvas.ForceUpdateCanvases();

        // 화면 높이를 띠 개수로 나눠 비스듬히 쌓는다(서로 살짝 겹치게)
        // 비스듬히 기울어 화면 모서리가 비지 않게 위아래로 두 줄씩 더 쌓는다.
        float h = 1080f, bandH = h / BandCount * 1.35f;
        for (int i = -2; i < BandCount + 2; i++)
        {
            var band = NewImage(go.transform, "Band", (i & 1) == 0 ? bandColorA : bandColorB);
            RectTransform rt = band.rectTransform;
            rt.sizeDelta = new Vector2(4200f, bandH);
            rt.anchoredPosition = new Vector2(9999f, (i + 0.5f) * (h / BandCount) - h * 0.5f);
            rt.localEulerAngles = new Vector3(0f, 0f, Angle);
            // 띠 앞쪽 가장자리에 빛나는 선
            var edge = NewImage(rt, "Edge", (i & 1) == 0 ? edgeColor : edgeColor2);
            edge.rectTransform.anchorMin = new Vector2(0f, 0f);
            edge.rectTransform.anchorMax = new Vector2(0f, 1f);
            edge.rectTransform.pivot = new Vector2(1f, 0.5f);
            edge.rectTransform.sizeDelta = new Vector2((i & 1) == 0 ? 14f : 6f, 0f);
            bands.Add(rt);
        }

        centerGroup = new GameObject("Center", typeof(RectTransform)).GetComponent<RectTransform>();
        centerGroup.SetParent(go.transform, false);
        emblem = NewImage(centerGroup, "Emblem", edgeColor);
        emblem.rectTransform.sizeDelta = new Vector2(70f, 70f);
        emblem.rectTransform.anchoredPosition = new Vector2(0f, 70f);
        var inner = NewImage(emblem.rectTransform, "Inner", bandColorA);
        inner.rectTransform.sizeDelta = new Vector2(44f, 44f);

        title = new GameObject("Title", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        title.rectTransform.SetParent(centerGroup, false);
        title.rectTransform.sizeDelta = new Vector2(1200f, 160f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -30f);
        if (font != null) title.font = font;
        if (fontMaterial != null) title.fontSharedMaterial = fontMaterial;
        title.fontSize = 110f;
        title.fontStyle = FontStyles.Italic;
        title.characterSpacing = 10f;
        title.alignment = TextAlignmentOptions.Center;
        title.enableVertexGradient = true;
        title.colorGradient = new VertexGradient(Color.white, Color.white, edgeColor, edgeColor);
        title.raycastTarget = false;
        canvas.enabled = false;
    }

    private static Image NewImage(Transform parent, string name, Color color)
    {
        var img = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(parent, false);
        img.color = color;
        img.raycastTarget = false;
        return img;
    }
}
