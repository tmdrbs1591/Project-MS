using System.Collections;
using System.Collections.Generic;
using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 라운드 시작 연출: "ROUND n" → "READY..." → "FIGHT!". 연출 동안 양쪽 캐릭터 조작을 막고, FIGHT가 박히는 순간 푼다.
///   - 1라운드: 대전 시작 VS 연출(VsIntroUI)이 끝나면서 Play()를 불러 이어서 튼다.
///   - 2라운드부터: 라운드가 바뀌어 전투가 시작될 때 스스로 튼다.
///   - 연습장에서는 틀지 않는다.
/// 화면은 코드로 만든다(씬에 빈 오브젝트 하나에 붙이고 폰트/스프라이트/소리만 넣으면 된다).
/// </summary>
public class RoundIntroUI : MonoBehaviour
{
    public static RoundIntroUI Instance { get; private set; }
    /// <summary>연출 중인지.</summary>
    public static bool IsPlaying { get; private set; }
    /// <summary>지금 연출이 VS 연출에서 이어진 것인지(배경음 판단용).</summary>
    public static bool FromVs { get; private set; }
    /// <summary>FIGHT 순간 켜는 연출 오브젝트(다른 연출이 이걸 미리 켜지 않게 확인용).</summary>
    public GameObject FightVfx => fightVfx;

