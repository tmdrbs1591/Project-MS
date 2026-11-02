using System;
using System.Collections;
using System.Collections.Generic;
using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>로비 포탈에서 고를 수 있는 게임 모드. 새 모드는 뒤에 추가한다.</summary>
public enum GameModeOption
{
    OneVsOne,
    AiMatch,
    Practice,
    TwoVsTwo,
}

/// <summary>
/// 포탈 앞에서 상호작용하면 뜨는 모드 선택 창(1대1 대전 / AI 대전 / 연습 ...).
/// modes 배열 순서대로 모드 카드(ModeCard)를 가로로 깔고, available을 끈 모드는 "준비 중"으로 잠근다.
/// 카드마다 모드를 보여주는 그림(artTemplate)과 테마 색이 들어간다.
///
/// [연출]
///   - 열릴 때: 뒤의 로비가 흐려지고 HUD는 사라지며, 제목이 내려오고 카드들이 아래에서 차례로 떠오른다.
///   - 고를 때: 카드가 번쩍 튀고, 창이 사라지며 모드가 시작된다. Esc/뒤로 버튼으로 닫는다.
///
/// [모드별 동작]
///   - 1대1 대전: 매칭을 걸고 기다리는 동안 연습장에서 연습(NetworkLauncher.StartMatchmakingWithPractice).
///     로비 오브젝트를 지우는 연출은 연습과 같다.
///   - AI 대전: NetworkLauncher.StartAiMatch()로 바로 오프라인 AI 전
///   - 연습: 로비의 건물/포탈 등(fadeOutOnPractice)을 서서히 지운 뒤 NetworkLauncher.StartPractice()
///
/// [씬 설정]
///   - 로비 Canvas 아래 패널에 붙인다. 숨김은 CanvasGroup으로 하므로 켜둔 채로 둔다.
///   - modeCardTemplate: 꺼둔 모드 카드 하나(ModeCard). modes마다 복제해서 buttonContainer에 깐다.
///   - 각 모드의 artTemplate: 카드 위쪽에 들어갈 그림 묶음(꺼둔 오브젝트). 복제해서 카드에 넣는다.
/// </summary>
public class ModeSelectPanel : MonoBehaviour
{
    [Serializable]
    private class ModeEntry
    {
        public GameModeOption mode;
        public string title;
        [TextArea] public string description;
        public bool available = true;
        [Tooltip("카드 아래쪽 작은 태그(예: 실시간 매칭, 바로 시작).")]
        public string tag;
        public Color themeColor = new Color(0.5f, 0.75f, 1f, 1f);
        public Sprite background;
        public Color backgroundTint = Color.white;
        [Tooltip("카드 위쪽 그림 묶음(씬에 꺼둔 오브젝트).")]
        public GameObject artTemplate;
    }

    [SerializeField] private ModeEntry[] modes =
    {
        new ModeEntry { mode = GameModeOption.OneVsOne, title = "1대1 대전", description = "다른 플레이어와 겨룹니다. 상대를 찾는 동안 연습장에서 연습합니다.", available = true },
        new ModeEntry { mode = GameModeOption.AiMatch, title = "AI 대전", description = "AI 상대로 바로 대전합니다.", available = true },
        new ModeEntry { mode = GameModeOption.Practice, title = "연습", description = "연습 봇을 상대로 스킬을 자유롭게 써봅니다.", available = true },
        new ModeEntry { mode = GameModeOption.TwoVsTwo, title = "2대2 대전", description = "준비 중인 모드입니다.", available = false },
    };

    [SerializeField] private RectTransform buttonContainer;
    [SerializeField] private ModeCard modeCardTemplate;
    [SerializeField] private Button closeButton;
    [SerializeField] private MatchmakingManager matchmaking;

    [Header("연출")]
    [SerializeField] private RectTransform headerRoot;
    [Min(0.01f)] [SerializeField] private float openDuration = 0.3f;
    [Min(0.01f)] [SerializeField] private float closeDuration = 0.2f;
    [Tooltip("창이 열려 있는 동안 뒤의 로비 화면을 흐리게 하는 배경 블러.")]
    [SerializeField] private UIBackgroundBlur backgroundBlur;
    [Tooltip("창이 열려 있는 동안 페이드아웃해서 숨길 로비 HUD.")]
    [SerializeField] private CanvasGroup[] hideWhileOpen;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip selectSound;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.6f;

