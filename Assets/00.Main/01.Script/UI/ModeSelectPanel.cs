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
/// modes 배열 순서대로 버튼을 만들고, available을 끈 모드는 "준비 중"으로 보여주고 누를 수 없게 한다.
///
/// [모드별 동작]
///   - 1대1 대전: 매칭을 걸고 기다리는 동안 연습장에서 연습(NetworkLauncher.StartMatchmakingWithPractice).
///     로비 오브젝트를 지우는 연출은 연습과 같다.
///   - AI 대전: NetworkLauncher.StartAiMatch()로 바로 오프라인 AI 전
///   - 연습: 로비의 건물/포탈 등(fadeOutOnPractice)을 서서히 지운 뒤 NetworkLauncher.StartPractice()
///
/// [씬 설정]
///   - 로비 Canvas 아래 패널에 붙인다. 숨김은 CanvasGroup으로 하므로 켜둔 채로 둔다.
///   - modeButtonTemplate: 꺼둔 버튼 하나. 자식에 "Title", "Description", "Badge"(선택) TMP_Text를 둔다.
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
    }

    [SerializeField] private ModeEntry[] modes =
    {
        new ModeEntry { mode = GameModeOption.OneVsOne, title = "1대1 대전", description = "다른 플레이어와 겨룹니다. 상대를 찾는 동안 연습장에서 연습합니다.", available = true },
        new ModeEntry { mode = GameModeOption.AiMatch, title = "AI 대전", description = "AI 상대로 바로 대전합니다.", available = true },
        new ModeEntry { mode = GameModeOption.Practice, title = "연습", description = "연습 봇을 상대로 스킬을 자유롭게 써봅니다.", available = true },
        new ModeEntry { mode = GameModeOption.TwoVsTwo, title = "2대2 대전", description = "준비 중인 모드입니다.", available = false },
    };

    [SerializeField] private RectTransform buttonContainer;
    [SerializeField] private Button modeButtonTemplate;
    [SerializeField] private Button closeButton;
    [SerializeField] private MatchmakingManager matchmaking;

    [Header("연습장 전환 연출")]
    [Tooltip("연습을 고르면 서서히 사라질 로비 오브젝트들(마을 건물, 포탈 등).")]
    [SerializeField] private Transform[] fadeOutOnPractice;
    [SerializeField] private float practiceFadeDuration = 0.7f;

    public static ModeSelectPanel Instance { get; private set; }
    public bool IsOpen { get; private set; }

    private CanvasGroup canvasGroup;
    private bool lockedByMe;
    private bool starting;

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

        BuildButtons();
        SetVisible(false);
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
        if (buttonContainer == null || modeButtonTemplate == null)
            return;

        modeButtonTemplate.gameObject.SetActive(false);
        foreach (ModeEntry entry in modes)
        {
            Button button = Instantiate(modeButtonTemplate, buttonContainer);
            button.gameObject.SetActive(true);
            button.name = entry.mode.ToString();
            button.interactable = entry.available;

            SetChildText(button.transform, "Title", entry.title);
            SetChildText(button.transform, "Description", entry.description);
            Transform badge = button.transform.Find("Badge");
            if (badge != null)
                badge.gameObject.SetActive(!entry.available);

            GameModeOption mode = entry.mode;
            button.onClick.AddListener(() => Select(mode));
        }
    }

    private static void SetChildText(Transform parent, string childName, string text)
    {
        Transform child = parent.Find(childName);
        if (child != null && child.TryGetComponent(out TMP_Text label))
            label.text = text;
    }

    private void Select(GameModeOption mode)
    {
        if (starting)
            return;

        switch (mode)
        {
            case GameModeOption.OneVsOne:
                starting = true;
                SetVisible(false);
                // 접속(1~3초)을 페이드와 동시에 시작해서 기다리는 시간을 줄인다.
                NetworkLauncher.Instance?.StartMatchmakingWithPractice();
                StartCoroutine(PracticeTransition(null));
                break;

            case GameModeOption.AiMatch:
                starting = true;
                SetVisible(false);
                NetworkLauncher.Instance?.StartAiMatch();
                break;

            case GameModeOption.Practice:
                starting = true;
                SetVisible(false);
                StartCoroutine(PracticeTransition(() => NetworkLauncher.Instance?.StartPractice()));
                break;
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

    private void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
