using System.Collections;
using System.Collections.Generic;
using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 궁극기 컷인: 누가 궁극기를 쓰면 화면을 가로지르는 띠 위로 캐릭터 그림과 "ULTIMATE"가 휙 지나가고,
/// 시전자 자리에서 큰 이펙트가 터지며 화면이 번쩍·흔들린다. 강렬한 레이저 계열 효과음을 같이 낸다.
/// - CharacterBase.UltimateCast(모든 클라에서 RPC로 호출)를 받아 각자 화면에서 재생한다.
/// - 내 캐릭터는 금색 띠가 왼쪽에서, 상대는 빨간 띠가 오른쪽에서 들어온다.
/// - 띠/글자/집중선은 코드로 만든다(자체 오버레이 캔버스). 게임 진행(타임스케일)은 건드리지 않는다.
/// </summary>
public class UltimateCutIn : MonoBehaviour
{
    [System.Serializable]
    public struct WorldFx
    {
        public GameObject prefab;
        public float scale;
    }

    [Header("사운드")]
    [SerializeField] private AudioClip castClip;
    [Range(0f, 1f)] [SerializeField] private float castVolume = 1f;

    [Header("시전자 자리 이펙트")]
    [SerializeField] private WorldFx[] worldFx;
    [SerializeField] private string vfxSortingLayer = "Default";
    [SerializeField] private int vfxSortingOrder = 300;

    [Header("글자")]
    [SerializeField] private TMP_FontAsset titleFont;
    [SerializeField] private Material titleMaterial;
    [Tooltip("스킬 이름(한글)용 폰트.")]
    [SerializeField] private TMP_FontAsset nameFont;

    [Header("색")]
    [SerializeField] private Color allyColor = new Color(1f, 0.78f, 0.2f);
    [SerializeField] private Color enemyColor = new Color(1f, 0.25f, 0.28f);

    [Header("시간")]
    [SerializeField] private float bannerHold = 0.55f;
    [SerializeField] private int sortingOrder = 650;

    private RectTransform root;
    private RectTransform screen;
    private Vector2 rootBase;
    private Image flash;
    private Coroutine routine;

    private void OnEnable() => CharacterBase.UltimateCast += OnUltimate;
    private void OnDisable()
    {
        CharacterBase.UltimateCast -= OnUltimate;
        Clear();
    }

    private void OnUltimate(CharacterBase caster)
    {
        if (caster == null)
            return;
        EnsureCanvas();
        if (routine != null)
            StopCoroutine(routine);
        Clear();
        routine = StartCoroutine(Play(caster));
    }

    // ───────────────────────── 흐름 ─────────────────────────

    private IEnumerator Play(CharacterBase caster)
    {
        bool ally = caster.IsLocalPlayer;
        Color accent = ally ? allyColor : enemyColor;
        Color deep = new Color(accent.r * 0.45f, accent.g * 0.3f, accent.b * 0.35f, 1f);
        float side = ally ? -1f : 1f;   // 들어오는 쪽(-1 = 왼쪽)
        float angle = -5f * side;   // 너무 기울면 양 끝이 상단 HUD를 덮는다

        SoundManager.Instance?.PlaySfx(castClip, castVolume, 0f);
        SpawnWorldFx(caster.transform.position + Vector3.up * 0.4f);

        TwoPlayerCamera cam = TwoPlayerCamera.Instance;
        if (cam != null)
        {
            cam.Shake(0.4f, 0.45f);
            Vector2 toCaster = caster.transform.position - cam.transform.position;
            cam.PunchZoom(toCaster, 0.6f, 0.14f, 0.6f);
        }

        // ── 화면 전체: 살짝 어두워지고 가장자리에서 가운데로 집중선이 번뜩인다
        var dim = NewImage("Dim", screen, new Color(0f, 0f, 0f, 0f));
        Stretch(dim.rectTransform, 0f, 0f);
        var focus = new List<Image>();
        for (int i = 0; i < 56; i++)
        {
            var l = NewImage("Focus Line", screen, Color.white);
            l.rectTransform.pivot = new Vector2(0f, 0.5f);
            focus.Add(l);
        }

        // ── 띠: 뒤에 색 띠가 한 겹 더 깔려 두툼하게, 앞 띠는 불투명
        // (리니어 색 공간이라 낮은 알파도 생각보다 진하게 비친다 — 띠는 불투명으로.)
        var back = NewImage("Band Back", root, deep);
        back.rectTransform.sizeDelta = new Vector2(3400f, 380f);
        back.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle - 3f * side);
        var band = NewImage("Band", root, new Color(0.03f, 0.015f, 0.06f, 1f));
        band.rectTransform.sizeDelta = new Vector2(3400f, 300f);
        band.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        var bandRt = band.rectTransform;

