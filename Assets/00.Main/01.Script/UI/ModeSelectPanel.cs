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
    [Tooltip("모드를 누른 뒤 카드 반응을 보여주고 로딩으로 넘어가기까지의 시간(초).")]
    [Min(0f)] [SerializeField] private float confirmDelay = 0.45f;

    [Header("연습장 전환 연출")]
    [Tooltip("연습을 고르면 서서히 사라질 로비 오브젝트들(마을 건물, 포탈 등).")]
    [SerializeField] private Transform[] fadeOutOnPractice;
    [SerializeField] private float practiceFadeDuration = 0.7f;

    public static ModeSelectPanel Instance { get; private set; }
    public bool IsOpen { get; private set; }

    private CanvasGroup canvasGroup;
    private bool lockedByMe;
    private bool starting;
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

        // 바로 로딩으로 넘어가지 않고, 카드가 눌렸다 튀는 반응과 소리를 먼저 보여준다.
        starting = true;
        SoundManager.Instance?.PlaySfx(selectSound, sfxVolume);
        picked?.PlayConfirm();
        foreach (ModeCard c in cards)
            if (c != picked)
                c.SetDimmed(true);
        StartCoroutine(BeginAfterConfirm(mode));
    }

    private IEnumerator BeginAfterConfirm(GameModeOption mode)
    {
        float end = Time.unscaledTime + confirmDelay;
        while (Time.unscaledTime < end)
            yield return null;

        PlayTransition(mode);
        switch (mode)
        {
            case GameModeOption.OneVsOne:
                SetVisible(false);
                // 접속(1~3초)을 페이드와 동시에 시작해서 기다리는 시간을 줄인다.
                NetworkLauncher.Instance?.StartMatchmakingWithPractice();
                StartCoroutine(PracticeTransition(null));
                break;

            case GameModeOption.AiMatch:
                SetVisible(false);
                NetworkLauncher.Instance?.StartAiMatch();
                break;

            case GameModeOption.Practice:
                SetVisible(false);
                StartCoroutine(PracticeTransition(() => NetworkLauncher.Instance?.StartPractice()));
                break;

            default:
                starting = false;
                break;
        }
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
            hud.alpha = 1f - v;
            hud.blocksRaycasts = v < 0.5f;
        }
    }
}
