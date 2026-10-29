using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using ProjectMS.CharacterSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Fusion 2 (Shared 모드) 네트워크 진입점.
///
/// [역할]
///   - NetworkRunner 를 만들고 Shared 모드로 세션에 접속한다.
///   - "빠른 매칭": "quickmatch-0", "quickmatch-1" ... 정해진 이름의 세션에 순서대로
///     StartGame 을 시도한다. 이름이 있는 세션에 대한 StartGame 은 서버가 "있으면 참가,
///     없으면 생성"을 원자적으로 처리하므로, 목록을 조회해 클라이언트가 직접 판단하는
///     방식과 달리 두 클라가 동시에 접속해도 경쟁 조건이 생기지 않는다.
///   - 정원(playerCount)이 차면 Shared 모드 마스터가 게임 씬을 로드한다.
///   - 게임 씬이 준비되면 각 클라가 PlayerSpawner 를 통해 "자기" 캐릭터를 스폰한다.
///
/// [씬 설정]
///   - 로비 씬의 빈 GameObject 에 이 스크립트를 붙인다(NetworkRunner 는 자동 추가됨).
///   - 이 오브젝트는 씬 전환에도 살아남아야 하므로 DontDestroyOnLoad 로 둔다(자동 처리).
///   - 게임 씬에는 PlayerSpawner 를 하나 둔다.
///
/// [전제]
///   - Fusion App Id 가 PhotonAppSettings 에 설정되어 있어야 한다.
///   - Player 프리팹에 NetworkObject 가 붙어 있어야 한다.
/// </summary>
public class NetworkLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkLauncher Instance { get; private set; }

    [Header("매칭")]
    [Tooltip("한 세션 정원. 이 인원이 차면 게임 씬으로 이동")]
    [SerializeField] private int playerCount = 2;

    [Tooltip("게임 씬 이름 (Build Settings 에 등록되어 있어야 함)")]
    [SerializeField] private string gameSceneName = "SampleScene 1";

    [Tooltip("매치 종료 후 돌아갈 로비 씬 이름 (Build Settings 에 등록되어 있어야 함)")]
    [SerializeField] private string lobbySceneName = "Lobby";

    [Header("AI 봇")]
    [Tooltip("혼자 기다리는 시간이 길어지면 AI 상대로 게임을 시작한다. 정원이 1이면 바로 AI 전.")]
    [SerializeField] private bool fillWithBot = true;
    [Tooltip("매칭 버튼을 누른 뒤 이 시간(초) 동안 아무도 안 들어오면 AI 상대로 시작")]
    [SerializeField] private float botFillDelaySeconds = 3f;

    /// <summary>매칭 상태 메시지(연결/대기/성공/실패)를 외부 UI 로 전달한다.</summary>
    public event Action<string> StatusChanged;

    /// <summary>매칭 진행 중 문구. UI 는 이 값을 받으면 뒤에 점(. .. ...)을 반복해서 붙인다.</summary>
    public const string MatchingStatus = "매칭 중";
    public const string MatchedStatus = "매칭 완료!";

    private NetworkRunner runner;
    private bool isMatching;
    private bool playerSpawnedInGameScene; // 게임 씬에서 내 캐릭터를 이미 스폰했는지
    private bool isReturningToLobby; // 버튼 클릭과 OnPlayerLeft 가 동시에 겹쳐 중복 실행되는 것을 막는 가드
    private bool gameSceneRequested; // 게임 씬 로드를 이미 요청했는지(정원 충족/봇 채우기 중복 방지)
    private bool vsBot; // 이번 매치가 AI 상대인지 → 게임 씬에서 내 캐릭터 다음에 봇도 스폰
    private float sessionJoinedTime = -1f; // 세션에 들어간 시각(-1 = 아직 접속 전)
    private float matchStartTime; // 매칭 버튼을 누른 시각(봇 채우기 대기 시간 계산용)
    private bool switchingToOffline; // 대기 세션 → 오프라인 AI 전으로 갈아타는 중(그 사이 셧다운은 무시)
    private bool practiceWhileMatching; // 매칭 대기 중 그 세션 안에서 연습장을 여는 모드인지
    private bool practiceSceneRequested; // 매칭 세션 안에서 연습장 씬을 이미 띄웠는지
    private bool pendingOfflinePractice; // 다음 프레임에 오프라인 연습장을 다시 연다(셧다운 콜백 안에서 바로 열지 않기 위함)

    /// <summary>연습장에서 매칭을 기다리는 중인지(연습장 상단 "매칭 중" 띠 표시용).</summary>
    public bool IsMatchingInPractice => isMatching && practiceWhileMatching && !gameSceneRequested;
    /// <summary>연습장에서 오프라인 ↔ 매칭 세션을 바꾸는 중인지(내 캐릭터가 다시 생길 때까지). 연습장 전환 화면 표시용.</summary>
    public bool IsSwitchingSession { get; private set; }
    /// <summary>전환 중 화면에 띄울 문구.</summary>
    public string SwitchingMessage { get; private set; } = string.Empty;
    /// <summary>매칭을 건 뒤 지난 시간(초).</summary>
    public float MatchingElapsed => Time.unscaledTime - matchStartTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 러너는 PrepareRunner 에서 전용 자식 오브젝트에 생성한다.
        // (러너를 이 오브젝트에 직접 붙이면 Shutdown 이 NetworkLauncher 까지 파괴해 재매칭이 깨진다.)
    }

    /// <summary>매칭 시작(로비에서 기다림). 기존 매칭 UI(MatchmakingManager)에서 호출.</summary>
    public void StartMatchmaking()
    {
        if (isMatching)
            return;

        practiceWhileMatching = false;
        StartMatchmakingCore();
    }

    /// <summary>모드 선택의 "1대1 대전". 매칭을 걸고, 혼자 기다리는 동안 그 세션 안에서 연습장을 연다.
    /// 상대가 들어오면 씬을 다시 띄우지 않고 그 자리에서 대전으로 바뀐다(ConvertPracticeToMatch).</summary>
    public void StartMatchmakingWithPractice()
    {
        if (isMatching)
            return;

        practiceWhileMatching = true;
        StartMatchmakingCore();
    }

    /// <summary>오프라인 연습장에서 "매칭 시작". 오프라인 러너를 닫고 매칭 세션 안의 연습장으로 옮긴다.</summary>
    public async void StartMatchmakingFromPractice()
    {
        if (IsMatchingInPractice)
            return;

        BeginSessionSwitch("매칭 서버에 연결하는 중...");
        try
        {
            // 오프라인 연습(StartOfflineBotMatch)이 켜둔 상태를 정리. 이 셧다운은 의도한 것이라 OnShutdown을 건너뛴다.
            switchingToOffline = true;
            isMatching = false;
            gameSceneRequested = false;
            if (runner != null && !runner.IsShutdown)
                await runner.Shutdown();
        }
        finally
        {
            switchingToOffline = false;
        }

        StartMatchmakingWithPractice();
    }

    /// <summary>연습장에서 "매칭 취소". 매칭 세션을 닫고 오프라인 연습장으로 다시 연다.</summary>
    public void CancelPracticeMatchmaking()
    {
        if (!IsMatchingInPractice)
            return;

        BeginSessionSwitch("연습장으로 돌아가는 중...");
        isMatching = false;
        practiceWhileMatching = false;
        gameSceneRequested = false;
        StartPractice();
    }

    private void BeginSessionSwitch(string message)
    {
        IsSwitchingSession = true;
        SwitchingMessage = message;
    }

    private void StartMatchmakingCore()
    {

        isMatching = true;
        playerSpawnedInGameScene = false;
        gameSceneRequested = false;
        practiceSceneRequested = false;
        vsBot = false;
        sessionJoinedTime = -1f;
        matchStartTime = Time.unscaledTime;
        // 연습장에서 매칭을 거는 경우엔 연습 상태를 유지한다(잠깐이라도 끄면 연습 화면이 닫혔다 열린다).
        if (!practiceWhileMatching)
            PracticeSession.End();

        SetStatus(MatchingStatus);

        // "quickmatch-0" 부터 순서대로 참가/생성을 시도한다. 방 목록을 조회해서
        // 클라이언트가 직접 판단하지 않고, 정해진 이름으로 StartGame 을 바로 호출해
        // 서버가 "있으면 참가, 없으면 생성"을 원자적으로 처리하게 한다.
        // (목록 조회 후 판단하는 방식은 두 클라가 동시에 접속하면 서로 다른 방을
        //  각자 만들어버리는 경쟁 조건이 있어서 제거했다.)
        _ = TryJoinQuickMatchSlot(0);
    }

    /// <summary>모드 선택의 "AI 대전". 매칭 대기 없이 바로 오프라인 AI 전을 시작한다.</summary>
    public void StartAiMatch()
    {
        if (isMatching || gameSceneRequested)
            return;

        PracticeSession.End();
        BeginOfflineSession();
        _ = StartOfflineBotMatch();
    }

    /// <summary>모드 선택의 "연습". 오프라인으로 게임 씬을 연습장(연습 맵 + 연습 봇)으로 연다.</summary>
    public void StartPractice()
    {
        if (isMatching || gameSceneRequested)
            return;

        PracticeSession.Begin();
        BeginOfflineSession();
        _ = StartOfflineBotMatch();
    }

    private void BeginOfflineSession()
    {
        practiceWhileMatching = false;
        practiceSceneRequested = false;
        isMatching = true;
        playerSpawnedInGameScene = false;
        vsBot = false;
        sessionJoinedTime = -1f;
        matchStartTime = Time.unscaledTime;
    }

    /// <summary>
    /// StartGame 에 쓸 NetworkRunner 를 준비한다.
    /// 한 번 Shutdown 된 러너는 재사용할 수 없으므로, 매번 전용 자식 오브젝트에 새로 만든다.
    /// 러너가 자기 오브젝트에 있으므로 Shutdown 시 그 오브젝트만 파괴되고 NetworkLauncher 는 유지된다.
    /// </summary>
    private void PrepareRunner()
    {
        // 살아있고 종료되지 않은 러너가 있으면 재사용.
        if (runner != null && !runner.IsShutdown)
        {
            runner.ProvideInput = true;
            return;
        }

        // 종료됐는데 오브젝트가 남아있다면 정리.
        if (runner != null)
            Destroy(runner.gameObject);

        // 전용 자식 오브젝트에 러너 + 씬 매니저를 새로 만든다.
        GameObject runnerObject = new GameObject("NetworkRunner (Session)");
        runnerObject.transform.SetParent(transform, false);

        runner = runnerObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true; // Shared 모드에서도 입력 콜백을 받기 위해
        runner.AddCallbacks(this);
    }

    public void CancelMatchmaking()
    {
        if (!isMatching)
            return;

        isMatching = false;
        sessionJoinedTime = -1f;
        SetStatus("매칭 취소됨");

        // 러너는 전용 자식 오브젝트에 있으므로 기본 Shutdown 으로 그 오브젝트만 파괴된다.
        // (NetworkLauncher 는 유지 → 다음 매칭 때 PrepareRunner 가 새 러너를 만든다.)
        if (runner != null && !runner.IsShutdown)
            _ = runner.Shutdown();
    }

    /// <summary>매치 결과 화면의 "로비로" 버튼에서 호출. 나만 세션에서 나가 로비 씬으로 돌아간다
    /// (상대는 자기가 따로 눌러야 나간다 — 서로 강제로 끌고 나가지 않음).
    /// 대전 도중 상대가 먼저 나가버린 경우(OnPlayerLeft)에도 동일한 경로로 호출된다.</summary>
    public async void ReturnToLobby()
    {
        if (isReturningToLobby)
            return;
        isReturningToLobby = true;

        isMatching = false;
        playerSpawnedInGameScene = false;
        gameSceneRequested = false;
        practiceWhileMatching = false;
        practiceSceneRequested = false;
        vsBot = false;
        sessionJoinedTime = -1f;
        PracticeSession.End();
        IsSwitchingSession = false;
        SceneTransition.Instance?.Play("로비", "로비로 돌아가는 중", new Color(0.55f, 0.65f, 1f, 1f));

        try
        {
            if (runner != null && !runner.IsShutdown)
                await runner.Shutdown();

            SceneManager.LoadScene(lobbySceneName);
        }
        catch (Exception ex)
        {
            // 여기서 예외로 빠지면 isReturningToLobby 가 영원히 true 로 남아 이후 모든
            // ReturnToLobby() 호출이 조용히 무시된다 — finally 로 반드시 풀어준다.
            Debug.LogError($"[NetworkLauncher] 로비 복귀 중 예외 발생: {ex}");
        }
        finally
        {
            isReturningToLobby = false;
        }
    }

    // ---------------- 매칭(세션) 처리 ----------------

    // 동시에 여러 쌍이 매칭할 수 있도록 "quickmatch-0", "quickmatch-1" ... 여러 슬롯을 둔다.
    // 앞 슬롯부터 순서대로 StartGame 을 시도해서, 비어있으면 참가하고 꽉 차있으면
    // 다음 슬롯으로 넘어간다. 이름이 정해져 있는 방에 대한 StartGame 은 서버가
    // "있으면 참가, 없으면 생성"을 원자적으로 처리하므로 클라이언트끼리 경쟁할 여지가 없다.
    private const int MaxQuickMatchSlots = 30;

    private async Task TryJoinQuickMatchSlot(int slotIndex)
    {
        if (!isMatching)
            return;

        if (slotIndex >= MaxQuickMatchSlots)
        {
            SetStatus("매칭 가능한 방을 찾지 못했습니다.");
            FailMatchmaking();
            return;
        }

        // 종료됐던 러너는 재사용할 수 없으므로, 매 시도마다 확인해서 필요하면 새로 만든다.
        // try/catch로 감싸는 이유: 여기서 예외가 나면(예: 방금 상대가 튕겨서 서버 쪽 이전
        // 세션 정리가 아직 안 끝난 상태에서 같은 이름의 세션을 다시 잡으려다 생기는 타이밍
        // 이슈 등) isMatching 이 true 로 영원히 갇혀버린다 — 그러면 StartMatchmaking() 이
        // 맨 위 가드(if (isMatching) return;)에서 계속 조용히 아무것도 안 하고 끝나서,
        // 재매칭 버튼/포탈을 눌러도 반응이 없는 것처럼 보인다.
        try
        {
            PrepareRunner();

            string sessionName = "quickmatch-" + slotIndex;
            StartGameResult result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = sessionName,
                PlayerCount = playerCount,
                SceneManager = GetOrAddSceneManager()
            });

            if (!isMatching)
                return;

            if (result.Ok)
            {
                sessionJoinedTime = Time.unscaledTime;
                SetStatus(MatchingStatus);
                return;
            }

            // 그 슬롯이 이미 꽉 찼거나(GameIsFull) 닫혀있으면(GameClosed) 다음 슬롯을 시도한다.
            if (result.ShutdownReason == ShutdownReason.GameIsFull || result.ShutdownReason == ShutdownReason.GameClosed)
            {
                await TryJoinQuickMatchSlot(slotIndex + 1);
                return;
            }

            SetStatus($"접속 실패: {result.ShutdownReason}");
            FailMatchmaking();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkLauncher] 매칭 시도 중 예외 발생(slot {slotIndex}): {ex}");
            SetStatus("매칭 중 오류가 발생했습니다. 다시 시도해주세요.");
            FailMatchmaking();
        }
    }
    // 매칭 접속 실패. 연습하면서 매칭하던 중이면 갈 곳이 없어지므로 되돌려준다:
    // 게임 씬(연습장)에 있으면 오프라인 연습장으로 다시 열고, 로비에서 넘어가던 중이면 로비를 다시 띄운다.
    private void FailMatchmaking()
    {
        isMatching = false;
        IsSwitchingSession = false;
        if (!practiceWhileMatching)
            return;

        practiceWhileMatching = false;
        practiceSceneRequested = false;
        if (SceneManager.GetActiveScene().name == gameSceneName)
            pendingOfflinePractice = true;
        else
        {
            PracticeSession.End();
            SceneManager.LoadScene(lobbySceneName);
        }
    }


    private NetworkSceneManagerDefault GetOrAddSceneManager()
    {
        // 씬 매니저는 러너와 같은 오브젝트에 둔다(러너와 함께 생성/파괴되도록).
        NetworkSceneManagerDefault sceneManager = runner.GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null)
            sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        return sceneManager;
    }

    // 세션에 누군가 들어오거나 내가 들어왔을 때 정원 확인 → 마스터가 게임 씬 로드
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        TryStartGameScene(runner);
        TrySpawnLocalPlayer(runner);
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        SetStatus(MatchingStatus);
    }

    /// <summary>"방장만 하는 일"(게임 씬 로드, MatchManager/맵 구조물 스폰 등)을 이 클라가 해야 하는지.
    /// Shared 모드에선 마스터 클라, 오프라인 AI 전(Single 모드)에선 나 혼자니까 항상 true.</summary>
    public static bool HasSessionAuthority(NetworkRunner runner)
    {
        return runner != null && (runner.IsSharedModeMasterClient || runner.GameMode == GameMode.Single);
    }

    private void TryStartGameScene(NetworkRunner runner)
    {
        // 오프라인 AI 전은 StartOfflineBotMatch 가 직접 씬을 띄운다.
        if (runner.GameMode == GameMode.Single)
            return;

        // 이미 상대가 있는 방에 들어온 쪽은 연습장에서 매칭을 걸었더라도 여기서 연습을 끝낸다.
        // 같은 게임 씬이 다시 로드되지 않아서, 그대로 두면 연습 맵이 남고 연습 봇까지 만들어진다.
        // (연습장에서 기다리던 방장 쪽은 아래 ConvertPracticeToMatch에서 따로 처리한다.)
        bool hostingPractice = practiceSceneRequested && runner.IsSharedModeMasterClient;
        if (PracticeSession.IsActive && !hostingPractice && CountPlayers(runner) >= playerCount)
        {
            PracticeSession.End();
            MapManager.Instance?.EnsureMapForRound(runner, 1);
        }

        // 매칭하면서 연습: 혼자인 방장이면 연습장을 연다. 단, 접속 직후엔 인원/방장 정보가 아직 덜 들어와서
        // 남의 방에 들어온 쪽도 "혼자인 방장"처럼 보일 수 있으므로, 판단은 Update에서 잠깐 기다린 뒤에 한다.
        if (CountPlayers(runner) < playerCount)
            return;

        // 정원이 찼으면 마스터가 아니어도 완료 문구는 띄운다.
        SetStatus(MatchedStatus);

        // Shared 모드 마스터 클라이언트만 씬 로드를 트리거한다.
        if (!runner.IsSharedModeMasterClient)
            return;

        // 방장이 연습장에서 기다리던 중이면 씬을 다시 띄우지 않고 그 자리에서 대전으로 바꾼다.
        if (practiceSceneRequested && PracticeSession.IsActive)
        {
            ConvertPracticeToMatch(runner);
            return;
        }

        // 정원이 1(혼자 테스트)이면 바로 AI 상대로 시작한다.
        if (playerCount <= 1 && fillWithBot)
            _ = StartOfflineBotMatch();
        else
            LoadGameScene(runner);
    }

    // 매칭 누르고 botFillDelaySeconds 초 넘게 혼자면 AI 상대로 게임을 시작한다(마스터 = 혼자니까 나).
    // 매칭하면서 연습: 접속 후 PracticeHostDecisionDelay초 동안 상태가 확정되길 기다렸다가, 그때도 혼자인 방장이면 연습장을 연다.
    private const float PracticeHostDecisionDelay = 0.5f;

    private void TryOpenPracticeInSession()
    {
        if (!practiceWhileMatching || practiceSceneRequested || gameSceneRequested || !isMatching || sessionJoinedTime < 0f)
            return;
        if (runner == null || !runner.IsRunning || runner.GameMode != GameMode.Shared)
            return;
        if (Time.unscaledTime - sessionJoinedTime < PracticeHostDecisionDelay)
            return;
        if (!runner.IsSharedModeMasterClient || CountPlayers(runner) >= playerCount)
            return;

        practiceSceneRequested = true;
        playerSpawnedInGameScene = false;
        PracticeSession.Begin();
        LoadGameSceneOn(runner);
    }

    /// <summary>실제로 세션에 들어와 있는 플레이어 수. SessionInfo.PlayerCount는 접속 직후 늦게 갱신될 수 있다.</summary>
    private static int CountPlayers(NetworkRunner runner)
    {
        int count = 0;
        foreach (PlayerRef _ in runner.ActivePlayers)
            count++;
        return Mathf.Max(count, runner.SessionInfo != null && runner.SessionInfo.IsValid ? runner.SessionInfo.PlayerCount : 0);
    }

    private void Update()
    {
        TryOpenPracticeInSession();

        if (pendingOfflinePractice && (runner == null || runner.IsShutdown))
        {
            pendingOfflinePractice = false;
            StartPractice();
        }

        if (!isMatching || gameSceneRequested || !fillWithBot || sessionJoinedTime < 0f || practiceWhileMatching)
            return;
        if (runner == null || !runner.IsRunning || !runner.IsSharedModeMasterClient)
            return;
        if (Time.unscaledTime - matchStartTime < botFillDelaySeconds)
            return;
        if (runner.SessionInfo.PlayerCount != 1)
            return; // 누가 들어왔으면 원래 흐름(정원 충족)대로 간다.

        _ = StartOfflineBotMatch();
    }

    /// <summary>
    /// AI 전은 Photon 서버 없이 오프라인(Single 모드)으로 돌린다. 봇은 내 클라에서만 도는 캐릭터라
    /// 서버를 거칠 이유가 없고, Shared 세션에서 봇을 돌리면 서버가 세션을 끊는(ServerLogic) 문제가 있었다.
    /// 대기하던 Shared 세션을 닫고, 새 러너로 Single 게임을 시작한 뒤 게임 씬을 띄운다.
    /// </summary>
    private async Task StartOfflineBotMatch()
    {
        if (gameSceneRequested)
            return;
        gameSceneRequested = true;
        vsBot = true;
        SetStatus(MatchedStatus);

        try
        {
            // 대기용 Shared 세션 정리. 이 셧다운은 의도한 것이라 OnShutdown 의 정리/문구를 건너뛴다.
            switchingToOffline = true;
            if (runner != null && !runner.IsShutdown)
                await runner.Shutdown();

            PrepareRunner();
            StartGameResult result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Single,
                SessionName = "offline-bot-match",
                SceneManager = GetOrAddSceneManager()
            });
            switchingToOffline = false;

            if (!result.Ok)
            {
                SetStatus($"AI 전 시작 실패: {result.ShutdownReason}");
                isMatching = false;
                gameSceneRequested = false;
                return;
            }

            LoadGameSceneOn(runner);
        }
        catch (Exception ex)
        {
            switchingToOffline = false;
            Debug.LogError($"[NetworkLauncher] AI 전 시작 중 예외 발생: {ex}");
            SetStatus("매칭 중 오류가 발생했습니다. 다시 시도해주세요.");
            isMatching = false;
            gameSceneRequested = false;
        }
    }

    // 연습장에서 기다리던 방장 쪽: 연습 봇을 치우고 실제 맵으로 바꾼 뒤, 상대 캐릭터가 들어오면
    // MatchManager가 새 매치를 시작한다(시작 연출 포함). 들어온 상대는 평소처럼 게임 씬을 받아 자기 캐릭터를 만든다.
    private void ConvertPracticeToMatch(NetworkRunner runner)
    {
        gameSceneRequested = true;
        vsBot = false;
        runner.SessionInfo.IsOpen = false;
        runner.SessionInfo.IsVisible = false;

        PracticeSession.End();

        foreach (CharacterBase ch in new List<CharacterBase>(CharacterBase.All))
        {
            if (ch != null && ch.Object != null && ch.IsBot && ch.Object.HasStateAuthority)
                runner.Despawn(ch.Object);
        }

        MapManager.Instance?.EnsureMapForRound(runner, 1);
        MatchManager.Instance?.BeginMatchFromPractice();
    }

    private void LoadGameScene(NetworkRunner runner)
    {
        if (gameSceneRequested)
            return;
        gameSceneRequested = true;
        vsBot = false;

        SetStatus(MatchedStatus);

        // 정원이 찼으니 더 못 들어오게 막고 게임 씬 로드
        runner.SessionInfo.IsOpen = false;
        runner.SessionInfo.IsVisible = false;

        LoadGameSceneOn(runner);
    }

    private void LoadGameSceneOn(NetworkRunner runner)
    {
        int buildIndex = SceneUtility.GetBuildIndexByScenePath(gameSceneName);
        if (buildIndex < 0)
        {
            // 이름으로 못 찾으면 Build Settings 에 등록된 이름이 경로가 아닐 수 있으니 직접 인덱스 탐색
            buildIndex = FindSceneBuildIndex(gameSceneName);
        }

        if (buildIndex < 0)
        {
            SetStatus($"게임 씬 '{gameSceneName}' 을 Build Settings 에서 찾지 못함");
            return;
        }

        runner.LoadScene(SceneRef.FromIndex(buildIndex), LoadSceneMode.Single);
    }

    private static int FindSceneBuildIndex(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName)
                return i;
        }
        return -1;
    }

    public void OnSceneLoadStart(NetworkRunner runner) { }

    // 게임 씬 로드가 끝나면 각 클라가 자기 캐릭터를 스폰한다.
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        TrySpawnLocalPlayer(runner);
    }

    private void TrySpawnLocalPlayer(NetworkRunner runner)
    {
        if (playerSpawnedInGameScene)
            return;

        // 현재 활성 씬이 게임 씬일 때만 스폰
        if (SceneManager.GetActiveScene().name != gameSceneName)
            return;

        PlayerSpawner spawner = FindObjectOfType<PlayerSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("[NetworkLauncher] 게임 씬에 PlayerSpawner 가 없습니다.");
            return;
        }

        // 매칭하면서 연습: 혼자인 동안 "연습장을 열지"(TryOpenPracticeInSession) 판단이 나기 전엔 캐릭터를 만들지 않는다.
        // 여기서 먼저 만들면 아래 검사가 "남의 방에 들어온 쪽"으로 오판해 연습을 끝내버린다.
        if (runner.GameMode == GameMode.Shared && practiceWhileMatching && !practiceSceneRequested &&
            !gameSceneRequested && CountPlayers(runner) < playerCount)
            return;

        // 매칭 세션에서 연습을 이어가도 되는 건 "연습장을 연 방장"뿐이다. 남의 방에 들어온 쪽이
        // 연습 상태로 남아 있으면(인원 수 갱신이 늦게 보이는 경우 등) 캐릭터를 만들기 전에 끝낸다.
        if (runner.GameMode == GameMode.Shared && PracticeSession.IsActive &&
            !(practiceSceneRequested && runner.IsSharedModeMasterClient))
        {
            PracticeSession.End();
            MapManager.Instance?.EnsureMapForRound(runner, 1);
        }

        playerSpawnedInGameScene = true;
        IsSwitchingSession = false;
        spawner.SpawnLocalPlayer(runner);

        if (vsBot || PracticeSession.IsActive)
            spawner.SpawnBot(runner);
    }

    private void SetStatus(string message)
    {
        Debug.Log($"[NetworkLauncher] {message}");
        StatusChanged?.Invoke(message);
    }

    // ---------------- INetworkRunnerCallbacks (미사용은 비워둠) ----------------

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    // 대전 도중(게임 씬) 상대가 나가면 나도 로비로 돌아간다. 매칭 대기 중(로비 씬)에
    // 상대가 취소하는 경우는 게임 씬이 아니므로 영향받지 않는다.
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (player == runner.LocalPlayer)
            return;

        if (SceneManager.GetActiveScene().name != gameSceneName)
            return;

        // 결과 화면에서는 바로 끌어내지 않는다. MatchEndUI가 "상대가 나갔습니다"를 띄우고
        // 재대결을 막은 뒤, 내가 "로비로"를 눌러 직접 나간다.
        if (MatchEndUI.IsShowing)
        {
            SetStatus("상대방이 나갔습니다.");
            return;
        }

        SetStatus("상대방이 나갔습니다. 로비로 돌아갑니다.");
        ReturnToLobby();
    }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        // 오프라인 AI 전으로 갈아타면서 대기 세션을 닫은 것 — 매칭은 계속 진행 중이다.
        if (switchingToOffline)
            return;

        // GameIsFull/GameClosed는 TryJoinQuickMatchSlot이 다음 슬롯으로 재시도하는 도중
        // 정상적으로 거치는 중간 셧다운이다(실패한 StartGame이 내부적으로 OnShutdown도
        // 같이 호출시킴). 여기서 isMatching을 꺼버리면 재시도 로직이 "취소됨"으로 오판해서
        // await 직후의 가드(if (!isMatching) return;)에 걸려 다음 슬롯 시도 자체를 못 하고
        // 멈춰버린다 — 그래서 이 두 사유는 isMatching을 건드리지 않고 TryJoinQuickMatchSlot이
        // 알아서 처리하게 둔다. 그 외(진짜 연결 끊김/취소 등)는 기존대로 정리한다.
        if (shutdownReason != ShutdownReason.GameIsFull && shutdownReason != ShutdownReason.GameClosed)
        {
            // 매칭 세션 안 연습장에서 기다리다 연결이 끊기면 캐릭터가 사라진 채로 남으므로 오프라인 연습장으로 다시 연다.
            bool wasPracticeMatching = practiceWhileMatching && practiceSceneRequested && !gameSceneRequested && !isReturningToLobby;
            isMatching = false;
            if (wasPracticeMatching && SceneManager.GetActiveScene().name == gameSceneName)
            {
                practiceWhileMatching = false;
                practiceSceneRequested = false;
                pendingOfflinePractice = true;
                return;
            }
        }

        playerSpawnedInGameScene = false;
        SetStatus($"세션 종료: {shutdownReason}");
    }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        if (switchingToOffline)
            return;
        SetStatus($"연결 끊김: {reason}");
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        SetStatus($"접속 실패: {reason}");
    }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
