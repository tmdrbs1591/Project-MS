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
/// [맵 선택 — 권한자가 추첨하고 결과를 동기화]
///   - MatchManager.MapSelection에 세션 이름/라운드/맵 인덱스를 함께 저장한다.
///     같은 세션/라운드는 현재 결과를 반환하고, 변경 시 직전 맵을 제외하고 랜덤 선택한다.
///   - PlayerSpawner가 MatchManager를 먼저 준비한다. 다른 클라이언트는 선택 결과를
///     받을 때까지 맵/캐릭터 생성을 기다리므로 각자 다른 맵을 뽑지 않는다.
///   - rotateMapEachRound를 켜면 라운드가 바뀔 때마다(MatchManager.RoundNumber 폴링) 새로
///     선택해서 맵을 바꾼다. 끄면(기본값) 매치 시작 때 고른 맵을 그대로 쓴다.
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

    [Tooltip("켜면 라운드가 바뀔 때마다 직전 맵을 제외하고 랜덤 선택한다. " +
             "끄면 매치 시작 때 고른 맵을 매치 내내 그대로 쓴다.")]
    [SerializeField] private bool rotateMapEachRound = false;
    [Tooltip("연습장(PracticeSession)일 때 mapPrefabs 대신 쓰는 맵. 비워두면 평소 맵을 쓴다.")]
    [SerializeField] private GameObject practiceMapPrefab;

    private GameObject activeMapInstance;
    private MapDefinition activeMapDefinition;
    private int activeRoundNumber = -1;
    private bool activeIsPracticeMap;
    private int activeMapIndex = -1;
    private NetworkRunner activeRunner;
    private string activeSessionName;
    private int lastBattleMapIndex = -1;

    /// <summary>지금 깔려 있는 맵의 mapPrefabs 인덱스(연습 맵이거나 아직 없으면 -1).</summary>
    public int ActiveMapIndex => activeIsPracticeMap ? -1 : activeMapIndex;

    /// <summary>mapPrefabs 순서대로 각 맵의 대표 이미지(MapDefinition.Thumbnail). 하나라도 비어 있으면 null.</summary>
    public Sprite[] GetMapThumbnails()
    {
        if (mapPrefabs == null || mapPrefabs.Length == 0)
            return null;

        var thumbnails = new Sprite[mapPrefabs.Length];
        for (int i = 0; i < mapPrefabs.Length; i++)
        {
            MapDefinition def = mapPrefabs[i] != null ? mapPrefabs[i].GetComponent<MapDefinition>() : null;
            if (def == null || def.Thumbnail == null)
                return null;
            thumbnails[i] = def.Thumbnail;
        }
        return thumbnails;
    }

    /// <summary>mapPrefabs 순서대로 각 맵의 표시 이름(MapDefinition.DisplayName). 비어 있으면 프리팹 이름.</summary>
    public string[] GetMapNames()
    {
        if (mapPrefabs == null || mapPrefabs.Length == 0)
            return null;

        var names = new string[mapPrefabs.Length];
        for (int i = 0; i < mapPrefabs.Length; i++)
        {
            MapDefinition def = mapPrefabs[i] != null ? mapPrefabs[i].GetComponent<MapDefinition>() : null;
            names[i] = def != null && !string.IsNullOrEmpty(def.DisplayName) ? def.DisplayName
                : mapPrefabs[i] != null ? mapPrefabs[i].name : string.Empty;
        }
        return names;
    }

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
        MatchManager match = MatchManager.Instance;
        if (match == null || match.Object == null || !match.Object.IsValid ||
            match.Runner == null || !match.Runner.IsRunning)
            return;

        // 동기화 대기 중인 첫 생성도 재시도한다. 회전 OFF면 이미 선택한 라운드를 유지한다.
        int round = !rotateMapEachRound && match.MapSelection.HasValue
            ? match.MapSelection.RoundNumber : match.RoundNumber;
        EnsureMapForRound(match.Runner, round);
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

    /// <summary>현재 떠 있는 맵이 있는지. 스폰 위치만 필요한 호출부가 "맵 교체"를 유발하지 않고
    /// 이미 떠 있는 맵을 그대로 쓸 수 있게 한다.</summary>
    public bool HasActiveMap => activeMapInstance != null;

    /// <summary>다음 라운드를 시작하기 직전에(캐릭터 스폰 위치를 구하기 전에) 그 라운드 맵을 미리 준비한다.
    /// rotateMapEachRound가 꺼져 있으면 매치 내내 같은 맵을 쓰므로 아무 것도 하지 않는다.</summary>
    public void PrepareMapForRound(NetworkRunner runner, int roundNumber)
    {
        if (!rotateMapEachRound)
            return;

        EnsureMapForRound(runner, roundNumber);
    }

    /// <summary>이번 라운드에 맞는 맵을 반환한다. 같은 세션/라운드는 기존 인스턴스를 재사용한다.
    /// 선택 결과가 아직 동기화되지 않았거나 설정이 잘못됐으면 null을 반환한다.</summary>
    public MapDefinition EnsureMapForRound(NetworkRunner runner, int roundNumber)
    {
        if (runner == null || !runner.IsRunning || roundNumber <= 0)
            return null;

        string sessionName = runner.SessionInfo != null ? runner.SessionInfo.Name ?? string.Empty : string.Empty;
        // 연습장 → 대전 전환(매칭하면서 연습)처럼 같은 1라운드라도 연습 맵 여부가 바뀌면 다시 깐다.
        bool wantPractice = PracticeSession.IsActive && practiceMapPrefab != null;
        int index = -1;
        GameObject prefab = practiceMapPrefab;
        if (!wantPractice)
        {
            if (mapPrefabs == null || mapPrefabs.Length == 0)
            {
                Debug.LogError("[MapManager] mapPrefabs가 비어있습니다.", this);
                return null;
            }

            MatchManager match = MatchManager.Instance;
            if (match == null || match.Runner != runner ||
                !match.TryGetMapIndexForRound(sessionName, roundNumber, mapPrefabs.Length, lastBattleMapIndex, out index))
                return null;

            if (index < 0 || index >= mapPrefabs.Length)
            {
                Debug.LogError("[MapManager] 동기화된 맵 인덱스가 범위를 벗어났습니다. 모든 클라이언트의 맵 목록을 확인하세요.", this);
                return null;
            }
            prefab = mapPrefabs[index];
        }

        if (activeMapInstance != null && activeRunner == runner && activeSessionName == sessionName &&
            roundNumber == activeRoundNumber && activeIsPracticeMap == wantPractice && activeMapIndex == index)
            return activeMapDefinition;

        if (prefab == null || prefab.GetComponent<MapDefinition>() == null)
        {
            Debug.LogError($"[MapManager] 맵 프리팹({index})이 비어있거나 MapDefinition이 없습니다.", this);
            return null;
        }

        DespawnActiveStructures(activeRunner);
        DestroyAllMapInstances();

        activeMapInstance = Instantiate(prefab, transform);
        activeMapDefinition = activeMapInstance.GetComponent<MapDefinition>();
        activeRoundNumber = roundNumber;
        activeIsPracticeMap = wantPractice;
        activeMapIndex = index;
        activeRunner = runner;
        activeSessionName = sessionName;
        if (!wantPractice)
            lastBattleMapIndex = index;

        TwoPlayerCamera.Instance?.SetBoundsCollider(activeMapDefinition.CameraBounds);

        if (NetworkLauncher.HasSessionAuthority(runner))
            SpawnStructures(runner, activeMapDefinition);

        return activeMapDefinition;
    }

    // 기억하고 있는 맵(activeMapInstance) 하나만이 아니라 이 매니저 아래에 있는 MapDefinition 맵을
    // 전부 지운다 — 교체가 한 프레임에 겹치거나 참조를 잃은 맵이 있어도 이전 맵이 남지 않게 한다.
    // Destroy는 프레임 끝에야 반영되므로, 물리/렌더에서 바로 빠지도록 먼저 비활성화한다.
    private void DestroyAllMapInstances()
    {
        MapDefinition[] existing = GetComponentsInChildren<MapDefinition>(true);
        foreach (MapDefinition map in existing)
        {
            if (map == null)
                continue;

            map.gameObject.SetActive(false);
            Destroy(map.gameObject);
        }

        activeMapInstance = null;
        activeMapDefinition = null;
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
        if (runner == null || !runner.IsRunning || !NetworkLauncher.HasSessionAuthority(runner))
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
}
