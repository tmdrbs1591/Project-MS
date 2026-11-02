using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 증강 선택 화면의 연출 담당(AugmentSelectUI가 부른다). 패널 애니메이터 대신 코드로 그린다.
///   1) 화면이 어둑해지고, 카드팩이 빙글 돌며 아래에서 튀어 올라와 쿵 착지 → 뒤에서 후광(FIGHT 후광 이펙트)이 차오른다
///   2) 팩이 점점 떨리며 기를 모으고(빛 알갱이가 빨려 들어감)
///   3) 펑! 섬광·마법 구름과 함께 윗부분이 찢겨 날아가고, 카드가 하나씩 빙글빙글 돌며 촤라락 제자리로 날아간다
///   4) 카드가 다 놓이면 제목/타이머가 위에서 내려온다. 마우스를 올린 카드 뒤에는 카드 빛 이펙트가 감돈다
///   5) 고르면 그 카드 뒤로 팩 색 후광과 카드 빛이 터지고, 나머지 카드는 기울며 사라진다
/// 후광/구름/카드 빛은 이펙트 프리팹을 쓴다. 파티클이 카드 "뒤"에 그려지도록 패널을 별도 캔버스로 올리고,
/// 그 사이에 후광을 끼워 넣는다(배경 어둡게 → 후광 → 카드/팩 → 앞쪽 이펙트 순).
/// 모든 연출은 실제 시간(unscaled) 기준.
///
/// [씬 설정]
///   - AugmentSelectUI와 같은 오브젝트에 붙이고 팩 이미지/이펙트 프리팹/소리를 연결한다.
/// </summary>
public class AugmentSelectFx : MonoBehaviour
{
    /// <summary>이펙트 프리팹 하나와 크기(nativeSize = 크기 1일 때 월드 지름, uiSize = 화면에 띄울 캔버스 픽셀 지름).</summary>
    [Serializable]
    public struct FxEntry
    {
        public GameObject prefab;
        public float nativeSize;
        public float uiSize;
    }

    [Header("팩")]
    [SerializeField] private RectTransform packRoot;
    [SerializeField] private Image packFull;
    [SerializeField] private Image packOpen;
    [SerializeField] private Image packFlap;
    [Tooltip("코드 연출에서는 쓰지 않으므로 꺼 두는 오브젝트들(예전 애니메이터용 장식).")]
    [SerializeField] private GameObject[] hideOnIntro;
    [SerializeField] private float packRestY = 0f;
    [Tooltip("카드가 제자리에 놓였을 때 크기(예전 애니메이터 마지막 값).")]
    [SerializeField] private float cardScale = 1.18f;

    [Header("머리말(카드가 다 놓인 뒤 위에서 내려온다)")]
    [SerializeField] private RectTransform[] headerParts;

    [Header("이펙트 프리팹")]
    [Tooltip("팩/고른 카드 뒤에서 길게 뻗는 후광(VS 화면 맵 액자 뒤와 같은 TreasureChestGlowRays). 화면을 향해 사방으로 뻗게 돌려 쓴다.")]
    [SerializeField] private GameObject haloPrefab;
    [Tooltip("후광 프리팹을 크기 1로 놓았을 때 지름(월드 단위).")]
    [SerializeField] private float haloNativeSize = 8.6f;
    [SerializeField] private float haloIntensity = 2.2f;
    [SerializeField] private float haloDensity = 3f;
    [Tooltip("개봉 순간 앞쪽에서 터지는 반짝임/고리 이펙트들.")]
    [SerializeField] private FxEntry[] openBursts;
    [Tooltip("고르는 동안 화면에 은은하게 떠다니는 반짝임(카드 뒤).")]
    [SerializeField] private FxEntry[] ambient;
    [Tooltip("화면 반짝임의 퍼지는 범위 배율(입자 크기는 그대로 두고 범위만 넓힌다).")]
    [SerializeField] private float ambientSpread = 4f;
    [Tooltip("고른 순간 앞쪽에서 터지는 이펙트들.")]
    [SerializeField] private FxEntry[] pickBursts;
    [Header("카드 테두리(카드 모양 그대로 빛남)")]
    [SerializeField] private Shader outlineShader;
    [Tooltip("테두리 두께(카드 그림 텍스처 픽셀 기준).")]
    [SerializeField] private float outlineWidth = 34f;