        var top = NewImage("Stripe Top", bandRt, accent);
        top.rectTransform.anchorMin = new Vector2(0f, 1f);
        top.rectTransform.anchorMax = new Vector2(1f, 1f);
        top.rectTransform.sizeDelta = new Vector2(0f, 12f);
        var bottom = NewImage("Stripe Bottom", bandRt, accent);
        bottom.rectTransform.anchorMin = new Vector2(0f, 0f);
        bottom.rectTransform.anchorMax = new Vector2(1f, 0f);
        bottom.rectTransform.sizeDelta = new Vector2(0f, 12f);

        var lines = new List<(RectTransform rt, float speed)>();
        for (int i = 0; i < 26; i++)
        {
            bool tinted = i % 3 == 0;
            var l = NewImage("Speed Line", bandRt, tinted ? new Color(accent.r, accent.g, accent.b, Random.Range(0.25f, 0.6f)) : new Color(1f, 1f, 1f, Random.Range(0.1f, 0.35f)));
            l.rectTransform.sizeDelta = new Vector2(Random.Range(180f, 700f), Random.Range(3f, 10f));
            l.rectTransform.anchoredPosition = new Vector2(Random.Range(-1700f, 1700f), Random.Range(-135f, 135f));
            lines.Add((l.rectTransform, Random.Range(3600f, 7000f)));
        }

        // 띠 위를 한 번 쓸고 지나가는 빛
        var sweep = NewImage("Sweep", bandRt, new Color(1f, 1f, 1f, 0.85f));
        sweep.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        sweep.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        sweep.rectTransform.sizeDelta = new Vector2(60f, 0f);
        var sweep2 = NewImage("Sweep Thin", bandRt, new Color(1f, 1f, 1f, 0.6f));
        sweep2.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        sweep2.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        sweep2.rectTransform.sizeDelta = new Vector2(24f, 0f);

        // ── 캐릭터 그림 + 잔상
        Image art = null;
        var ghosts = new List<Image>();
        Sprite sprite = null;
        if (caster.Definition != null)
            sprite = caster.Definition.CharacterMainSprite != null ? caster.Definition.CharacterMainSprite : caster.Definition.Portrait;
        if (sprite != null)
        {
            for (int i = 0; i < 3; i++)
            {
                var g = NewImage("Art Ghost", root, new Color(accent.r, accent.g, accent.b, 0f));
                g.sprite = sprite;
                g.preserveAspect = true;
                g.rectTransform.sizeDelta = new Vector2(560f, 560f);
                g.transform.SetAsFirstSibling();
                ghosts.Add(g);
            }
            // 잔상은 띠보다 앞, 본 그림보다 뒤
            foreach (Image g in ghosts) g.transform.SetSiblingIndex(back.transform.GetSiblingIndex() + 2);
            art = NewImage("Art", root, Color.white);
            art.sprite = sprite;
            art.preserveAspect = true;
            art.rectTransform.sizeDelta = new Vector2(560f, 560f);
        }

        // ── 글자: 쾅 내려찍히고, 같은 글자가 크게 퍼지며 사라지는 잔향
        TMP_Text echo = NewText("ULTIMATE", root, titleFont, titleMaterial, 160f, new Color(1f, 1f, 1f, 0f));
        echo.fontStyle = FontStyles.Italic;
        echo.characterSpacing = 8f;
        TMP_Text title = NewText("ULTIMATE", root, titleFont, titleMaterial, 160f, Color.white);
        title.fontStyle = FontStyles.Italic;
        title.characterSpacing = 8f;
        title.enableVertexGradient = true;
        title.colorGradient = new VertexGradient(Color.white, Color.white, accent, accent);
        string skillName = caster.Definition != null ? caster.Definition.GetSkillInfo(CharacterActionType.Ultimate).name : null;
        TMP_Text sub = NewText(string.IsNullOrEmpty(skillName) ? "" : skillName, root, nameFont, null, 60f, accent);
        sub.fontStyle = FontStyles.Bold;

        var sparks = new List<(RectTransform rt, Vector2 vel, float spin)>();

        StartCoroutine(Flash(0.35f, 0.22f, accent));
        Vector2 artFrom = new Vector2(side * 1500f, -10f), artTo = new Vector2(side * 440f, -10f);
        Vector2 titleTo = new Vector2(-side * 150f, 42f);
        Vector2 subFrom = new Vector2(-side * 1700f, -82f), subTo = new Vector2(-side * 150f, -82f);
        const float slamAt = 0.1f, slamTime = 0.13f;
        bool slammed = false;
        float t = 0f, inTime = 0.15f, outTime = 0.22f, total = inTime + bannerHold + outTime;
        var trail = new List<Vector2>();
        while (t < total)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            float open = t < inTime ? OutBack(Mathf.Clamp01(t / inTime), 2.2f)
                       : t > total - outTime ? 1f - Mathf.SmoothStep(0f, 1f, (t - (total - outTime)) / outTime) : 1f;
            bandRt.localScale = new Vector3(1f, open, 1f);
            float openBack = t < inTime + 0.04f ? OutBack(Mathf.Clamp01((t - 0.04f) / inTime), 2.5f) : open;
            back.rectTransform.localScale = new Vector3(1f, Mathf.Max(0f, openBack), 1f);

