using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 라운드/매치 결과 배너 UI. MatchManager 의 네트워크 상태를 매 프레임 읽어와 표시한다.
/// (체력바와 같은 이유로 이벤트 대신 폴링 방식을 쓴다: 원격 동기화 타이밍 문제를 피하기 위함)
///
/// [씬 설정]
///   - 게임 씬의 Screen Space Canvas 하위에 빈 오브젝트를 만들고 이 스크립트를 붙인다.
///   - resultPanel: 평소엔 꺼져 있다가 라운드/매치 종료 때만 켜지는 패널.
///   - titleText: "ROUND WIN" / "ROUND LOSE" / "VICTORY" / "DEFEAT" 표시.
///   - scoreText: "내 승수 : 상대 승수" 표시(예: "1 : 0").
///   - returnToLobbyButton: 매치가 완전히 끝났을 때만(MatchEnd) 나타나는 "로비로" 버튼.
///     누르면 나만 세션에서 나가 로비 씬으로 돌아간다(상대는 영향 없음).
///   - 씬에는 하나만 존재해야 한다(싱글턴).
/// </summary>
public class MatchResultUI : MonoBehaviour
{
    public static MatchResultUI Instance { get; private set; }

    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Button returnToLobbyButton;

    private void Awake()
    {
        Instance = this;
        SetPanelActive(false);
        SetReturnButtonActive(false);

        if (returnToLobbyButton != null)
            returnToLobbyButton.onClick.AddListener(OnReturnToLobbyClicked);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        MatchManager match = MatchManager.Instance;
        CharacterBase local = CharacterBase.LocalPlayer;

        if (match == null || local == null)
        {
            SetPanelActive(false);
            SetReturnButtonActive(false);
            return;
        }

        PlayerRef localPlayer = local.Object.InputAuthority;
        bool wonLastRound = match.LastRoundWinner == localPlayer;
        int myWins = match.GetWinsFor(localPlayer);
        int opponentWins = match.TotalWins - myWins;

        switch (match.Phase)
        {
            case MatchPhase.Fighting:
                SetPanelActive(false);
                SetReturnButtonActive(false);
                break;

            case MatchPhase.RoundEnd:
            case MatchPhase.AugmentSelect:
                SetPanelActive(true);
                SetReturnButtonActive(false);
                SetTitle(wonLastRound ? "ROUND WIN" : "ROUND LOSE");
                SetScore(myWins, opponentWins);
                break;

            case MatchPhase.MatchEnd:
                SetPanelActive(true);
                SetReturnButtonActive(true);
                SetTitle(wonLastRound ? "VICTORY" : "DEFEAT");
                SetScore(myWins, opponentWins);
                break;
        }
    }

    private void OnReturnToLobbyClicked()
    {
        NetworkLauncher.Instance?.ReturnToLobby();
    }

    private void SetPanelActive(bool active)
    {
        if (resultPanel != null)
            resultPanel.SetActive(active);
    }

    private void SetReturnButtonActive(bool active)
    {
        if (returnToLobbyButton != null)
            returnToLobbyButton.gameObject.SetActive(active);
    }

    private void SetTitle(string text)
    {
        if (titleText != null)
            titleText.text = text;
    }

    private void SetScore(int myWins, int opponentWins)
    {
        if (scoreText != null)
            scoreText.text = $"{myWins} : {opponentWins}";
    }
}
