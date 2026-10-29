using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 선택 카드 안의 캐릭터 그림을 캐릭터마다 다르게 움직이는 연출.
/// 카드가 선택됐거나 마우스가 올라가 있을 때(CharacterSelectCard.IsHighlighted)만 움직이고,
/// 벗어나면 부드럽게 원래 자세로 돌아온다.
///
///   - Gunner: 몸이 통통 튀고 손과 총이 리듬 타듯 흔들리는 자연스러운 대기 동작
///   - Chaser: 총을 들어 겨누고 → 한 발 쏘며 반동 + 총구 섬광 → 내리는 저격 동작 반복
///   - Spark : 뒤의 전기 오라가 찌릿찌릿 떨리며 번쩍이고 몸이 감전된 듯 미세하게 떨린다
///             (오라는 평소에도 은은하게 숨쉰다)
///   - Popup : 인게임과 같은 글리치 셰이더(Custom/UI/Glitch)가 켜지고 몸이 지직거리며 튄다
///   - Zipper: 양손이 몸 주위를 빙글빙글 돌고 몸이 좌우로 흔들리며 출렁인다
///
/// [씬 설정]
///   - 캐릭터 카드(CharacterSelectCard가 붙은 오브젝트)에 붙인다.
///   - character: 캐릭터 그림 묶음. 그 아래 "Main"(실제 그림)과 "Shadow"(그림자) 그룹이 있고,
///     각 그룹 안의 이름이 같은 파츠("Body", "Arm Root_L" 등)를 함께 움직인다.
/// </summary>
public class CharacterCardAnimator : MonoBehaviour
{
    public enum Style
    {
        Gunner,
        Chaser,
        Spark,
        Popup,
        Zipper,
    }

    [SerializeField] private Style style;
    [SerializeField] private CharacterSelectCard card;
    [SerializeField] private RectTransform character;
    [Tooltip("켜면 카드 없이도 항상 움직인다(전투 시작 VS 화면의 캐릭터 그림 등).")]
    [SerializeField] private bool alwaysOn;
    [Tooltip("하이라이트될 때/풀릴 때 연출이 섞여 들어오는 속도.")]
    [Min(0.1f)] [SerializeField] private float blendSpeed = 8f;

    [Header("Chaser")]
    [Tooltip("총구 섬광으로 쓸 부드러운 원 스프라이트.")]
    [SerializeField] private Sprite flashSprite;
    [SerializeField] private Color flashColor = new Color(1f, 0.85f, 0.45f, 1f);
    [Min(0.5f)] [SerializeField] private float snipeCycle = 1.8f;

    [Header("Spark")]
    [SerializeField] private Image aura;

    [Header("Popup")]
    [Tooltip("Custom/UI/Glitch 셰이더 머티리얼. 카드마다 복사해서 쓴다.")]
    [SerializeField] private Material glitchMaterial;

    private sealed class Part
    {
        public RectTransform main;
        public RectTransform shadow;
        public Vector2 basePos;
        public float baseRot;
        public Vector3 baseScale;
    }

    private Part body;
    private Part handL;
    private Part handR;
    private Part gun;

    private float weight;
    private float time;

    private RectTransform flash;
    private Image flashImage;
    private Image auraZap;
    private Vector3 auraBaseScale;
    private float auraBaseAlpha;
    private readonly List<Graphic> glitchGraphics = new List<Graphic>();
    private float glitchBurstUntil;
    private bool wasHighlighted;

    /// <summary>실루엣(단색으로 칠한 복사본)으로 쓸 때 부른다: 몸 움직임만 남기고 색을 바꾸는 효과(전기 오라, 총구 섬광, 글리치)는 끈다.
    /// Awake 전에(꺼진 상태에서) 불러야 효과 오브젝트가 만들어지지 않는다.</summary>
    public void MakeSilhouette()
    {
        if (aura != null)
            Destroy(aura.gameObject);
        aura = null;
        flashSprite = null;
        glitchMaterial = null;
    }

