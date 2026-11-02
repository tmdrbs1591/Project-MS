using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>
    /// 체이서 궁극기(데드아이) 연출 — 모든 클라에서 각자 그린다(체이서의 네트워크 상태만 읽음).
    ///   - 조준 표시: 조준점에 원형 눈금 게이지가 뜨고, 꾹 누르는 동안 칸이 딱딱딱 채워지며 아래에 0~100%가 표시된다.
    ///     내 체이서면 마우스 위치(즉시 반응), 상대 체이서면 동기화된 조준점에 빨간색으로 뜬다(상대가 어딜 노리는지 보이게).
    ///   - 조준점 주위로 기가 모이는 이펙트, 쏘는 순간 탄착 지점에서 모은 만큼 커지는 폭발.
    ///   - 머리 위 작은 바: 저격 모드(무적) 남은 시간. 화면을 가리지 않게 캐릭터 위에만 작게.
    /// </summary>
    [RequireComponent(typeof(ChaserCharacter))]
    public class ChaserSnipeVisuals : MonoBehaviour
    {
        [System.Serializable]
        public struct ImpactFx
        {
            public GameObject prefab;
            public float scale;
        }

        [Header("조준 게이지")]
        [SerializeField] private int segments = 24;
        [SerializeField] private float radius = 62f;
        [SerializeField] private Vector2 segmentSize = new Vector2(7f, 18f);
        [SerializeField] private Color allyColor = new Color(0.85f, 0.45f, 1f, 1f);
        [SerializeField] private Color enemyColor = new Color(1f, 0.25f, 0.3f, 1f);
        [SerializeField] private Color fullColor = new Color(1f, 0.95f, 1f, 1f);
        [SerializeField] private TMP_FontAsset percentFont;
        [Tooltip("꾹 눌러 모으기 시작할 때 소리(Q 기 모으기 소리와 같은 것).")]
        [SerializeField] private AudioClip chargeClip;
        [Range(0f, 2f)] [SerializeField] private float chargeVolume = 0.9f;
        [SerializeField] private AudioClip tickClip;
        [Range(0f, 1f)] [SerializeField] private float tickVolume = 0.45f;
        [SerializeField] private AudioClip fullClip;
        [Range(0f, 1f)] [SerializeField] private float fullVolume = 0.8f;

        [Header("기 모으기 / 탄착")]
        [SerializeField] private GameObject chargeVfx;
        [SerializeField] private float chargeVfxScale = 0.55f;
        [SerializeField] private Color chargeTint = new Color(0.8f, 0.4f, 1f, 1f);
        [SerializeField] private ImpactFx[] impactFx;
        [Tooltip("0% 발사일 때 탄착 이펙트 크기 배율(100%면 1).")]
        [Range(0.1f, 1f)] [SerializeField] private float minImpactScale = 0.55f;
        [Tooltip("쏘는 순간 총구에서 탄착점까지 번쩍이는 탄도선 색(블룸이 걸리게 밝게).")]
        [SerializeField] private Color tracerColor = new Color(0.9f, 0.45f, 1f, 1f);
        [Min(0.05f)] [SerializeField] private float tracerDuration = 0.3f;
        [Min(0.01f)] [SerializeField] private float tracerWidth = 0.55f;

        [Header("남은 시간 바(머리 위)")]
        [SerializeField] private Vector2 barOffset = new Vector2(0f, 1.75f);
        [SerializeField] private Vector2 barSize = new Vector2(1.3f, 0.09f);

        private ChaserCharacter chaser;
        private RectTransform root;
        private Canvas canvas;
        private readonly List<Image> segs = new List<Image>();
        private readonly List<Image> crossParts = new List<Image>();
        private TMP_Text percent;
        private int lastFilled;
        private bool wasFull;
        private bool wasCharging;
        private GameObject chargeInstance;
        private Transform chargeAnchor;
        private int lastShot;
        private bool shotInit;
        private SpriteRenderer barBack, barFill;
        private LineRenderer tracerCore, tracerGlow;
        private float tracerTime = -10f;
        private Vector3 tracerFrom, tracerTo;
        private float tracerCharge;
        private static Sprite whiteSprite;

        private void Awake() => chaser = GetComponent<ChaserCharacter>();

        private void OnDisable() => HideAll();
        private void OnDestroy()
        {
            if (canvas != null) Destroy(canvas.gameObject);
            if (chargeAnchor != null) Destroy(chargeAnchor.gameObject);
            if (barBack != null) Destroy(barBack.gameObject);
            if (barFill != null) Destroy(barFill.gameObject);
            if (tracerCore != null) Destroy(tracerCore.gameObject);
            if (tracerGlow != null) Destroy(tracerGlow.gameObject);
        }

        private void LateUpdate()
        {
            if (chaser == null || chaser.Object == null || !chaser.Object.IsValid)
            {
                HideAll();
                return;
            }

            // 쏜 순간: 탄착 지점에서 모은 만큼 큰 폭발
            int shot = chaser.SnipeShotCount;
            if (!shotInit) { shotInit = true; lastShot = shot; }
            if (shot != lastShot)
            {
                lastShot = shot;
                float k = Mathf.Lerp(minImpactScale, 1f, chaser.LastSnipeShotCharge);
                if (impactFx != null)
                    foreach (ImpactFx fx in impactFx)
                        FxSpawner.Spawn(fx.prefab, chaser.LastSnipeShotPosition, Quaternion.identity, (fx.scale <= 0f ? 1f : fx.scale) * k);
                // 탄도선: 총구 → 탄착점이 하얗게 번쩍였다가 보라색으로 사라진다(쏜 쪽과 맞는 쪽 모두에게 보이게).
                tracerTime = Time.time;
                tracerFrom = transform.position + new Vector3(0f, 0.45f, 0f);
                tracerTo = chaser.LastSnipeShotPosition;
                tracerCharge = chaser.LastSnipeShotCharge;

                if (TwoPlayerCamera.Instance != null)
                {
                    float c = chaser.LastSnipeShotCharge;
                    // 쏜 쪽은 Chaser가 크게 흔들고 당긴다. 상대 화면도 확 흔들려서 "맞았다"가 느껴지게.
                    if (!chaser.IsLocalPlayer)
                        TwoPlayerCamera.Instance.Shake(0.45f + 0.45f * c, 0.35f);
                }
                // 화면이 번쩍: 모은 만큼 세게(쏜 쪽은 하얗게, 상대는 보라로)
                ScreenFlash.Play(chaser.IsLocalPlayer ? Color.white : new Color(0.85f, 0.4f, 1f),
                    0.22f + 0.28f * chaser.LastSnipeShotCharge, 0.03f, 0.25f);
            }
            UpdateTracer();

            bool sniping = chaser.IsSniping && !chaser.IsDead;
            if (!sniping)
            {
                HideAll();
                return;
            }

            UpdateReticle();
            UpdateChargeVfx();
            UpdateBar();
        }

        // ───────── 조준 게이지 ─────────

        private void UpdateReticle()
        {
            EnsureReticle();
            canvas.enabled = true;

            Camera cam = TwoPlayerCamera.Instance != null ? TwoPlayerCamera.Instance.GetComponent<Camera>() : Camera.main;
            Vector2 screen;
            if (chaser.IsLocalPlayer && Mouse.current != null)
                screen = Mouse.current.position.ReadValue();
            else if (cam != null)
                screen = cam.WorldToScreenPoint(chaser.SnipeAimWorld);
            else
                return;
            root.position = screen;

            bool charging = chaser.IsSnipeCharging;
            float ratio = chaser.SnipeChargeRatio;
            bool full = charging && ratio >= 0.999f;
            Color accent = chaser.IsLocalPlayer ? allyColor : enemyColor;
            int filled = charging ? Mathf.FloorToInt(ratio * segments + 0.0001f) : 0;

            for (int i = 0; i < segs.Count; i++)
            {
                bool on = i < filled;
                Color c = full ? fullColor : accent;
                segs[i].color = on ? c : new Color(accent.r, accent.g, accent.b, 0.22f);
                segs[i].rectTransform.localScale = on && i == filled - 1 && !full ? new Vector3(1.25f, 1.25f, 1f) : Vector3.one;
            }

            // 다 차면 두근두근
            float pulse = full ? 1f + 0.08f * Mathf.Sin(Time.time * 22f) : 1f;
            float spin = charging ? 0f : Time.time * 25f;
            root.localScale = Vector3.one * pulse;
            root.localEulerAngles = new Vector3(0f, 0f, -spin);
            foreach (Image part in crossParts)
                part.color = full ? fullColor : accent;

            percent.enabled = charging;
            percent.rectTransform.localEulerAngles = new Vector3(0f, 0f, spin);   // 글자는 안 돌게
            if (charging)
            {
                int pct = Mathf.RoundToInt(ratio * 100f);
                percent.text = full ? "MAX" : pct + "%";
                percent.color = full ? fullColor : accent;
            }

            // 모으기 시작: Q 기 모으기와 같은 소리(모든 클라에서 — 상대도 듣고 대비할 수 있게)
            if (charging && !wasCharging)
                SoundManager.Instance?.PlaySfx(chargeClip, chargeVolume, 0f);
            wasCharging = charging;

            // 딱딱딱: 한 칸 찰 때마다(내 화면에서만, 틱 소리를 넣었을 때), 다 차면 띵
            if (chaser.IsLocalPlayer)
            {
                if (filled > lastFilled && !full)
                    SoundManager.Instance?.PlaySfx(tickClip, tickVolume, 0.04f);
                if (full && !wasFull)
                    SoundManager.Instance?.PlaySfx(fullClip, fullVolume, 0f);
            }
            lastFilled = filled;
            wasFull = full;
        }

        private void EnsureReticle()
        {
            if (canvas != null)
                return;
            var go = new GameObject("Chaser Snipe Reticle");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 640;
            root = new GameObject("Root", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(go.transform, false);
            root.sizeDelta = Vector2.zero;

            for (int i = 0; i < segments; i++)
            {
                float ang = 360f * i / segments;
                var img = NewImage(root, "Seg");
                RectTransform rt = img.rectTransform;
                rt.sizeDelta = segmentSize;
                // 12시 방향부터 시계방향으로 채워진다
                float rad = (90f - ang) * Mathf.Deg2Rad;
                rt.anchoredPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
                rt.localEulerAngles = new Vector3(0f, 0f, -ang);
                segs.Add(img);
            }
            // 가운데 십자 + 점
            for (int i = 0; i < 4; i++)
            {
                var c = NewImage(root, "Cross");
                c.rectTransform.sizeDelta = new Vector2(3f, 16f);
                float rad = i * Mathf.PI * 0.5f;
                c.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 16f;
                c.rectTransform.localEulerAngles = new Vector3(0f, 0f, i * 90f + 90f);
                crossParts.Add(c);
            }
            var dot = NewImage(root, "Dot");
            dot.rectTransform.sizeDelta = new Vector2(5f, 5f);
            crossParts.Add(dot);

            percent = new GameObject("Percent", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            percent.rectTransform.SetParent(root, false);
            percent.rectTransform.anchoredPosition = new Vector2(0f, -radius - 26f);
            percent.rectTransform.sizeDelta = new Vector2(200f, 40f);
            if (percentFont != null) percent.font = percentFont;
            percent.fontSize = 30f;
            percent.fontStyle = FontStyles.Bold | FontStyles.Italic;
            percent.alignment = TextAlignmentOptions.Center;
            percent.raycastTarget = false;
            percent.outlineWidth = 0.25f;
            percent.outlineColor = new Color32(20, 0, 30, 255);
        }

        private static Image NewImage(Transform parent, string name)
        {
            var img = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            img.transform.SetParent(parent, false);
            img.raycastTarget = false;
            return img;
        }

        // ───────── 조준점에 기 모으기 ─────────

        private void UpdateChargeVfx()
        {
            bool charging = chaser.IsSnipeCharging;
            if (charging && chargeInstance == null && chargeVfx != null)
            {
                if (chargeAnchor == null)
                    chargeAnchor = new GameObject("Chaser Snipe Charge Anchor").transform;
                chargeInstance = FxSpawner.Spawn(chargeVfx, chargeAnchor.position, Quaternion.identity, chargeVfxScale, 0f,
                    FxSpawner.DefaultSortingOrder, chargeAnchor);
                FxSpawner.Tint(chargeInstance, chargeTint);
            }
            if (!charging && chargeInstance != null)
            {
                Destroy(chargeInstance);
                chargeInstance = null;
            }
            if (chargeAnchor != null)
            {
                Vector2 aim = chaser.SnipeAimWorld;
                if (chaser.IsLocalPlayer && Mouse.current != null && TwoPlayerCamera.Instance != null)
                {
                    Camera cam = TwoPlayerCamera.Instance.GetComponent<Camera>();
                    Vector3 sp = Mouse.current.position.ReadValue();
                    sp.z = Mathf.Abs(cam.transform.position.z);
                    aim = cam.ScreenToWorldPoint(sp);
                }
                chargeAnchor.position = new Vector3(aim.x, aim.y, 0f);
            }
        }

        // ───────── 탄도선 ─────────

        private void UpdateTracer()
        {
            float t = (Time.time - tracerTime) / tracerDuration;
            if (t < 0f || t >= 1f)
            {
                if (tracerCore != null) tracerCore.enabled = tracerGlow.enabled = false;
                return;
            }
            if (tracerCore == null)
            {
                tracerGlow = NewLine("Snipe Tracer Glow");
                tracerCore = NewLine("Snipe Tracer Core");
            }
            float k = 1f - t;
            float w = tracerWidth * Mathf.Lerp(0.6f, 1f, tracerCharge);
            Color hot = Color.Lerp(tracerColor, Color.white, k * k);
            SetLine(tracerCore, w * 0.35f * k, new Color(hot.r, hot.g, hot.b, k));
            SetLine(tracerGlow, w * (1.2f + t), new Color(tracerColor.r, tracerColor.g, tracerColor.b, 0.55f * k));
        }

        private void SetLine(LineRenderer lr, float width, Color color)
        {
            lr.enabled = true;
            lr.SetPosition(0, new Vector3(tracerFrom.x, tracerFrom.y, 0f));
            lr.SetPosition(1, new Vector3(tracerTo.x, tracerTo.y, 0f));
            lr.widthMultiplier = width;
            var g = new Gradient();
            Color c = new Color(color.r, color.g, color.b, 1f);
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                      new[] { new GradientAlphaKey(color.a * 0.5f, 0f), new GradientAlphaKey(color.a, 0.85f), new GradientAlphaKey(color.a, 1f) });
            lr.colorGradient = g;
        }

        private static Material lineMaterial;
        private LineRenderer NewLine(string name)
        {
            var go = new GameObject(name);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.numCapVertices = 6;
            if (lineMaterial == null)
            {
                lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Snipe Tracer (Additive)" };
                lineMaterial.SetFloat("_Surface", 1f);
                lineMaterial.SetFloat("_Blend", 2f);
                lineMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                lineMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                lineMaterial.SetFloat("_ZWrite", 0f);
                lineMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                lineMaterial.EnableKeyword("_BLENDMODE_ADD");
                lineMaterial.SetColor("_BaseColor", new Color(2f, 2f, 2f, 1f));
                lineMaterial.renderQueue = 3000;
            }
            lr.sharedMaterial = lineMaterial;
            lr.sortingOrder = FxSpawner.DefaultSortingOrder + 40;
            lr.enabled = false;
            return lr;
        }

        // ───────── 남은 시간 바 ─────────

        private void UpdateBar()
        {
            if (barBack == null)
            {
                barBack = NewBar("Snipe Time Back", new Color(0.05f, 0.02f, 0.08f, 0.85f), 70);
                barFill = NewBar("Snipe Time Fill", allyColor, 71);
            }
            float r = chaser.SnipeTimeRemaining01;
            Vector3 basePos = transform.position + (Vector3)barOffset;
            basePos.z = 0f;
            barBack.enabled = barFill.enabled = true;
            barBack.transform.position = basePos;
            barBack.transform.localScale = new Vector3(barSize.x + 0.06f, barSize.y + 0.06f, 1f);
            barFill.transform.position = basePos + new Vector3(-barSize.x * (1f - r) * 0.5f, 0f, 0f);
            barFill.transform.localScale = new Vector3(barSize.x * r, barSize.y, 1f);
            // 얼마 안 남으면 깜빡여서 곧 무적이 풀린다는 걸 알린다
            Color c = chaser.IsLocalPlayer ? allyColor : enemyColor;
            if (r < 0.25f && Mathf.Sin(Time.time * 20f) > 0f)
                c = Color.white;
            barFill.color = c;
        }

        private SpriteRenderer NewBar(string name, Color color, int order)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSprite;
            Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (unlit != null)
                sr.sharedMaterial = new Material(unlit);
            sr.color = color;
            sr.sortingOrder = FxSpawner.DefaultSortingOrder + order;
            return sr;
        }

        private static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite == null)
                {
                    var tex = new Texture2D(4, 4);
                    var px = new Color[16];
                    for (int i = 0; i < 16; i++) px[i] = Color.white;
                    tex.SetPixels(px);
                    tex.Apply();
                    whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                }
                return whiteSprite;
            }
        }

        private void HideAll()
        {
            if (canvas != null) canvas.enabled = false;
            if (chargeInstance != null) { Destroy(chargeInstance); chargeInstance = null; }
            if (barBack != null) barBack.enabled = barFill.enabled = false;
            lastFilled = 0;
            wasFull = false;
            wasCharging = false;
        }
    }
}
