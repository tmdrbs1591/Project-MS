using System.Collections.Generic;
using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 설정 창. 볼륨(마스터/효과음/음악), 화면 모드, 해상도, VSync, 프레임 제한, 화면 흔들림을 다룬다.
/// 값을 바꾸면 즉시 적용·저장된다(볼륨은 SoundManager, 나머지는 GameSettings).
///
/// [열기/닫기]
///   - Open()/Close()/Toggle()을 버튼 OnClick에 연결하거나 Esc로 연다.
///   - Esc는 다른 창(캐릭터 선택, 매칭 대기 등)이 조작을 잠가둔 동안에는 설정을 열지 않는다.
///     그 창들이 Esc를 "닫기"로 먼저 쓰기 때문이다.
///   - 열려 있는 동안 내 캐릭터 조작을 막는다(로비/인게임 공통). 멀티라 게임 시간은 멈추지 않는다.
///   - 게임 씬에서는 "나가기" 버튼(leaveButton)으로 대전/연습장을 나갈 수 있다(확인 창을 거친다).
///     로비에서는 비워두면 된다.
///
/// [씬 설정]
///   - Canvas 아래 패널을 만들고 이 스크립트를 붙인다. 숨김은 CanvasGroup으로 하므로(없으면 자동 추가)
///     패널을 켜둔 채로 둬도 된다.
///   - 필요한 UI만 연결하면 된다. 비워둔 항목은 무시된다.
///   - 슬라이더 범위는 코드가 0~1로 맞춘다. 드롭다운 항목도 코드가 채운다.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("열기/닫기")]
    [SerializeField] private Button closeButton;
    [SerializeField] private bool openWithEscape = true;
    [Tooltip("키 설정 창을 여는 버튼(선택). 키 설정 창(KeyBindingsPanel)은 씬에서 찾아 연다.")]
    [SerializeField] private Button keyBindingsButton;

    [Header("볼륨")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;

    [Header("화면")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle vSyncToggle;
    [SerializeField] private TMP_Dropdown frameLimitDropdown;

    [Header("화면 흔들림")]
    [SerializeField] private Slider screenShakeSlider;

    [Header("나가기 (게임 씬 전용, 로비에선 비워둠)")]
    [SerializeField] private Button leaveButton;
    [SerializeField] private TMP_Text leaveButtonLabel;
    [SerializeField] private CanvasGroup leaveConfirm;
    [SerializeField] private TMP_Text leaveConfirmText;
    [SerializeField] private Button leaveYesButton;
    [SerializeField] private Button leaveNoButton;

    private CanvasGroup canvasGroup;
    private bool isOpen;
    private bool lockedByMe;
    private bool lockedAtEndOfLastFrame;
    private bool typingAtEndOfLastFrame;
    private bool keyBindingsOpenAtEndOfLastFrame;
    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();

    public bool IsOpen => isOpen;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (keyBindingsButton != null)
            keyBindingsButton.onClick.AddListener(() => { if (KeyBindingsPanel.Instance != null) KeyBindingsPanel.Instance.Open(); });

        SetupSlider(masterVolumeSlider, v => SoundManager.Instance?.SetMasterVolume(v));
        SetupSlider(sfxVolumeSlider, v => SoundManager.Instance?.SetSfxVolume(v));
        SetupSlider(musicVolumeSlider, v => SoundManager.Instance?.SetMusicVolume(v));
        SetupSlider(screenShakeSlider, GameSettings.SetScreenShakeScale);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(GameSettings.SetFullscreen);
        if (vSyncToggle != null)
            vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);

        SetupResolutionDropdown();
        SetupFrameLimitDropdown();

        if (leaveButton != null)
            leaveButton.onClick.AddListener(ShowLeaveConfirm);
        if (leaveYesButton != null)
            leaveYesButton.onClick.AddListener(() => NetworkLauncher.Instance?.ReturnToLobby());
        if (leaveNoButton != null)
            leaveNoButton.onClick.AddListener(() => SetGroupVisible(leaveConfirm, false));
        SetGroupVisible(leaveConfirm, false);

        SetVisible(false);
    }

    private void OnDisable()
    {
        // 열린 채로 씬이 바뀌거나 꺼져도 조작 잠금이 남지 않게 한다.
        ReleaseLock();
    }

    private void Update()
    {
        if (!openWithEscape)
            return;

        Keyboard kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame)
            return;

        // 키 설정 창이 열려 있으면 Esc는 그 창(입력 취소/창 닫기)이 쓴다. 같은 프레임에 그 창이 먼저
        // 닫혔을 수도 있으니 직전 프레임 상태도 본다.
        if (KeyBindingsPanel.IsOpen || keyBindingsOpenAtEndOfLastFrame)
            return;

        if (isOpen)
        {
            // 나가기 확인 창이 떠 있으면 Esc는 그 창만 닫는다.
            if (leaveConfirm != null && leaveConfirm.alpha > 0.5f)
                SetGroupVisible(leaveConfirm, false);
            else
                Close();
        }
        else if (CanOpenWithEscape())
            Open();
    }

    private void LateUpdate()
    {
        lockedAtEndOfLastFrame = CharacterBase.IsLobbyControlLocked;
        typingAtEndOfLastFrame = IsTypingInInputField();
        keyBindingsOpenAtEndOfLastFrame = KeyBindingsPanel.IsOpen;
    }

    // 다른 창(매칭 대기, 캐릭터 선택 등)이 같은 Esc로 자기를 닫으면서 잠금을 이번 프레임에 풀 수 있다.
    // 스크립트 실행 순서에 따라 그 직후에 이 검사가 돌면 "잠금 없음"으로 보여 설정까지 같이 열리므로,
    // 직전 프레임 끝에도 잠겨 있지 않았을 때만 연다. 채팅 등 입력창에 글을 쓰는 중에도 열지 않는다.
    private bool CanOpenWithEscape()
    {
        return !CharacterBase.IsLobbyControlLocked && !lockedAtEndOfLastFrame &&
               !IsTypingInInputField() && !typingAtEndOfLastFrame;
    }

    private static bool IsTypingInInputField()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected == null)
            return false;

        TMP_InputField tmpInput = selected.GetComponent<TMP_InputField>();
        if (tmpInput != null && tmpInput.isFocused)
            return true;
        InputField input = selected.GetComponent<InputField>();
        return input != null && input.isFocused;
    }

    public void Toggle()
    {
        if (isOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        if (isOpen)
            return;

        isOpen = true;
        RefreshValues();
        SetVisible(true);
        if (leaveButtonLabel != null)
            leaveButtonLabel.text = PracticeSession.IsActive ? "연습장 나가기" : "로비로 나가기";

        if (!CharacterBase.IsLobbyControlLocked)
        {
            lockedByMe = true;
            CharacterBase.SetLobbyControlLocked(true);
            LobbyCharacterController.SetLocked(true);
        }
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        SetVisible(false);
        SetGroupVisible(leaveConfirm, false);
        ReleaseLock();
        if (KeyBindingsPanel.IsOpen)
            KeyBindingsPanel.Instance.Close();
    }

    private void ReleaseLock()
    {
        if (!lockedByMe)
            return;

        lockedByMe = false;
        CharacterBase.SetLobbyControlLocked(false);
        LobbyCharacterController.SetLocked(false);
    }

    // 열 때마다 저장된 현재 값으로 UI를 맞춘다(변경 이벤트는 발생시키지 않는다).
    private void RefreshValues()
    {
        SoundManager sound = SoundManager.Instance;
        SetSliderSilently(masterVolumeSlider, sound != null ? sound.MasterVolume : 1f);
        SetSliderSilently(sfxVolumeSlider, sound != null ? sound.SfxVolume : 1f);
        SetSliderSilently(musicVolumeSlider, sound != null ? sound.MusicVolume : 1f);
        SetSliderSilently(screenShakeSlider, GameSettings.ScreenShakeScale);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        if (vSyncToggle != null)
            vSyncToggle.SetIsOnWithoutNotify(GameSettings.VSync);

        if (resolutionDropdown != null)
        {
            int index = resolutions.FindIndex(r => r.x == Screen.width && r.y == Screen.height);
            if (index >= 0)
                resolutionDropdown.SetValueWithoutNotify(index);
        }

        if (frameLimitDropdown != null)
        {
            int index = System.Array.IndexOf(GameSettings.FrameLimitOptions, GameSettings.FrameLimit);
            frameLimitDropdown.SetValueWithoutNotify(Mathf.Max(0, index));
            frameLimitDropdown.interactable = !GameSettings.VSync;
        }
    }

    private void OnVSyncChanged(bool enabled)
    {
        GameSettings.SetVSync(enabled);
        // VSync가 켜져 있으면 프레임 제한이 무시되므로 드롭다운을 잠근다.
        if (frameLimitDropdown != null)
            frameLimitDropdown.interactable = !enabled;
    }

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null)
            return;

        // 같은 해상도가 주사율별로 여러 번 나오므로 가로x세로 기준으로 중복을 뺀다.
        resolutions.Clear();
        var labels = new List<string>();
        foreach (Resolution r in Screen.resolutions)
        {
            var size = new Vector2Int(r.width, r.height);
            if (resolutions.Contains(size))
                continue;
            resolutions.Add(size);
            labels.Add($"{r.width} x {r.height}");
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(labels);
        resolutionDropdown.onValueChanged.AddListener(index =>
        {
            if (index >= 0 && index < resolutions.Count)
                GameSettings.SetResolution(resolutions[index].x, resolutions[index].y);
        });
    }

    private void SetupFrameLimitDropdown()
    {
        if (frameLimitDropdown == null)
            return;

        var labels = new List<string>();
        foreach (int fps in GameSettings.FrameLimitOptions)
            labels.Add(fps > 0 ? $"{fps} FPS" : "제한 없음");

        frameLimitDropdown.ClearOptions();
        frameLimitDropdown.AddOptions(labels);
        frameLimitDropdown.onValueChanged.AddListener(index =>
        {
            if (index >= 0 && index < GameSettings.FrameLimitOptions.Length)
                GameSettings.SetFrameLimit(GameSettings.FrameLimitOptions[index]);
        });
    }

    private static void SetupSlider(Slider slider, UnityEngine.Events.UnityAction<float> onChanged)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.onValueChanged.AddListener(onChanged);
    }

    private static void SetSliderSilently(Slider slider, float value)
    {
        if (slider != null)
            slider.SetValueWithoutNotify(value);
    }

    private void ShowLeaveConfirm()
    {
        if (leaveConfirmText != null)
            leaveConfirmText.text = PracticeSession.IsActive ? "연습장을 나갈까요?" : "대전을 포기하고 로비로 나갈까요?";
        SetGroupVisible(leaveConfirm, true);
    }

    private static void SetGroupVisible(CanvasGroup group, bool visible)
    {
        if (group == null)
            return;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