    private void Awake()
    {
        if (card == null)
            card = GetComponent<CharacterSelectCard>();

        Transform main = character != null ? character.Find("Main") : null;
        Transform shadow = character != null ? character.Find("Shadow") : null;
        if (main == null)
            return;

        foreach (Transform t in main)
        {
            Part part = MakePart((RectTransform)t, shadow != null ? shadow.Find(t.name) as RectTransform : null);
            if (t.name == "Body")
                body = part;
            else if (t.name.EndsWith("_L") || t.name.EndsWith("LeftArm"))
                handL = part;
            else if (t.name.EndsWith("RightArm") || t.name == "Arm Root_R")
                handR = part;
            else if (t.name == "R_R")
                gun = part;
        }

        if (style == Style.Chaser && flashSprite != null)
            CreateFlash(main);
        if (style == Style.Spark && aura != null)
            CreateAuraZap();
        if (style == Style.Popup && glitchMaterial != null)
            SetupGlitch(main);
    }

    private void OnDisable()
    {
        weight = 0f;
        ResetPose();
    }

    private void Update()
    {
        bool highlighted = alwaysOn || (card != null && card.IsHighlighted);
        float dt = Time.unscaledDeltaTime;
        weight = Mathf.MoveTowards(weight, highlighted ? 1f : 0f, dt * blendSpeed * (highlighted ? 1f : 0.7f));
        time += dt;

        // 팝업: 하이라이트되는 순간 글리치를 한 번 세게 터뜨린다.
        if (highlighted && !wasHighlighted)
            glitchBurstUntil = time + 0.35f;
        wasHighlighted = highlighted;

        if (weight <= 0.0001f && style != Style.Spark)
        {
            ResetPose();
            UpdateGlitch(0f);
            return;
        }

        switch (style)
        {
            case Style.Gunner: AnimateGunner(); break;
            case Style.Chaser: AnimateChaser(); break;
            case Style.Spark: AnimateSpark(); break;
            case Style.Popup: AnimatePopup(); break;
            case Style.Zipper: AnimateZipper(); break;
        }
    }

    // ── 캐릭터별 동작 ───────────────────────────────────────────────────────────

    private void AnimateGunner()
    {
        float t = time;
        float bob = Mathf.Sin(t * 5f);
        Pose(body, new Vector2(0f, bob * 5f), Mathf.Sin(t * 2.5f) * 4f, new Vector2(1f + bob * 0.025f, 1f - bob * 0.025f));
        Pose(handL, new Vector2(0f, Mathf.Sin(t * 5f + 1.2f) * 7f), 0f, Vector2.one);

        Vector2 right = new Vector2(Mathf.Sin(t * 2.5f) * 3f, Mathf.Sin(t * 5f + 0.6f) * 6f);
        Pose(handR, right, 0f, Vector2.one);
        Pose(gun, right, Mathf.Sin(t * 2.5f + 0.4f) * 8f, Vector2.one);
    }