    [Header("포탈 입장 연출")]
    [Tooltip("모드를 고르면 카메라가 포탈로 줌인할 크기(orthographicSize).")]
    [SerializeField] private float portalZoomSize = 3.2f;
    [Min(0.1f)] [SerializeField] private float suckDuration = 0.75f;
    [Tooltip("로딩 화면이 페이드로 다 덮일 때까지 기다린 뒤 씬 전환을 시작한다.")]
    [Min(0f)] [SerializeField] private float loadingCoverDelay = 0.5f;
    [Tooltip("빨려 들어갈 때 화면 가장자리에 조여 오는 소용돌이 셰이더(Custom/UI/PortalVortex).")]
    [SerializeField] private Shader vortexShader;
    [Tooltip("캐릭터 기준점(발밑)에서 몸 가운데까지 높이(캐릭터 스케일 1 기준).")]
    [SerializeField] private float bodyCenterOffset = 0.9f;
    [SerializeField] private AudioClip suckSound;
    [SerializeField] private AudioClip enterPortalSound;

    [Header("연습장 전환 연출")]
    [Tooltip("연습을 고르면 서서히 사라질 로비 오브젝트들(마을 건물, 포탈 등).")]
    [SerializeField] private Transform[] fadeOutOnPractice;
    [SerializeField] private float practiceFadeDuration = 0.7f;

    public static ModeSelectPanel Instance { get; private set; }
    public bool IsOpen { get; private set; }

    private CanvasGroup canvasGroup;
    private bool lockedByMe;
    private bool starting;
    private bool portalSequence;
    private readonly List<ModeCard> cards = new List<ModeCard>();
    private Coroutine fadeRoutine;
    private float visibleAmount;
    private Vector2 headerBase;

    private void Awake()
    {
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (matchmaking == null)
            matchmaking = FindObjectOfType<MatchmakingManager>();
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (headerRoot != null)
            headerBase = headerRoot.anchoredPosition;

        BuildButtons();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        ReleaseLock();
    }

