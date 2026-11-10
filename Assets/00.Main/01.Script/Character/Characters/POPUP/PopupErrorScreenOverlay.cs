using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectMS.CharacterSystem.Examples
{
    /// <summary>화면 덮기 연출에 쓰는 그림/폰트/소리. 팝업 에러창(설치물) 프리팹에서 지정한다.</summary>
    [Serializable]
    public sealed class PopupErrorOverlaySettings
    {
        [Tooltip("화면을 덮을 에러창 그림.")]
        public Sprite windowSprite;
        [Tooltip("에러창에 씌울 UI 글리치 머티리얼(Custom/UI/Glitch). 창마다 복사해서 따로 튀게 한다.")]
        public Material windowGlitchMaterial;
        [Tooltip("창 크기(1920x1080 기준 픽셀).")]
        public Vector2 windowSizeRange = new Vector2(620f, 800f);
        [Tooltip("창 뒤에 한 장 더 까는 같은 그림의 색(창 그림이 반투명해서 뒤가 비치지 않게).")]
        public Color windowBackingColor = new Color(0.22f, 0.16f, 0.38f, 1f);
        [Tooltip("창이 하나씩 뜨는 간격(초).")]
        [Min(0.01f)] public float popInterval = 0.06f;

        [Header("글자")]
        public TMP_FontAsset promptFont;
        public Material promptMaterial;
        public TMP_FontAsset counterFont;
        public Material counterMaterial;
        public Color accentColor = new Color(0.35f, 0.95f, 1f, 1f);

        [Header("소리")]
        public AudioClip popClip;
        [Range(0f, 1f)] public float popVolume = 0.35f;
        public AudioClip closeClip;
        [Range(0f, 1f)] public float closeVolume = 0.6f;
        public AudioClip clearClip;
        [Range(0f, 1f)] public float clearVolume = 0.8f;
    }

    /// <summary>
    /// 팝업 궁극기에 걸린 사람 본인 화면에만 뜨는 연출. 에러창이 와르르 떠서 화면을 덮고,
    /// 가운데에 "SPACE 연타!"와 진행도(0 / 10)를 띄운다. 누를 때마다 맨 위 창이 하나씩 닫혀 화면이 다시 보인다.
    /// 실제 속박/해제 판정은 시전자 쪽(PopupCharacter)이 하고, 여기는 보기만 한다 — 누르는 즉시 반응하도록
    /// 내 입력을 직접 세되, 시전자가 센 값(네트워크)이 더 크면 그걸 따른다.
    /// </summary>
    public sealed class PopupErrorScreenOverlay : MonoBehaviour
    {
        private sealed class Window
        {
            public RectTransform Rect;
            public Image Image;
            public Image Back;
            public Material Material;
            public float BaseRotation;
            public float BaseScale;
            public float AppearAt;
            public float CloseAt = -1f;
            public bool Appeared;
        }

        private const float PopDuration = 0.16f;
        private const float CloseDuration = 0.16f;

        private PopupErrorOverlaySettings settings;
        private readonly List<Window> windows = new List<Window>();
        private RectTransform root;
        private RectTransform promptGroup;
        private TMP_Text promptText;
        private TMP_Text counterText;
        private RectTransform barFill;
        private float barWidth;

        private int required;
        private int localPresses;
        private int netProgress;
        private int shownProgress;
        private float punchTime = -10f;
        private float startTime;
        private bool ended;
        private float endTime;

        public static PopupErrorScreenOverlay Show(PopupErrorOverlaySettings settings, int required)
        {
            var go = new GameObject("Popup Error Screen Overlay");
            var overlay = go.AddComponent<PopupErrorScreenOverlay>();
            overlay.Build(settings, Mathf.Max(1, required));
            return overlay;
        }

        /// <summary>시전자가 센 진행도(네트워크 값).</summary>
        public void SetNetProgress(int progress)
        {
            netProgress = Mathf.Max(netProgress, progress);
        }

        /// <summary>팝업이 사라질 때. 남은 창을 우르르 닫고 스스로 지운다.</summary>
        public void End(bool cleared)
        {
            if (ended)
                return;
            ended = true;
            endTime = Time.unscaledTime;

            float t = Time.unscaledTime;
            for (int i = windows.Count - 1, k = 0; i >= 0; i--)
            {
                Window w = windows[i];
                if (w.CloseAt >= 0f)
                    continue;
                w.CloseAt = t + 0.025f * k++;
            }

            if (cleared)
            {
                SoundManager.Instance?.PlaySfx(settings.clearClip, settings.clearVolume, 0f);
                ScreenFlash.Play(new Color(0.7f, 1f, 1f), 0.18f, 0.02f, 0.2f);
                if (promptText != null)
                    promptText.text = "해킹 해제!";
                punchTime = t;
            }
        }

        private void Build(PopupErrorOverlaySettings s, int req)
        {
            settings = s;
            required = req;
            startTime = Time.unscaledTime;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 700;   // HUD보다 위, 궁 컷인(650)보다 위, 화면 번쩍(810)보다 아래
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            root = new GameObject("Windows", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(transform, false);
            Stretch(root);

            // 화면을 칸으로 나눠 칸마다 창 하나 — 크게 띄워 서로 겹치게 해서 빈틈 없이 덮는다.
            int cols = Mathf.CeilToInt(Mathf.Sqrt(required * 16f / 9f));
            int rows = Mathf.CeilToInt(required / (float)cols);
            var cells = new List<int>();
            for (int i = 0; i < cols * rows; i++) cells.Add(i);
            Shuffle(cells);

            Vector2 cell = new Vector2(1920f / cols, 1080f / rows);
            for (int i = 0; i < required; i++)
            {
                int c = cells[i] % cols, r = cells[i] / cols;
                Vector2 pos = new Vector2(-960f + cell.x * (c + 0.5f), 540f - cell.y * (r + 0.5f));
                pos += new Vector2(UnityEngine.Random.Range(-0.25f, 0.25f) * cell.x, UnityEngine.Random.Range(-0.2f, 0.2f) * cell.y);
                windows.Add(CreateWindow(pos, startTime + i * settings.popInterval));
            }

            BuildPrompt();
        }

        private Window CreateWindow(Vector2 position, float appearAt)
        {
            var rect = new GameObject("Error Window", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(root, false);
            float size = UnityEngine.Random.Range(settings.windowSizeRange.x, settings.windowSizeRange.y);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = position;

            // 창 그림이 유리처럼 반투명해서 그대로면 뒤 화면이 비친다 — 같은 그림을 어둡게 한 장 깔아 확실히 가린다.
            Image back = NewWindowImage(rect, "Back");
            back.color = settings.windowBackingColor;
            Image img = NewWindowImage(rect, "Front");

            Material mat = null;
            if (settings.windowGlitchMaterial != null)
            {
                mat = new Material(settings.windowGlitchMaterial);
                img.material = mat;
            }

            rect.gameObject.SetActive(false);
            return new Window
            {
                Rect = rect,
                Image = img,
                Back = back,
                Material = mat,
                BaseRotation = UnityEngine.Random.Range(-8f, 8f),
                BaseScale = 1f,
                AppearAt = appearAt,
            };
        }

        private Image NewWindowImage(RectTransform parent, string name)
        {
            var img = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            img.rectTransform.SetParent(parent, false);
            img.rectTransform.anchorMin = Vector2.zero;
            img.rectTransform.anchorMax = Vector2.one;
            img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
            img.sprite = settings.windowSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        private void BuildPrompt()
        {
            promptGroup = new GameObject("Prompt", typeof(RectTransform)).GetComponent<RectTransform>();
            promptGroup.SetParent(transform, false);
            promptGroup.anchoredPosition = new Vector2(0f, -20f);

            var band = new GameObject("Band", typeof(RectTransform)).AddComponent<Image>();
            band.rectTransform.SetParent(promptGroup, false);
            band.rectTransform.sizeDelta = new Vector2(2400f, 300f);
            band.color = new Color(0.07f, 0.03f, 0.16f, 0.82f);
            band.raycastTarget = false;

            string key = KeyBindings.GetKeyLabel(BindableAction.Jump);
            string keyName = key == "SPACE" ? "스페이스바" : key;
            promptText = NewText(promptGroup, settings.promptFont, settings.promptMaterial, 64f, new Vector2(0f, 78f));
            promptText.text = keyName + "를 연타하세요!";

            counterText = NewText(promptGroup, settings.counterFont, settings.counterMaterial, 130f, new Vector2(0f, -28f));

            barWidth = 720f;
            var barBack = new GameObject("Bar", typeof(RectTransform)).AddComponent<Image>();
            barBack.rectTransform.SetParent(promptGroup, false);
            barBack.rectTransform.sizeDelta = new Vector2(barWidth + 12f, 26f);
            barBack.rectTransform.anchoredPosition = new Vector2(0f, -118f);
            barBack.color = new Color(0f, 0f, 0f, 0.6f);
            barBack.raycastTarget = false;

            var fill = new GameObject("Fill", typeof(RectTransform)).AddComponent<Image>();
            barFill = fill.rectTransform;
            barFill.SetParent(barBack.rectTransform, false);
            barFill.anchorMin = barFill.anchorMax = new Vector2(0f, 0.5f);
            barFill.pivot = new Vector2(0f, 0.5f);
            barFill.anchoredPosition = new Vector2(6f, 0f);
            barFill.sizeDelta = new Vector2(0f, 16f);
            fill.color = settings.accentColor;
            fill.raycastTarget = false;

            UpdateCounterText();
        }

        private static TMP_Text NewText(RectTransform parent, TMP_FontAsset font, Material mat, float size, Vector2 pos)
        {
            var t = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            t.rectTransform.SetParent(parent, false);
            t.rectTransform.sizeDelta = new Vector2(1600f, 200f);
            t.rectTransform.anchoredPosition = pos;
            if (font != null) t.font = font;
            if (mat != null) t.fontSharedMaterial = mat;
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.enableWordWrapping = false;
            t.raycastTarget = false;
            return t;
        }

        private void Update()
        {
            float now = Time.unscaledTime;

            if (!ended && WasBreakPressed())
                localPresses++;

            int target = Mathf.Clamp(Mathf.Max(localPresses, netProgress), 0, required);
            while (shownProgress < target)
            {
                CloseTopWindow(now);
                shownProgress++;
                punchTime = now;
                UpdateCounterText();
            }

            for (int i = 0; i < windows.Count; i++)
                AnimateWindow(windows[i], now);

            AnimatePrompt(now);

            if (ended && now - endTime > 0.9f)
                Destroy(gameObject);
        }

        private static bool WasBreakPressed()
        {
            // 시전자 쪽은 점프(스페이스)와 기본공격(좌클릭)을 "깨기 시도"로 센다 — 같은 입력을 센다.
            if (KeyBindings.WasPressed(BindableAction.Jump) || KeyBindings.WasPressed(BindableAction.JumpAlt))
                return true;
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }

        private void CloseTopWindow(float now)
        {
            for (int i = windows.Count - 1; i >= 0; i--)
            {
                Window w = windows[i];
                if (w.CloseAt >= 0f)
                    continue;
                w.CloseAt = now;
                if (w.Appeared)
                    SoundManager.Instance?.PlaySfx(settings.closeClip, settings.closeVolume, 0.12f);
                return;
            }
        }

        private void AnimateWindow(Window w, float now)
        {
            if (w.Rect == null)
                return;

            // 아직 안 뜬 창이 먼저 닫혔으면(빨리 누른 경우) 아예 안 띄운다.
            if (w.CloseAt >= 0f && !w.Appeared)
            {
                w.Rect.gameObject.SetActive(false);
                return;
            }

            if (!w.Appeared)
            {
                if (now < w.AppearAt)
                    return;
                w.Appeared = true;
                w.Rect.gameObject.SetActive(true);
                SoundManager.Instance?.PlaySfx(settings.popClip, settings.popVolume, 0.15f);
            }

            float scale, glitch, alpha = 1f;
            float jitter = 0f;
            if (w.CloseAt >= 0f && now >= w.CloseAt)
            {
                // 닫힘: 글리치가 확 튀면서 살짝 커졌다가 납작하게 사라진다.
                float k = Mathf.Clamp01((now - w.CloseAt) / CloseDuration);
                scale = Mathf.Lerp(1.08f, 0f, k * k);
                alpha = 1f - k;
                glitch = 0.8f;
                jitter = 18f * (1f - k);
                if (k >= 1f)
                {
                    w.Rect.gameObject.SetActive(false);
                    return;
                }
                w.Rect.localScale = new Vector3(scale * (1f + 0.4f * k), scale, 1f) * w.BaseScale;
            }
            else
            {
                // 뜰 때: 작게 튀어나와 살짝 넘쳤다 자리잡고, 처음 잠깐 글리치가 세게 걸린다.
                float k = Mathf.Clamp01((now - w.AppearAt) / PopDuration);
                scale = OutBack(k, 2.6f);
                glitch = Mathf.Lerp(1f, 0.3f, Mathf.Clamp01((now - w.AppearAt) / 0.35f));
                jitter = 10f * (1f - k);
                w.Rect.localScale = Vector3.one * scale * w.BaseScale;
            }

            w.Rect.localEulerAngles = new Vector3(0f, 0f, w.BaseRotation + UnityEngine.Random.Range(-1f, 1f) * jitter * 0.3f);
            Color c = w.Image.color;
            c.a = alpha;
            w.Image.color = c;
            Color b = w.Back.color;
            b.a = alpha;
            w.Back.color = b;
            if (w.Material != null)
                w.Material.SetFloat("_Intensity", glitch);
        }

        private void AnimatePrompt(float now)
        {
            if (promptGroup == null)
                return;

            float appear = Mathf.Clamp01((now - startTime - 0.1f) / 0.2f);
            float punch = Mathf.Clamp01((now - punchTime) / 0.18f);
            float pulse = 1f + 0.05f * Mathf.Sin(now * 12f);
            float s = OutBack(appear, 2f) * pulse * Mathf.Lerp(1.25f, 1f, punch);
            if (ended)
                s *= 1f - Mathf.Clamp01((now - endTime - 0.25f) / 0.3f);
            promptGroup.localScale = Vector3.one * s;
            promptGroup.anchoredPosition = new Vector2(0f, -20f) +
                                           (punch < 1f ? UnityEngine.Random.insideUnitCircle * 14f * (1f - punch) : Vector2.zero);

            if (counterText != null)
                counterText.color = Color.Lerp(settings.accentColor, Color.white, punch);
            if (barFill != null)
            {
                float target = barWidth * shownProgress / required;
                float w = Mathf.Lerp(barFill.sizeDelta.x, target, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
                barFill.sizeDelta = new Vector2(w, barFill.sizeDelta.y);
            }
        }

        private void UpdateCounterText()
        {
            if (counterText != null)
                counterText.text = shownProgress + "<size=60%><alpha=#99> / " + required + "</size>";
        }

        private void OnDestroy()
        {
            foreach (Window w in windows)
                if (w.Material != null)
                    Destroy(w.Material);
        }

        private static float OutBack(float t, float s)
        {
            t -= 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