    private void AnimateChaser()
    {
        float p = Mathf.Repeat(time, snipeCycle) / snipeCycle;
        float aim = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.25f, p)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.8f, 1f, p)));
        float recoil = p >= 0.5f ? Mathf.Exp(-(p - 0.5f) * snipeCycle / 0.07f) : 0f;
        float shake = recoil * Mathf.Sin(time * 90f);

        Vector2 gunOffset = new Vector2(10f * aim - 18f * recoil, 4f * aim + 3f * recoil);
        Pose(gun, gunOffset, -4f * aim + 14f * recoil, Vector2.one);
        Pose(handR, gunOffset, 0f, Vector2.one);
        Pose(handL, new Vector2(14f * aim - 8f * recoil, 2f * aim), 0f, Vector2.one);
        Pose(body, new Vector2(-5f * recoil + shake * 2f, 0f), -4f * aim + 3f * recoil, Vector2.one);

        if (flash != null && gun != null)
        {
            flash.position = MuzzleWorldPosition(gun.main);
            float s = (0.5f + recoil) * weight;
            flash.localScale = new Vector3(s * 1.6f, s, 1f);
            Color c = flashColor;
            c.a = Mathf.Clamp01(recoil * 1.5f) * weight;
            flashImage.color = c;
        }
    }

    private void AnimateSpark()
    {
        float t = time;
        float w = weight;

        if (aura != null)
        {
            // 평소: 천천히 숨쉬기 / 하이라이트: 찌릿찌릿 떨리며 번쩍
            float idle = 0.5f + 0.5f * Mathf.Sin(t * 2.4f);
            float jitter = Mathf.PerlinNoise(t * 18f, 0.3f) - 0.5f;
            float scale = 1f + 0.05f * idle + w * (0.08f + 0.12f * jitter);
            aura.rectTransform.localScale = auraBaseScale * scale;
            aura.rectTransform.localRotation = Quaternion.Euler(0f, 0f, w * jitter * 14f);
            Color c = aura.color;
            c.a = auraBaseAlpha * Mathf.Lerp(0.7f + 0.3f * idle, 0.65f + 0.35f * Mathf.PerlinNoise(t * 25f, 7.1f), w);
            aura.color = c;

            if (auraZap != null)
            {
                float zap = Mathf.PerlinNoise(t * 14f, 3.3f) > 0.58f ? 1f : 0f;
                Color z = auraZap.color;
                z.a = zap * w * 0.85f;
                auraZap.color = z;
                auraZap.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 30f + Mathf.Floor(t * 12f) * 47f % 360f);
            }
        }

        Vector2 buzz = new Vector2(Mathf.PerlinNoise(t * 30f, 1.7f) - 0.5f, Mathf.PerlinNoise(2.9f, t * 30f) - 0.5f) * 6f;
        Pose(body, buzz, 0f, Vector2.one * (1f + 0.03f * Mathf.Sin(t * 7f)));
        Pose(handL, buzz + new Vector2(0f, Mathf.Sin(t * 6f) * 6f), 0f, Vector2.one);
        Vector2 right = buzz + new Vector2(0f, Mathf.Sin(t * 6f + 1f) * 6f);
        Pose(handR, right, 0f, Vector2.one);
        Pose(gun, right, Mathf.Sin(t * 3f) * 5f, Vector2.one);
    }

    private void AnimatePopup()
    {
        float t = time;
        bool burst = time < glitchBurstUntil;
        // 20fps로 끊어서 튀어야 "지직"거리는 느낌이 난다.
        float step = Mathf.Floor(t * 20f);
        float jumpChance = burst ? 1f : 0.25f;
        Vector2 jump = Hash(step) < jumpChance ? new Vector2(Hash(step + 1.3f) - 0.5f, Hash(step + 2.7f) - 0.5f) * (burst ? 14f : 6f) : Vector2.zero;

        Pose(body, jump + new Vector2(0f, Mathf.Sin(t * 3f) * 4f), 0f, Vector2.one);
        Pose(handL, jump * 0.5f + new Vector2(0f, Mathf.Sin(t * 3f + 1f) * 8f), Mathf.Sin(t * 3f) * 10f, Vector2.one);
        Pose(handR, jump * 0.5f + new Vector2(0f, Mathf.Sin(t * 3f + 2f) * 8f), -Mathf.Sin(t * 3f) * 10f, Vector2.one);

        UpdateGlitch(weight);
    }

    private void AnimateZipper()
    {
        float t = time;
        float spin = t * 4f;
        Vector2 center = body != null ? body.basePos : Vector2.zero;

        Pose(body, new Vector2(0f, Mathf.Sin(t * 6f) * 3f), Mathf.Sin(t * 3f) * 10f,
            new Vector2(1f + Mathf.Sin(t * 6f) * 0.05f, 1f - Mathf.Sin(t * 6f) * 0.05f));
        PoseOrbit(handL, center, spin);
        PoseOrbit(handR, center, spin);
    }

    // ── 공통 ─────────────────────────────────────────────────────────────────

    private static Part MakePart(RectTransform main, RectTransform shadow)
    {
        return new Part
        {
            main = main,
            shadow = shadow,
            basePos = main.anchoredPosition,
            baseRot = main.localEulerAngles.z,
            baseScale = main.localScale,
        };
    }

    /// <summary>파츠를 기본 자세에서 offset/rotation/scale만큼 움직인다(현재 weight만큼 섞어서). 그림자도 같이 움직인다.</summary>
    private void Pose(Part part, Vector2 offset, float rotation, Vector2 scale)
    {
        if (part == null)
            return;

        Vector2 pos = part.basePos + offset * weight;
        float rot = part.baseRot + rotation * weight;
        Vector3 s = Vector3.Scale(part.baseScale, new Vector3(Mathf.Lerp(1f, scale.x, weight), Mathf.Lerp(1f, scale.y, weight), 1f));
        Apply(part.main, pos, rot, s);
        Apply(part.shadow, pos, rot, s);
    }

    /// <summary>손이 몸 중심을 기준으로 빙글 돈다.</summary>
    private void PoseOrbit(Part part, Vector2 center, float angle)
    {
        if (part == null)
            return;

        Vector2 rel = part.basePos - center;
        float a = angle * weight;
        float cs = Mathf.Cos(a);
        float sn = Mathf.Sin(a);
        Vector2 rotated = new Vector2(rel.x * cs - rel.y * sn, rel.x * sn + rel.y * cs);
        Vector2 offset = (center + rotated - part.basePos) / Mathf.Max(weight, 0.0001f);
        Pose(part, offset, 0f, Vector2.one);
    }

    private static void Apply(RectTransform rt, Vector2 pos, float rot, Vector3 scale)
    {
        if (rt == null)
            return;
        rt.anchoredPosition = pos;
        rt.localRotation = Quaternion.Euler(0f, 0f, rot);
        rt.localScale = scale;
    }

    private void ResetPose()
    {
        foreach (Part part in new[] { body, handL, handR, gun })
        {
            if (part == null)
                continue;
            Apply(part.main, part.basePos, part.baseRot, part.baseScale);
            Apply(part.shadow, part.basePos, part.baseRot, part.baseScale);
        }

        if (flashImage != null)
            flashImage.color = Color.clear;
    }

    private void CreateFlash(Transform main)
    {
        GameObject go = new GameObject("Muzzle Flash", typeof(RectTransform));
        go.layer = main.gameObject.layer;
        flash = (RectTransform)go.transform;
        flash.SetParent(main, false);
        flash.sizeDelta = new Vector2(46f, 46f);
        flashImage = go.AddComponent<Image>();
        flashImage.sprite = flashSprite;
        flashImage.raycastTarget = false;
        flashImage.color = Color.clear;
    }

    /// <summary>총 그림의 양 끝 중 오른쪽(총구 쪽) 끝의 월드 위치.</summary>
    private static Vector3 MuzzleWorldPosition(RectTransform gunRect)
    {
        Rect r = gunRect.rect;
        Vector3 a = gunRect.TransformPoint(new Vector3(r.xMin + r.width * 0.06f, r.center.y, 0f));
        Vector3 b = gunRect.TransformPoint(new Vector3(r.xMax - r.width * 0.06f, r.center.y, 0f));
        return a.x > b.x ? a : b;
    }

    private void CreateAuraZap()
    {
        auraBaseScale = aura.rectTransform.localScale;
        auraBaseAlpha = aura.color.a;

        GameObject go = Instantiate(aura.gameObject, aura.transform.parent);
        go.name = "Aura Zap";
        go.transform.SetSiblingIndex(aura.transform.GetSiblingIndex() + 1);
        auraZap = go.GetComponent<Image>();
        auraZap.color = new Color(0.75f, 1f, 1f, 0f);
        auraZap.rectTransform.localScale = auraBaseScale * 0.9f;
    }

    private void SetupGlitch(Transform main)
    {
        Material instance = new Material(glitchMaterial);
        foreach (Transform t in main)
        {
            Image img = t.GetComponent<Image>();
            if (img == null)
                continue;
            img.material = instance;
            glitchGraphics.Add(img);
        }
        UpdateGlitch(0f);
    }

    private void UpdateGlitch(float amount)
    {
        if (glitchGraphics.Count == 0)
            return;

        bool burst = amount > 0f && time < glitchBurstUntil;
        foreach (Graphic g in glitchGraphics)
        {
            // 카드 마스크(스텐실) 때문에 실제 그리는 건 복사된 머티리얼이라 그쪽 값을 바꿔야 반영된다.
            // (materialForRendering은 부를 때마다 스텐실 머티리얼 참조 카운트를 올리므로 렌더러에서 직접 읽는다)
            Material m = g.canvasRenderer.materialCount > 0 ? g.canvasRenderer.GetMaterial(0) : null;
            if (m == null)
                continue;
            m.SetFloat("_Intensity", amount);
            if (burst)
                m.EnableKeyword("_ALWAYSON_ON");
            else
                m.DisableKeyword("_ALWAYSON_ON");
        }
    }

    private static float Hash(float x)
    {
        return Mathf.Repeat(Mathf.Sin(x * 127.1f) * 43758.5453f, 1f);
    }
}
