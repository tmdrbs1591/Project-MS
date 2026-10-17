using UnityEngine;

/// <summary>
/// 게임 설정 저장소. 값은 PlayerPrefs에 저장되고, 게임 시작 시 한 번 자동으로 적용된다.
/// 볼륨은 이미 SoundManager가 자체적으로 저장/적용하므로 여기서는 화면/프레임/흔들림만 다룬다.
/// SettingsPanel(UI)이 이 값을 읽고 바꾼다.
/// </summary>
public static class GameSettings
{
    private const string FullscreenKey = "Settings.Fullscreen";
    private const string ResolutionWidthKey = "Settings.ResolutionWidth";
    private const string ResolutionHeightKey = "Settings.ResolutionHeight";
    private const string VSyncKey = "Settings.VSync";
    private const string FrameLimitKey = "Settings.FrameLimit";
    private const string ScreenShakeKey = "Settings.ScreenShake";

    /// <summary>프레임 제한 선택지. 0 = 제한 없음.</summary>
    public static readonly int[] FrameLimitOptions = { 30, 60, 120, 144, 240, 0 };

    /// <summary>화면 흔들림 배율(0 = 끔, 1 = 기본). TwoPlayerCamera.Shake가 곱해서 쓴다.</summary>
    public static float ScreenShakeScale { get; private set; } = 1f;

    public static bool Fullscreen => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
    public static bool VSync => PlayerPrefs.GetInt(VSyncKey, 1) == 1;
    public static int FrameLimit => PlayerPrefs.GetInt(FrameLimitKey, 60);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedOnStartup()
    {
        ScreenShakeScale = PlayerPrefs.GetFloat(ScreenShakeKey, 1f);
        ApplyFrameSettings();

        // 해상도는 저장된 값이 있을 때만 적용한다(처음 실행 때는 OS/플레이어 기본값을 존중).
        int width = PlayerPrefs.GetInt(ResolutionWidthKey, 0);
        int height = PlayerPrefs.GetInt(ResolutionHeightKey, 0);
        if (width > 0 && height > 0)
            Screen.SetResolution(width, height, Fullscreen);
        else if (PlayerPrefs.HasKey(FullscreenKey))
            Screen.fullScreen = Fullscreen;
    }

    public static void SetFullscreen(bool fullscreen)
    {
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
        Screen.fullScreen = fullscreen;
    }

    public static void SetResolution(int width, int height)
    {
        PlayerPrefs.SetInt(ResolutionWidthKey, width);
        PlayerPrefs.SetInt(ResolutionHeightKey, height);
        PlayerPrefs.Save();
        Screen.SetResolution(width, height, Fullscreen);
    }

    public static void SetVSync(bool enabled)
    {
        PlayerPrefs.SetInt(VSyncKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyFrameSettings();
    }

    public static void SetFrameLimit(int fps)
    {
        PlayerPrefs.SetInt(FrameLimitKey, fps);
        PlayerPrefs.Save();
        ApplyFrameSettings();
    }

    public static void SetScreenShakeScale(float scale)
    {
        ScreenShakeScale = Mathf.Clamp01(scale);
        PlayerPrefs.SetFloat(ScreenShakeKey, ScreenShakeScale);
        PlayerPrefs.Save();
    }

    // VSync가 켜져 있으면 프레임 제한은 모니터 주사율을 따르므로 무시된다(Unity 규칙).
    private static void ApplyFrameSettings()
    {
        QualitySettings.vSyncCount = VSync ? 1 : 0;
        Application.targetFrameRate = VSync || FrameLimit <= 0 ? -1 : FrameLimit;
    }
}
