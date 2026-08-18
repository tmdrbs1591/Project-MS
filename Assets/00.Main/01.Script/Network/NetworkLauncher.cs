using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
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

    /// <summary>매칭 상태 메시지(연결/대기/성공/실패)를 외부 UI 로 전달한다.</summary>
    public event Action<string> StatusChanged;

    private NetworkRunner runner;
    private bool isMatching;
    private bool playerSpawnedInGameScene; // 게임 씬에서 내 캐릭터를 이미 스폰했는지
    private bool isReturningToLobby; // 버튼 클릭과 OnPlayerLeft 가 동시에 겹쳐 중복 실행되는 것을 막는 가드

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

    /// <summary>매칭 시작. (포탈/버튼에서 호출)</summary>
    public void StartMatchmaking()
    {
        if (isMatching)
            return;

        isMatching = true;
        playerSpawnedInGameScene = false;

        SetStatus("상대 찾는 중...");

        // "quickmatch-0" 부터 순서대로 참가/생성을 시도한다. 방 목록을 조회해서
        // 클라이언트가 직접 판단하지 않고, 정해진 이름으로 StartGame 을 바로 호출해
        // 서버가 "있으면 참가, 없으면 생성"을 원자적으로 처리하게 한다.
        // (목록 조회 후 판단하는 방식은 두 클라가 동시에 접속하면 서로 다른 방을
        //  각자 만들어버리는 경쟁 조건이 있어서 제거했다.)
        _ = TryJoinQuickMatchSlot(0);
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
            isMatching = false;
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
                SetStatus("상대 찾는 중...");
                return;
            }

            // 그 슬롯이 이미 꽉 찼거나(GameIsFull) 닫혀있으면(GameClosed) 다음 슬롯을 시도한다.
            if (result.ShutdownReason == ShutdownReason.GameIsFull || result.ShutdownReason == ShutdownReason.GameClosed)
            {
                await TryJoinQuickMatchSlot(slotIndex + 1);
                return;
            }

            SetStatus($"접속 실패: {result.ShutdownReason}");
            isMatching = false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkLauncher] 매칭 시도 중 예외 발생(slot {slotIndex}): {ex}");
            SetStatus("매칭 중 오류가 발생했습니다. 다시 시도해주세요.");
            isMatching = false;
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
        SetStatus("상대 찾는 중...");
    }

    private void TryStartGameScene(NetworkRunner runner)
    {
        // Shared 모드 마스터 클라이언트만 씬 로드를 트리거한다.
        if (!runner.IsSharedModeMasterClient)
            return;

        if (runner.SessionInfo.PlayerCount < playerCount)
            return;

        // 정원이 찼으니 더 못 들어오게 막고 게임 씬 로드
        runner.SessionInfo.IsOpen = false;
        runner.SessionInfo.IsVisible = false;

        SetStatus("매칭 성공! 게임 시작");

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

        playerSpawnedInGameScene = true;
        spawner.SpawnLocalPlayer(runner);
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

        SetStatus("상대방이 나갔습니다. 로비로 돌아갑니다.");
        ReturnToLobby();
    }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        // GameIsFull/GameClosed는 TryJoinQuickMatchSlot이 다음 슬롯으로 재시도하는 도중
        // 정상적으로 거치는 중간 셧다운이다(실패한 StartGame이 내부적으로 OnShutdown도
        // 같이 호출시킴). 여기서 isMatching을 꺼버리면 재시도 로직이 "취소됨"으로 오판해서
        // await 직후의 가드(if (!isMatching) return;)에 걸려 다음 슬롯 시도 자체를 못 하고
        // 멈춰버린다 — 그래서 이 두 사유는 isMatching을 건드리지 않고 TryJoinQuickMatchSlot이
        // 알아서 처리하게 둔다. 그 외(진짜 연결 끊김/취소 등)는 기존대로 정리한다.
        if (shutdownReason != ShutdownReason.GameIsFull && shutdownReason != ShutdownReason.GameClosed)
            isMatching = false;

        playerSpawnedInGameScene = false;
        SetStatus($"세션 종료: {shutdownReason}");
    }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
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
