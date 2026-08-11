using UnityEngine;

/// <summary>
/// 전역 사운드 재생 매니저(효과음 + 배경음). 클립이 안 물려있으면 조용히 아무 일도
/// 안 하므로, 실제 오디오 에셋이 아직 없어도 호출부는 안전하다.
///
/// [SFX 풀]
///   - 같은 프레임에 여러 소리가 겹쳐도 서로 pitch를 안 덮어쓰게 AudioSource를 여러 개
///     라운드로빈으로 돌려쓴다(하나만 쓰면 겹치는 원샷끼리 pitch를 공유해서 랜덤화가 꼬임).
///   - 인스펙터에 sfxSources를 안 채워놓으면 Awake에서 자식 오브젝트를 만들어 자동 채운다
///     (CharacterVisualController.Awake()가 옵션 참조를 자동 보정하는 것과 같은 방식).
///
/// [볼륨]
///   - Master/Sfx/Music 세 값을 곱해서 최종 볼륨을 낸다. PlayerPrefs에 저장되어 앱을
///     재시작해도 유지된다(AugmentPackManager가 PlayerPrefs로 해금 여부를 저장하는 것과 동일 패턴).
///   - 지금은 이 값을 바꿀 설정 UI가 없지만, 나중에 설정 메뉴를 붙일 때 바로 연결되도록
///     API를 먼저 마련해둔 것이다.
///
/// [씬 설정]
///   - 로비 씬(최초 진입 씬)에 빈 오브젝트를 만들고 이 스크립트만 붙이면 된다. 나머지(소스 풀,
///     음악 소스)는 비어있으면 자동 생성된다.
///   - NetworkLauncher와 같은 패턴으로 DontDestroyOnLoad + 중복 인스턴스 가드를 쓴다 —
///     씬 전환(로비 ↔ 게임)에도 하나만 살아남는다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private const string MasterVolumeKey = "Sound_MasterVolume";
    private const string SfxVolumeKey = "Sound_SfxVolume";
    private const string MusicVolumeKey = "Sound_MusicVolume";

    [Header("SFX 풀")]
    [Tooltip("동시에 겹쳐 재생될 수 있는 최대 효과음 수. 비워두면 poolSize만큼 자동 생성된다.")]
    [SerializeField] private AudioSource[] sfxSources;
    [Min(1)] [SerializeField] private int poolSize = 6;

    [Header("배경음")]
    [Tooltip("비워두면 자동 생성된다.")]
    [SerializeField] private AudioSource musicSource;

    private int nextSfxIndex;

    public float MasterVolume { get; private set; } = 1f;
    public float SfxVolume { get; private set; } = 1f;
    public float MusicVolume { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureSfxPool();
        EnsureMusicSource();
        LoadVolumes();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>효과음 하나를 재생한다. clip이 없으면 아무 일도 안 한다.</summary>
    /// <param name="volumeScale">이 소리 고유의 볼륨 배율(0~1). 최종 볼륨 = volumeScale * SfxVolume * MasterVolume.</param>
    /// <param name="pitchVariance">0보다 크면 1±pitchVariance 범위에서 피치를 무작위로 살짝 바꿔
    /// 같은 소리가 반복돼도 기계적으로 안 들리게 한다.</param>
    public void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitchVariance = 0f)
    {
        if (clip == null || sfxSources == null || sfxSources.Length == 0)
            return;

        AudioSource source = sfxSources[nextSfxIndex];
        nextSfxIndex = (nextSfxIndex + 1) % sfxSources.Length;

        if (source == null)
            return;

        source.pitch = pitchVariance > 0f ? 1f + Random.Range(-pitchVariance, pitchVariance) : 1f;
        source.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * SfxVolume * MasterVolume);
    }

    /// <summary>배경음을 재생한다(같은 클립이 이미 재생 중이면 무시).</summary>
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null)
            return;

        EnsureMusicSource();

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = MusicVolume * MasterVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
        ApplyMusicVolume();
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        ApplyMusicVolume();
    }

    private void LoadVolumes()
    {
        MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        ApplyMusicVolume();
    }

    private void ApplyMusicVolume()
    {
        if (musicSource != null)
            musicSource.volume = MusicVolume * MasterVolume;
    }

    private void EnsureSfxPool()
    {
        if (sfxSources != null && sfxSources.Length > 0)
            return;

        sfxSources = new AudioSource[Mathf.Max(1, poolSize)];
        for (int i = 0; i < sfxSources.Length; i++)
        {
            GameObject child = new GameObject($"SfxSource_{i}");
            child.transform.SetParent(transform, false);

            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            sfxSources[i] = source;
        }
    }

    private void EnsureMusicSource()
    {
        if (musicSource != null)
            return;

        GameObject child = new GameObject("MusicSource");
        child.transform.SetParent(transform, false);

        musicSource = child.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
    }
}
