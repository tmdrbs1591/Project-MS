using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// 게임 씬 안에서 맵 프리팹을 갈아끼우는 매니저. (Fusion 2 / Shared 모드)
///
/// [왜 씬 전체를 새로 로드하지 않는가]
///   - Fusion 세션 전체가 씬 전환을 거치는 건 무겁고, 매 라운드 맵을 바꾸고 싶을 때 매번
///     로딩 화면이 낄 수 있다. 그래서 게임 씬은 하나만 유지하고, 그 안에서 캐릭터 선택
///     (PlayerSpawner.characterPrefabs)과 같은 방식으로 맵도 "프리팹 배열에서 골라 Instantiate"
///     한다.
///
/// [맵 선택 — 새 네트워크 상태 없이 동기화]
///   - 맵 인덱스는 Runner.SessionInfo.Name(세션 이름) + roundNumber를 해시해서 계산한다.
///     이 두 값은 이미 양쪽 클라가 동일하게 알고 있으므로, 각자 독립적으로 계산해도 항상
///     같은 결과가 나온다 — [Networked] 필드나 RPC가 필요 없다.
///   - 매치 시작(라운드 1) 시점엔 MatchManager가 아직 없을 수도 있지만 Runner.SessionInfo.Name은
///     연결 직후부터 항상 존재하므로, MatchManager 존재 여부와 무관하게 즉시 계산 가능하다
///     (게스트가 리플리케이션을 기다려야 하는 레이스가 아예 없음).
///   - rotateMapEachRound를 켜면 라운드가 바뀔 때마다(MatchManager.RoundNumber 폴링) 새로
///     계산해서 맵을 바꾼다. 끄면(기본값) 매치 시작 때 고른 맵을 그대로 쓴다.
///
/// [동적 구조물]
///   - RopePlank처럼 실제 물리 시뮬레이션이 필요한(NetworkObject) 것만
///     MapDefinition.StructureSpawns에 등록해두면, 마스터 클라가 맵이 바뀔 때마다
///     이전 것들을 Despawn하고 새로 Spawn한다. 낙사존/벽 같은 정적 콘텐츠는 그냥 맵
///     프리팹의 평범한 자식이면 된다(모든 클라가 로컬로 동일하게 Instantiate하므로
///     네트워크 동기화가 필요 없다).
///
/// [씬 설정]
///   - 게임 씬에 빈 오브젝트 하나 만들고 이 스크립트를 붙인 뒤, mapPrefabs에 맵 프리팹들
///     (각각 루트에 MapDefinition 필요)을 등록한다.
/// </summary>
public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [Tooltip("루트에 MapDefinition이 붙은 맵 프리팹들.")]
    [SerializeField] private GameObject[] mapPrefabs;

    [Tooltip("켜면 라운드가 바뀔 때마다 맵을 다시 고른다(세션이름+라운드번호 해시). " +
             "끄면 매치 시작 때 고른 맵을 매치 내내 그대로 쓴다.")]
    [SerializeField] private bool rotateMapEachRound = false;

    private GameObject activeMapInstance;
    private MapDefinition activeMapDefinition;
    private int activeRoundNumber = -1;
    private readonly List<NetworkObject> spawnedStructures = new List<NetworkObject>();

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!rotateMapEachRound)
            return;

        MatchManager match = MatchManager.Instance;
        if (match == null || match.Runner == null)
            return;

        if (match.RoundNumber != activeRoundNumber)
            EnsureMapForRound(match.Runner, match.RoundNumber);
    }

    /// <summary>PlayerId(1, 2, ...)에 대응하는 스폰 위치. 활성 맵이 없거나 스폰 지점이
    /// 비어있으면 기존 PlayerSpawner의 폴백과 동일하게 좌우로 자동 분배한다.</summary>
    public Vector3 GetSpawnPosition(int playerId)
    {
        Transform[] points = activeMapDefinition != null ? activeMapDefinition.SpawnPoints : null;
        if (points != null && points.Length > 0)
        {
            int index = Mathf.Abs(playerId - 1) % points.Length;
            return points[index].position;
        }

        float x = (playerId - 1) * 4f - 2f;
        return new Vector3(x, 1f, 0f);
    }

    /// <summary>이번 라운드에 맞는 맵이 이미 떠있으면 아무 것도 안 한다(멱등) — 여러 곳에서
    /// 방어적으로 호출해도 안전하다.</summary>
    public void EnsureMapForRound(NetworkRunner runner, int roundNumber)
    {
        if (activeMapInstance != null && roundNumber == activeRoundNumber)
            return;

        if (mapPrefabs == null || mapPrefabs.Length == 0)
        {
            Debug.LogError("[MapManager] mapPrefabs가 비어있습니다.", this);
            return;
        }

        int index = ResolveMapIndex(runner, roundNumber, mapPrefabs.Length);

        if (mapPrefabs[index] == null)
        {
            // 배열에 빈 칸이 있으면 여기서 막아서 최소한 캐릭터 스폰(GetSpawnPosition의 폴백)은
            // 계속 진행되게 한다 — Instantiate(null)은 예외를 던져서 이 호출부인
            // PlayerSpawner.GetSpawnPosition을 통째로 중단시키고, 그러면 그 뒤에 있는
            // 캐릭터 스폰 코드까지 같이 멈춰버린다.
            Debug.LogError($"[MapManager] mapPrefabs[{index}]가 비어있습니다 — 배열의 빈 칸을 채워주세요.", this);
            return;
        }

        DespawnActiveStructures(runner);
        if (activeMapInstance != null)
            Destroy(activeMapInstance);

        activeMapInstance = Instantiate(mapPrefabs[index], transform);
        activeMapDefinition = activeMapInstance.GetComponent<MapDefinition>();
        activeRoundNumber = roundNumber;

        if (activeMapDefinition == null)
        {
            Debug.LogError($"[MapManager] 맵 프리팹 '{mapPrefabs[index].name}'에 MapDefinition이 없습니다.", activeMapInstance);
            return;
        }

        TwoPlayerCamera.Instance?.SetBoundsCollider(activeMapDefinition.CameraBounds);

        if (runner != null && runner.IsSharedModeMasterClient)
            SpawnStructures(runner, activeMapDefinition);
    }

    private void SpawnStructures(NetworkRunner runner, MapDefinition map)
    {
        foreach (MapStructureSpawn spawn in map.StructureSpawns)
        {
            if (spawn.prefab == null)
                continue;

            Vector3 worldPosition = activeMapInstance.transform.TransformPoint(spawn.localPosition);
            Quaternion worldRotation = activeMapInstance.transform.rotation * spawn.localRotation;

            NetworkObject spawned = runner.Spawn(spawn.prefab, worldPosition, worldRotation);
            if (spawned != null)
                spawnedStructures.Add(spawned);
        }
    }

    private void DespawnActiveStructures(NetworkRunner runner)
    {
        if (runner == null || !runner.IsSharedModeMasterClient)
        {
            spawnedStructures.Clear();
            return;
        }

        foreach (NetworkObject structure in spawnedStructures)
        {
            if (structure != null && structure.IsValid)
                runner.Despawn(structure);
        }

        spawnedStructures.Clear();
    }

    private static int ResolveMapIndex(NetworkRunner runner, int roundNumber, int mapCount)
    {
        string sessionName = runner != null && runner.SessionInfo != null ? runner.SessionInfo.Name : string.Empty;
        int hash = DeterministicHash(sessionName + "_" + roundNumber);
        // Mathf.Abs(int.MinValue)는 오버플로로 음수가 그대로 나올 수 있어서(극히 드묾) Abs 대신
        // 나머지를 한 번 더 보정하는 방식으로 항상 [0, mapCount) 범위를 보장한다.
        return ((hash % mapCount) + mapCount) % mapCount;
    }

    /// <summary>string.GetHashCode()는 .NET/Mono에서 프로세스마다(보안상 이유로) 다른 값을
    /// 낼 수 있다 — 호스트와 게스트가 완전히 같은 문자열을 넣어도 서로 다른 맵을 고를 수
    /// 있다는 뜻이라, 맵 동기화 목적으로는 쓰면 안 된다. 대신 이 수동 해시(FNV 계열, 문자
    /// 값에만 의존하는 단순 연산)는 플랫폼/프로세스와 무관하게 항상 같은 결과를 낸다.</summary>
    private static int DeterministicHash(string value)
    {
        unchecked
        {
            int hash = 17;
            foreach (char c in value)
                hash = hash * 31 + c;
            return hash;
        }
    }
}