    [Header("재료")]
    [SerializeField] private Sprite softCircle;
    [SerializeField] private Color accent = new Color(1f, 0.82f, 0.4f, 1f);
    [SerializeField] private Color packGlow = new Color(0.62f, 0.45f, 1f, 1f);
    [Range(0f, 1f)] [SerializeField] private float dimAlpha = 0.62f;

    [Header("사운드")]
    [SerializeField] private AudioClip landClip;
    [SerializeField] private AudioClip chargeClip;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip cardClip;
    [SerializeField] private AudioClip pickClip;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.9f;

    [Header("타이밍(초)")]
    [SerializeField] private float riseTime = 0.42f;
    [SerializeField] private float chargeTime = 0.55f;
    [SerializeField] private float cardFlyTime = 0.42f;
    [SerializeField] private float cardStagger = 0.14f;

    private Canvas rootCanvas;
    private Canvas panelCanvas;
    private RectTransform front;
    private Image dim;
    private Coroutine dimRoutine;
    private readonly Dictionary<RectTransform, Vector2> headerBase = new Dictionary<RectTransform, Vector2>();
    // 카드 제자리(부모 기준 로컬 좌표). 처음 본 값을 계속 쓴다 — 중간에 끊긴 연출이나 카메라 이동으로 어긋난 값을 다시 잡지 않게.
    private readonly Dictionary<RectTransform, Vector3> cardHome = new Dictionary<RectTransform, Vector3>();
    private IList<AugmentChoiceSlot> cards;
    private readonly Dictionary<AugmentChoiceSlot, Image> outlines = new Dictionary<AugmentChoiceSlot, Image>();
    private readonly HashSet<AugmentChoiceSlot> pickFlashing = new HashSet<AugmentChoiceSlot>();
    private static readonly int WidthId = Shader.PropertyToID("_Width");
    private const float OutlineInflate = 0.1f;
    private bool introPlaying;

    // 루트 캔버스 기준 그리는 순서: 어둡게(-10) → 후광 파티클(-5) → 일반 UI(0) → 카드 패널(10) → 앞쪽 이펙트(15)
    private const int DimOrder = -10, HaloOrder = -5, PanelOrder = 10, FrontOrder = 15;

    /// <summary>첫 등장 연출 전체 길이(조작 가능해지는 시점).</summary>
    // 마지막 카드의 착지 출렁임(Squash 0.18초)까지 끝난 뒤에 슬롯이 기준값을 잡아야 카드가 찌그러진 채 굳지 않는다.
    public float IntroDuration(int count) => riseTime + chargeTime + 0.08f + cardStagger * Mathf.Max(0, count - 1) + cardFlyTime + 0.3f;

    private void Awake()
    {
        foreach (RectTransform rt in headerParts)
            if (rt != null) headerBase[rt] = rt.anchoredPosition;
    }

    // ───────────────────────── 첫 등장 ─────────────────────────

    public void PlayIntro(IList<AugmentChoiceSlot> slots)
    {
        StopAllCoroutines();
        Setup();
        cards = slots;
        ResetOutlines();
        StartCoroutine(Intro(slots));
    }

    /// <summary>증강 선택 화면이 닫힐 때.</summary>
    public void Hide()
    {
        StopAllCoroutines();
        introPlaying = false;
        ResetOutlines();
        StopAmbient();
        foreach (GameObject go in liveFx) if (go != null) Destroy(go);
        liveFx.Clear();
        if (front != null) foreach (Transform c in front) Destroy(c.gameObject);
        if (dim != null) dim.gameObject.SetActive(false);
    }

