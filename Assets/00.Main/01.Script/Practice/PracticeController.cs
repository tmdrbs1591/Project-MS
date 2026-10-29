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
///   - 연출(새 패널 항목을 연결했을 때): 기록 숫자가 바뀌면 칸이 톡 튀고, 쿨타임 스위치가 미끄러지며,
///     매칭 띠는 위에서 내려오고 로딩 링이 돈다. 패널은 들어올 때 왼쪽에서 밀려오고 창은 톡 튀어나온다.
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
    [Tooltip("이름표 외곽선 머티리얼(nameTagFont용). 비우면 외곽선을 직접 켠다.")]
    [SerializeField] private Material nameTagMaterial;
    [SerializeField] private string nameTagText = "연습 봇";
    [SerializeField] private float nameTagHeight = 1.6f;

    [Header("콤보")]
    [Tooltip("이 시간(초) 안에 다음 타격이 이어지면 같은 콤보로 센다.")]
    [SerializeField] private float comboGap = 2f;

    [Header("표시 (새 패널)")]
    [Tooltip("초당 피해 큰 숫자. 비워두면 statsText 한 줄 표시를 쓴다.")]
    [SerializeField] private TMP_Text dpsValueText;
    [SerializeField] private TMP_Text comboValueText;
    [Tooltip("콤보 칸 아래 작은 글자(누적 피해).")]
    [SerializeField] private TMP_Text comboDamageText;
    [Tooltip("값이 바뀔 때 톡 튀는 칸들.")]
    [SerializeField] private RectTransform dpsTile;
    [SerializeField] private RectTransform comboTile;
    [Tooltip("쿨타임 없음 스위치: 바탕 알약과 미끄러지는 동그라미.")]
    [SerializeField] private Image noCooldownSwitch;
    [SerializeField] private RectTransform noCooldownKnob;
    [SerializeField] private Color switchOnColor = new Color(0.35f, 0.9f, 0.55f, 1f);
    [SerializeField] private Color switchOffColor = new Color(0.22f, 0.27f, 0.4f, 1f);
    [Tooltip("매칭 버튼 바탕(매칭 중엔 취소 색으로 바뀐다).")]
    [SerializeField] private Image matchButtonImage;
    [SerializeField] private Color matchIdleColor = new Color(1f, 0.78f, 0.3f, 1f);
    [SerializeField] private Color matchActiveColor = new Color(0.95f, 0.38f, 0.42f, 1f);
    [Tooltip("매칭 띠의 경과 시간 글자(비우면 matchBannerText 한 줄에 같이 쓴다).")]
    [SerializeField] private TMP_Text matchTimerText;
    [SerializeField] private RectTransform matchSpinner;
    [SerializeField] private Image matchDot;

    [Header("사운드")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip switchOnSound;
    [SerializeField] private AudioClip switchOffSound;
    [SerializeField] private AudioClip ultimateFillSound;
    [SerializeField] private AudioClip cycleSound;
    [Tooltip("증강 창/나가기 확인 창이 열릴 때.")]
    [SerializeField] private AudioClip windowOpenSound;
    [SerializeField] private AudioClip windowCloseSound;
    [Tooltip("증강 목록에서 증강을 눌렀을 때.")]
    [SerializeField] private AudioClip augmentPickSound;
    [SerializeField] private AudioClip matchStartSound;
    [SerializeField] private AudioClip matchCancelSound;
    [Range(0f, 1f)] [SerializeField] private float uiVolume = 0.6f;

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
    private bool hasSettingsPanel;

    // 연출 상태
    private float dpsPunch;
    private float comboPunch;
    private int shownComboHits = -1;
    private int shownDps = -1;
    private float switchT;
    private float bannerT;
    private Vector2 bannerBase;
    private Vector2 panelBase;
    private float panelIntroTime = -10f;

    private void Awake()
    {
        bool active = PracticeSession.IsActive;
        hasSettingsPanel = FindObjectOfType<SettingsPanel>(true) != null;
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
        if (matchBanner != null)
            bannerBase = ((RectTransform)matchBanner.transform).anchoredPosition;
        if (panel != null)
            panelBase = ((RectTransform)panel.transform).anchoredPosition;
        panelIntroTime = Time.unscaledTime;

        if (!active)
        {
            enabled = false;
            return;
        }

        if (noCooldownButton != null) noCooldownButton.onClick.AddListener(ToggleNoCooldown);
        if (fillUltimateButton != null) fillUltimateButton.onClick.AddListener(FillUltimate);
        if (dummyModeButton != null) dummyModeButton.onClick.AddListener(CycleDummyMode);
        if (augmentButton != null) augmentButton.onClick.AddListener(ToggleAugmentWindow);
        if (augmentClearButton != null) augmentClearButton.onClick.AddListener(ClearAugments);
        if (augmentCloseButton != null) augmentCloseButton.onClick.AddListener(() => CloseWindow(augmentWindow));
        if (exitYesButton != null) exitYesButton.onClick.AddListener(() => NetworkLauncher.Instance?.ReturnToLobby());
        if (exitNoButton != null) exitNoButton.onClick.AddListener(() => CloseWindow(exitConfirm));
        if (matchButton != null) matchButton.onClick.AddListener(ToggleMatchmaking);

        AddHoverSound(noCooldownButton, fillUltimateButton, dummyModeButton, augmentButton, augmentClearButton,
            augmentCloseButton, exitYesButton, exitNoButton, matchButton);
    }

    private void AddHoverSound(params Button[] buttons)
    {
        if (hoverSound == null)
            return;
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;
            if (!button.TryGetComponent(out UIHoverSound hover))
                hover = button.gameObject.AddComponent<UIHoverSound>();
            hover.clip = hoverSound;
        }
    }

    private void Play(AudioClip clip, float volume = 1f) => SoundManager.Instance?.PlaySfx(clip, uiVolume * volume, 0.04f);

    private void OpenWindow(CanvasGroup window)
    {
        if (window == null || IsShown(window))
            return;
        SetGroup(window, true);
        Play(windowOpenSound);
    }

    private void CloseWindow(CanvasGroup window)
    {
        if (window == null || !IsShown(window))
            return;
        SetGroup(window, false);
        Play(windowCloseSound);
    }

    private void ClearAugments()
    {
        CharacterBase.LocalPlayer?.PracticeClearAugments();
        Play(switchOffSound);
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
            panelIntroTime = Time.unscaledTime;
        }

        HandleHotkeys();

        CharacterBase local = CharacterBase.LocalPlayer;
        if (PracticeSession.NoCooldown && local != null)
            local.PracticeRefreshSkills();

        UpdateDummy();
        UpdateLabels();
        AnimateUi();
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
            // 게임 씬에 설정 창이 있으면 Esc는 설정 창(그 안의 "연습장 나가기")이 쓴다.
            if (IsShown(augmentWindow))
                CloseWindow(augmentWindow);
            else if (IsShown(exitConfirm))
                CloseWindow(exitConfirm);
            else if (!hasSettingsPanel)
                OpenWindow(exitConfirm);
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
        {
            launcher.CancelPracticeMatchmaking();
            Play(matchCancelSound);
        }
        else
        {
            launcher.StartMatchmakingFromPractice();
            Play(matchStartSound);
        }
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
        bannerT = 0f;
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

    private void ToggleNoCooldown()
    {
        PracticeSession.NoCooldown = !PracticeSession.NoCooldown;
        Play(PracticeSession.NoCooldown ? switchOnSound : switchOffSound);
    }

    private void FillUltimate()
    {
        CharacterBase.LocalPlayer?.PracticeFillUltimate();
        Play(ultimateFillSound);
    }

    private void CycleDummyMode()
    {
        PracticeSession.DummyMode = (PracticeDummyMode)(((int)PracticeSession.DummyMode + 1) % 3);
        Play(cycleSound);
    }

    private void ToggleAugmentWindow()
    {
        if (augmentWindow == null)
            return;

        if (IsShown(augmentWindow))
        {
            CloseWindow(augmentWindow);
            return;
        }
        BuildAugmentList();
        OpenWindow(augmentWindow);
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
            button.onClick.AddListener(() =>
            {
                CharacterBase.LocalPlayer?.PracticeGrantAugment(type);
                Play(augmentPickSound);
            });
            AddHoverSound(button);
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

        // 로딩 화면이 떠 있는 동안엔 등장 페이드를 시작하지 않는다(로딩이 걷힌 뒤에 나타나는 게 보이게).
        if (SceneTransition.Instance != null && SceneTransition.Instance.IsBusy)
            dummyFoundAt = Time.time;

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
        nameTag.fontSize = 2.6f;
        nameTag.alignment = TextAlignmentOptions.Center;
        nameTag.color = new Color(0.92f, 0.94f, 1f, 1f);
        if (nameTagMaterial != null)
            nameTag.fontSharedMaterial = nameTagMaterial;
        else
        {
            nameTag.outlineWidth = 0.2f;
            nameTag.outlineColor = new Color32(0, 0, 0, 255);
        }
        nameTag.sortingLayerID = SortingLayer.NameToID("UI");
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

        bool newPanel = dpsValueText != null;
        if (newPanel)
        {
            int dps = Mathf.RoundToInt(lastComboDps);
            if (dps != shownDps)
            {
                if (shownDps >= 0)
                    dpsPunch = 1f;
                shownDps = dps;
                dpsValueText.text = dps.ToString();
            }
            if (lastComboHits != shownComboHits)
            {
                if (shownComboHits >= 0)
                    comboPunch = 1f;
                shownComboHits = lastComboHits;
                if (comboValueText != null)
                    comboValueText.text = lastComboHits + "<size=60%> 타</size>";
            }
            if (comboDamageText != null)
                comboDamageText.text = "누적 " + lastComboDamage.ToString("0");
        }
        else if (statsText != null)
            statsText.text = $"초당 피해  {lastComboDps:0}\n콤보  {lastComboHits}타 / {lastComboDamage:0}";

        // 새 패널에선 이 글자들이 "상태 값"만 보여준다(키/이름은 줄에 따로 적혀 있다).
        if (noCooldownLabel != null)
            noCooldownLabel.text = newPanel
                ? (PracticeSession.NoCooldown ? "ON" : "OFF")
                : (PracticeSession.NoCooldown ? "F1  쿨타임 없음  ON" : "F1  쿨타임 없음  OFF");
        if (dummyModeLabel != null)
            dummyModeLabel.text = newPanel
                ? DummyModeName(PracticeSession.DummyMode) + "  ›"
                : "F3  봇: " + DummyModeName(PracticeSession.DummyMode);

        NetworkLauncher launcher = NetworkLauncher.Instance;
        bool matching = launcher != null && launcher.IsMatchingInPractice;
        if (matchButtonLabel != null)
        {
            matchButtonLabel.text = matching ? "매칭 취소" : "매칭 시작";
            if (matchButtonImage != null)
                matchButtonLabel.color = matching ? Color.white : new Color(0.12f, 0.08f, 0.2f, 1f);
        }
        if (matchButtonImage != null)
            matchButtonImage.color = Color.Lerp(matchButtonImage.color, matching ? matchActiveColor : matchIdleColor, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));

        if (matchBanner != null)
        {
            // 띠는 바로 켜고 끄지 않고 위에서 미끄러져 내려오고 올라간다.
            bannerT = Mathf.MoveTowards(bannerT, matching ? 1f : 0f, Time.unscaledDeltaTime * 4f);
            float e = 1f - Mathf.Pow(1f - bannerT, 3f);
            matchBanner.alpha = e;
            matchBanner.blocksRaycasts = false;
            ((RectTransform)matchBanner.transform).anchoredPosition = bannerBase + new Vector2(0f, (1f - e) * 90f);
        }
        if (matching)
        {
            int seconds = Mathf.FloorToInt(launcher.MatchingElapsed);
            string time = $"{seconds / 60:00}:{seconds % 60:00}";
            if (matchTimerText != null)
                matchTimerText.text = time;
            else if (matchBannerText != null)
                matchBannerText.text = "매칭 중...  " + time;
        }
    }

    /// <summary>숫자 칸 튀기, 스위치 미끄러짐, 매칭 띠 로딩 링, 패널 등장, 창 팝업을 매 프레임 처리한다.</summary>
    private void AnimateUi()
    {
        float dt = Time.unscaledDeltaTime;
        float now = Time.unscaledTime;

        dpsPunch = Mathf.MoveTowards(dpsPunch, 0f, dt * 5f);
        comboPunch = Mathf.MoveTowards(comboPunch, 0f, dt * 5f);
        Punch(dpsTile, dpsPunch);
        Punch(comboTile, comboPunch);

        switchT = Mathf.MoveTowards(switchT, PracticeSession.NoCooldown ? 1f : 0f, dt * 7f);
        float s = switchT * switchT * (3f - 2f * switchT);
        if (noCooldownSwitch != null)
            noCooldownSwitch.color = Color.Lerp(switchOffColor, switchOnColor, s);
        if (noCooldownKnob != null)
        {
            RectTransform track = (RectTransform)noCooldownKnob.parent;
            float travel = (track.rect.width - noCooldownKnob.rect.width) * 0.5f - 3f;
            noCooldownKnob.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, s), 0f);
        }

        if (matchSpinner != null)
            matchSpinner.localRotation = Quaternion.Euler(0f, 0f, -now * 220f);
        if (matchDot != null)
        {
            Color c = matchDot.color;
            c.a = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(now * 3f));
            matchDot.color = c;
        }

        // 연습장에 들어오면 패널이 왼쪽에서 살며시 밀려 들어온다.
        if (panel != null)
        {
            float t = Mathf.Clamp01((now - panelIntroTime - 0.2f) / 0.45f);
            float e = 1f - Mathf.Pow(1f - t, 3f);
            ((RectTransform)panel.transform).anchoredPosition = panelBase + new Vector2((1f - e) * -60f, 0f);
            if (uiShown)
                panel.alpha = e;
        }

        // 창은 열릴 때 살짝 작게 시작해서 튕기듯 제 크기가 된다.
        PopTowardsFull(augmentWindow, dt);
        PopTowardsFull(exitConfirm, dt);
    }

    private static void Punch(RectTransform rt, float amount)
    {
        if (rt == null)
            return;
        float s = 1f + Mathf.Sin(amount * Mathf.PI) * 0.08f;
        rt.localScale = new Vector3(s, s, 1f);
    }

    private static void PopTowardsFull(CanvasGroup group, float dt)
    {
        if (group == null)
            return;
        Transform t = group.transform;
        float s = t.localScale.x;
        if (group.alpha < 0.5f)
        {
            t.localScale = new Vector3(0.9f, 0.9f, 1f);
            return;
        }
        s = Mathf.Lerp(s, 1f, 1f - Mathf.Exp(-16f * dt));
        t.localScale = new Vector3(s, s, 1f);
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
