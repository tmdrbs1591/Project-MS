using System.Collections.Generic;
using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
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

    private CanvasGroup canvasGroup;
    private bool isOpen;
    private bool lockedByMe;
    private readonly List<Vector2Int> resolutions = new List<Vector2Int>();

    public bool IsOpen => isOpen;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

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

        if (isOpen)
            Close();
        else if (!CharacterBase.IsLobbyControlLocked)
            Open();
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
        ReleaseLock();
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

    private void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
