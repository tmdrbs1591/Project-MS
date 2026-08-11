using System.Collections;
using Fusion;
using ProjectMS.CharacterSystem;
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
///   - scoreText: 스폰 위치 기준 "왼쪽 : 오른쪽" 승수 표시, 내 쪽엔 "(나)" 표시(예: "1(나) : 0"). 필요 없으면 비워둬도 됨.
///   - leftScoreSlots/rightScoreSlots: PlayerCornerHUD와 같은 패턴의 동그라미(개수는 MatchManager.
///     WinsRequired(=2)만큼 — 3판 2선승이라 2승 찍으면 매치가 끝나므로 그 이상은 필요 없다).
///     승수만큼 앞에서부터 채워지고, 이번에 새로 채워진 칸 하나만 통통 튀는 팝 애니메이션이 재생된다.
///   - returnToLobbyButton: 매치가 완전히 끝났을 때만(MatchEnd) 나타나는 "로비로" 버튼.
///     누르면 나만 세션에서 나가 로비 씬으로 돌아간다(상대는 영향 없음).
///   - 씬에는 하나만 존재해야 한다(싱글턴).
/// </summary>
public class MatchResultUI : MonoBehaviour
{
    public static MatchResultUI Instance { get; private set; }

    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text titleText;
    [Tooltip("텍스트로도 스코어를 보여주고 싶으면 연결한다. 동그라미만 쓸 거면 비워둬도 안전함.")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Button returnToLobbyButton;

    [Header("스코어 동그라미 (3판 2선승 = WinsRequired 2, 2칸)")]
    [Tooltip("스폰 위치 기준 왼쪽 플레이어(Player1)의 동그라미. MatchManager.WinsRequired(=2)와\n같은 개수로 맞춘다 — 그 이상은 누구도 채울 일이 없다(2승 찍는 순간 매치가 끝남).")]
    [SerializeField] private Image[] leftScoreSlots;
    [Tooltip("스폰 위치 기준 오른쪽 플레이어(Player2)의 동그라미. 개수는 왼쪽과 동일하게.")]
    [SerializeField] private Image[] rightScoreSlots;
    [SerializeField] private Color scoreFilledColor = Color.white;
    [SerializeField] private Color scoreEmptyColor = new Color(1f, 1f, 1f, 0.25f);
    [Tooltip("이번에 새로 채워진 동그라미 하나가 통통 튀며 나타나는 연출 시간(초).")]
    [Min(0f)] [SerializeField] private float scorePopDuration = 0.35f;
    [Tooltip("팝 애니메이션 중 최대로 커지는 배율(1 = 안 커짐).")]
    [Min(1f)] [SerializeField] private float scorePopScale = 1.4f;

    // 직전에 화면에 표시했던 승수. 이보다 늘어난 칸만 "새로 채워짐"으로 보고 팝 애니메이션을 튼다
    // (Update가 매 프레임 SetScore를 다시 부르므로, 이게 없으면 화면에 떠있는 내내 매 프레임 재생된다).
    private int lastShownLeftWins = -1;
    private int lastShownRightWins = -1;

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
        bool isLocalLeft = match.IsPlayer1(localPlayer);

        switch (match.Phase)
        {
            case MatchPhase.Fighting:
                SetPanelActive(false);
                SetReturnButtonActive(false);
                break;

            case MatchPhase.RoundEnd:
            case MatchPhase.AugmentSelect:
                // RoundEnd 진입 직후엔 RoundFinishController가 히트스톱/줌/슬로우모션을 로컬로
                // 재생 중일 수 있다. 그 연출이 화면을 다 차지하는 동안은 배너를 띄우지 않고
                // 기다렸다가, 연출이 끝나고 화면이 정상으로 돌아온 뒤에 띄운다.
                bool finishPlaying = match.Phase == MatchPhase.RoundEnd
                    && RoundFinishController.Instance != null
                    && RoundFinishController.Instance.IsPlaying;

                SetPanelActive(!finishPlaying);
                SetReturnButtonActive(false);

                if (!finishPlaying)
                {
                    SetTitle(wonLastRound ? "ROUND WIN" : "ROUND LOSE");
                    SetScore(match.Player1Wins, match.Player2Wins, isLocalLeft);
                }
                break;

            case MatchPhase.MatchEnd:
                SetPanelActive(true);
                SetReturnButtonActive(true);
                SetTitle(wonLastRound ? "VICTORY" : "DEFEAT");
                SetScore(match.Player1Wins, match.Player2Wins, isLocalLeft);
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

    /// <summary>스폰 위치 기준(왼쪽:오른쪽) 순서로 표시하고, 내 쪽에 "(나)" 표시를 붙인다.</summary>
    private void SetScore(int leftWins, int rightWins, bool isLocalLeft)
    {
        if (scoreText != null)
        {
            string leftLabel = isLocalLeft ? $"{leftWins}(나)" : $"{leftWins}";
            string rightLabel = isLocalLeft ? $"{rightWins}" : $"{rightWins}(나)";
            scoreText.text = $"{leftLabel} : {rightLabel}";
        }

        UpdateScoreSlots(leftScoreSlots, leftWins, ref lastShownLeftWins);
        UpdateScoreSlots(rightScoreSlots, rightWins, ref lastShownRightWins);
    }

    /// <summary>슬롯 색을 승수에 맞춰 채우고, 직전(lastShownWins)엔 안 채워져 있던 칸만
    /// 새로 채워진 것으로 보고 팝 애니메이션(크기+색 둘 다)을 튼다. 이미 채워져 있던 칸이나
    /// 계속 비어있는 칸은 애니메이션 없이 즉시 색만 맞춘다.</summary>
    private void UpdateScoreSlots(Image[] slots, int wins, ref int lastShownWins)
    {
        if (slots == null || slots.Length == 0)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            bool filled = i < wins;
            bool isNewlyFilled = filled && i >= lastShownWins;

            if (isNewlyFilled)
                StartCoroutine(PlayScorePop(slots[i]));
            else
                slots[i].color = filled ? scoreFilledColor : scoreEmptyColor;
        }

        lastShownWins = wins;
    }

    /// <summary>슬롯 하나가 통통 튀며 커짐과 동시에 scoreEmptyColor에서 scoreFilledColor로
    /// 색이 서서히 채워지는 연출. Time.timeScale이 낮아진 동안(RoundFinishController의
    /// 히트스톱/슬로우모션)에도 정상 속도로 재생되도록 unscaledDeltaTime을 쓴다.</summary>
    private IEnumerator PlayScorePop(Image slotImage)
    {
        if (slotImage == null)
            yield break;

        if (scorePopDuration <= 0f)
        {
            slotImage.color = scoreFilledColor;
            yield break;
        }

        Transform slotTransform = slotImage.transform;
        Vector3 baseScale = slotTransform.localScale;
        float elapsed = 0f;

        while (elapsed < scorePopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / scorePopDuration);
            float bump = Mathf.Sin(t * Mathf.PI); // 0 → 1 → 0 로 커졌다가 원래 크기로 돌아온다.
            slotTransform.localScale = baseScale * (1f + bump * (scorePopScale - 1f));
            slotImage.color = Color.Lerp(scoreEmptyColor, scoreFilledColor, t);
            yield return null;
        }

        slotTransform.localScale = baseScale;
        slotImage.color = scoreFilledColor;
    }
}
