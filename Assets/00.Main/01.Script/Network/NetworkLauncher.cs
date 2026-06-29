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
///   - "빠른 매칭": 세션 로비에 들어가 빈 방이 있으면 들어가고, 없으면 새로 만든다
///     (PUN 의 JoinRandomOrCreateRoom 대체).
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
[RequireComponent(typeof(NetworkRunner))]
public class NetworkLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkLauncher Instance { get; private set; }

    [Header("매칭")]
    [Tooltip("한 세션 정원. 이 인원이 차면 게임 씬으로 이동")]
    [SerializeField] private int playerCount = 2;

    [Tooltip("게임 씬 이름 (Build Settings 에 등록되어 있어야 함)")]
    [SerializeField] private string gameSceneName = "SampleScene 1";

    /// <summary>매칭 상태 메시지(연결/대기/성공/실패)를 외부 UI 로 전달한다.</summary>
    public event Action<string> StatusChanged;

    private NetworkRunner runner;
    private bool isMatching;
    private bool joinedViaList;            // 로비 세션 목록에서 빈 방을 찾아 들어갔는지
    private bool playerSpawnedInGameScene; // 게임 씬에서 내 캐릭터를 이미 스폰했는지

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        runner = GetComponent<NetworkRunner>();
        runner.ProvideInput = true; // Shared 모드에서도 입력 콜백을 받기 위해
    }

    /// <summary>매칭 시작. (포탈/버튼에서 호출)</summary>
    public async void StartMatchmaking()
    {
        if (isMatching)
            return;

        isMatching = true;
        SetStatus("로비 접속 중...");

        runner.AddCallbacks(this);

        // 1) 세션 로비에 들어가 현재 열려있는 방 목록을 받는다 (OnSessionListUpdated).
        StartGameResult lobbyResult = await runner.JoinSessionLobby(SessionLobby.Shared);
        if (!lobbyResult.Ok)
        {
            SetStatus($"로비 접속 실패: {lobbyResult.ShutdownReason}");
            isMatching = false;
            return;
        }

        SetStatus("상대 찾는 중...");
        // 이후 OnSessionListUpdated 에서 빈 방을 찾으면 그 방으로, 없으면 새 방으로 접속한다.
    }

    public void CancelMatchmaking()
    {
        if (!isMatching)
            return;

        isMatching = false;
        SetStatus("매칭 취소됨");
        _ = runner.Shutdown();
    }

    // ---------------- 매칭(세션) 처리 ----------------

    private bool startGameRequested;

    public async void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (!isMatching || startGameRequested)
            return;

        // 들어갈 수 있는(열려있고 정원이 안 찬) 방을 찾는다.
        SessionInfo open = null;
        foreach (SessionInfo s in sessionList)
        {
            if (s.IsOpen && s.IsVisible && s.PlayerCount < s.MaxPlayers)
            {
                open = s;
                break;
            }
        }

        startGameRequested = true;

        if (open != null)
        {
            joinedViaList = true;
            SetStatus("방 입장 중...");
            await StartShared(open.Name);
        }
        else
        {
            // 빈 방이 없으면 새 방을 만든다 (고유한 이름).
            joinedViaList = false;
            SetStatus("방 생성 중...");
            await StartShared("game-" + Guid.NewGuid().ToString("N").Substring(0, 8));
        }
    }

    private async Task StartShared(string sessionName)
    {
        StartGameResult result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = sessionName,
            PlayerCount = playerCount,
            SceneManager = GetOrAddSceneManager()
        });

        if (!result.Ok)
        {
            SetStatus($"접속 실패: {result.ShutdownReason}");
            isMatching = false;
            startGameRequested = false;
        }
    }

    private NetworkSceneManagerDefault GetOrAddSceneManager()
    {
        NetworkSceneManagerDefault sceneManager = GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null)
            sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
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
        SetStatus("접속됨. 상대 대기 중...");
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
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        isMatching = false;
        startGameRequested = false;
        playerSpawnedInGameScene = false;
        SetStatus($"세션 종료: {shutdownReason}");
    }
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
