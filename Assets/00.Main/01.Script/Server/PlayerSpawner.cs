using Fusion;
using ProjectMS.CharacterSystem;
using ProjectMS.CharacterSystem.AI;
using UnityEngine;

/// <summary>
/// 게임 씬에서 "자기" 캐릭터를 네트워크 스폰한다. (Fusion 2 / Shared 모드)
///
/// [동작]
///   - NetworkLauncher 가 게임 씬 로드 완료 후 SpawnLocalPlayer(runner) 를 호출한다.
///   - Shared 모드에서는 스폰한 클라가 그 NetworkObject 의 StateAuthority 가 되므로,
///     각자 자기 캐릭터를 스폰하면 서로의 캐릭터가 양쪽 화면에 나타난다.
///
/// [씬 설정]
///   - 게임 씬에 빈 GameObject 하나 만들고 이 스크립트를 붙인다.
///   - Player Prefab 에는 NetworkObject 가 붙은 Player 프리팹을 연결한다.
///     (PUN 때와 달리 Resources 폴더에 둘 필요는 없다)
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [Header("캐릭터 프리팹 목록")]
    [SerializeField] private NetworkObject[] characterPrefabs;

    [Header("스폰")]
    [Tooltip("NetworkObject 가 붙은 Player 프리팹")]
    [SerializeField] private NetworkObject playerPrefab;

    [Header("매치")]
    [Tooltip("라운드/승패를 관리하는 MatchManager 프리팹. NetworkObject 가 붙어 있어야 한다.")]
    [SerializeField] private MatchManager matchManagerPrefab;

    [Header("AI 봇")]
    [Tooltip("봇이 고를 수 있는 캐릭터 인덱스(characterPrefabs 기준). 이 중 랜덤. 지금은 AI 가 있는 거너(0)만.")]
    [SerializeField] private int[] botCharacterIndices = { 0 };
    [SerializeField] private BotDifficulty botDifficulty = BotDifficulty.Normal;

    // MatchManager가 라운드 리셋 시 GetSpawnPosition(playerId)만으로 재호출하므로(러너를 안 넘김),
    // 처음 SpawnLocalPlayer 때 받은 러너를 기억해뒀다가 MapManager 호출에 재사용한다.
    private NetworkRunner currentRunner;

    public void SpawnLocalPlayer(NetworkRunner runner)
    {
        currentRunner = runner;
        PlayerCharacterChange();

        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] Player Prefab 이 연결되지 않았습니다.");
            return;
        }

        // 로비 매칭 대기 중 걸었던 조작 잠금을 게임 씬 진입 시 해제한다.
        ProjectMS.CharacterSystem.CharacterBase.SetLobbyControlLocked(false);
        LobbyCharacterController.SetLocked(false);

        Vector3 spawnPos = GetSpawnPosition(runner.LocalPlayer.PlayerId);

        // Shared 모드: 스폰한 클라가 StateAuthority + InputAuthority 를 가진다.
        runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, runner.LocalPlayer);

        Debug.Log($"[PlayerSpawner] 캐릭터 스폰 @ {spawnPos} (Player {runner.LocalPlayer.PlayerId})");

        SpawnMatchManagerIfNeeded(runner);
    }

    /// <summary>
    /// AI 상대를 스폰한다(혼자 매칭된 경우 NetworkLauncher 가 SpawnLocalPlayer 다음에 호출).
    /// 봇은 이 클라가 StateAuthority 를 갖고 직접 시뮬레이션한다. InputAuthority 는 없고(키보드 입력 안 받음),
    /// 대신 봇 전용 번호(내 번호 + 1)를 CharacterBase.MatchPlayer 로 들고 있어서 이 번호가 P2 슬롯/
    /// 스폰 위치/라운드 리셋/팀 구분에 쓰인다.
    /// </summary>
    public void SpawnBot(NetworkRunner runner)
    {
        currentRunner = runner;

        NetworkObject prefab = PickBotPrefab();
        if (prefab == null)
        {
            Debug.LogError("[PlayerSpawner] 봇으로 쓸 캐릭터 프리팹이 없습니다.");
            return;
        }

        PlayerRef botPlayer = PlayerRef.FromIndex(runner.LocalPlayer.PlayerId + 1);
        Vector3 spawnPos = GetSpawnPosition(botPlayer.PlayerId);

        // InputAuthority 는 비워둔다(세션에 없는 번호를 Fusion 권한에 넣으면 서버가 연결을 끊는다).
        // 봇 번호는 캐릭터의 MatchPlayer 로만 들고 있는다.
        NetworkObject bot = runner.Spawn(prefab, spawnPos, Quaternion.identity, null,
            (_, obj) => obj.GetComponent<CharacterBase>()?.MarkAsBot(botPlayer));

        CharacterBase character = bot != null ? bot.GetComponent<CharacterBase>() : null;
        if (character == null)
        {
            Debug.LogError("[PlayerSpawner] 봇 스폰 실패");
            return;
        }

        CharacterBotBrain.AttachTo(character, botDifficulty);
        Debug.Log($"[PlayerSpawner] 봇 스폰 @ {spawnPos} ({prefab.name}, Player {botPlayer.PlayerId}, {botDifficulty})");
    }

    private NetworkObject PickBotPrefab()
    {
        if (characterPrefabs == null || characterPrefabs.Length == 0)
            return null;

        if (botCharacterIndices == null || botCharacterIndices.Length == 0)
            return characterPrefabs[0];

        int index = botCharacterIndices[Random.Range(0, botCharacterIndices.Length)];
        return index >= 0 && index < characterPrefabs.Length ? characterPrefabs[index] : characterPrefabs[0];
    }

    private void PlayerCharacterChange()
    {
        int selectedCharacterIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 0);

        if (characterPrefabs == null || characterPrefabs.Length == 0)
        {
            Debug.LogError("[PlayerSpawner] 캐릭터 프리팹이 연결되지 않았습니다.");
            return;
        }

        if (selectedCharacterIndex < 0 || selectedCharacterIndex >= characterPrefabs.Length)
        {
            Debug.LogError("[PlayerSpawner] 선택한 캐릭터 인덱스가 유효하지 않습니다. 0번 캐릭터로 기본 설정합니다.");
            selectedCharacterIndex = 0;
        }

        playerPrefab = characterPrefabs[selectedCharacterIndex];
    }

    private void SpawnMatchManagerIfNeeded(NetworkRunner runner)
    {
        if (matchManagerPrefab == null || MatchManager.Instance != null)
            return;

        // 마스터 클라만 스폰해서 딱 하나만 생기게 한다(그 클라가 StateAuthority가 됨).
        if (!NetworkLauncher.HasSessionAuthority(runner))
            return;

        runner.Spawn(matchManagerPrefab, Vector3.zero, Quaternion.identity);
    }

    /// <summary>PlayerId 에 대응하는 스폰 위치. MatchManager 가 라운드 리셋 시 재사용한다.
    /// 실제 위치는 MapManager(활성 맵의 스폰 지점)에서 가져온다 — 맵이 아직 없으면 방어적으로
    /// 1라운드용 맵을 먼저 띄운다(멱등이라 이미 떠있으면 그냥 통과).</summary>
    public Vector3 GetSpawnPosition(int playerId)
    {
        if (MapManager.Instance == null)
        {
            Debug.LogError("[PlayerSpawner] 씬에 MapManager 가 없습니다.");
            float x = (playerId - 1) * 4f - 2f;
            return new Vector3(x, 1f, 0f);
        }

        MapManager.Instance.EnsureMapForRound(currentRunner, 1);
        return MapManager.Instance.GetSpawnPosition(playerId);
    }
}
