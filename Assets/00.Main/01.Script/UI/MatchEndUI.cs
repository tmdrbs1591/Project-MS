using System;
using System.Collections.Generic;
using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 매치가 완전히 끝났을 때(MatchEnd) 뜨는 결과 화면. 승패, 최종 스코어, 양쪽 통계(가한 피해/적중/
/// 궁극기 사용), 획득한 증강을 보여주고 "재대결"과 "로비로" 버튼을 제공한다.
/// 라운드 사이의 간단한 배너는 계속 MatchResultUI가 맡는다.
///
/// [동작]
///   - MatchManager/캐릭터의 네트워크 값을 매 프레임 폴링한다(PlayerCornerHUD와 같은 이유).
///   - 통계는 1대1이라 "상대가 받은 피해/적중"을 "내가 가한 피해/적중"으로 쓴다.
///   - 재대결: 양쪽이 모두 누르면 MatchManager가 새 매치를 시작하고, 이 화면은 저절로 닫힌다.
///     AI 대전에서는 봇이 항상 동의하므로 누르면 바로 시작된다.
///   - 상대가 나가면 마지막으로 본 통계를 그대로 둔 채 "상대가 나갔습니다"를 띄우고 재대결을 막는다.
///
/// [씬 설정]
///   - 항상 켜져 있는 오브젝트에 붙인다. 숨김은 panel(CanvasGroup) 알파로 하므로 패널도 켜둔 채로 둔다.
///   - left/right: 스폰 기준 왼쪽(P1)/오른쪽(P2). 비워둔 항목은 무시된다.
///   - augmentIconTemplate: 꺼둔 아이콘 하나(Image, 자식에 중첩 수 TMP_Text가 있으면 "x2"처럼 표시).
///     각 쪽 augmentContainer(가로 레이아웃 권장) 아래에 복제해서 쓴다.
///   - revealRows: 타이틀 뒤에 차례로 나타날 줄들(스코어, 피해, 적중, ...). 각자 CanvasGroup이 있어야 한다.
/// </summary>
public class MatchEndUI : MonoBehaviour
{
    [Serializable]
    private class SideView
    {
        public TMP_Text nameText;
        public Image portraitImage;
        public TMP_Text damageText;
        public Image damageBar;
        public TMP_Text hitsText;
        public Image hitsBar;
        public TMP_Text ultimateText;
        public RectTransform augmentContainer;
        [NonSerialized] public readonly List<GameObject> icons = new List<GameObject>();
    }

    private class SideStats
    {
        public string name = string.Empty;
        public Sprite portrait;
        public bool isLocal;
        public float damageDealt;
        public int hits;
        public int ultimates;
        public readonly List<KeyValuePair<AugmentData, int>> augments = new List<KeyValuePair<AugmentData, int>>();
        public int augmentSignature = -1;
    }

    public static MatchEndUI Instance { get; private set; }
    /// <summary>결과 화면이 떠 있는지. 이 동안엔 상대가 나가도 로비로 끌려가지 않는다(NetworkLauncher).</summary>
    public static bool IsShowing => Instance != null && Instance.showing;

    [SerializeField] private CanvasGroup panel;

    [Header("타이틀/스코어")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Color victoryColor = new Color(1f, 0.82f, 0.3f, 1f);
    [SerializeField] private Color defeatColor = new Color(0.65f, 0.7f, 0.85f, 1f);

    [Header("양쪽 정보")]
    [SerializeField] private SideView left = new SideView();
    [SerializeField] private SideView right = new SideView();
    [SerializeField] private Image augmentIconTemplate;

    [Header("버튼")]
    [SerializeField] private Button rematchButton;
    [SerializeField] private TMP_Text rematchLabel;
    [SerializeField] private Button lobbyButton;
    [SerializeField] private TMP_Text statusText;

    [Header("연출")]
    [SerializeField] private CanvasGroup[] revealRows;
    [SerializeField] private float showDelay = 0.6f;
    [SerializeField] private float rowInterval = 0.12f;
    [SerializeField] private float rowFadeDuration = 0.25f;
    [SerializeField] private float countDuration = 0.9f;
    [SerializeField] private float titlePopDuration = 0.35f;

    [Header("효과음")]
    [SerializeField] private AudioClip victoryClip;
    [SerializeField] private AudioClip defeatClip;
    [SerializeField] private AudioClip rematchClip;

    private bool showing;
    private float shownAt;
    private bool won;
    private bool isLocalLeft;
    private int leftWins;
    private int rightWins;
    private bool opponentLeft;
    private bool rematchRequested;
    private bool revealSoundPlayed;
    private readonly SideStats leftStats = new SideStats();
    private readonly SideStats rightStats = new SideStats();

    private void Awake()
    {
        Instance = this;

        if (rematchButton != null)
            rematchButton.onClick.AddListener(OnRematchClicked);
        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(OnLobbyClicked);
        if (augmentIconTemplate != null)
            augmentIconTemplate.gameObject.SetActive(false);

        SetVisible(false);
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

        if (!showing)
        {
            if (match != null && local != null && match.Phase == MatchPhase.MatchEnd)
                Show(match, local);
            return;
        }

        // 재대결이 시작되면(페이즈가 MatchEnd를 벗어남) 닫는다.
        if (match != null && match.Phase != MatchPhase.MatchEnd)
        {
            Hide();
            return;
        }

        CharacterBase opponent = FindOpponent(local);
        if (match == null || local == null || opponent == null)
            opponentLeft = true;
        else
            CaptureStats(match, local, opponent);

        UpdateRematchState(match, local, opponent);
        Render();
    }

    private void Show(MatchManager match, CharacterBase local)
    {
        showing = true;
        shownAt = Time.unscaledTime;
        opponentLeft = false;
        rematchRequested = false;
        revealSoundPlayed = false;
        won = match.LastRoundWinner == local.MatchPlayer;
        isLocalLeft = match.IsPlayer1(local.MatchPlayer);
        leftWins = match.Player1Wins;
        rightWins = match.Player2Wins;
        leftStats.augmentSignature = -1;
        rightStats.augmentSignature = -1;

        if (titleText != null)
        {
            titleText.text = won ? "VICTORY" : "DEFEAT";
            titleText.color = won ? victoryColor : defeatColor;
        }

        CharacterBase opponent = FindOpponent(local);
        if (opponent != null)
            CaptureStats(match, local, opponent);

        SetVisible(true);
        Render();
    }

    private void Hide()
    {
        showing = false;
        SetVisible(false);
    }

    private static CharacterBase FindOpponent(CharacterBase local)
    {
        if (local == null)
            return null;

        foreach (CharacterBase ch in CharacterBase.All)
        {
            if (ch != null && ch != local && ch.Object != null)
                return ch;
        }
        return null;
    }

    private void CaptureStats(MatchManager match, CharacterBase local, CharacterBase opponent)
    {
        leftWins = match.Player1Wins;
        rightWins = match.Player2Wins;

        CharacterBase leftChar = isLocalLeft ? local : opponent;
        CharacterBase rightChar = isLocalLeft ? opponent : local;
        Capture(leftStats, leftChar, rightChar, leftChar == local);
        Capture(rightStats, rightChar, leftChar, rightChar == local);
    }

    private static void Capture(SideStats stats, CharacterBase self, CharacterBase other, bool isLocal)
    {
        CharacterDefinition def = self.Definition;
        stats.name = def != null ? def.DisplayName : string.Empty;
        stats.portrait = def != null ? (def.CharacterMainSprite != null ? def.CharacterMainSprite : def.Portrait) : null;
        stats.isLocal = isLocal;
        stats.damageDealt = other.MatchDamageTaken;
        stats.hits = other.MatchHitsTaken;
        stats.ultimates = self.MatchUltimatesUsed;
        self.GetOwnedAugments(stats.augments);
    }

    private void UpdateRematchState(MatchManager match, CharacterBase local, CharacterBase opponent)
    {
        bool localWants = rematchRequested;
        bool opponentWants = false;
        if (!opponentLeft && match != null)
        {
            localWants |= match.WantsRematch(local.MatchPlayer);
            opponentWants = match.WantsRematch(opponent.MatchPlayer);
        }

        if (rematchButton != null)
            rematchButton.interactable = !opponentLeft && !localWants;

        if (rematchLabel != null)
            rematchLabel.text = localWants && !opponentLeft ? "요청함" : "재대결";

        string status;
        if (opponentLeft)
            status = "상대가 나갔습니다";
        else if (localWants && opponentWants)
            status = "곧 재대결을 시작합니다";
        else if (localWants)
            status = "상대의 응답을 기다리는 중...";
        else if (opponentWants)
            status = "상대가 재대결을 원합니다!";
        else
            status = string.Empty;

        if (statusText != null)
            statusText.text = status;
    }

    private void Render()
    {
        float elapsed = Time.unscaledTime - shownAt;

        // 타이틀: 살짝 크게 튀어나왔다가 제자리로.
        if (titleText != null)
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, titlePopDuration));
            float scale = t < 1f ? Mathf.Lerp(1.6f, 1f, 1f - (1f - t) * (1f - t)) : 1f;
            titleText.rectTransform.localScale = Vector3.one * scale;
            titleText.alpha = t;
        }

        float revealTime = elapsed - showDelay;
        if (revealTime >= 0f && !revealSoundPlayed)
        {
            revealSoundPlayed = true;
            SoundManager.Instance?.PlaySfx(won ? victoryClip : defeatClip);
        }

        if (revealRows != null)
        {
            for (int i = 0; i < revealRows.Length; i++)
            {
                if (revealRows[i] == null)
                    continue;
                float rowT = Mathf.Clamp01((revealTime - i * rowInterval) / Mathf.Max(0.01f, rowFadeDuration));
                revealRows[i].alpha = rowT;
                revealRows[i].transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, rowT);
            }
        }

        float count = Mathf.Clamp01(revealTime / Mathf.Max(0.01f, countDuration));
        count = 1f - (1f - count) * (1f - count);

        if (scoreText != null)
            scoreText.text = $"{leftWins} : {rightWins}";

        float maxDamage = Mathf.Max(1f, Mathf.Max(leftStats.damageDealt, rightStats.damageDealt));
        float maxHits = Mathf.Max(1f, Mathf.Max(leftStats.hits, rightStats.hits));
        RenderSide(left, leftStats, count, maxDamage, maxHits);
        RenderSide(right, rightStats, count, maxDamage, maxHits);
    }

    private void RenderSide(SideView view, SideStats stats, float count, float maxDamage, float maxHits)
    {
        if (view.nameText != null)
            view.nameText.text = stats.isLocal ? $"{stats.name} (나)" : stats.name;

        if (view.portraitImage != null)
        {
            view.portraitImage.sprite = stats.portrait;
            view.portraitImage.enabled = stats.portrait != null;
        }

        if (view.damageText != null)
            view.damageText.text = Mathf.RoundToInt(stats.damageDealt * count).ToString("N0");
        if (view.damageBar != null)
            view.damageBar.fillAmount = stats.damageDealt / maxDamage * count;
        if (view.hitsText != null)
            view.hitsText.text = Mathf.RoundToInt(stats.hits * count).ToString();
        if (view.hitsBar != null)
            view.hitsBar.fillAmount = stats.hits / maxHits * count;
        if (view.ultimateText != null)
            view.ultimateText.text = Mathf.RoundToInt(stats.ultimates * count).ToString();

        RefreshAugmentIcons(view, stats);
    }

    private void RefreshAugmentIcons(SideView view, SideStats stats)
    {
        if (view.augmentContainer == null || augmentIconTemplate == null)
            return;

        int signature = 17;
        foreach (KeyValuePair<AugmentData, int> pair in stats.augments)
            signature = signature * 31 + pair.Key.GetInstanceID() * 7 + pair.Value;
        if (signature == stats.augmentSignature)
            return;
        stats.augmentSignature = signature;

        foreach (GameObject icon in view.icons)
            Destroy(icon);
        view.icons.Clear();

        foreach (KeyValuePair<AugmentData, int> pair in stats.augments)
        {
            Image icon = Instantiate(augmentIconTemplate, view.augmentContainer);
            icon.gameObject.SetActive(true);
            icon.sprite = pair.Key.icon;
            TMP_Text stackText = icon.GetComponentInChildren<TMP_Text>(true);
            if (stackText != null)
                stackText.text = pair.Value > 1 ? $"x{pair.Value}" : string.Empty;
            view.icons.Add(icon.gameObject);
        }
    }

    private void OnRematchClicked()
    {
        if (opponentLeft || rematchRequested || MatchManager.Instance == null)
            return;

        rematchRequested = true;
        MatchManager.Instance.RequestRematch();
        SoundManager.Instance?.PlaySfx(rematchClip);
    }

    private void OnLobbyClicked()
    {
        NetworkLauncher.Instance?.ReturnToLobby();
    }

    private void SetVisible(bool visible)
    {
        if (panel == null)
            return;

        panel.alpha = visible ? 1f : 0f;
        panel.interactable = visible;
        panel.blocksRaycasts = visible;
    }
}
