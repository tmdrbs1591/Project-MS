using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼 하나로 동작하는 간단한 PUN2 매칭 시스템.
/// 1. 버튼 클릭 -> 포톤 서버 연결
/// 2. 랜덤 방 입장 시도 (없으면 새 방 생성)
/// 3. 방에 정원(maxPlayers)이 차면 매칭 성공 -> 게임 씬으로 이동
///
/// AutomaticallySyncScene 를 켜두므로 마스터 클라이언트가 LoadLevel 하면
/// 같은 방의 모든 클라이언트가 동일한 씬으로 함께 이동한다.
/// </summary>
public class MatchmakingManager : MonoBehaviourPunCallbacks
{
    [Header("매칭 설정")]
    [Tooltip("매칭에 필요한 인원 수 (이 인원이 모이면 게임 씬으로 이동)")]
    [SerializeField] private byte maxPlayers = 2;

    [Tooltip("매칭 성공 시 이동할 게임 씬 이름 (Build Settings 에 반드시 등록되어 있어야 함)")]
    [SerializeField] private string gameSceneName = "SampleScene 1";

    [Header("UI (선택)")]
    [Tooltip("매칭 시작 버튼")]
    [SerializeField] private Button matchButton;

    [Tooltip("상태 메시지를 표시할 텍스트 (없어도 동작함)")]
    [SerializeField] private TMP_Text statusText;

    // 매칭이 진행 중인지 여부 (중복 클릭 방지)
    private bool isMatching;

    private void Awake()
    {
        // 마스터가 씬을 로드하면 나머지 클라이언트도 자동으로 같은 씬으로 이동
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        if (matchButton != null)
            matchButton.onClick.AddListener(StartMatching);

        SetStatus("매칭 대기 중");
    }

    /// <summary>
    /// 매칭 시작 버튼에 연결할 메서드. (버튼 OnClick 에 직접 연결해도 됨)
    /// </summary>
    public void StartMatching()
    {
        if (isMatching)
            return;

        isMatching = true;
        SetButtonInteractable(false);
        SetStatus("서버 연결 중...");

        if (PhotonNetwork.IsConnectedAndReady)
        {
            // 이미 연결돼 있으면 바로 방 찾기
            JoinRandomRoom();
        }
        else
        {
            // 포톤 클라우드 서버에 연결 (PhotonServerSettings 의 AppId 사용)
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    /// <summary>
    /// 매칭 취소. (원하면 버튼에 연결해 사용)
    /// </summary>
    public void CancelMatching()
    {
        if (!isMatching)
            return;

        isMatching = false;
        SetStatus("매칭 취소됨");
        SetButtonInteractable(true);

        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
    }

    private void JoinRandomRoom()
    {
        SetStatus("상대 찾는 중...");

        // 들어갈 방이 있으면 입장, 없으면 새로 생성까지 서버가 한 번에 처리한다.
        // (JoinRandomRoom + CreateRoom 을 따로 하면 두 클라이언트가 동시에 눌렀을 때
        //  둘 다 방을 새로 만들어 서로 1/2 로 갈리는 경쟁 상태가 생긴다.)
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayers
        };

        PhotonNetwork.JoinRandomOrCreateRoom(expectedMaxPlayers: maxPlayers, roomOptions: options);
    }

    // ---------------- PUN2 콜백 ----------------

    public override void OnConnectedToMaster()
    {
        // 서버 연결 완료 -> 방 찾기 시작
        if (isMatching)
            JoinRandomRoom();
    }

    public override void OnJoinedRoom()
    {
        SetStatus($"방 입장 ({PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayers})");
        TryStartGame();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        SetStatus($"플레이어 입장 ({PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayers})");
        TryStartGame();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        isMatching = false;
        SetStatus($"연결 끊김: {cause}");
        SetButtonInteractable(true);
    }

    // ---------------- 매칭 성공 처리 ----------------

    private void TryStartGame()
    {
        // 정원이 다 차면 매칭 성공
        if (PhotonNetwork.CurrentRoom.PlayerCount < maxPlayers)
            return;

        SetStatus("매칭 성공! 게임 시작");

        // 새 플레이어가 도중에 들어오지 못하도록 방을 닫는다
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        // 마스터 클라이언트만 씬 로드 (나머지는 AutomaticallySyncScene 으로 따라옴)
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel(gameSceneName);
    }

    // ---------------- 유틸 ----------------

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        Debug.Log($"[Matchmaking] {message}");
    }

    private void SetButtonInteractable(bool value)
    {
        if (matchButton != null)
            matchButton.interactable = value;
    }
}
