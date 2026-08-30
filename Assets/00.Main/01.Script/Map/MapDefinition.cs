using System;
using Fusion;
using UnityEngine;

/// <summary>
/// 맵 프리팹 하나가 들고 있는 데이터. 맵 프리팹의 루트 오브젝트에 붙인다.
///
/// [씬 설정]
///   - spawnPoints: PlayerId 오름차순(1, 2, ...)에 대응하는 스폰 지점. MapManager가
///     PlayerSpawner를 대신해 이 순서로 찾는다.
///   - cameraBounds: TwoPlayerCamera의 카메라 영역 콜라이더. 이 프리팹의 자식으로 두면
///     맵을 갈아끼울 때 자동으로 같이 교체된다.
///   - structureSpawns: RopePlank처럼 실제 물리 시뮬레이션이 필요한(=NetworkObject인)
///     동적 구조물. MapManager가 마스터 클라에서만 Runner.Spawn한다. 낙사존이나 벽처럼
///     정적인 것들은 여기 등록할 필요 없이 그냥 이 프리팹의 평범한 자식으로 두면 된다
///     (네트워크 동기화가 필요 없는 정적 콘텐츠라 모든 클라가 로컬로 동일하게 Instantiate
///     하는 것만으로 충분하다).
/// </summary>
public class MapDefinition : MonoBehaviour
{
    [Tooltip("PlayerId 오름차순(1, 2, ...)에 대응하는 스폰 지점.")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("TwoPlayerCamera 카메라 영역 콜라이더. 이 프리팹의 자식으로 두면 맵 교체 시 같이 바뀐다.")]
    [SerializeField] private Collider2D cameraBounds;

    [Tooltip("RopePlank 등 마스터 클라가 Runner.Spawn해야 하는 동적 구조물.")]
    [SerializeField] private MapStructureSpawn[] structureSpawns = Array.Empty<MapStructureSpawn>();

    public Transform[] SpawnPoints => spawnPoints;
    public Collider2D CameraBounds => cameraBounds;
    public MapStructureSpawn[] StructureSpawns => structureSpawns;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (cameraBounds == null)
            Debug.LogWarning($"[{nameof(MapDefinition)}] '{name}'에 Camera Bounds가 비어있습니다 — " +
                "이대로면 TwoPlayerCamera가 화면 제한 없이 자유롭게 줌/이동해서 벽 바깥이 보일 수 있습니다.", this);
    }

    // TwoPlayerCamera의 클램프가 실제로 지키는 영역이 정확히 cameraBounds.bounds다 — 이 노란
    // 사각형이 벽 안쪽 끝에서 끝나야 좌우 벽이 화면에 절대 안 잡힌다. 맵 프리팹 편집 중
    // 이 컴포넌트를 선택하면 바로 눈으로 확인할 수 있다.
    private void OnDrawGizmosSelected()
    {
        if (cameraBounds != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(cameraBounds.bounds.center, cameraBounds.bounds.size);
        }

        if (spawnPoints == null)
            return;

        Gizmos.color = Color.cyan;
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
                Gizmos.DrawWireSphere(point.position, 0.3f);
        }
    }
#endif
}

/// <summary>맵 프리팹 기준 상대 위치에 스폰할 동적 구조물 하나.</summary>
[Serializable]
public struct MapStructureSpawn
{
    public NetworkObject prefab;
    public Vector3 localPosition;
    public Quaternion localRotation;
}
