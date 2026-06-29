using Fusion;
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
    [Header("스폰")]
    [Tooltip("NetworkObject 가 붙은 Player 프리팹")]
    [SerializeField] private NetworkObject playerPrefab;

    [Tooltip("스폰 위치들. 비워두면 PlayerId 로 좌우로 자동 분배")]
    [SerializeField] private Transform[] spawnPoints;

    public void SpawnLocalPlayer(NetworkRunner runner)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] Player Prefab 이 연결되지 않았습니다.");
            return;
        }

        Vector3 spawnPos = GetSpawnPosition(runner.LocalPlayer.PlayerId);

        // Shared 모드: 스폰한 클라가 StateAuthority + InputAuthority 를 가진다.
        runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, runner.LocalPlayer);

        Debug.Log($"[PlayerSpawner] 캐릭터 스폰 @ {spawnPos} (Player {runner.LocalPlayer.PlayerId})");
    }

    private Vector3 GetSpawnPosition(int playerId)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int index = Mathf.Abs(playerId - 1) % spawnPoints.Length;
            return spawnPoints[index].position;
        }

        // 기본: PlayerId 1 → 왼쪽(-2), 2 → 오른쪽(+2) ...
        float x = (playerId - 1) * 4f - 2f;
        return new Vector3(x, 1f, 0f);
    }
}