            // 등장 순간 띠 전체가 덜덜 떨린다
            float jitter = 16f * Mathf.Max(0f, 1f - t / 0.4f);
            root.anchoredPosition = rootBase + new Vector2(Random.Range(-jitter, jitter), Random.Range(-jitter, jitter));

            float fade = t > total - outTime ? 1f - (t - (total - outTime)) / outTime : 1f;
            dim.color = new Color(0f, 0f, 0f, 0.12f * Mathf.Max(0f, 1f - t / 0.25f));   // 등장 순간만 살짝

            // 집중선: 매 프레임 새로 뿌려 번뜩이게(화면 가장자리에서 가운데 쪽으로)
            float focusA = Mathf.Max(0f, 1f - t / 0.45f) * fade;
            foreach (Image f in focus)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                RectTransform frt = f.rectTransform;
                frt.anchoredPosition = new Vector2(dir.x * 1150f, dir.y * 700f);
                frt.sizeDelta = new Vector2(Random.Range(450f, 900f), Random.Range(2f, 7f));
                frt.localEulerAngles = new Vector3(0f, 0f, ang * Mathf.Rad2Deg + 180f);
                f.color = new Color(1f, 1f, 1f, focusA * Random.Range(0.15f, 0.55f));
            }

            float slideIn = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.2f), 4f);
            float drift = Mathf.Max(0f, t - 0.2f) * 70f;   // 멈추지 않고 살짝 계속 흘러간다
            float exit = Mathf.Clamp01((t - (total - outTime)) / outTime);
            exit *= exit;
            if (art != null)
            {
                Vector2 ap = Vector2.LerpUnclamped(artFrom, artTo, slideIn) + new Vector2(-side * drift, 0f) + new Vector2(side * 1700f * exit, 0f);
                art.rectTransform.anchoredPosition = ap;
                art.rectTransform.localScale = Vector3.one * (1f + 0.2f * (1f - slideIn));
                trail.Add(ap);
                if (trail.Count > 12) trail.RemoveAt(0);
                float ghostA = Mathf.Max(0.3f, 1f - t / 0.6f) * fade;
                for (int i = 0; i < ghosts.Count; i++)
                {
                    int idx = Mathf.Max(0, trail.Count - 1 - (i + 1) * 3);
                    ghosts[i].rectTransform.anchoredPosition = trail[idx] + new Vector2(side * 34f * (i + 1), 0f);
                    ghosts[i].rectTransform.localScale = art.rectTransform.localScale * (1f + 0.05f * (i + 1));
                    ghosts[i].color = new Color(accent.r, accent.g, accent.b, 0.5f / (i + 1) * ghostA);
                }
            }

            // 글자: 크게 떠 있다가 쾅 내려찍힘
            float s = t < slamAt ? 0f : Mathf.Clamp01((t - slamAt) / slamTime);
            title.rectTransform.localScale = Vector3.one * (t < slamAt ? 0f : Mathf.LerpUnclamped(2.6f, 1f, OutBack(s, 1.4f)));
            title.alpha = t < slamAt ? 0f : Mathf.Clamp01(s * 3f);
            title.rectTransform.anchoredPosition = titleTo + new Vector2(side * drift, 0f) + new Vector2(-side * 1900f * exit, 0f);
            if (!slammed && s >= 1f)
            {
                slammed = true;
                StartCoroutine(Flash(0.18f, 0.14f, accent));
                if (cam != null) cam.Shake(0.3f, 0.25f);
                for (int i = 0; i < 26; i++)
                {
                    var sp = NewImage("Spark", root, i % 2 == 0 ? accent : Color.white);
                    float sz = Random.Range(10f, 26f);
                    sp.rectTransform.sizeDelta = new Vector2(sz, sz);
                    sp.rectTransform.anchoredPosition = titleTo + new Vector2(Random.Range(-380f, 380f), Random.Range(-40f, 40f));
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    sparks.Add((sp.rectTransform, new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.7f) * Random.Range(500f, 1300f), Random.Range(-720f, 720f)));
                }
            }
            if (slammed)
            {
                float e = Mathf.Clamp01((t - slamAt - slamTime) / 0.4f);
                echo.rectTransform.anchoredPosition = title.rectTransform.anchoredPosition;
                echo.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.7f, 1f - Mathf.Pow(1f - e, 2f));
                echo.color = new Color(accent.r, accent.g, accent.b, 0.7f * (1f - e));
            }

            float subIn = 1f - Mathf.Pow(1f - Mathf.Clamp01((t - 0.12f) / 0.24f), 4f);
            sub.rectTransform.anchoredPosition = Vector2.LerpUnclamped(subFrom, subTo, subIn) + new Vector2(side * drift, 0f) + new Vector2(-side * 1900f * exit, 0f);

            // 빛 쓸기: 등장 직후 한 번 휙
            float sw = Mathf.Clamp01((t - 0.12f) / 0.45f);
            float sx = Mathf.Lerp(side * 1800f, -side * 1800f, sw);
            sweep.rectTransform.anchoredPosition = new Vector2(sx, 0f);
            sweep2.rectTransform.anchoredPosition = new Vector2(sx + side * 70f, 0f);
            sweep.enabled = sweep2.enabled = sw > 0f && sw < 1f;
            

            foreach (var (rt, speed) in lines)
            {
                Vector2 p = rt.anchoredPosition + new Vector2(side * speed * dt, 0f);
                if (p.x > 1900f) p.x -= 3800f;
                if (p.x < -1900f) p.x += 3800f;
                rt.anchoredPosition = p;
            }

            for (int i = 0; i < sparks.Count; i++)
            {
                var (rt, vel, spin) = sparks[i];
                if (rt == null) continue;
                vel *= 1f - 3.5f * dt;
                rt.anchoredPosition += vel * dt;
                rt.localEulerAngles += new Vector3(0f, 0f, spin * dt);
                rt.localScale = Vector3.one * Mathf.Max(0f, rt.localScale.x - dt * 1.6f);
                sparks[i] = (rt, vel, spin);
            }
            yield return null;
        }

        root.anchoredPosition = rootBase;
        Clear();
        routine = null;
    }

    private static float OutBack(float t, float s)
    {
        t -= 1f;
        return t * t * ((s + 1f) * t + s) + 1f;
    }

    private IEnumerator Flash(float alpha, float dur, Color accent)
    {
        Color c = Color.Lerp(Color.white, accent, 0.25f);
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float k = 1f - t / dur;
            flash.color = new Color(c.r, c.g, c.b, alpha * k * k);
            yield return null;
        }
        flash.color = new Color(1f, 1f, 1f, 0f);
    }

    // ───────────────────────── 만들기 ─────────────────────────

    private void SpawnWorldFx(Vector3 at)
    {
        if (worldFx == null)
            return;
        at.z = 0f;
        foreach (WorldFx fx in worldFx)
        {
            if (fx.prefab == null) continue;
            GameObject go = Instantiate(fx.prefab, at, Quaternion.identity);
            go.transform.localScale *= fx.scale <= 0f ? 1f : fx.scale;
            UrpParticleMaterial.ConvertAll(go);
            foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
            foreach (ParticleSystemRenderer r in go.GetComponentsInChildren<ParticleSystemRenderer>())
            {
                r.sortingLayerName = vfxSortingLayer;
                r.sortingOrder = vfxSortingOrder;
            }
            Destroy(go, 3f);
        }
    }

    private void EnsureCanvas()
    {
        if (root != null)
            return;
        var go = new GameObject("Ultimate CutIn Canvas");
        go.transform.SetParent(transform, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        screen = new GameObject("Screen", typeof(RectTransform)).GetComponent<RectTransform>();
        screen.SetParent(go.transform, false);
        Stretch(screen, 0f, 0f);
        flash = NewImage("Flash", go.transform, new Color(1f, 1f, 1f, 0f));
        Stretch(flash.rectTransform, 0f, 0f);
        root = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(go.transform, false);
        // 화면 가운데보다 살짝 위: 캐릭터 싸움을 너무 가리지 않게
        // 화면 위쪽에 작게: 아래쪽 싸움은 그대로 보이게
        rootBase = new Vector2(0f, 225f);
        root.localScale = Vector3.one * 0.72f;
        root.anchoredPosition = rootBase;
    }

    private void Clear()
    {
        if (root != null)
            foreach (Transform c in root) Destroy(c.gameObject);
        if (screen != null)
            foreach (Transform c in screen) Destroy(c.gameObject);
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        var img = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(parent, false);
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TMP_Text NewText(string text, Transform parent, TMP_FontAsset font, Material mat, float size, Color color)
    {
        var t = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        t.transform.SetParent(parent, false);
        if (font != null) t.font = font;
        if (mat != null) t.fontSharedMaterial = mat;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        t.rectTransform.sizeDelta = new Vector2(1200f, 200f);
        return t;
    }

    private static void Stretch(RectTransform rt, float x, float y)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-x, -y);
        rt.offsetMax = new Vector2(x, y);
    }
}
