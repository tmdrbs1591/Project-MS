using System.Collections.Generic;
using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 연습장 화면과 규칙. 게임 씬에 두고, PracticeSession이 켜져 있을 때만 동작한다(평소엔 패널을 숨긴 채 쉼).
///   - 연습 패널: 초당 피해/콤보 표시, F1 쿨타임 없음, F2 궁극기 게이지 채우기, F3 봇 움직임 바꾸기,
///     F4 증강 넣어보기(목록 창). 버튼으로 눌러도 된다.
///   - 연습 봇: 회색으로 칠하고(처음엔 서서히 나타남) 머리 위에 이름표를 띄운다. 3초 동안 안 맞으면 체력이 다시 찬다.
///   - Esc: "연습장 나가기" 확인 창.
///   - 매칭: 패널의 매칭 버튼으로 연습하면서 매칭을 걸거나 취소한다. 매칭 중에는 상단 띠에 경과 시간을 보여주고,
///     상대가 들어오면 연습이 끝나며(PracticeSession.End) 이 화면도 저절로 닫힌다.
///
/// [씬 설정]
///   - 게임 씬 Canvas 아래 패널에 붙인다. 숨김은 CanvasGroup으로 한다.
///   - 비워둔 UI 항목은 무시된다.
/// </summary>
public class PracticeController : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Button noCooldownButton;
    [SerializeField] private TMP_Text noCooldownLabel;
    [SerializeField] private Button fillUltimateButton;
    [SerializeField] private Button dummyModeButton;
    [SerializeField] private TMP_Text dummyModeLabel;
    [SerializeField] private Button augmentButton;

    [Header("매칭")]
    [SerializeField] private Button matchButton;
    [SerializeField] private TMP_Text matchButtonLabel;
    [SerializeField] private CanvasGroup matchBanner;
    [SerializeField] private TMP_Text matchBannerText;
    [Tooltip("오프라인 ↔ 매칭 세션을 바꾸는 동안(캐릭터가 잠깐 사라짐) 화면을 덮는 패널.")]
    [SerializeField] private CanvasGroup switchOverlay;
    [SerializeField] private TMP_Text switchOverlayText;
    [SerializeField] private float switchOverlayFadeSpeed = 6f;

    [Header("증강 넣어보기")]
    [SerializeField] private CanvasGroup augmentWindow;
    [SerializeField] private RectTransform augmentListContainer;
    [SerializeField] private Button augmentEntryTemplate;
    [SerializeField] private Button augmentClearButton;
    [SerializeField] private Button augmentCloseButton;

    [Header("나가기 확인")]
    [SerializeField] private CanvasGroup exitConfirm;
    [SerializeField] private Button exitYesButton;
    [SerializeField] private Button exitNoButton;

    [Header("연습 봇")]
    [Tooltip("연습 봇 색. 캐릭터가 원래 어두운 색이 많아서 회색만으론 티가 안 나므로 반투명으로 구분한다.")]
    [SerializeField] private Color dummyTint = new Color(0.8f, 0.8f, 0.85f, 0.55f);
    [SerializeField] private float dummyFadeInDuration = 0.8f;
    [SerializeField] private float dummyRegenDelay = 3f;
    [SerializeField] private TMP_FontAsset nameTagFont;
    [SerializeField] private string nameTagText = "연습 봇";
    [SerializeField] private float nameTagHeight = 1.6f;

    [Header("콤보")]
    [Tooltip("이 시간(초) 안에 다음 타격이 이어지면 같은 콤보로 센다.")]
    [SerializeField] private float comboGap = 2f;

    private CharacterBase dummy;
    private float dummyFoundAt;
    private float lastDummyHitAt = -100f;
    private bool dummyNeedsHeal;
    private TextMeshPro nameTag;
    private readonly List<SpriteRenderer> dummyRenderers = new List<SpriteRenderer>();

    private float comboStart;
    private float comboLastHit = -100f;
    private float comboDamage;
    private int comboHits;
    private float lastComboDamage;
    private int lastComboHits;
    private float lastComboDps;
    private bool augmentListBuilt;
    private bool lockedByMe;
    private bool uiShown = true;

    private void Awake()
    {
        bool active = PracticeSession.IsActive;
        SetGroup(panel, active);
        SetGroup(augmentWindow, false);
        SetGroup(exitConfirm, false);
        SetGroup(matchBanner, false);
        if (switchOverlay != null)
        {
            switchOverlay.alpha = 0f;
            switchOverlay.blocksRaycasts = false;
            switchOverlay.interactable = false;
        }
        if (augmentEntryTemplate != null)
            augmentEntryTemplate.gameObject.SetActive(false);

        if (!active)
        {
            enabled = false;
            return;
        }

        if (noCooldownButton != null) noCooldownButton.onClick.AddListener(ToggleNoCooldown);
        if (fillUltimateButton != null) fillUltimateButton.onClick.AddListener(FillUltimate);
        if (dummyModeButton != null) dummyModeButton.onClick.AddListener(CycleDummyMode);
        if (augmentButton != null) augmentButton.onClick.AddListener(ToggleAugmentWindow);
        if (augmentClearButton != null) augmentClearButton.onClick.AddListener(() => CharacterBase.LocalPlayer?.PracticeClearAugments());
        if (augmentCloseButton != null) augmentCloseButton.onClick.AddListener(() => SetGroup(augmentWindow, false));
        if (exitYesButton != null) exitYesButton.onClick.AddListener(() => NetworkLauncher.Instance?.ReturnToLobby());
        if (exitNoButton != null) exitNoButton.onClick.AddListener(() => SetGroup(exitConfirm, false));
        if (matchButton != null) matchButton.onClick.AddListener(ToggleMatchmaking);
    }

    private void OnEnable() => CharacterBase.HitRendered += OnHitRendered;
    private void OnDisable() => CharacterBase.HitRendered -= OnHitRendered;

    private void OnDestroy()
    {
        if (lockedByMe)
            CharacterBase.SetLobbyControlLocked(false);
        if (nameTag != null)
            Destroy(nameTag.gameObject);
    }

    private void Update()
    {
        UpdateSwitchOverlay();

        // 매칭하면서 연습하다가 상대가 들어오면 연습이 끝난다 → 연습 화면을 숨긴다.
        // (오프라인 ↔ 매칭 전환 중엔 같은 씬이 그대로 쓰이므로 꺼버리지 않고, 다시 연습이 켜지면 되살린다.)
        if (!PracticeSession.IsActive)
        {
            HidePracticeUi();
            return;
        }
        if (!uiShown)
        {
            uiShown = true;
            SetGroup(panel, true);
        }

        HandleHotkeys();

        CharacterBase local = CharacterBase.LocalPlayer;
        if (PracticeSession.NoCooldown && local != null)
            local.PracticeRefreshSkills();

        UpdateDummy();
        UpdateLabels();
        UpdateControlLock();
    }

    // 창(증강 목록/나가기 확인)이 떠 있는 동안엔 클릭이 공격으로 나가지 않게 조작을 잠근다.
    private void UpdateControlLock()
    {
        bool windowOpen = IsShown(augmentWindow) || IsShown(exitConfirm);
        if (windowOpen && !lockedByMe && !CharacterBase.IsLobbyControlLocked)
        {
            lockedByMe = true;
            CharacterBase.SetLobbyControlLocked(true);
        }
        else if (!windowOpen && lockedByMe)
        {
            lockedByMe = false;
            CharacterBase.SetLobbyControlLocked(false);
        }
    }

    private void HandleHotkeys()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (IsShown(augmentWindow))
                SetGroup(augmentWindow, false);
            else
                SetGroup(exitConfirm, !IsShown(exitConfirm));
        }

        if (keyboard.f1Key.wasPressedThisFrame) ToggleNoCooldown();
        if (keyboard.f2Key.wasPressedThisFrame) FillUltimate();
        if (keyboard.f3Key.wasPressedThisFrame) CycleDummyMode();
        if (keyboard.f4Key.wasPressedThisFrame) ToggleAugmentWindow();
    }

    private void ToggleMatchmaking()
    {
        NetworkLauncher launcher = NetworkLauncher.Instance;
        if (launcher == null)
            return;

        if (launcher.IsMatchingInPractice)
            launcher.CancelPracticeMatchmaking();
        else
            launcher.StartMatchmakingFromPractice();
    }

    private void HidePracticeUi()
    {
        if (!uiShown)
            return;

        uiShown = false;
        SetGroup(panel, false);
        SetGroup(augmentWindow, false);
        SetGroup(exitConfirm, false);
        SetGroup(matchBanner, false);
        UpdateControlLock();
        if (nameTag != null)
            Destroy(nameTag.gameObject);
        dummy = null;
    }

    private void UpdateSwitchOverlay()
    {
        if (switchOverlay == null)
            return;

        NetworkLauncher launcher = NetworkLauncher.Instance;
        bool switching = launcher != null && launcher.IsSwitchingSession;
        if (switching && switchOverlayText != null)
            switchOverlayText.text = launcher.SwitchingMessage;

        float target = switching ? 1f : 0f;
        switchOverlay.alpha = Mathf.MoveTowards(switchOverlay.alpha, target, switchOverlayFadeSpeed * Time.unscaledDeltaTime);
        switchOverlay.blocksRaycasts = switchOverlay.alpha > 0.5f;
    }

    private void ToggleNoCooldown() => PracticeSession.NoCooldown = !PracticeSession.NoCooldown;

    private void FillUltimate() => CharacterBase.LocalPlayer?.PracticeFillUltimate();

    private void CycleDummyMode()
    {
        PracticeSession.DummyMode = (PracticeDummyMode)(((int)PracticeSession.DummyMode + 1) % 3);
    }

    private void ToggleAugmentWindow()
    {
        if (augmentWindow == null)
            return;

        bool show = !IsShown(augmentWindow);
        if (show)
            BuildAugmentList();
        SetGroup(augmentWindow, show);
    }

    private void BuildAugmentList()
    {
        CharacterBase local = CharacterBase.LocalPlayer;
        if (augmentListBuilt || local == null || augmentListContainer == null || augmentEntryTemplate == null)
            return;
        augmentListBuilt = true;

        foreach (AugmentPoolEntry entry in local.GetAugmentPool())
        {
            AugmentData data = entry.Data;
            if (data == null)
                continue;

            Button button = Instantiate(augmentEntryTemplate, augmentListContainer);
            button.gameObject.SetActive(true);
            button.name = data.title;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = data.title;
            Transform iconTransform = button.transform.Find("Icon");
            if (iconTransform != null && iconTransform.TryGetComponent(out Image icon))
            {
                icon.sprite = data.icon;
                icon.enabled = data.icon != null;
            }

            AugmentType type = data.type;
            button.onClick.AddListener(() => CharacterBase.LocalPlayer?.PracticeGrantAugment(type));
        }
    }

    // ---------------- 연습 봇 ----------------

    private void UpdateDummy()
    {
        if (dummy == null || dummy.Object == null)
        {
            dummy = FindDummy();
            if (dummy == null)
                return;

            dummyFoundAt = Time.time;
            dummyRenderers.Clear();
            dummy.GetComponentsInChildren(true, dummyRenderers);
            CreateNameTag();
        }

        // 맞은 뒤 일정 시간 조용하면 체력을 채운다.
        if (dummyNeedsHeal && Time.time - lastDummyHitAt >= dummyRegenDelay)
        {
            dummyNeedsHeal = false;
            dummy.PracticeFullHeal();
        }

        // 회색 칠하기 + 처음 등장 페이드. 흰색(기본색)인 부분만 바꿔서 피격 번쩍임 같은 색 연출은 건드리지 않는다.
        float fade = dummyFadeInDuration > 0f ? Mathf.Clamp01((Time.time - dummyFoundAt) / dummyFadeInDuration) : 1f;
        foreach (SpriteRenderer sr in dummyRenderers)
        {
            if (sr == null)
                continue;
            Color c = sr.color;
            bool isWhite = c.r >= 0.99f && c.g >= 0.99f && c.b >= 0.99f;
            bool isTinted = Mathf.Approximately(c.r, dummyTint.r) && Mathf.Approximately(c.g, dummyTint.g) && Mathf.Approximately(c.b, dummyTint.b);
            if (isWhite || isTinted)
                sr.color = new Color(dummyTint.r, dummyTint.g, dummyTint.b, dummyTint.a * fade);
        }

        if (nameTag != null)
        {
            nameTag.transform.position = dummy.transform.position + Vector3.up * nameTagHeight;
            nameTag.alpha = fade;
        }
    }

    private static CharacterBase FindDummy()
    {
        foreach (CharacterBase ch in CharacterBase.All)
        {
            if (ch != null && ch.Object != null && ch.IsBot)
                return ch;
        }
        return null;
    }

    private void CreateNameTag()
    {
        if (nameTag != null)
            return;

        var go = new GameObject("Practice Dummy Name Tag");
        nameTag = go.AddComponent<TextMeshPro>();
        if (nameTagFont != null)
            nameTag.font = nameTagFont;
        nameTag.text = nameTagText;
        nameTag.fontSize = 4f;
        nameTag.alignment = TextAlignmentOptions.Center;
        nameTag.color = new Color(0.85f, 0.85f, 0.9f, 1f);
        nameTag.outlineWidth = 0.2f;
        nameTag.outlineColor = new Color32(0, 0, 0, 255);
        nameTag.sortingOrder = 50;
        nameTag.rectTransform.sizeDelta = new Vector2(6f, 1f);
    }

    private void OnHitRendered(CharacterBase victim, CharacterHitRenderInfo info)
    {
        if (victim == null || victim != dummy)
            return;

        lastDummyHitAt = Time.time;
        dummyNeedsHeal = true;

        CharacterBase local = CharacterBase.LocalPlayer;
        if (local == null || info.Attacker != local.MatchPlayer)
            return;

        if (Time.time - comboLastHit > comboGap)
        {
            comboStart = Time.time;
            comboDamage = 0f;
            comboHits = 0;
        }
        comboLastHit = Time.time;
        comboDamage += info.Amount;
        comboHits++;
    }

    // ---------------- 표시 ----------------

    private void UpdateLabels()
    {
        bool comboActive = Time.time - comboLastHit <= comboGap;
        if (comboActive)
        {
            lastComboDamage = comboDamage;
            lastComboHits = comboHits;
            lastComboDps = comboDamage / Mathf.Max(1f, comboLastHit - comboStart + 0.5f);
        }

        if (statsText != null)
            statsText.text = $"초당 피해  {lastComboDps:0}\n콤보  {lastComboHits}타 / {lastComboDamage:0}";
        if (noCooldownLabel != null)
            noCooldownLabel.text = PracticeSession.NoCooldown ? "F1  쿨타임 없음  ON" : "F1  쿨타임 없음  OFF";
        if (dummyModeLabel != null)
            dummyModeLabel.text = "F3  봇: " + DummyModeName(PracticeSession.DummyMode);

        NetworkLauncher launcher = NetworkLauncher.Instance;
        bool matching = launcher != null && launcher.IsMatchingInPractice;
        if (matchButtonLabel != null)
            matchButtonLabel.text = matching ? "매칭 취소" : "매칭 시작";
        SetGroup(matchBanner, matching);
        if (matching && matchBannerText != null)
        {
            int seconds = Mathf.FloorToInt(launcher.MatchingElapsed);
            matchBannerText.text = $"매칭 중...  {seconds / 60:00}:{seconds % 60:00}";
        }
    }

    private static string DummyModeName(PracticeDummyMode mode)
    {
        switch (mode)
        {
            case PracticeDummyMode.Walk: return "좌우 이동";
            case PracticeDummyMode.Jump: return "점프 반복";
            default: return "서있기";
        }
    }

    private static bool IsShown(CanvasGroup group) => group != null && group.alpha > 0.5f;

    private static void SetGroup(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }
}
