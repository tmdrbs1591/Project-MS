using System.Collections;
using System.Collections.Generic;
using Fusion;
using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 라운드가 끝났을 때(처치 연출이 끝난 뒤) 뜨는 결과 화면.
///   - 띠가 쫙 열리며 "ROUND WIN" / "ROUND LOSE"가 박히고
///   - 양쪽 점수 동그라미(5판 3선승 = 3칸)가 보이며, 이번 라운드 승자의 빈 칸으로 빛 구슬이 날아가
///     "탕!" 하고 채워진다(충격파 + 반짝임 + "+1").
///   - 라운드 끝 시간이 지나 증강 선택으로 넘어가면 사라진다.
/// 화면은 코드로 만든다(씬의 빈 오브젝트에 붙이고 폰트/머티리얼/소리만 넣으면 된다).
/// 이게 씬에 있으면 MatchResultUI는 라운드 끝 배너를 띄우지 않는다.
/// </summary>
public class RoundResultUI : MonoBehaviour
{
    public static RoundResultUI Instance { get; private set; }

    [Header("재료")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Material fontMaterial;
    [SerializeField] private Sprite softCircle;
    [SerializeField] private Color winColor = new Color(1f, 0.82f, 0.32f, 1f);
    [SerializeField] private Color loseColor = new Color(0.62f, 0.68f, 0.9f, 1f);
    [SerializeField] private Color player1Color = new Color(0.3f, 0.72f, 1f, 1f);
    [SerializeField] private Color player2Color = new Color(1f, 0.36f, 0.42f, 1f);

    [Header("사운드")]
    [SerializeField] private AudioClip bannerClip;
    [SerializeField] private AudioClip pointClip;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.9f;

    private RectTransform root;
    private CanvasGroup group;
    private RectTransform bandRect;
    private TextMeshProUGUI title;
    private TextMeshProUGUI plusOne;
    private readonly List<Image>[] slots = { new List<Image>(), new List<Image>() };
    private readonly List<Image>[] gems = { new List<Image>(), new List<Image>() };
    private TextMeshProUGUI[] sideLabels = new TextMeshProUGUI[2];
    private RectTransform fx;
    private Sprite ringSprite, gemSprite;
    private Coroutine routine;
    private int shownKey = -1;

    private void Awake()
    {
        Instance = this;
        ringSprite = MakeCircle(false);
        gemSprite = MakeCircle(true);
        Build();
        group.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        MatchManager match = MatchManager.Instance;
        if (match == null || match.Phase != MatchPhase.RoundEnd)
        {
            if (routine != null) { StopCoroutine(routine); routine = null; }
            // 증강 선택 화면이 뜨면 바로 걷어낸다(겹쳐 보이지 않게).
            if (group.alpha > 0f)
                group.alpha = match != null && match.Phase == MatchPhase.AugmentSelect
                    ? 0f
                    : Mathf.MoveTowards(group.alpha, 0f, Time.unscaledDeltaTime * 4f);
            return;
        }

        // 처치 연출(히트스톱/슬로우/날아가기)이 끝나고 나서 띄운다.
        if (RoundFinishController.Instance != null && RoundFinishController.Instance.IsPlaying)
            return;

        int key = match.MatchNumber * 100 + match.TotalWins;
        if (key != shownKey)
        {
            shownKey = key;
            routine = StartCoroutine(Show(match));
        }
    }

    // ---------------- 흐름 ----------------

    private IEnumerator Show(MatchManager match)
    {
        CharacterBase local = CharacterBase.LocalPlayer;
        PlayerRef me = local != null ? local.MatchPlayer : PlayerRef.None;
        bool won = match.LastRoundWinner == me;
        bool localLeft = local != null && match.IsPlayer1(me);
        int[] wins = { match.Player1Wins, match.Player2Wins };
        int winnerSide = match.IsPlayer1(match.LastRoundWinner) ? 0 : 1;

        // 승자의 이번 칸은 아직 비워 둔 상태로 시작한다.
        for (int side = 0; side < 2; side++)
        {
            int shown = side == winnerSide ? wins[side] - 1 : wins[side];
            for (int i = 0; i < slots[side].Count; i++)
            {
                gems[side][i].enabled = i < shown;
                gems[side][i].rectTransform.localScale = Vector3.one;
            }
            sideLabels[side].text = (side == 0) == localLeft ? "YOU" : "ENEMY";
            sideLabels[side].color = side == 0 ? player1Color : player2Color;
        }

        Color c = won ? winColor : loseColor;
        title.text = won ? "ROUND WIN" : "ROUND LOSE";
        title.colorGradient = new VertexGradient(Color.white, Color.white, c, c);
        plusOne.alpha = 0f;

        // 1) 띠가 열리며 글자가 커다랗게 박힌다
        group.alpha = 1f;
        Play(bannerClip, 1f);
        RectTransform tr = title.rectTransform;
        yield return Tween(0.18f, t =>
        {
            float e = 1f - Mathf.Pow(1f - t, 4f);
            bandRect.localScale = new Vector3(1f, e, 1f);
            tr.localScale = Vector3.one * Mathf.Lerp(2.4f, 1f, e);
            title.alpha = e;
        });
        TwoPlayerCamera.Instance?.Shake(0.15f, 0.18f);
        StartCoroutine(Squash(tr, 0.1f, 0.22f));
        StartCoroutine(Ring(tr.anchoredPosition, c, 1100f, 0.4f, softCircle, 0.45f));
        yield return Wait(0.35f);

        // 2) 빛 구슬이 승자의 빈 칸으로 날아가 탕! 채워진다
        int slot = Mathf.Clamp(wins[winnerSide] - 1, 0, slots[winnerSide].Count - 1);
        Image target = gems[winnerSide][slot];
        Color sideColor = winnerSide == 0 ? player1Color : player2Color;
        Vector2 from = tr.anchoredPosition + new Vector2(0f, -10f);
        Vector2 to = (Vector2)root.InverseTransformPoint(target.rectTransform.position);
        Image orb = NewImage("Orb", fx, softCircle, Color.white);
        orb.rectTransform.sizeDelta = Vector2.one * 70f;
        yield return Tween(0.38f, t =>
        {
            float e = t * t * (3f - 2f * t);
            Vector2 p = Vector2.Lerp(from, to, e) + new Vector2(0f, Mathf.Sin(t * Mathf.PI) * 120f);
            orb.rectTransform.anchoredPosition = p;
            orb.color = Color.Lerp(Color.white, sideColor, t);
            orb.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(70f, 46f, t);
        });
        Destroy(orb.gameObject);

        target.enabled = true;
        Play(pointClip, 1f);
        StartCoroutine(Pop(target.rectTransform));
        StartCoroutine(Ring(to, sideColor, 260f, 0.4f));
        StartCoroutine(Ring(to, Color.white, 170f, 0.3f));
        StartCoroutine(Sparkles(to, sideColor));
        plusOne.color = sideColor;
        plusOne.rectTransform.anchoredPosition = to + new Vector2(0f, 50f);
        StartCoroutine(Tween(0.8f, t =>
        {
            plusOne.alpha = t < 0.15f ? t / 0.15f : 1f - Mathf.Clamp01((t - 0.6f) / 0.4f);
            plusOne.rectTransform.anchoredPosition = to + new Vector2(0f, 50f + t * 40f);
            plusOne.rectTransform.localScale = Vector3.one * (t < 0.15f ? Mathf.Lerp(1.8f, 1f, t / 0.15f) : 1f);
        }));
        routine = null;
    }

    // ---------------- 조각 ----------------

    private IEnumerator Ring(Vector2 at, Color color, float size, float dur, Sprite sprite = null, float alpha = 0.9f)
    {
        if (softCircle == null) yield break;
        Image ring = NewImage("Ring", fx, sprite != null ? sprite : ringSprite, color);
        ring.rectTransform.anchoredPosition = at;
        yield return Tween(dur, t =>
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            ring.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(size * 0.2f, size, e);
            ring.color = new Color(color.r, color.g, color.b, (1f - t) * alpha);
        });
        Destroy(ring.gameObject);
    }

