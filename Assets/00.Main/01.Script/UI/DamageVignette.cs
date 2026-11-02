using ProjectMS.CharacterSystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 내 캐릭터가 맞으면 화면 가장자리가 빨갛게 번쩍였다가 빠지는 피격 연출. 체력이 낮으면 가장자리가 은은하게 맥박친다.
/// - 내 캐릭터(IsLocalPlayer)의 체력을 매 프레임 보고 줄어든 만큼 세게 번쩍인다(네트워크 이벤트 불필요 — 모든 클라 동일).
/// - 맞을 때와 상대를 맞힐 때 화면도 흔든다(피해가 클수록 세게).
/// - 비네트 텍스처는 코드로 만든다(가운데 투명 → 가장자리 빨강). 자체 오버레이 캔버스라 씬 어디에 붙여도 된다.
/// </summary>
public class DamageVignette : MonoBehaviour
{
    [SerializeField] private Color color = new Color(0.85f, 0.02f, 0.02f, 1f);
    [Tooltip("맞았을 때 최소 번쩍임 세기(0~1).")]
    [Range(0f, 1f)] [SerializeField] private float minHitAlpha = 0.45f;
    [Tooltip("최대 체력 대비 이만큼(비율) 깎이면 최대 세기로 번쩍인다.")]
    [Range(0.01f, 1f)] [SerializeField] private float fullHitRatio = 0.25f;
    [Tooltip("번쩍임이 빠지는 속도(초당 알파).")]
    [SerializeField] private float fadeSpeed = 1.2f;
    [Tooltip("화면 전체에 살짝 깔리는 붉은 기운(번쩍임 세기 대비).")]
    [Range(0f, 1f)] [SerializeField] private float fullScreenTint = 0.12f;
    [Tooltip("체력이 이 비율 아래면 가장자리가 맥박친다.")]
    [Range(0f, 1f)] [SerializeField] private float lowHealthRatio = 0.3f;
    [Range(0f, 1f)] [SerializeField] private float lowHealthAlpha = 0.3f;
    [SerializeField] private int sortingOrder = 700;

    [Header("화면 흔들림")]
    [Tooltip("내가 맞았을 때 흔들림 세기(작은 피해 ~ 큰 피해).")]
    [SerializeField] private float hitShakeMin = 0.25f;
    [SerializeField] private float hitShakeMax = 0.55f;
    [Tooltip("내가 상대를 맞혔을 때 흔들림 세기(작은 피해 ~ 큰 피해).")]
    [SerializeField] private float dealShakeMin = 0.12f;
    [SerializeField] private float dealShakeMax = 0.3f;

    private Image vignette;
    private Image tint;
    private CharacterBase tracked;
    private float lastHealth;
    private CharacterBase trackedEnemy;
    private float lastEnemyHealth;
    private float hit;

    private void Awake()
    {
        var canvasGo = new GameObject("Damage Vignette Canvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        tint = MakeImage(canvasGo.transform, "Tint", null);
        vignette = MakeImage(canvasGo.transform, "Vignette", BuildVignetteSprite());
        SetAlpha(0f, 0f);
    }

    private static Image MakeImage(Transform parent, string name, Sprite sprite)
    {
        var img = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(parent, false);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    private static Sprite BuildVignetteSprite()
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 화면 비율과 상관없이 가장자리 쪽으로 진해지게: 사각형에 가까운 둥근 거리
                float u = Mathf.Abs(x / (size - 1f) * 2f - 1f);
                float v = Mathf.Abs(y / (size - 1f) * 2f - 1f);
                float d = Mathf.Pow(Mathf.Pow(u, 4f) + Mathf.Pow(v, 4f), 0.25f);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.05f, d));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private void Update()
    {
        CharacterBase local = FindLocal();
        if (local != tracked)
        {
            tracked = local;
            lastHealth = local != null ? local.CurrentHealth : 0f;
            hit = 0f;
        }

        // 싸우는 중에만: 라운드 사이(체력 리셋·증강 선택)에는 번쩍이지 않고 바로 꺼둔다.
        bool fighting = MatchManager.Instance != null && MatchManager.Instance.Phase == MatchPhase.Fighting;
        if (!fighting)
        {
            hit = 0f;
            if (tracked != null)
                lastHealth = tracked.CurrentHealth;
        }

        float low = 0f;
        if (fighting && tracked != null && !tracked.IsDead)
        {
            float hp = tracked.CurrentHealth;
            float max = tracked.MaxHealth;
            if (hp < lastHealth - 0.01f && max > 0f)
            {
                float k = Mathf.Clamp01((lastHealth - hp) / (max * fullHitRatio));
                hit = Mathf.Max(hit, Mathf.Lerp(minHitAlpha, 1f, k));
                TwoPlayerCamera.Instance?.Shake(Mathf.Lerp(hitShakeMin, hitShakeMax, k), 0.22f);
            }
            lastHealth = hp;

            float ratio = max > 0f ? hp / max : 1f;
            if (ratio < lowHealthRatio)
            {
                float pulse = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 6f);
                low = lowHealthAlpha * pulse * (1f - ratio / lowHealthRatio * 0.5f);
            }
        }

        TrackEnemyHit(fighting);

        hit = Mathf.MoveTowards(hit, 0f, fadeSpeed * Time.unscaledDeltaTime);
        SetAlpha(Mathf.Max(hit, low), hit * fullScreenTint);
    }

    /// <summary>내가 상대를 맞혔을 때(상대 체력이 깎일 때)도 화면을 가볍게 흔들어 타격감을 준다.</summary>
    private void TrackEnemyHit(bool fighting)
    {
        CharacterBase enemy = null;
        foreach (CharacterBase c in CharacterBase.All)
            if (c != null && !c.IsLocalPlayer)
                enemy = c;
        if (enemy != trackedEnemy)
        {
            trackedEnemy = enemy;
            lastEnemyHealth = enemy != null ? enemy.CurrentHealth : 0f;
        }
        if (enemy == null)
            return;

        float hp = enemy.CurrentHealth;
        float max = enemy.MaxHealth;
        if (fighting && !enemy.IsDead && hp < lastEnemyHealth - 0.01f && max > 0f)
        {
            float k = Mathf.Clamp01((lastEnemyHealth - hp) / (max * fullHitRatio));
            TwoPlayerCamera.Instance?.Shake(Mathf.Lerp(dealShakeMin, dealShakeMax, k), 0.15f);
        }
        lastEnemyHealth = hp;
    }

    private void SetAlpha(float edge, float full)
    {
        vignette.color = new Color(color.r, color.g, color.b, edge);
        tint.color = new Color(color.r, color.g, color.b, full);
        vignette.enabled = edge > 0.001f;
        tint.enabled = full > 0.001f;
    }

    private static CharacterBase FindLocal()
    {
        foreach (CharacterBase c in CharacterBase.All)
            if (c != null && c.IsLocalPlayer)
                return c;
        return null;
    }
}