    [Header("재료")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Material fontMaterial;
    [SerializeField] private Sprite softCircle;
    [SerializeField] private Sprite raySprite;
    [SerializeField] private Sprite diamondSprite;
    [SerializeField] private Color gold = new Color(1f, 0.86f, 0.42f, 1f);
    [SerializeField] private Color player1Color = new Color(0.3f, 0.72f, 1f, 1f);
    [SerializeField] private Color player2Color = new Color(1f, 0.36f, 0.42f, 1f);
    [Tooltip("글자 아래쪽 색(위는 흰색). FIGHT! 이미지처럼 흰색→연보라로 떨어지게.")]
    [SerializeField] private Color textBottomColor = new Color(0.86f, 0.86f, 0.95f, 1f);

    [Header("FIGHT")]
    [Tooltip("FIGHT 순간 켤 시작 연출 오브젝트(Main Camera 자식의 Vfx_UI_BattleStart). 비우면 코드로 만든 FIGHT! 글자를 쓴다.")]
    [SerializeField] private GameObject fightVfx;

    [Header("타이밍 (초)")]
    [SerializeField] private float roundHold = 0.65f;
    [SerializeField] private float readyHold = 0.7f;

    [Header("사운드")]
    [SerializeField] private AudioClip roundClip;
    [SerializeField] private AudioClip readyClip;
    [SerializeField] private AudioClip fightClip;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.9f;

    private RectTransform root;
    private CanvasGroup band;
    private RectTransform bandRect;
    private TextMeshProUGUI roundText;
    private TextMeshProUGUI readyText;
    private TextMeshProUGUI fightText;
    private RectTransform fx;
    private readonly List<Image> wins = new List<Image>();
    private Coroutine routine;
    private int playedKey = -1;

    private void Awake()
    {
        Instance = this;
        Build();
        HideAll();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        if (IsPlaying)
            Unlock();
    }

    private void Update()
    {
        if (IsPlaying)
        {
            // 늦게 등록된 캐릭터도 잠금에서 빠지지 않게 연출 동안 계속 건다.
            CharacterBase.IntroLocked = true;
            foreach (CharacterBase c in CharacterBase.All)
                c?.SetGameplayLocked(true);
            return;
        }

        // 2라운드부터는 라운드 전투가 시작되는 순간 스스로 튼다(1라운드는 VS 연출이 부른다).
        MatchManager match = MatchManager.Instance;
        if (match == null || PracticeSession.IsActive || match.Phase != MatchPhase.Fighting || match.RoundNumber <= 1)
            return;
        if (Key(match) != playedKey)
            Play(false);
    }

    private static int Key(MatchManager match) => match.MatchNumber * 100 + match.RoundNumber;

    /// <summary>라운드 시작 연출을 튼다. fromVs면 VS 연출 직후라 배경음을 바꾸지 않는다.</summary>
    public void Play(bool fromVs)
    {
        MatchManager match = MatchManager.Instance;
        if (match != null)
            playedKey = Key(match);
        FromVs = fromVs;
        IsPlaying = true;
        CharacterBase.IntroLocked = true;
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(Sequence(match != null ? Mathf.Max(1, match.RoundNumber) : 1));
    }

    private void Unlock()
    {
        IsPlaying = false;
        FromVs = false;
        CharacterBase.IntroLocked = false;
        foreach (CharacterBase c in CharacterBase.All)
            c?.SetGameplayLocked(false);
    }

    // ---------------- 흐름 ----------------

    private IEnumerator Sequence(int round)
    {
        HideAll();
        MatchManager match = MatchManager.Instance;

        // 1) ROUND n: 띠가 위아래로 쫙 열리고 글자가 날아와 박힌다 + 승리 다이아.
        roundText.text = "ROUND " + round;
        int[] w = { match != null ? match.Player1Wins : 0, match != null ? match.Player2Wins : 0 };
        for (int i = 0; i < wins.Count; i++)
        {
            int side = i / MatchManager.WinsRequired, n = i % MatchManager.WinsRequired;
            wins[i].color = n < w[side] ? (side == 0 ? player1Color : player2Color) : new Color(0.18f, 0.2f, 0.34f, 0.95f);
            wins[i].rectTransform.localScale = Vector3.zero;
        }
        Play(roundClip, 1f);
        band.alpha = 1f;
        RectTransform rt = roundText.rectTransform;
        roundText.alpha = 1f;
        yield return Tween(0.16f, t =>
        {
            float e = 1f - Mathf.Pow(1f - t, 4f);
            bandRect.localScale = new Vector3(1f, e, 1f);
            rt.anchoredPosition = new Vector2(Mathf.Lerp(1100f, 0f, e), 0f);
            rt.localScale = new Vector3(Mathf.Lerp(1.7f, 1f, e), Mathf.Lerp(0.7f, 1f, e), 1f);
        });
        TwoPlayerCamera.Instance?.Shake(0.12f, 0.15f);
        StartCoroutine(Squash(rt, 0.08f, 0.18f));
        for (int i = 0; i < wins.Count; i++)
            StartCoroutine(PopIn(wins[i].rectTransform, i * 0.05f));
        yield return Wait(roundHold);

        // 2) READY: ROUND가 왼쪽으로 빠지고, READY가 숨 쉬듯 깜빡인다.
        yield return Tween(0.14f, t =>
        {
            float e = t * t;
            rt.anchoredPosition = new Vector2(Mathf.Lerp(0f, -1100f, e), 0f);
            roundText.alpha = 1f - e;
            foreach (Image g in wins) g.color = new Color(g.color.r, g.color.g, g.color.b, 1f - e);
        });
        Play(readyClip, 0.9f);
        RectTransform rr = readyText.rectTransform;
        readyText.alpha = 1f;
        float readyStart = Time.unscaledTime;
        yield return Tween(readyHold, t =>
        {
            float since = Time.unscaledTime - readyStart;
            float pop = since < 0.15f ? Mathf.Lerp(1.4f, 1f, since / 0.15f) : 1f + Mathf.Sin(since * 9f) * 0.03f;
            rr.localScale = Vector3.one * pop;
            readyText.characterSpacing = Mathf.Lerp(30f, 12f, Mathf.Clamp01(since / 0.6f));
            bandRect.localScale = new Vector3(1f, Mathf.Lerp(1f, 0.75f, t), 1f);
        });

        // 3) FIGHT!: 띠가 닫히는 순간 만들어둔 FIGHT 연출을 터뜨리고, 이 순간 조작이 풀린다.
        readyText.alpha = 0f;
        Play(fightClip, 1f);
        Unlock();
        TwoPlayerCamera.Instance?.Shake(0.3f, 0.3f);
        StartCoroutine(Tween(0.18f, t => bandRect.localScale = new Vector3(1f, Mathf.Lerp(0.75f, 0f, t), 1f)));
        if (fightVfx != null)
        {
            StartCoroutine(Flash(0.4f, 0.2f));
            // 이미 켜져 있어도 파티클이 처음부터 다시 돌도록 껐다 켠다.
            fightVfx.SetActive(false);
            fightVfx.SetActive(true);
            yield return Wait(0.3f);
        }
        else
        {
            StartCoroutine(Flash(0.7f, 0.25f));
            StartCoroutine(Rays());
            StartCoroutine(Shockwave(0f));
            StartCoroutine(Shockwave(0.08f));
            RectTransform fr = fightText.rectTransform;
            fightText.alpha = 1f;
            yield return Tween(0.16f, t =>
            {
                float e = 1f - Mathf.Pow(1f - t, 3f);
                fr.localScale = Vector3.one * Mathf.Lerp(3f, 1f, e);
                fr.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-10f, -4f, e));
            });
            yield return Squash(fr, 0.1f, 0.2f);
            yield return Wait(0.4f);
            yield return Tween(0.25f, t =>
            {
                fr.localScale = Vector3.one * Mathf.Lerp(1f, 1.4f, t);
                fightText.alpha = 1f - t * t;
            });
        }
        HideAll();
        routine = null;
    }

    // ---------------- 조각 ----------------

    private IEnumerator Flash(float alpha, float duration)
    {
        Image img = NewImage("Flash", fx, null, new Color(1f, 1f, 1f, alpha));
        Stretch(img.rectTransform);
        yield return Tween(duration, t => img.color = new Color(1f, 1f, 1f, alpha * (1f - t) * (1f - t)));
        Destroy(img.gameObject);
    }

    private IEnumerator Rays()
    {
        if (raySprite == null)
            yield break;
        var holder = NewRect("Rays", fx);
        var imgs = new List<Image>();
        for (int i = 0; i < 18; i++)
        {
            Image r = NewImage("Ray", holder, raySprite, new Color(gold.r, gold.g, gold.b, 0.9f));
            r.rectTransform.pivot = new Vector2(0.5f, 0f);
            r.rectTransform.sizeDelta = new Vector2(i % 2 == 0 ? 90f : 50f, i % 2 == 0 ? 1400f : 1000f);
            r.rectTransform.localEulerAngles = new Vector3(0f, 0f, 360f / 18f * i);
            imgs.Add(r);
        }
        yield return Tween(0.75f, t =>
        {
            holder.localScale = Vector3.one * Mathf.Lerp(0.2f, 1.3f, 1f - Mathf.Pow(1f - t, 3f));
            holder.localEulerAngles = new Vector3(0f, 0f, t * 25f);
            foreach (Image r in imgs) r.color = new Color(gold.r, gold.g, gold.b, 0.9f * (1f - t));
        });
        Destroy(holder.gameObject);
    }

    private IEnumerator Shockwave(float delay)
    {
        if (softCircle == null)
            yield break;
        yield return Wait(delay);
        Image ring = NewImage("Shockwave", fx, softCircle, new Color(1f, 0.95f, 0.8f, 0.8f));
        yield return Tween(0.45f, t =>
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            ring.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(150f, 2600f, e);
            ring.color = new Color(1f, 0.95f, 0.8f, 0.8f * (1f - t));
        });
        Destroy(ring.gameObject);
    }

    private static IEnumerator PopIn(RectTransform rt, float delay)
    {
        yield return Wait(delay);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.22f)
        {
            const float c1 = 2f;
            float x = t - 1f;
            rt.localScale = Vector3.one * Mathf.Max(0f, 1f + (c1 + 1f) * x * x * x + c1 * x * x);
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
        var go = new GameObject("Round Intro Canvas");
        go.transform.SetParent(transform, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 850;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        root = (RectTransform)go.transform;

        bandRect = NewRect("Band", root);
        band = bandRect.gameObject.AddComponent<CanvasGroup>();
        bandRect.sizeDelta = new Vector2(4000f, 230f);
        NewImage("Fill", bandRect, null, new Color(0.02f, 0.03f, 0.1f, 0.88f)).rectTransform.sizeDelta = new Vector2(4000f, 230f);
        for (int i = 0; i < 2; i++)
        {
            Image edge = NewImage("Edge", bandRect, null, gold);
            edge.rectTransform.sizeDelta = new Vector2(4000f, 5f);
            edge.rectTransform.anchoredPosition = new Vector2(0f, i == 0 ? 115f : -115f);
        }

        roundText = NewText("Round", 140f, 8f);
        readyText = NewText("READY", 120f, 14f);
        readyText.text = "READY...";
        fightText = NewText("FIGHT!", 220f, 4f);
        fightText.text = "FIGHT!";

        for (int side = 0; side < 2; side++)
            for (int i = 0; i < MatchManager.WinsRequired; i++)
            {
                Image gem = NewImage("Win", root, diamondSprite, Color.white);
                gem.rectTransform.sizeDelta = new Vector2(44f, 44f);
                gem.rectTransform.anchoredPosition = new Vector2((side == 0 ? -1f : 1f) * (520f + i * 60f), 0f);
                wins.Add(gem);
            }

        fx = NewRect("FX", root);
        Stretch(fx);
    }

    private TextMeshProUGUI NewText(string name, float size, float spacing)
    {
        var t = NewRect(name, root).gameObject.AddComponent<TextMeshProUGUI>();
        t.rectTransform.sizeDelta = new Vector2(1800f, 300f);
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
        t.colorGradient = new VertexGradient(Color.white, Color.white, textBottomColor, textBottomColor);
        return t;
    }

    private void HideAll()
    {
        if (band == null)
            return;
        band.alpha = 0f;
        roundText.alpha = 0f;
        readyText.alpha = 0f;
        fightText.alpha = 0f;
        foreach (Image g in wins)
            g.rectTransform.localScale = Vector3.zero;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
    {
        Image img = NewRect(name, parent).gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }
}
