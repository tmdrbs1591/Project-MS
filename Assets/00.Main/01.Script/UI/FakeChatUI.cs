using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 로비의 가짜 채팅창. 실제 네트워크 채팅이 아니라 "사람 많은 온라인 게임 로비" 분위기를 내기 위한 연출이다.
/// 채팅 내용은 전부 영어.
///
/// [진짜처럼 보이게 하는 것들]
///   - 랜덤한 한 줄이 아니라 <b>대화 스레드</b>가 흘러간다. 누가 물으면 다른 사람이 답하고, 또 받아친다.
///   - 답장은 글자 수에 비례해 늦게 온다(타이핑하는 시간). 스레드 사이엔 길게 조용하기도 하다.
///   - 접속자 명단이 따로 있고, 그 안의 사람들만 떠든다. 입장/퇴장하면 실제로 명단이 바뀌고
///     방금 들어온 사람이 인사하기도 한다.
///   - 내가 채팅을 치면 가끔(전부는 아니고) 누가 반응한다. 무시당하기도 하는 게 더 진짜 같다.
///   - 최근에 나온 대화는 한동안 다시 안 나온다.
///
/// [동작]
///   - 입력창에 치고 전송(또는 Enter)하면 "내이름 : 메시지" 가 노란색으로 올라간다.
///   - 열기/닫기 버튼 두 개로 채팅 로그만 접고 편다(입력창은 그대로 남는다). 둘 중 하나만 보인다.
///   - 채팅창에 포커스가 있는 동안엔 로비 캐릭터가 움직이지 않는다(안 그러면 "wasd"를 치는 순간
///     캐릭터가 같이 뛴다). LobbyCharacterController.SetChatTyping() 사용.
///
/// [씬 설정]
///   - Canvas 하위에 Scroll View를 만들고 Content에 Vertical Layout Group + Content Size Fitter
///     (Vertical Fit = Preferred Size)를 붙인다.
///   - 채팅 한 줄로 쓸 TMP Text를 프리팹으로 만들어 messagePrefab에 넣는다.
///   - 입력창(TMP Input Field), 전송 버튼, ScrollRect, Content를 각각 연결한다.
///   - 접을 영역(chatLogPanel)에는 Scroll View만 넣는다 — 입력창과 버튼을 그 안에 두면
///     접을 때 같이 사라져서 다시 펼 수가 없다.
/// </summary>
public class FakeChatUI : MonoBehaviour
{
    [Header("씬 연결")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private ScrollRect scrollRect;
    [Tooltip("채팅 줄이 쌓일 부모(Scroll View > Viewport > Content).")]
    [SerializeField] private RectTransform content;
    [Tooltip("채팅 한 줄로 복제해서 쓸 TMP Text 프리팹.")]
    [SerializeField] private TMP_Text messagePrefab;

    [Header("열기/닫기")]
    [Tooltip("접었다 폈다 할 채팅 로그 영역(Scroll View 등). 입력창과 버튼은 여기 넣지 말 것.")]
    [SerializeField] private GameObject chatLogPanel;
    [Tooltip("채팅창을 접는 버튼. 열려 있을 때만 보인다.")]
    [SerializeField] private Button closeButton;
    [Tooltip("채팅창을 펴는 버튼. 접혀 있을 때만 보인다.")]
    [SerializeField] private Button openButton;
    [SerializeField] private bool startOpen = true;

    [Header("내 정보")]
    [SerializeField] private string myName = "Me";
    [SerializeField] private Color myColor = new Color(1f, 0.85f, 0.2f);
    [Tooltip("남의 채팅 본문 색. 닉네임만 사람마다 다른 색으로 나온다.")]
    [SerializeField] private Color otherMessageColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color systemColor = new Color(0.55f, 0.6f, 0.65f);

    [Header("흐름")]
    [SerializeField] private bool autoChatter = true;
    [Tooltip("동시에 로비에 접속해 있는(=떠들 수 있는) 인원 수.")]
    [Min(2)] [SerializeField] private int onlineCount = 10;
    [Tooltip("대화 하나가 끝나고 다음 대화가 시작되기까지의 간격(초).")]
    [SerializeField] private Vector2 threadGapRange = new Vector2(4f, 13f);
    [Tooltip("가끔 채팅창이 이만큼 조용해진다(초). 계속 떠들기만 하면 오히려 가짜 같다.")]
    [SerializeField] private Vector2 lullRange = new Vector2(16f, 32f);
    [Range(0f, 1f)] [SerializeField] private float lullChance = 0.22f;
    [Tooltip("대화 대신 혼잣말 한 줄만 올라올 확률.")]
    [Range(0f, 1f)] [SerializeField] private float fillerChance = 0.3f;
    [Tooltip("입장/퇴장 안내가 뜰 확률.")]
    [Range(0f, 1f)] [SerializeField] private float joinLeaveChance = 0.1f;
    [Tooltip("내가 채팅을 쳤을 때 누군가 반응할 확률. 100%면 오히려 가짜 같다.")]
    [Range(0f, 1f)] [SerializeField] private float replyToMeChance = 0.35f;
    [Min(10)] [SerializeField] private int maxMessages = 80;
    [Tooltip("Enter로 입력창 포커스, Esc로 해제.")]
    [SerializeField] private bool enterToFocus = true;

    // ── 대사 데이터 ────────────────────────────────────────────────────────────

    private static readonly string[] Names =
    {
        "xXShadowXx", "Mike_99", "Luna", "Kenji", "NoScope420", "BlueFalcon",
        "toasted_bread", "Vex", "SleepyPanda", "M4RCUS", "zeroping", "grimm",
        "pastel", "notaboT", "Ravioli", "dust", "TinyTank", "OwenR",
        "96kmh", "crumbs", "Halcyon", "mika", "BigYoshi", "quietstorm",
    };

    /// <summary>대화 한 줄. Speaker는 이 대화의 몇 번째 참가자가 말하는지(0,1,2...).</summary>
    private readonly struct ScriptedLine
    {
        public readonly int Speaker;
        public readonly string Text;

        public ScriptedLine(int speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }
    }

    private static ScriptedLine L(int speaker, string text) => new ScriptedLine(speaker, text);

    /// <summary>대화 한 덩어리. 참가자 수는 등장하는 Speaker 번호에서 자동으로 계산한다.</summary>
    private static readonly ScriptedLine[][] Threads =
    {
        new[] { L(0, "anyone up for 2v2?"), L(1, "im in"), L(0, "cool, inviting you") },
        new[] { L(0, "that ult is so broken lol"), L(1, "skill issue"), L(0, "bro") },
        new[] { L(0, "gg"), L(1, "gg"), L(2, "gg wp") },
        new[] { L(0, "how do i change character"), L(1, "theres a building on the left"), L(0, "oh found it, ty") },
        new[] { L(0, "my ping is like 300 rip"), L(1, "same since yesterday"), L(0, "servers are cooked") },
        new[] { L(0, "wait is that a bug"), L(1, "what"), L(0, "getting stuck in the wall"), L(1, "oh yeah thats always been like that") },
        new[] { L(0, "new here, any tips?"), L(1, "just pick anyone and play, youll figure it out"), L(0, "ok ty") },
        new[] { L(0, "one more?"), L(1, "cant, sleeping. gn"), L(2, "gn"), L(0, "night") },
        new[] { L(0, "spark feels a bit strong"), L(1, "yeah the ult is nuts"), L(2, "or you guys are just bad"), L(1, "lmao") },
        new[] { L(0, "brb food"), L(1, "k") },
        new[] { L(0, "who is the best char rn"), L(1, "zipper if you can aim"), L(2, "nah popup"), L(1, "popup is free kills") },
        new[] { L(0, "that was so close"), L(1, "you had him"), L(0, "i panicked") },
        new[] { L(0, "anyone else lagging"), L(1, "nope fine here"), L(0, "must be me then") },
        new[] { L(0, "how long is queue"), L(1, "like 10 sec"), L(0, "oh not bad") },
        new[] { L(0, "lol what was that"), L(1, "?"), L(0, "you flew across the map"), L(1, "thats the dash lmao") },
        new[] { L(0, "team?"), L(1, "sure"), L(0, "inv sent") },
        new[] { L(0, "im so done with this map"), L(1, "same its way too small"), L(2, "i like it tho") },
        new[] { L(0, "first win lets goooo"), L(1, "congrats"), L(2, "grats") },
    };

    private static readonly string[] Fillers =
    {
        "lol", "brb", "anyone here", "lets gooo", "wait what", "one more?", "nice",
        "im so bad at this", "hello?", "this is fun", "afk 2 min", "wow", "rip",
    };

    private static readonly string[] Reactions =
    {
        "lol", "hi", "who dis", "yea", "?", "welcome", "true", "nah", "hey", "sure", "lmao",
    };

    private static readonly string[] Greetings = { "hey", "hello", "sup", "hi all", "yo" };

    private static readonly Color[] NamePalette =
    {
        new Color(0.45f, 0.78f, 1f),
        new Color(0.62f, 0.9f, 0.5f),
        new Color(1f, 0.6f, 0.75f),
        new Color(0.8f, 0.68f, 1f),
        new Color(1f, 0.72f, 0.45f),
        new Color(0.5f, 0.92f, 0.86f),
    };

    // ── 상태 ──────────────────────────────────────────────────────────────────

    private readonly List<int> online = new List<int>();
    private readonly List<int> offline = new List<int>();
    private readonly List<TMP_Text> lines = new List<TMP_Text>();
    private readonly Queue<int> recentThreads = new Queue<int>();

    private ScriptedLine[] activeThread;
    private int[] activeSpeakers;
    private int activeLineIndex = -1;
    private float nextEventTime;
    private float pendingReplyTime = float.MaxValue;
    // 방금 입장한 사람이 인사할 예정이면 여기 담겨 있다가 다음 이벤트 때 나간다.
    private (int User, string Text)? pendingGreeting;

    private bool wasFocused;
    private bool isOpen = true;

    // ── 라이프사이클 ───────────────────────────────────────────────────────────

    private void Awake()
    {
        BuildRoster();

        if (messagePrefab != null)
            messagePrefab.gameObject.SetActive(false);

        if (sendButton != null)
            sendButton.onClick.AddListener(SendMyMessage);

        if (closeButton != null)
            closeButton.onClick.AddListener(() => SetChatPanelOpen(false));

        if (openButton != null)
            openButton.onClick.AddListener(() => SetChatPanelOpen(true));

        SetChatPanelOpen(startOpen);

        if (inputField != null)
        {
            // onSubmit은 Enter를 눌렀을 때만 온다. onEndEdit는 포커스가 풀려도 와서
            // 다른 곳을 클릭했을 뿐인데 빈 메시지가 나가버린다.
            inputField.onSubmit.AddListener(_ => SendMyMessage());
            inputField.lineType = TMP_InputField.LineType.SingleLine;
        }

        // 들어오자마자 대화가 시작되면 짜여진 티가 난다. 잠깐 뒤부터.
        nextEventTime = Time.unscaledTime + Random.Range(1.5f, 5f);
    }

    private void OnDestroy()
    {
        // 채팅창이 켜진 채로 씬이 바뀌어도 로비 이동이 잠긴 채 남지 않게.
        LobbyCharacterController.SetChatTyping(false);
    }

    private void Update()
    {
        UpdateTypingLock();
        HandleFocusHotkeys();

        if (!autoChatter)
            return;

        float now = Time.unscaledTime;

        if (now >= pendingReplyTime)
        {
            pendingReplyTime = float.MaxValue;
            SpeakReactionToMe();
        }

        if (now >= nextEventTime)
            AdvanceChatter();
    }

    // ── 채팅 흐름 ─────────────────────────────────────────────────────────────

    /// <summary>대화가 진행 중이면 다음 줄을, 아니면 새 이벤트(대화/혼잣말/입퇴장)를 시작한다.</summary>
    private void AdvanceChatter()
    {
        if (pendingGreeting.HasValue)
        {
            (int user, string text) = pendingGreeting.Value;
            pendingGreeting = null;
            Say(user, text);
            ScheduleNextEvent();
            return;
        }

        if (activeLineIndex >= 0)
        {
            ContinueThread();
            return;
        }

        if (Random.value < joinLeaveChance)
        {
            PlayJoinOrLeave();

            // 인사할 사람이 예약됐으면 곧바로(타이핑 시간만큼 뒤에) 인사부터 나가게 한다.
            if (pendingGreeting.HasValue)
                nextEventTime = Time.unscaledTime + TypingDelay(pendingGreeting.Value.Text);
            else
                ScheduleNextEvent();

            return;
        }

        if (Random.value < fillerChance)
        {
            SpeakFiller();
            ScheduleNextEvent();
            return;
        }

        StartThread();
    }

    private void StartThread()
    {
        int index = PickThreadIndex();
        if (index < 0)
        {
            ScheduleNextEvent();
            return;
        }

        activeThread = Threads[index];
        activeSpeakers = AssignSpeakers(CountSpeakers(activeThread));
        if (activeSpeakers == null)
        {
            // 접속자가 참가자 수보다 적으면 이번 판은 건너뛴다.
            activeLineIndex = -1;
            ScheduleNextEvent();
            return;
        }

        activeLineIndex = 0;
        ContinueThread();
    }

    private void ContinueThread()
    {
        ScriptedLine line = activeThread[activeLineIndex];
        Say(activeSpeakers[line.Speaker], line.Text);

        activeLineIndex++;

        if (activeLineIndex >= activeThread.Length)
        {
            activeLineIndex = -1;
            ScheduleNextEvent();
            return;
        }

        // 다음 줄은 "읽고 타이핑하는 시간"만큼 뒤에 온다.
        nextEventTime = Time.unscaledTime + TypingDelay(activeThread[activeLineIndex].Text);
    }

    /// <summary>다음 이벤트까지의 간격. 가끔은 꽤 길게 조용해진다.</summary>
    private void ScheduleNextEvent()
    {
        float delay = Random.value < lullChance
            ? Random.Range(lullRange.x, lullRange.y)
            : Random.Range(threadGapRange.x, threadGapRange.y);

        nextEventTime = Time.unscaledTime + delay;
    }

    /// <summary>글자 수에 비례한 타이핑 시간. 짧은 "k"는 바로, 긴 문장은 늦게 올라온다.</summary>
    private static float TypingDelay(string text)
    {
        return Random.Range(0.7f, 1.9f) + text.Length * Random.Range(0.015f, 0.045f);
    }

    private static int CountSpeakers(ScriptedLine[] thread)
    {
        int max = 0;
        foreach (ScriptedLine line in thread)
            max = Mathf.Max(max, line.Speaker);

        return max + 1;
    }

    /// <summary>최근에 쓴 대화는 한동안 다시 고르지 않는다(같은 대화가 반복되면 바로 티가 난다).</summary>
    private int PickThreadIndex()
    {
        for (int attempt = 0; attempt < 12; attempt++)
        {
            int index = Random.Range(0, Threads.Length);
            if (recentThreads.Contains(index))
                continue;

            recentThreads.Enqueue(index);
            while (recentThreads.Count > Mathf.Min(8, Threads.Length - 2))
                recentThreads.Dequeue();

            return index;
        }

        return -1;
    }

    /// <summary>대화 참가자를 서로 다른 접속자로 배정한다.</summary>
    private int[] AssignSpeakers(int count)
    {
        if (online.Count < count)
            return null;

        int[] speakers = new int[count];
        for (int slot = 0; slot < count; slot++)
        {
            int picked;
            do
            {
                picked = online[Random.Range(0, online.Count)];
            }
            while (System.Array.IndexOf(speakers, picked, 0, slot) >= 0);

            speakers[slot] = picked;
        }

        return speakers;
    }

    private void SpeakFiller()
    {
        if (online.Count == 0)
            return;

        Say(online[Random.Range(0, online.Count)], Fillers[Random.Range(0, Fillers.Length)]);
    }

    /// <summary>내가 친 채팅에 대한 반응. 항상 반응하면 오히려 어색해서 확률로 걸러진다.</summary>
    private void SpeakReactionToMe()
    {
        if (online.Count == 0)
            return;

        Say(online[Random.Range(0, online.Count)], Reactions[Random.Range(0, Reactions.Length)]);
    }

    private void PlayJoinOrLeave()
    {
        bool joining = offline.Count > 0 && (online.Count <= 4 || Random.value < 0.55f);

        if (joining)
        {
            int index = Random.Range(0, offline.Count);
            int user = offline[index];
            offline.RemoveAt(index);
            online.Add(user);

            AppendSystemMessage($"{Names[user]} joined the lobby.");

            // 들어오자마자 인사하는 사람도 있다(항상은 아니고).
            if (Random.value < 0.45f)
                pendingGreeting = (user, Greetings[Random.Range(0, Greetings.Length)]);

            return;
        }

        if (online.Count <= 3)
            return;

        int slot = Random.Range(0, online.Count);
        int leaver = online[slot];
        online.RemoveAt(slot);
        offline.Add(leaver);
        AppendSystemMessage($"{Names[leaver]} left the lobby.");
    }

    // ── 출력 ──────────────────────────────────────────────────────────────────

    private void Say(int user, string text)
    {
        Color nameColor = NamePalette[user % NamePalette.Length];
        AppendLine($"{Colored(Names[user], nameColor)} : {Colored(text, otherMessageColor)}");
    }

    /// <summary>전송 버튼 / Enter 에서 호출. 빈 줄은 무시한다.</summary>
    public void SendMyMessage()
    {
        if (inputField == null)
            return;

        string text = inputField.text.Trim();
        inputField.text = string.Empty;

        if (!string.IsNullOrEmpty(text))
        {
            AppendLine($"{Colored(myName, myColor)} : {Colored(text, myColor)}");

            // 가끔 누가 반응한다.
            if (autoChatter && Random.value < replyToMeChance)
                pendingReplyTime = Time.unscaledTime + Random.Range(1.4f, 4.5f);
        }

        // 보내고 나서도 계속 칠 수 있게 포커스를 유지한다.
        inputField.ActivateInputField();
    }

    /// <summary>바깥에서도 안내 한 줄 끼워 넣고 싶을 때(예: 매칭 시작 안내).</summary>
    public void AppendSystemMessage(string message)
    {
        AppendLine(Colored(message, systemColor));
    }

    private void AppendLine(string richText)
    {
        if (messagePrefab == null || content == null)
        {
            Debug.LogWarning($"[{nameof(FakeChatUI)}] messagePrefab / content 가 연결되지 않았습니다.", this);
            return;
        }

        TMP_Text line = Instantiate(messagePrefab, content);
        line.gameObject.SetActive(true);
        line.richText = true;
        line.text = richText;
        lines.Add(line);

        while (lines.Count > maxMessages)
        {
            TMP_Text oldest = lines[0];
            lines.RemoveAt(0);
            if (oldest != null)
                Destroy(oldest.gameObject);
        }

        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        if (scrollRect == null)
            return;

        // 방금 추가한 줄까지 레이아웃이 계산돼야 맨 아래 위치가 제대로 나온다.
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private static string Colored(string text, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    }

    // ── 접속자 명단 / UI ───────────────────────────────────────────────────────

    /// <summary>전체 명단을 섞어서 앞쪽 onlineCount 명만 지금 접속 중인 걸로 둔다.</summary>
    private void BuildRoster()
    {
        online.Clear();
        offline.Clear();

        List<int> shuffled = new List<int>(Names.Length);
        for (int i = 0; i < Names.Length; i++)
            shuffled.Add(i);

        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        int count = Mathf.Clamp(onlineCount, 2, shuffled.Count);
        for (int i = 0; i < shuffled.Count; i++)
        {
            if (i < count)
                online.Add(shuffled[i]);
            else
                offline.Add(shuffled[i]);
        }
    }

    /// <summary>채팅 로그를 접었다 폈다 한다(입력창은 항상 남는다).
    /// 열기/닫기 버튼은 서로 번갈아 하나만 보인다.</summary>
    public void SetChatPanelOpen(bool open)
    {
        isOpen = open;

        if (chatLogPanel != null)
            chatLogPanel.SetActive(open);

        if (closeButton != null)
            closeButton.gameObject.SetActive(open);

        if (openButton != null)
            openButton.gameObject.SetActive(!open);

        if (open)
            ScrollToBottom();
    }

    /// <summary>한 버튼으로 토글하고 싶을 때(버튼 OnClick에 직접 연결).</summary>
    public void ToggleChatPanel() => SetChatPanelOpen(!isOpen);

    /// <summary>입력창에 포커스가 들어간 동안 로비 캐릭터를 세워둔다.</summary>
    private void UpdateTypingLock()
    {
        bool focused = inputField != null && inputField.isFocused;
        if (focused == wasFocused)
            return;

        wasFocused = focused;
        LobbyCharacterController.SetChatTyping(focused);
    }

    private void HandleFocusHotkeys()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || inputField == null)
            return;

        if (enterToFocus && !inputField.isFocused
            && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
        {
            inputField.ActivateInputField();
            return;
        }

        if (inputField.isFocused && kb.escapeKey.wasPressedThisFrame)
            inputField.DeactivateInputField();
    }
}
