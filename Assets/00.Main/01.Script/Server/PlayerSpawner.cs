using Photon.Pun;
using UnityEngine;

/// <summary>
/// 매칭이 잡혀 게임 씬으로 넘어오면, 각 클라이언트가 자기 Player 프리팹을
/// PhotonNetwork.Instantiate 로 생성한다. PUN 이 알아서 다른 모든 클라이언트에도
/// 같은 캐릭터를 복제해 주므로, 서로의 캐릭터가 양쪽 화면에 나타난다.
///
/// 움직임 동기화는 Player 프리팹의 PhotonView + SlimeCharacterController(IPunObservable)
/// 가 담당한다. 여기서는 "생성"과 "전송 빈도(지연 최소화)" 설정만 한다.
///
/// [씬 설정]
///   - 게임 씬에 빈 GameObject 하나 만들고 이 스크립트를 붙인다.
///   - Player 프리팹은 반드시 "Resources" 폴더 안에 있어야 한다
///     (PhotonNetwork.Instantiate 는 이름으로 Resources 에서 프리팹을 찾는다).
/// </summary>
public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [Header("스폰")]
    [Tooltip("Resources 폴더 안의 Player 프리팹 이름 (확장자 제외)")]
    [SerializeField] private string playerPrefabName = "Player";

    [Tooltip("스폰 위치들. 비워두면 ActorNumber 로 좌우로 자동 분배")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("동기화 빈도 (지연 최소화)")]
    [Tooltip("초당 패킷 전송 횟수. 높을수록 즉각적이지만 트래픽 증가 (기본 20 → 30 권장)")]
    [SerializeField] private int sendRate = 30;

    [Tooltip("초당 OnPhotonSerializeView 호출 횟수. 높을수록 부드럽고 지연이 적다")]
    [SerializeField] private int serializationRate = 30;

    private void Start()
    {
        // 전송 빈도를 올려 "움직이면 바로바로" 보이게 한다
        PhotonNetwork.SendRate = sendRate;
        PhotonNetwork.SerializationRate = serializationRate;

        if (!PhotonNetwork.InRoom)
        {
            // 방 없이 게임 씬을 직접 연 경우(단독 테스트) — 스폰 생략
            Debug.LogWarning("[PlayerSpawner] 방에 들어가 있지 않아 캐릭터를 스폰하지 않습니다. " +
                             "(매칭을 통해 들어왔는지 확인하세요)");
            return;
        }

        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        Vector3 spawnPos = GetSpawnPosition();

        // 각 클라이언트가 자기 캐릭터를 생성 → PUN 이 모두에게 복제.
        // 생성한 클라이언트가 그 캐릭터의 소유자(IsMine)가 되어 조종 권한을 가진다.
        PhotonNetwork.Instantiate(playerPrefabName, spawnPos, Quaternion.identity);

        Debug.Log($"[PlayerSpawner] '{playerPrefabName}' 스폰 @ {spawnPos} " +
                  $"(Actor {PhotonNetwork.LocalPlayer.ActorNumber})");
    }

    /// <summary>
    /// 스폰 위치 결정. 스폰포인트가 있으면 ActorNumber 로 분배하고,
    /// 없으면 ActorNumber 기준으로 좌우로 벌려 겹치지 않게 한다.
    /// </summary>
    private Vector3 GetSpawnPosition()
    {
        int actor = PhotonNetwork.LocalPlayer.ActorNumber; // 1, 2, 3 ...

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int index = (actor - 1) % spawnPoints.Length;
            return spawnPoints[index].position;
        }

        // 기본: 1번은 왼쪽(-2), 2번은 오른쪽(+2), 그 이상은 2칸씩 벌림
        float x = (actor - 1) * 4f - 2f;
        return new Vector3(x, 1f, 0f);
    }
}