    private IEnumerator Sparkles(Vector2 at, Color color)
    {
        var bits = new List<(Image img, Vector2 v)>();
        for (int i = 0; i < 12; i++)
        {
            Image b = NewImage("Spark", fx, softCircle, Color.white);
            b.rectTransform.anchoredPosition = at;
            b.rectTransform.sizeDelta = Vector2.one * Random.Range(10f, 18f);
            float a = i / 12f * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            bits.Add((b, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(220f, 380f)));
        }
        yield return Tween(0.5f, t =>
        {
            foreach (var (img, v) in bits)
            {
                img.rectTransform.anchoredPosition = at + v * (1f - Mathf.Pow(1f - t, 2f)) * 0.6f;
                img.color = Color.Lerp(Color.white, color, t) * new Color(1f, 1f, 1f, 1f - t);
            }
        });
        foreach (var (img, _) in bits) Destroy(img.gameObject);
    }

    private static IEnumerator Pop(RectTransform rt)
    {
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.3f)
        {
            rt.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * (1f - t * 0.4f) * 0.7f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private static IEnumerator Squash(RectTransform rt, float amount, float duration)
    {
        Vector3 s = rt.localScale;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
        {
            float k = Mathf.Sin(t * Mathf.PI) * (1f - t) * amount * 2f;
            rt.localScale = new Vector3(s.x * (1f + k), s.y * (1f - k), s.z);
            yield return null;
        }
        rt.localScale = s;
    }

    private static IEnumerator Tween(float duration, System.Action<float> step)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            step(Mathf.Clamp01(t / duration));
            yield return null;
        }
        step(1f);
    }

    private static IEnumerator Wait(float seconds)
    {
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end)
            yield return null;
    }

    private void Play(AudioClip clip, float scale) => SoundManager.Instance?.PlaySfx(clip, volume * scale);

    // ---------------- 만들기 ----------------

    private void Build()
    {
        var go = new GameObject("Round Result Canvas");
        go.transform.SetParent(transform, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 840;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        root = (RectTransform)go.transform;
        group = go.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        bandRect = NewRect("Band", root);
        bandRect.anchoredPosition = new Vector2(0f, 60f);
        NewImage("Fill", bandRect, null, new Color(0.02f, 0.03f, 0.1f, 0.82f)).rectTransform.sizeDelta = new Vector2(4000f, 380f);
        for (int i = 0; i < 2; i++)
        {
            Image edge = NewImage("Edge", bandRect, null, new Color(1f, 1f, 1f, 0.55f));
            edge.rectTransform.sizeDelta = new Vector2(4000f, 4f);
            edge.rectTransform.anchoredPosition = new Vector2(0f, i == 0 ? 190f : -190f);
        }

        title = NewText("Title", 150f, 6f);
        title.rectTransform.anchoredPosition = new Vector2(0f, 120f);

        // 점수판: [라벨] ○ ○ ○    ○ ○ ○ [라벨]
        for (int side = 0; side < 2; side++)
        {
            float sign = side == 0 ? -1f : 1f;
            for (int i = 0; i < MatchManager.WinsRequired; i++)
            {
                // 가운데에서 바깥으로 1칸, 2칸, 3칸 → 왼쪽은 오른쪽부터 채워지는 게 아니라 바깥쪽부터 채운다
                float x = sign * (300f - i * 90f);
                Image ring = NewImage("Slot", root, ringSprite, new Color(1f, 1f, 1f, 0.85f));
                ring.rectTransform.sizeDelta = Vector2.one * 66f;
                ring.rectTransform.anchoredPosition = new Vector2(x, -30f);
                Image gem = NewImage("Gem", ring.rectTransform, gemSprite, side == 0 ? player1Color : player2Color);
                gem.rectTransform.sizeDelta = Vector2.one * 52f;
                slots[side].Add(ring);
                gems[side].Add(gem);
            }
            var label = NewText("Side Label", 44f, 4f);
            label.rectTransform.anchoredPosition = new Vector2(sign * 470f, -30f);
            label.enableVertexGradient = false;
            sideLabels[side] = label;
        }
        var vs = NewText("Dash", 46f, 0f);
        vs.text = "-";
        vs.rectTransform.anchoredPosition = new Vector2(0f, -30f);

        plusOne = NewText("+1", 70f, 0f);
        plusOne.text = "+1";
        plusOne.enableVertexGradient = false;

        fx = NewRect("FX", root);
        fx.anchorMin = fx.anchorMax = new Vector2(0.5f, 0.5f);
    }

    private TextMeshProUGUI NewText(string name, float size, float spacing)
    {
        var t = NewRect(name, root).gameObject.AddComponent<TextMeshProUGUI>();
        t.rectTransform.sizeDelta = new Vector2(1600f, 220f);
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        if (font != null) t.font = font;
        if (fontMaterial != null) t.fontSharedMaterial = fontMaterial;
        t.fontStyle = FontStyles.Italic;
        t.fontSize = size;
        t.characterSpacing = spacing;
        t.color = Color.white;
        t.enableVertexGradient = true;
        return t;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
    {
        Image img = NewRect(name, parent).gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>점수 칸(빈 고리)과 채움(빛나는 보석)을 코드로 그린다. 크기를 키워도 가장자리가 깨끗하게 매끈한 원.</summary>
    private static Sprite MakeCircle(bool filled)
    {
        const int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float aa = 2f / n;
            float a; float l;
            if (filled)
            {
                a = Mathf.Clamp01((1f - r) / aa);
                // 위쪽이 밝은 하이라이트가 있는 보석
                l = Mathf.Lerp(0.75f, 1f, Mathf.Clamp01(-dy * 0.6f + 0.5f)) + Mathf.Clamp01(1f - new Vector2(dx + 0.3f, dy - 0.35f).magnitude * 3f) * 0.6f;
            }
            else
            {
                float ring = 1f - Mathf.Abs(r - 0.86f) / 0.12f;
                a = Mathf.Clamp01(ring / (aa * 4f)) * Mathf.Clamp01((1f - r) / aa);
                a = Mathf.Max(a, Mathf.Clamp01((0.8f - r) / aa) * 0.35f); // 안쪽은 어두운 반투명
                l = r > 0.74f ? 1f : 0.05f;
            }
            byte v = (byte)(Mathf.Clamp01(l) * 255f);
            px[y * n + x] = new Color32(v, v, v, (byte)(a * 255f));
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
}