    private IEnumerator Intro(IList<AugmentChoiceSlot> slots)
    {
        introPlaying = true;
        foreach (GameObject g in hideOnIntro) if (g != null) g.SetActive(false);
        SetHeader(0f, 1f);
        FadeDim(dimAlpha, 0.3f);

        // 카드는 제자리 값을 기억해 두고 숨긴다.
        var rest = new List<(RectTransform rt, Vector3 pos, CanvasGroup cg)>();
        foreach (AugmentChoiceSlot c in slots)
        {
            var rt = (RectTransform)c.transform;
            var cg = c.GetComponent<CanvasGroup>();
            if (!cardHome.TryGetValue(rt, out Vector3 home))
                cardHome[rt] = home = rt.localPosition;
            rest.Add((rt, home, cg));
            cg.alpha = 0f;
        }

        packRoot.gameObject.SetActive(true);
        packFull.enabled = true;
        packOpen.enabled = false;
        packFlap.enabled = false;
        packRoot.localScale = Vector3.one;
        Vector2 restPos = new Vector2(packRoot.anchoredPosition.x, packRestY);

        // 1) 빙글 돌며 튀어 올라와 착지
        Play(cardClip, 0.6f);
        yield return Tween(riseTime, t =>
        {
            float e = OutBack(t, 1.6f);
            packRoot.anchoredPosition = Vector2.LerpUnclamped(restPos + new Vector2(0f, -1150f), restPos, e);
            packRoot.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-200f, 0f, 1f - Mathf.Pow(1f - t, 3f)));
            float s = Mathf.Lerp(0.5f, 1f, t);
            packRoot.localScale = new Vector3(s, s, 1f);
        });
        packRoot.localEulerAngles = Vector3.zero;
        Play(landClip);
        StartCoroutine(Squash(packRoot, 0.12f, 0.22f));
        StartCoroutine(Sparkles(ToFront(packRoot.position), accent, 10, 260f, 0.4f));
        // 팩 뒤에서 길게 뻗는 후광이 차오른다
        GameObject packHalo = SpawnHalo(packRoot.position, 1500f, Color.Lerp(packGlow, Color.white, 0.2f), life: 0f);

        // 2) 기 모으기: 떨림이 커지고 후광도 점점 커진다
        Play(chargeClip);
        StartCoroutine(Converge(ToFront(packRoot.position), accent, 22, chargeTime));
        float haloBase = packHalo != null ? packHalo.transform.localScale.x : 1f;
        yield return Tween(chargeTime, t =>
        {
            float shake = t * t * 14f;
            packRoot.anchoredPosition = restPos + new Vector2(UnityEngine.Random.Range(-shake, shake), UnityEngine.Random.Range(-shake, shake) * 0.5f);
            packRoot.localEulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(-1f, 1f) * t * 4f);
            float s = 1f + t * 0.1f;
            packRoot.localScale = new Vector3(s, s, 1f);
            if (packHalo != null) packHalo.transform.localScale = Vector3.one * haloBase * (1f + t * 0.5f);
        });
        packRoot.anchoredPosition = restPos;
        packRoot.localEulerAngles = Vector3.zero;

        // 3) 펑! 개봉: 후광이 확 커지며 금빛으로 터지고, 반짝임·고리가 앞에서 터진다
        Play(openClip);
        packFull.enabled = false;
        packOpen.enabled = true;
        StartCoroutine(Flash(0.22f, 0.22f));
        StopFx(packHalo, 0.2f);
        SpawnHalo(packRoot.position, 2200f, accent, life: 1.1f);
        foreach (FxEntry e in openBursts)
            SpawnFx(e.prefab, packRoot.position, e.uiSize, e.nativeSize, HaloOrder, Color.white, 1f, 1f, null, life: 1.6f);   // 카드가 날아올 때 가리지 않게 뒤에서
        StartCoroutine(Sparkles(ToFront(packRoot.position), accent, 26, 620f, 0.7f));
        // 고르는 동안 화면에 은은한 반짝임이 떠다닌다
        StartAmbient();
        StartCoroutine(Flap());
        StartCoroutine(Squash(packRoot, -0.15f, 0.2f));
        yield return Wait(0.08f);

        StartCoroutine(PackFall(restPos));

        // 카드: 팩에서 하나씩 빙글빙글 돌며 촤라락 제자리로
        for (int i = 0; i < rest.Count; i++)
        {
            StartCoroutine(CardFly(rest[i].rt, rest[i].cg, packRoot, rest[i].pos));
            Play(cardClip, 0.85f);
            yield return Wait(cardStagger);
        }
        yield return Wait(cardFlyTime - cardStagger + 0.05f);

        // 4) 머리말이 위에서 내려온다
        yield return Tween(0.3f, t => SetHeader(t, 1f - t));
        introPlaying = false;
    }

    private IEnumerator CardFly(RectTransform rt, CanvasGroup cg, Transform source, Vector3 to)
    {
        // 카메라가 움직여도(캔버스가 카메라를 따라감) 어긋나지 않게 전부 부모 기준 로컬 좌표로 움직인다.
        Vector3 scale = Vector3.one * cardScale;
        Vector3 from = rt.parent.InverseTransformPoint(source.position);
        from.z = to.z;
        float side = Mathf.Sign(to.x - from.x + 0.0001f);
        Vector3 arc = new Vector3(0f, 260f, 0f);
        cg.alpha = 1f;
        yield return Tween(cardFlyTime, t =>
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            rt.localPosition = Vector3.LerpUnclamped(from, to, e) + arc * Mathf.Sin(e * Mathf.PI) * 0.6f;
            float flip = Mathf.Abs(Mathf.Cos((1f - e) * Mathf.PI * 2f));   // 빙글빙글 도는 느낌(가로 폭이 접혔다 펴짐)
            float s = Mathf.Lerp(0.25f, 1f, e) * (1f + Mathf.Sin(t * Mathf.PI) * 0.12f);
            rt.localScale = new Vector3(scale.x * s * Mathf.Max(0.04f, flip), scale.y * s, scale.z);
            rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-side * 35f, 0f, e));
        });
        rt.localPosition = to;
        rt.localScale = scale;
        rt.localEulerAngles = Vector3.zero;
        StartCoroutine(Sparkles(ToFront(rt.position), Color.white, 8, 200f, 0.35f));
        StartCoroutine(Squash(rt, 0.08f, 0.18f));
    }

    private IEnumerator Flap()
    {
        packFlap.enabled = true;
        RectTransform f = packFlap.rectTransform;
        Vector2 start = f.anchoredPosition;
        Color c = packFlap.color;
        yield return Tween(0.5f, t =>
        {
            f.anchoredPosition = start + new Vector2(-260f * t, 420f * t - 520f * t * t);
            f.localEulerAngles = new Vector3(0f, 0f, 220f * t);
            packFlap.color = new Color(c.r, c.g, c.b, 1f - t);
        });
        packFlap.enabled = false;
        f.anchoredPosition = start;
        f.localEulerAngles = Vector3.zero;
        packFlap.color = c;
    }

    private IEnumerator PackFall(Vector2 restPos)
    {
        yield return Tween(0.4f, t =>
        {
            packRoot.anchoredPosition = restPos + new Vector2(0f, -1100f * t * t);
            packRoot.localEulerAngles = new Vector3(0f, 0f, 25f * t);
            float s = Mathf.Lerp(1.05f, 0.7f, t);
            packRoot.localScale = new Vector3(s, s, 1f);
        });
        packRoot.gameObject.SetActive(false);
        packRoot.anchoredPosition = restPos;
        packRoot.localEulerAngles = Vector3.zero;
        packRoot.localScale = Vector3.one;
    }

    // ───────────────────────── 호버 / 공개 / 선택 ─────────────────────────

    private void Update()
    {
        if (cards == null)
            return;

        // 카드 모양 그대로 빛나는 테두리: 마우스를 올린 카드만 서서히 켜지고 은은하게 숨 쉰다.
        foreach (AugmentChoiceSlot c in cards)
        {
            if (c == null || !c.gameObject.activeInHierarchy || pickFlashing.Contains(c)) continue;
            Image o = Outline(c);
            if (o == null) continue;
            float target = !introPlaying && c.IsHovered ? 1f : 0f;
            float a = Mathf.MoveTowards(o.color.a, target, Time.unscaledDeltaTime * 7f);
            Color pc = PackColor(c);
            o.color = new Color(pc.r, pc.g, pc.b, a);
            o.material.SetFloat(WidthId, outlineWidth * (1f + 0.12f * Mathf.Sin(Time.unscaledTime * 6f)));
        }
    }

    private void ClearHover()
    {
        if (cards == null) return;
        foreach (AugmentChoiceSlot c in cards)
            if (c != null && outlines.TryGetValue(c, out Image o) && o != null && !pickFlashing.Contains(c))
                o.color = new Color(o.color.r, o.color.g, o.color.b, 0f);
    }

    /// <summary>선택 번쩍임이 중간에 끊겨도(화면 닫힘 등) 테두리가 남지 않게 모두 끈다.</summary>
    private void ResetOutlines()
    {
        pickFlashing.Clear();
        foreach (var kv in outlines)
        {
            if (kv.Value == null) continue;
            kv.Value.color = new Color(1f, 1f, 1f, 0f);
            kv.Value.material.SetFloat(WidthId, outlineWidth);
            kv.Value.material.SetFloat("_Glow", 0.7f);
        }
    }

    private Color PackColor(AugmentChoiceSlot c)
    {
        Color pc = c.CurrentEntry.SourcePack != null ? c.CurrentEntry.SourcePack.themeColor : accent;
        return Color.Lerp(pc, Color.white, 0.35f);
    }

    /// <summary>카드 배경 그림과 같은 모양의 테두리 이미지(카드 자식, 카드보다 조금 크게).</summary>
    private Image Outline(AugmentChoiceSlot c)
    {
        if (outlines.TryGetValue(c, out Image o) && o != null) return o;
        Image src = c.GetComponent<Image>();
        if (src == null || src.sprite == null || outlineShader == null) return null;
        o = NewImage("Card Outline", c.transform, src.sprite, new Color(1f, 1f, 1f, 0f));
        RectTransform rt = o.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        Vector2 size = ((RectTransform)c.transform).rect.size;
        rt.offsetMin = -size * OutlineInflate;
        rt.offsetMax = size * OutlineInflate;
        rt.SetAsFirstSibling();
        o.material = new Material(outlineShader);
        o.material.SetFloat("_Inflate", OutlineInflate);
        o.material.SetFloat(WidthId, outlineWidth);
        outlines[c] = o;
        return o;
    }

    private IEnumerator PickOutline(AugmentChoiceSlot c, Color color)
    {
        Image o = Outline(c);
        if (o == null) yield break;
        pickFlashing.Add(c);
        // 번쩍! 하얗게 두껍게 터졌다가 팩 색으로 가늘어진다
        yield return Tween(0.4f, t =>
        {
            Color col = Color.Lerp(Color.white, color, t);
            o.color = new Color(col.r, col.g, col.b, 1f);
            o.material.SetFloat(WidthId, Mathf.Lerp(outlineWidth * 1.5f, outlineWidth, t));
            o.material.SetFloat("_Glow", Mathf.Lerp(1.3f, 0.7f, t));
        });
        o.color = new Color(color.r, color.g, color.b, 0f);
        o.material.SetFloat("_Glow", 0.7f);
        pickFlashing.Remove(c);
    }

    /// <summary>두 번째 픽에서 새 카드가 펼쳐질 때 반짝임.</summary>
    public void PlayReveal(AugmentChoiceSlot card, float delay)
    {
        Setup();
        if (outlines.TryGetValue(card, out Image ol) && ol != null) { pickFlashing.Remove(card); ol.color = new Color(1f, 1f, 1f, 0f); ol.material.SetFloat(WidthId, outlineWidth); }
        StartCoroutine(RevealBurst(card, delay));
    }

    private IEnumerator RevealBurst(AugmentChoiceSlot card, float delay)
    {
        yield return Wait(delay);
        Play(cardClip, 0.8f);
        yield return Wait(0.12f);
        StartCoroutine(Sparkles(ToFront(card.transform.position), Color.white, 10, 220f, 0.4f));
    }

    /// <summary>고른 카드 뒤로 팩 색 후광과 카드 빛이 터지고, 나머지 카드는 기울며 사라진다.</summary>
    public void PlayPick(AugmentChoiceSlot picked, IEnumerable<AugmentChoiceSlot> others, Color packColor)
    {
        Setup();
        ClearHover();
        Play(pickClip);
        Vector3 at = picked.transform.position;
        Color c = Color.Lerp(packColor, Color.white, 0.25f);
        SpawnHalo(at, 1900f, c, life: 0.9f);
        StartCoroutine(PickOutline(picked, c));
        foreach (FxEntry e in pickBursts)
            SpawnFx(e.prefab, at, e.uiSize, e.nativeSize, HaloOrder, Color.white, 1f, 1f, null, life: 1.4f);   // 카드 뒤에서 터진다
        StartCoroutine(Sparkles(ToFront(at), c, 24, 520f, 0.6f));
        TwoPlayerCamera.Instance?.Shake(0.12f, 0.15f);
        foreach (AugmentChoiceSlot o in others)
            StartCoroutine(Tilt((RectTransform)o.transform, Mathf.Sign(o.transform.position.x - at.x + 0.0001f)));
    }

    private IEnumerator Tilt(RectTransform rt, float side)
    {
        yield return Tween(0.25f, t => rt.localEulerAngles = new Vector3(0f, 0f, -side * 14f * t));
        yield return Wait(0.25f);
        rt.localEulerAngles = Vector3.zero;
    }

    // ───────────────────────── 이펙트 프리팹 ─────────────────────────

    /// <summary>캔버스 위 위치(world)에 이펙트 프리팹을 uiSize(캔버스 픽셀) 크기로 띄운다.
    /// order는 루트 캔버스 기준 상대 순서(5 = 카드 뒤, 15 = 카드 앞).</summary>
    private GameObject SpawnFx(GameObject prefab, Vector3 world, float uiSize, float nativeSize, int order, Color tint,
        float alpha, float speed, string[] strip, float life = 3f)
    {
        if (prefab == null || rootCanvas == null)
            return null;
        Camera cam = rootCanvas.worldCamera;
        // 부모 없이 띄워야 캔버스 쪽 축소 배율이 겹쳐 곱해지지 않는다.
        GameObject go = Instantiate(prefab, world, cam != null ? cam.transform.rotation : Quaternion.identity);
        if (strip != null)
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                if (t != go.transform && Array.IndexOf(strip, t.name) >= 0)
                    t.gameObject.SetActive(false);
        go.transform.localScale = Vector3.one * (uiSize * rootCanvas.transform.lossyScale.x / Mathf.Max(nativeSize, 0.001f));
        foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.useUnscaledTime = true;
            main.simulationSpeed *= speed;
            if (life > 0f) main.loop = false;   // 수명이 정해진 이펙트는 한 번만 터진다
            Color c = main.startColor.color;
            main.startColor = new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a * alpha);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = UrpParticleMaterial.Convert(r.sharedMaterial);   // 옛 셰이더는 그리는 순서를 무시해 카드 앞에 나온다
            r.sortingLayerID = rootCanvas.sortingLayerID;
            r.sortingOrder = rootCanvas.sortingOrder + order;
            ps.Play(true);
        }
        if (life > 0f)
            StartCoroutine(DestroyLater(go, life / Mathf.Max(speed, 0.1f)));   // 화면이 멈춰 있어도(타임스케일 0) 실제 시간으로 지운다
        liveFx.Add(go);
        return go;
    }

    /// <summary>길게 뻗는 후광. 원래 위로 뻗는 빛줄기라 화면을 향하게 돌리고 퍼짐 각을 넓혀 사방으로 뻗게 한다(VS 화면과 같은 방식).</summary>
    private GameObject SpawnHalo(Vector3 world, float uiSize, Color tint, float life)
    {
        GameObject go = SpawnFx(haloPrefab, world, uiSize, haloNativeSize, HaloOrder, tint, 1f, 1f, null, life: 0f);
        if (go == null) return null;
        Camera cam = rootCanvas.worldCamera;
        if (cam != null) go.transform.rotation = Quaternion.LookRotation(-cam.transform.forward);
        foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            var shape = ps.shape;
            if (ps.GetComponent<ParticleSystemRenderer>().renderMode == ParticleSystemRenderMode.Stretch)
                shape.angle = 82f;
            var main = ps.main;
            Color c = main.startColor.color;
            main.startColor = new Color(c.r, c.g, c.b, Mathf.Clamp01(c.a * haloIntensity));
            var emission = ps.emission;
            emission.rateOverTimeMultiplier *= haloDensity;
            // 빛줄기가 천천히 하나씩 생기는 효과라, 미리 채워 둔 상태로 시작해야 바로 후광이 보인다.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            main.loop = true;
            main.prewarm = true;
            ps.Play(true);
        }
        // 수명이 정해졌으면 그때 방출을 멈춰, 빛줄기가 자연스럽게 사그라들게 한다.
        if (life > 0f)
            StartCoroutine(StopLater(go, life));
        return go;
    }

    private static IEnumerator DestroyLater(GameObject go, float seconds)
    {
        yield return Wait(seconds);
        if (go != null) Destroy(go);
    }

    private static IEnumerator StopLater(GameObject go, float seconds)
    {
        yield return Wait(seconds);
        StopFx(go, 0.6f);
    }

    /// <summary>방출을 멈추고 남은 입자가 사라지면 지운다.</summary>
    private static void StopFx(GameObject go, float after)
    {
        if (go == null) return;
        foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>())
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(go, after + 1.5f);
    }

    private readonly List<GameObject> ambientLive = new List<GameObject>();
    private readonly List<GameObject> liveFx = new List<GameObject>();

    private void StartAmbient()
    {
        StopAmbient();
        if (rootCanvas == null) return;
        Vector3 center = rootCanvas.transform.position;
        foreach (FxEntry e in ambient)
        {
            GameObject go = SpawnFx(e.prefab, center, e.uiSize, e.nativeSize, HaloOrder - 1, Color.white, 0.7f, 1f, null, life: 0f);
            if (go == null) continue;
            // 입자 크기는 그대로, 퍼지는 범위와 개수만 늘려 화면 전체에 은은하게 흩뿌린다.
            foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                var shape = ps.shape;
                shape.scale *= ambientSpread;
                shape.radius *= ambientSpread;
                var em = ps.emission;
                em.rateOverTimeMultiplier *= ambientSpread * 1.5f;
                var main = ps.main;
                main.maxParticles = Mathf.Max(main.maxParticles, 300);
            }
            ambientLive.Add(go);
        }
    }

    private void StopAmbient()
    {
        foreach (GameObject go in ambientLive) StopFx(go, 0f);
        ambientLive.Clear();
    }


    // ───────────────────────── 조각 ─────────────────────────

    private IEnumerator Sparkles(Vector2 at, Color color, int count, float dist, float dur)
    {
        var bits = new List<(Image img, Vector2 v, float s)>();
        for (int i = 0; i < count; i++)
        {
            Image b = NewImage("Spark", front, softCircle, Color.white);
            float a = UnityEngine.Random.value * Mathf.PI * 2f;
            float s = UnityEngine.Random.Range(10f, 24f);
            b.rectTransform.sizeDelta = Vector2.one * s;
            bits.Add((b, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * dist * UnityEngine.Random.Range(0.5f, 1f), s));
        }
        yield return Tween(dur, t =>
        {
            float e = 1f - Mathf.Pow(1f - t, 2f);
            foreach (var (img, v, s) in bits)
            {
                img.rectTransform.anchoredPosition = at + v * e + new Vector2(0f, -80f * t * t);
                img.rectTransform.sizeDelta = Vector2.one * s * (1f - t * 0.6f);
                Color c = Color.Lerp(Color.white, color, t);
                img.color = new Color(c.r, c.g, c.b, 1f - t);
            }
        });
        foreach (var (img, _, _) in bits) Destroy(img.gameObject);
    }

    private IEnumerator Converge(Vector2 at, Color color, int count, float dur)
    {
        var bits = new List<(Image img, Vector2 from, float delay)>();
        for (int i = 0; i < count; i++)
        {
            Image b = NewImage("Mote", front, softCircle, new Color(color.r, color.g, color.b, 0f));
            b.rectTransform.sizeDelta = Vector2.one * UnityEngine.Random.Range(8f, 16f);
            float a = UnityEngine.Random.value * Mathf.PI * 2f;
            bits.Add((b, at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * UnityEngine.Random.Range(380f, 620f), UnityEngine.Random.value * 0.5f));
        }
        yield return Tween(dur, t =>
        {
            foreach (var (img, from, delay) in bits)
            {
                float k = Mathf.Clamp01((t - delay * 0.6f) / (1f - delay * 0.6f));
                img.rectTransform.anchoredPosition = Vector2.Lerp(from, at, k * k);
                img.color = new Color(color.r, color.g, color.b, Mathf.Sin(k * Mathf.PI));
            }
        });
        foreach (var (img, _, _) in bits) Destroy(img.gameObject);
    }

    private IEnumerator Flash(float alpha, float dur)
    {
        Image f = NewImage("Flash", front, null, new Color(1f, 1f, 1f, alpha));
        f.rectTransform.sizeDelta = new Vector2(4000f, 4000f);
        yield return Tween(dur, t => f.color = new Color(1f, 1f, 1f, alpha * (1f - t) * (1f - t)));
        Destroy(f.gameObject);
    }

    private void FadeDim(float to, float dur)
    {
        if (dim == null) return;
        if (dimRoutine != null) StopCoroutine(dimRoutine);
        dim.gameObject.SetActive(true);
        float from = dim.color.a;
        dimRoutine = StartCoroutine(Tween(dur, t => dim.color = new Color(0f, 0f, 0.02f, Mathf.Lerp(from, to, t))));
    }

    private void SetHeader(float alpha, float drop)
    {
        foreach (var kv in headerBase)
        {
            if (kv.Key == null) continue;
            if (!kv.Key.TryGetComponent(out CanvasGroup g)) g = kv.Key.gameObject.AddComponent<CanvasGroup>();
            g.alpha = alpha;
            kv.Key.anchoredPosition = kv.Value + new Vector2(0f, 40f * drop);
        }
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

    // ───────────────────────── 준비 ─────────────────────────

    /// <summary>패널을 별도 캔버스로 올려 그 아래에 후광 파티클을, 더 아래에 화면 어둡게를 깐다.</summary>
    private void Setup()
    {
        Transform panel = packRoot.parent;
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

        if (panelCanvas == null)
        {
            // (Unity 오브젝트는 ?? 로 비교하면 안 된다 — 없는 컴포넌트도 null이 아닌 것으로 잡힌다)
            panelCanvas = panel.GetComponent<Canvas>();
            if (panelCanvas == null)
                panelCanvas = panel.gameObject.AddComponent<Canvas>();
            if (panel.GetComponent<GraphicRaycaster>() == null)
                panel.gameObject.AddComponent<GraphicRaycaster>();
        }
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingLayerID = rootCanvas.sortingLayerID;
        panelCanvas.sortingOrder = rootCanvas.sortingOrder + PanelOrder;

        if (dim == null)
        {
            dim = NewImage("Augment Dim", panel.parent, null, new Color(0f, 0f, 0.02f, 0f));
            RectTransform rt = dim.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-200f, -200f);
            rt.offsetMax = new Vector2(200f, 200f);
            rt.SetSiblingIndex(panel.GetSiblingIndex());
            // 어둡게 깔리는 막은 별도 캔버스로 맨 아래에 두고, 후광 파티클은 그 위·UI 전체 아래에 그린다
            // (파티클이 카드보다 절대 앞에 나오지 않게).
            var dimCanvas = dim.gameObject.AddComponent<Canvas>();
            dimCanvas.overrideSorting = true;
            dimCanvas.sortingLayerID = rootCanvas.sortingLayerID;
            dimCanvas.sortingOrder = rootCanvas.sortingOrder + DimOrder;
        }

        if (front == null)
        {
            front = NewRect("FX Front", panel);
            front.anchorMin = front.anchorMax = new Vector2(0.5f, 0.5f);
        }
        front.SetAsLastSibling();
    }

    private Vector2 ToFront(Vector3 world) => front.InverseTransformPoint(world);

    private static float OutBack(float t, float s)
    {
        float x = t - 1f;
        return 1f + (s + 1f) * x * x * x + s * x * x;
    }

    private static IEnumerator Tween(float duration, Action<float> step)
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
        while (Time.unscaledTime < end) yield return null;
    }

    private void Play(AudioClip clip, float scale = 1f) => SoundManager.Instance?.PlaySfx(clip, volume * scale);

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
}