    private void Update()
    {
        if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    public void Open()
    {
        if (IsOpen || starting)
            return;

        IsOpen = true;
        SetVisible(true);
        SoundManager.Instance?.PlaySfx(openSound, sfxVolume);
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].SetDimmed(false);
            cards[i].PlayReveal(0.06f + i * 0.07f);
        }
        if (!CharacterBase.IsLobbyControlLocked)
        {
            lockedByMe = true;
            CharacterBase.SetLobbyControlLocked(true);
            LobbyCharacterController.SetLocked(true);
        }
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        SoundManager.Instance?.PlaySfx(closeSound, sfxVolume);
        SetVisible(false);
        ReleaseLock();
    }

    private void ReleaseLock()
    {
        if (!lockedByMe)
            return;
        lockedByMe = false;
        CharacterBase.SetLobbyControlLocked(false);
        LobbyCharacterController.SetLocked(false);
    }

    private void BuildButtons()
    {
        if (buttonContainer == null || modeCardTemplate == null)
            return;

        modeCardTemplate.gameObject.SetActive(false);
        foreach (ModeEntry entry in modes)
        {
            ModeCard card = Instantiate(modeCardTemplate, buttonContainer);
            card.gameObject.SetActive(true);
            card.name = entry.mode.ToString();
            card.Bind(entry.title, entry.description, entry.available ? entry.tag : "준비 중",
                entry.themeColor, entry.background, entry.backgroundTint, entry.artTemplate, entry.available);

            GameModeOption mode = entry.mode;
            card.Clicked += c => Select(mode, c);
            cards.Add(card);
        }
    }

    private void Select(GameModeOption mode, ModeCard picked)
    {
        if (starting)
            return;

        if (mode != GameModeOption.OneVsOne && mode != GameModeOption.AiMatch && mode != GameModeOption.Practice)
            return;

        // 누르는 즉시 창을 닫고, 포탈로 빨려 들어가는 연출 → 로딩 화면 페이드 → 씬 전환.
        // 조작 잠금(lockedByMe)은 창을 열 때 건 것을 그대로 유지한다(씬이 바뀌며 풀린다).
        starting = true;
        portalSequence = true;
        SoundManager.Instance?.PlaySfx(selectSound, sfxVolume);
        picked?.PlayConfirm();
        IsOpen = false;
        SetVisible(false);
        StartCoroutine(PortalSequence(mode));
    }

    /// <summary>카메라가 포탈로 줌인하고, 캐릭터가 살짝 움츠렸다가 포탈 가운데로 빙글 돌며 작아지며 빨려 들어간다.
    /// 다 들어가면 로딩 화면이 페이드로 덮이고, 덮인 뒤에 실제 씬 전환을 시작한다.</summary>
    private IEnumerator PortalSequence(GameModeOption mode)
    {
        Portal portal = FindObjectOfType<Portal>();
        LobbyCharacterChange player = LobbyCharacterChange.Instance;

        if (portal != null && LobbyCameraFocusController.Instance != null)
            LobbyCameraFocusController.Instance.FocusOn(portal.transform, portalZoomSize);

        if (portal != null && player != null)
        {
            Transform body = player.transform;
            var rb = body.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
            }
            var controller = body.GetComponent<LobbyCharacterController>();
            if (controller != null)
                controller.enabled = false;

            Vector3 startPos = body.position;
            Vector3 baseScale = body.localScale;
            // 포탈 오브젝트의 기준점이 그림 가운데가 아닐 수 있어서, 실제로 보이는 그림의 정중앙을 노린다.
            var portalSprite = portal.GetComponent<SpriteRenderer>();
            Vector3 target = portalSprite != null ? portalSprite.bounds.center : portal.transform.position;
            target.z = startPos.z;
            // 캐릭터 기준점(발밑)이 아니라 몸 가운데가 포탈 중앙에 오도록 몸 높이 절반만큼 보정한다.
            target.y -= bodyCenterOffset * baseScale.y;

            Material vortex = CreateVortexOverlay();

            // 1) 움츠림: 아래로 눌렸다가
            yield return Animate(0.22f, t =>
            {
                float k = Mathf.Sin(t * Mathf.PI * 0.5f);
                body.localScale = new Vector3(baseScale.x * (1f + 0.18f * k), baseScale.y * (1f - 0.2f * k), baseScale.z);
                if (vortex != null) vortex.SetFloat(VortexIntensityId, 0.25f * t);
            });

            // 2) 빨려 들어감: 처음엔 천천히 끌리다 점점 빨라지며 빙글 돌고 작아진다.
            SoundManager.Instance?.PlaySfx(suckSound, sfxVolume);
            // 아주 낮은 호로 끌려가 포탈 정중앙에 딱 들어가게(높이 올라갔다 떨어지지 않게).
            Vector3 control = Vector3.Lerp(startPos, target, 0.5f) + Vector3.up * 0.25f;
            float spinDir = target.x >= startPos.x ? -1f : 1f;
            yield return Animate(suckDuration, t =>
            {
                // 앞 75%: 점점 빨라지며 포탈 정중앙까지 끌려온다. 뒤 25%: 정중앙에서 빙글 돌며 작아져 사라진다.
                float travel = Mathf.Clamp01(t / 0.65f);
                float e = travel * travel;
                // 포탈은 다가가면 바닥 기준으로 커져서 중심이 조금씩 움직인다 → 매 프레임 실제 중심을 다시 잡는다.
                if (portalSprite != null)
                {
                    Vector3 live = portalSprite.bounds.center;
                    target = new Vector3(live.x, live.y - bodyCenterOffset * baseScale.y, startPos.z);
                }
                Vector3 a = Vector3.Lerp(startPos, control, e);
                Vector3 b = Vector3.Lerp(control, target, e);
                body.position = Vector3.Lerp(a, b, e);
                // 끝으로 갈수록 천천히 사라지게(뚝 꺼지지 않게) 부드러운 곡선으로 줄인다.
                float shrink = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, t));
                float s = Mathf.Lerp(1f, 0f, shrink);
                float stretch = 1f + Mathf.Sin(travel * Mathf.PI) * 0.35f;
                body.localScale = new Vector3(baseScale.x * s / stretch, baseScale.y * s * stretch, baseScale.z);
                body.rotation = Quaternion.Euler(0f, 0f, spinDir * (360f * e + 720f * shrink));
                portal.transform.localScale = portalBaseScale(portal) * (1f + 0.15f * Mathf.Sin(t * Mathf.PI));
                // 화면 가장자리 소용돌이가 점점 조여 온다.
                if (vortex != null) vortex.SetFloat(VortexIntensityId, Mathf.Lerp(0.25f, 0.9f, Mathf.SmoothStep(0f, 1f, t)));
            });
            body.localScale = Vector3.zero;
            SoundManager.Instance?.PlaySfx(enterPortalSound, sfxVolume);

            // 3) 끊김 없이 이어서: 로딩 화면이 페이드로 덮이는 동안 소용돌이는 계속 조여 오고 카메라는 포탈로 더 밀고 들어간다.
            PlayTransition(mode);
            if (LobbyCameraFocusController.Instance != null)
                LobbyCameraFocusController.Instance.FocusOn(portal.transform, portalZoomSize * 0.6f);
            yield return Animate(loadingCoverDelay, t =>
            {
                if (vortex != null) vortex.SetFloat(VortexIntensityId, Mathf.Lerp(0.9f, 1f, t));
            });
        }
        else
        {
            PlayTransition(mode);
            yield return Animate(loadingCoverDelay, t => { });
        }

        switch (mode)
        {
            case GameModeOption.OneVsOne:
                NetworkLauncher.Instance?.StartMatchmakingWithPractice();
                break;
            case GameModeOption.AiMatch:
                NetworkLauncher.Instance?.StartAiMatch();
                break;
            case GameModeOption.Practice:
                NetworkLauncher.Instance?.StartPractice();
                break;
        }
    }

    private static readonly int VortexIntensityId = Shader.PropertyToID("_Intensity");

    /// <summary>화면 전체를 덮는 소용돌이 오버레이(로딩 화면보다 아래)를 만든다. 씬이 바뀌면 같이 사라진다.</summary>
    private Material CreateVortexOverlay()
    {
        if (vortexShader == null)
            return null;
        var go = new GameObject("Portal Vortex Overlay");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4000;
        var image = new GameObject("Vortex", typeof(RectTransform)).AddComponent<RawImage>();
        image.transform.SetParent(go.transform, false);
        image.raycastTarget = false;
        RectTransform rt = image.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var material = new Material(vortexShader);
        material.SetFloat("_Aspect", (float)Screen.width / Mathf.Max(1, Screen.height));
        material.SetFloat(VortexIntensityId, 0f);
        image.material = material;
        return material;
    }

    private Vector3 portalScale;
    private bool portalScaleCaptured;

    private Vector3 portalBaseScale(Portal portal)
    {
        if (!portalScaleCaptured)
        {
            portalScaleCaptured = true;
            portalScale = portal.transform.localScale;
        }
        return portalScale;
    }

    private static IEnumerator Animate(float duration, Action<float> step)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            step(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        step(1f);
    }

    /// <summary>모드 이름과 테마 색으로 화면 전환 연출을 띄운다(다음 씬이 열리면 알아서 걷힌다).</summary>
    private void PlayTransition(GameModeOption mode)
    {
        if (SceneTransition.Instance == null)
            return;

        foreach (ModeEntry entry in modes)
        {
            if (entry.mode != mode)
                continue;

            string subtitle = mode == GameModeOption.OneVsOne ? "매칭을 돌리며 연습장으로 이동합니다"
                : mode == GameModeOption.AiMatch ? "전투를 준비하는 중"
                : "연습장으로 이동하는 중";
            SceneTransition.Instance.Play(entry.title, subtitle, entry.themeColor);
            return;
        }
    }

    // 로비 오브젝트를 서서히 지운 뒤 연습장을 연다(연습장에서는 탈출구와 연습 봇이 서서히 나타난다).
    private IEnumerator PracticeTransition(Action onFaded)
    {
        var sprites = new List<SpriteRenderer>();
        var texts = new List<TMP_Text>();
        if (fadeOutOnPractice != null)
        {
            foreach (Transform root in fadeOutOnPractice)
            {
                if (root == null)
                    continue;
                sprites.AddRange(root.GetComponentsInChildren<SpriteRenderer>());
                texts.AddRange(root.GetComponentsInChildren<TMP_Text>());
            }
        }

        float[] spriteAlpha = new float[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
            spriteAlpha[i] = sprites[i].color.a;
        float[] textAlpha = new float[texts.Count];
        for (int i = 0; i < texts.Count; i++)
            textAlpha[i] = texts[i].alpha;

        float elapsed = 0f;
        while (elapsed < practiceFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, practiceFadeDuration));
            for (int i = 0; i < sprites.Count; i++)
            {
                if (sprites[i] == null) continue;
                Color c = sprites[i].color;
                c.a = spriteAlpha[i] * k;
                sprites[i].color = c;
            }
            for (int i = 0; i < texts.Count; i++)
            {
                if (texts[i] != null)
                    texts[i].alpha = textAlpha[i] * k;
            }
            yield return null;
        }

        onFaded?.Invoke();
    }

    /// <summary>창을 부드럽게 보이거나 숨긴다. 배경 블러와 HUD 숨김도 같이 따라간다.</summary>
    private void SetVisible(bool visible)
    {
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
        if (backgroundBlur != null)
            backgroundBlur.SetTarget(visible ? 1f : 0f);

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(visible ? 1f : 0f, visible ? openDuration : closeDuration));
    }

    private IEnumerator FadeRoutine(float target, float duration)
    {
        float from = visibleAmount;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyVisible(Mathf.Lerp(from, target, target > from ? 1f - Mathf.Pow(1f - t, 3f) : t * t));
            yield return null;
        }
        ApplyVisible(target);
        fadeRoutine = null;
    }

    private void ApplyVisible(float v)
    {
        visibleAmount = v;
        canvasGroup.alpha = v;
        if (headerRoot != null)
            headerRoot.anchoredPosition = headerBase + new Vector2(0f, (1f - v) * 30f);

        if (hideWhileOpen == null)
            return;
        foreach (CanvasGroup hud in hideWhileOpen)
        {
            if (hud == null)
                continue;
            // 포탈 입장 연출 중엔 HUD를 다시 띄우지 않는다.
            hud.alpha = portalSequence ? 0f : 1f - v;
            hud.blocksRaycasts = !portalSequence && v < 0.5f;
        }
    }
}
