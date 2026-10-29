using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 모드를 시작해서 씬이 바뀔 때 띄우는 PC 게임식 로딩 화면.
///
///   1) 들어가기: 화면이 부드럽게 어두워지고, 로딩 화면이 페이드인한다. 오른쪽엔 지금 고른 캐릭터가
///      크게(뒤에 모드 색 빛) 서서 둥실거리고, 왼쪽 아래엔 모드 이름/상태 문구, 맨 아래엔 진행 바와 팁.
///   2) 기다리기: 다음 씬이 열릴 때까지 띄워 둔다(씬이 바뀐 걸 감지해서 자동으로 넘어간다).
///      진행 바는 기다리는 동안 천천히 차다가, 씬이 열리면 끝까지 채운다.
///   3) 나가기: 로딩 화면이 페이드아웃되며 새 화면이 드러난다.
/// 씬이 끝내 안 바뀌면(접속 실패 등) maxWait초 뒤에 그냥 걷어낸다.
///
/// [씬 설정]
///   - 첫 씬(로비)의 빈 오브젝트에 붙이고 폰트/스프라이트/사운드를 넣어둔다. UI는 Awake에서
///     코드로 만들고, DontDestroyOnLoad로 씬이 바뀌어도 살아남는다(로비로 돌아와 생기는 중복은 지운다).
///   - 다른 스크립트는 SceneTransition.Instance?.Play(제목, 문구, 색)만 부르면 된다.
///   - 캐릭터 그림은 Play를 부르는 순간 그 씬의 캐릭터 선택 카드(CharacterSelectCard)에서 지금 고른
///     캐릭터를 복사해 온다. 캐릭터 선택 UI가 없는 씬(게임 씬 → 로비 등)에서는 캐릭터 없이 띄운다.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [SerializeField] private TMP_FontAsset font;
    [Tooltip("캐릭터 뒤 큰 영문 이름 워터마크에 쓸 폰트(만화풍 영문 폰트). 비우면 기본 폰트.")]
    [SerializeField] private TMP_FontAsset watermarkFont;
    [SerializeField] private Sprite solidSprite;
    [SerializeField] private Sprite glowSprite;
    [SerializeField] private Sprite gradientSprite;
    [SerializeField] private Sprite vignetteSprite;
    [Tooltip("배경에 떠다니는 반짝이.")]
    [SerializeField] private Sprite sparkleSprite;
    [Tooltip("배경을 비스듬히 가르는 빛줄기(세로로 긴 부드러운 띠).")]
    [SerializeField] private Sprite shineSprite;
    [Tooltip("캐릭터 이름 워터마크/이름표에 쓸 외곽선 폰트 머티리얼(비우면 기본).")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private AudioClip enterSound;
    [SerializeField] private AudioClip exitSound;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.6f;

    [Header("팁")]
    [SerializeField] private string[] tips =
    {
        "스킬은 Q / E / R 로 쓰고, Shift 로 대시합니다.",
        "궁극기 게이지는 피해를 주거나 받을 때 차오릅니다.",
        "증강은 라운드가 끝날 때마다 하나씩 고릅니다.",
        "같은 증강을 여러 번 고르면 효과가 겹쳐집니다.",
        "로비의 캐릭터 건물에서 언제든 캐릭터를 바꿀 수 있습니다.",
    };

    [Header("타이밍")]
    [Min(0.05f)] [SerializeField] private float fadeInDuration = 0.45f;
    [Min(0.05f)] [SerializeField] private float fadeOutDuration = 0.55f;
    [Tooltip("최소한 이만큼은 보여준다(너무 빨리 사라지면 깜빡이는 것처럼 보인다).")]
    [Min(0f)] [SerializeField] private float minHold = 1.2f;
    [Tooltip("씬이 바뀐 뒤 캐릭터가 나타날 때까지 조금 더 기다린다.")]
    [Min(0f)] [SerializeField] private float afterLoadDelay = 0.4f;
    [Min(1f)] [SerializeField] private float maxWait = 20f;

    public bool IsBusy { get; private set; }

    private CanvasGroup rootGroup;
    private Image accentGlow;
    private RectTransform characterSlot;
    private CanvasGroup characterGroup;
    private RectTransform info;
    private CanvasGroup infoGroup;
    private TMP_Text modeLabel;
    private TMP_Text titleText;
    private TMP_Text subtitleText;
    private TMP_Text tipText;
    private Image progressFill;
    private Image progressShine;
    private Image titleAccent;
    private string subtitleBase;
    private Coroutine routine;
    private bool sceneChanged;
    private float progress;
    private GameObject characterCopy;

    // 로비에서 복사해 보관해 둔 캐릭터(게임 씬에서 로비로 돌아갈 때도 보여주려고)
    private GameObject characterTemplate;
    private Vector2 templatePosition;
    private Vector3 templateScale = Vector3.one;
    private string cachedName = string.Empty;
    private string cachedEnglishName = string.Empty;

    // 배경 꾸밈
    private TMP_Text watermarkText;
    private TMP_Text charLabelText;
    private TMP_Text charNameText;
    private Image[] rays;
    private RectTransform[] particles;
    private Image[] particleImages;
    private float[] particleSpeed;
    private float[] particlePhase;
    private const int ParticleCount = 28;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
        SetHidden();
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }
    }

    /// <summary>로딩 화면을 띄우고, 다음 씬이 열리면 자동으로 걷어낸다.</summary>
    public void Play(string title, string subtitle, Color accent)
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(PlayRoutine(title, subtitle, accent));
    }

    private void OnActiveSceneChanged(Scene from, Scene to)
    {
        sceneChanged = true;
    }

    private void Update()
    {
        if (!IsBusy)
            return;

        float now = Time.unscaledTime;
        if (characterSlot != null)
            characterSlot.anchoredPosition = new Vector2(0f, Mathf.Sin(now * 0.9f) * 14f);
        if (watermarkText != null)
            watermarkText.rectTransform.anchoredPosition = new Vector2(-560f + Mathf.Sin(now * 0.25f) * 40f, 330f);
        AnimateBackground(now);
        if (subtitleText != null && !string.IsNullOrEmpty(subtitleBase))
            subtitleText.text = subtitleBase + new string('.', Mathf.FloorToInt(now * 2.5f) % 4);
        if (progressShine != null)
        {
            // 진행 바 위를 은은한 빛이 계속 훑고 지나간다(멈춰 보이지 않게).
            RectTransform bar = (RectTransform)progressShine.transform.parent;
            float w = bar.rect.width;
            float x = Mathf.Repeat(now * 0.6f, 1.3f) * (w + 200f) - 200f;
            progressShine.rectTransform.anchoredPosition = new Vector2(x, 0f);
        }
        if (progressFill != null)
            progressFill.fillAmount = progress;
    }

    private IEnumerator PlayRoutine(string title, string subtitle, Color accent)
    {
        IsBusy = true;
        sceneChanged = false;
        progress = 0f;
        rootGroup.blocksRaycasts = true;

        modeLabel.color = accent;
        titleText.text = title;
        subtitleBase = subtitle;
        subtitleText.text = subtitle;
        tipText.text = tips != null && tips.Length > 0 ? "TIP   " + tips[Random.Range(0, tips.Length)] : string.Empty;
        Color glow = accent;
        glow.a = 0.4f;
        accentGlow.color = glow;
        progressFill.color = accent;
        titleAccent.color = accent;
        charLabelText.color = accent;
        TintBackground(accent);
        CopySelectedCharacter();

        SoundManager.Instance?.PlaySfx(enterSound, sfxVolume);

        // 1) 화면이 어두워지며 로딩 화면이 드러나고, 내용이 양옆에서 살며시 들어온다
        float elapsed = 0f;
        float total = fadeInDuration + 0.5f;
        while (elapsed < total)
        {
            elapsed += Time.unscaledDeltaTime;
            rootGroup.alpha = EaseOutCubic(Mathf.Clamp01(elapsed / fadeInDuration));
            ApplyContent(Mathf.Clamp01((elapsed - fadeInDuration * 0.5f) / 0.55f));
            yield return null;
        }
        rootGroup.alpha = 1f;
        ApplyContent(1f);

        // 2) 씬이 바뀔 때까지(최소 minHold) 기다린다. 진행 바는 천천히 90%까지 찬다.
        float held = 0f;
        while ((held < minHold || !sceneChanged) && held < maxWait)
        {
            held += Time.unscaledDeltaTime;
            progress = Mathf.Lerp(progress, 0.9f, 1f - Mathf.Exp(-0.8f * Time.unscaledDeltaTime));
            yield return null;
        }

        // 씬이 열렸으면 진행 바를 끝까지 채우고 잠깐 기다린다
        float wait = 0f;
        float startProgress = progress;
        while (wait < afterLoadDelay)
        {
            wait += Time.unscaledDeltaTime;
            progress = Mathf.Lerp(startProgress, 1f, EaseOutCubic(Mathf.Clamp01(wait / afterLoadDelay)));
            yield return null;
        }
        progress = 1f;

        // 3) 로딩 화면이 사라지며 새 화면이 드러난다
        SoundManager.Instance?.PlaySfx(exitSound, sfxVolume);
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            rootGroup.alpha = 1f - t * t;
            yield return null;
        }

        SetHidden();
        routine = null;
    }

    /// <summary>고른 캐릭터 그림을 로딩 화면에 띄운다. 캐릭터 선택 카드가 있는 씬(로비)에서는 카드에서 새로
    /// 복사해 보관해 두고, 카드가 없는 씬(게임 → 로비로 돌아갈 때)에서는 보관해 둔 것을 쓴다.</summary>
    private void CopySelectedCharacter()
    {
        if (characterCopy != null)
            Destroy(characterCopy);
        characterCopy = null;

        RefreshCharacterTemplate();

        bool hasCharacter = characterTemplate != null;
        watermarkText.text = hasCharacter ? cachedEnglishName : string.Empty;
        charNameText.text = hasCharacter ? cachedName : string.Empty;
        charLabelText.gameObject.SetActive(hasCharacter && !string.IsNullOrEmpty(cachedName));
        if (!hasCharacter)
            return;

        characterCopy = Instantiate(characterTemplate, characterSlot);
        characterCopy.SetActive(true);
        RectTransform copy = (RectTransform)characterCopy.transform;
        // 카드 안에서는 몸 높이 182px 기준으로 맞춰져 있다 — 로딩 화면에선 약 3.3배로 화면을 채울 만큼 크게.
        const float scale = 3.3f;
        copy.anchorMin = copy.anchorMax = new Vector2(0.5f, 0.5f);
        copy.anchoredPosition = (templatePosition - new Vector2(-14f, 14f)) * scale + new Vector2(0f, -60f);
        copy.localScale = templateScale * scale;
    }

    /// <summary>지금 씬에 캐릭터 선택 카드가 있으면, 고른 캐릭터의 그림/이름을 새로 보관한다.</summary>
    private void RefreshCharacterTemplate()
    {
        int selected = PlayerPrefs.GetInt(LobbyCharacterChange.SelectedCharacterIndexKey, 0);
        foreach (CharacterSelectCard card in FindObjectsByType<CharacterSelectCard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            CharacterChangeUi change = card.GetComponent<CharacterChangeUi>();
            if (change == null || change.CharacterIndex != selected)
                continue;

            Transform holder = card.transform.Find("Visual/Body/Character");
            if (holder == null)
                return;

            if (characterTemplate != null)
                Destroy(characterTemplate);

            // 이 오브젝트(DontDestroyOnLoad) 밑에 꺼둔 채로 보관해서 씬이 바뀌어도 남게 한다.
            characterTemplate = Instantiate(holder.gameObject, transform);
            characterTemplate.name = "Character Template";
            characterTemplate.SetActive(false);
            foreach (Graphic g in characterTemplate.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;

            RectTransform src = (RectTransform)holder;
            templatePosition = src.anchoredPosition;
            templateScale = src.localScale;

            // 카드에 적힌 이름을 그대로 가져와 이름표와 큰 영문 워터마크로 쓴다.
            Transform koName = card.transform.Find("Visual/Body/Name");
            Transform enName = card.transform.Find("Visual/Body/English Name");
            cachedName = koName != null ? koName.GetComponent<TMP_Text>().text : string.Empty;
            cachedEnglishName = enName != null ? enName.GetComponent<TMP_Text>().text : string.Empty;
            return;
        }
    }

    // ── UI 만들기 ────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        rootGroup = gameObject.AddComponent<CanvasGroup>();

        // 바탕: 짙은 남색 + 오른쪽에 모드 색 빛 + 가장자리 어둠
        Image bg = NewImage("Background", transform, solidSprite, new Color(0.03f, 0.04f, 0.09f, 1f));
        Stretch(bg.rectTransform);
        bg.raycastTarget = true;
        accentGlow = NewImage("Accent Glow", transform, glowSprite, Color.white);
        Place(accentGlow.rectTransform, new Vector2(1f, 0.5f), new Vector2(-580f, 40f), new Vector2(1300f, 1300f));

        // 비스듬히 가르는 빛줄기 몇 개(천천히 흔들린다)
        rays = new Image[3];
        for (int i = 0; i < rays.Length; i++)
        {
            rays[i] = NewImage("Ray " + i, transform, shineSprite, Color.white);
            Place(rays[i].rectTransform, new Vector2(1f, 0.5f), new Vector2(-900f + i * 260f, 0f), new Vector2(150f + i * 40f, 1700f));
            rays[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, 22f);
        }

        // 떠다니는 반짝이
        RectTransform particleRoot = NewRect("Particles", transform);
        Stretch(particleRoot);
        particles = new RectTransform[ParticleCount];
        particleImages = new Image[ParticleCount];
        particleSpeed = new float[ParticleCount];
        particlePhase = new float[ParticleCount];
        for (int i = 0; i < ParticleCount; i++)
        {
            particleImages[i] = NewImage("Particle", particleRoot, sparkleSprite, Color.white);
            particles[i] = particleImages[i].rectTransform;
            float size = Random.Range(8f, 26f);
            particles[i].sizeDelta = new Vector2(size, size);
            particles[i].anchoredPosition = new Vector2(Random.Range(-960f, 960f), Random.Range(-540f, 540f));
            particleSpeed[i] = Random.Range(20f, 60f);
            particlePhase[i] = Random.Range(0f, Mathf.PI * 2f);
        }

        // 캐릭터 뒤 큰 영문 이름 워터마크
        watermarkText = NewText("Watermark", transform, "", 300f, Vector2.zero, new Vector2(1800f, 340f));
        watermarkText.rectTransform.anchorMin = watermarkText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        watermarkText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        watermarkText.alignment = TextAlignmentOptions.Center;
        if (watermarkFont != null)
            watermarkText.font = watermarkFont;
        watermarkText.characterSpacing = 6f;
        watermarkText.color = new Color(1f, 1f, 1f, 0.09f);
        // 폰트에 기울임꼴이 따로 없어서 TMP가 글자를 비스듬히 눕혀 그린다(기울기 정도는 폰트 에셋의 Italic Style).
        watermarkText.fontStyle = FontStyles.Italic;

        Image bottomShade = NewImage("Bottom Shade", transform, gradientSprite, new Color(0f, 0f, 0f, 0.75f));
        bottomShade.rectTransform.anchorMin = new Vector2(0f, 0f);
        bottomShade.rectTransform.anchorMax = new Vector2(1f, 0f);
        bottomShade.rectTransform.pivot = new Vector2(0.5f, 0f);
        bottomShade.rectTransform.sizeDelta = new Vector2(0f, 420f);
        Image vignette = NewImage("Vignette", transform, vignetteSprite, new Color(0f, 0f, 0f, 0.55f));
        Stretch(vignette.rectTransform);

        // 오른쪽 캐릭터
        RectTransform charArea = NewRect("Character Area", transform);
        Place(charArea, new Vector2(1f, 0.5f), new Vector2(-620f, 90f), new Vector2(10f, 10f));
        characterGroup = charArea.gameObject.AddComponent<CanvasGroup>();
        characterSlot = NewRect("Character Slot", charArea);
        characterSlot.sizeDelta = Vector2.zero;

        // 캐릭터 아래 이름표
        charLabelText = NewText("Character Label", charArea, "SELECTED", 18f, Vector2.zero, new Vector2(500f, 26f));
        charLabelText.rectTransform.anchorMin = charLabelText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        charLabelText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        charLabelText.rectTransform.anchoredPosition = new Vector2(0f, -365f);
        charLabelText.alignment = TextAlignmentOptions.Center;
        charLabelText.characterSpacing = 16f;
        charNameText = NewText("Character Name", charArea, "", 56f, Vector2.zero, new Vector2(600f, 70f));
        charNameText.rectTransform.anchorMin = charNameText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        charNameText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        charNameText.rectTransform.anchoredPosition = new Vector2(0f, -412f);
        charNameText.alignment = TextAlignmentOptions.Center;
        if (outlineMaterial != null)
            charNameText.fontSharedMaterial = outlineMaterial;

        // 왼쪽 아래 모드 정보
        info = NewRect("Info", transform);
        Place(info, new Vector2(0f, 0f), new Vector2(140f, 250f), new Vector2(900f, 260f));
        info.pivot = new Vector2(0f, 0f);
        infoGroup = info.gameObject.AddComponent<CanvasGroup>();
        modeLabel = NewText("Mode Label", info, "MODE", 20f, new Vector2(0f, 190f), new Vector2(600f, 30f));
        modeLabel.characterSpacing = 18f;
        titleText = NewText("Title", info, "", 86f, new Vector2(0f, 90f), new Vector2(1000f, 110f));
        titleAccent = NewImage("Title Accent", info, solidSprite, Color.white);
        titleAccent.rectTransform.anchorMin = titleAccent.rectTransform.anchorMax = Vector2.zero;
        titleAccent.rectTransform.pivot = Vector2.zero;
        titleAccent.rectTransform.anchoredPosition = new Vector2(0f, 74f);
        titleAccent.rectTransform.sizeDelta = new Vector2(90f, 4f);
        subtitleText = NewText("Subtitle", info, "", 26f, new Vector2(0f, 26f), new Vector2(800f, 40f));
        subtitleText.color = new Color(0.78f, 0.84f, 0.96f, 1f);

        // 맨 아래 진행 바 + 팁
        RectTransform bar = NewRect("Progress", transform);
        bar.anchorMin = new Vector2(0f, 0f);
        bar.anchorMax = new Vector2(1f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.anchoredPosition = new Vector2(0f, 150f);
        bar.sizeDelta = new Vector2(-280f, 4f);
        Image barBg = bar.gameObject.AddComponent<Image>();
        barBg.sprite = solidSprite;
        barBg.color = new Color(1f, 1f, 1f, 0.12f);
        barBg.raycastTarget = false;
        bar.gameObject.AddComponent<RectMask2D>();
        progressFill = NewImage("Fill", bar, solidSprite, Color.white);
        Stretch(progressFill.rectTransform);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressShine = NewImage("Shine", bar, glowSprite, new Color(1f, 1f, 1f, 0.6f));
        progressShine.rectTransform.anchorMin = progressShine.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        progressShine.rectTransform.sizeDelta = new Vector2(200f, 14f);

        tipText = NewText("Tip", transform, "", 20f, Vector2.zero, new Vector2(1400f, 30f));
        Place(tipText.rectTransform, new Vector2(0f, 0f), new Vector2(140f, 92f), new Vector2(1400f, 30f));
        tipText.rectTransform.pivot = new Vector2(0f, 0f);
        tipText.color = new Color(0.62f, 0.68f, 0.82f, 1f);
    }

    private void TintBackground(Color accent)
    {
        for (int i = 0; i < rays.Length; i++)
        {
            Color c = accent;
            c.a = 0.07f + i * 0.025f;
            rays[i].color = c;
        }
        for (int i = 0; i < particleImages.Length; i++)
        {
            Color c = i % 3 == 0 ? Color.white : accent;
            c.a = 0f;
            particleImages[i].color = c;
        }
    }

    /// <summary>빛줄기는 천천히 흔들리고, 반짝이는 위로 떠오르며 깜빡인다.</summary>
    private void AnimateBackground(float now)
    {
        for (int i = 0; i < rays.Length; i++)
            rays[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, 22f + Mathf.Sin(now * 0.4f + i * 1.7f) * 3f);

        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < particles.Length; i++)
        {
            Vector2 p = particles[i].anchoredPosition;
            p.y += particleSpeed[i] * dt;
            p.x += Mathf.Sin(now * 0.7f + particlePhase[i]) * 10f * dt;
            if (p.y > 560f)
                p = new Vector2(Random.Range(-960f, 960f), -560f);
            particles[i].anchoredPosition = p;

            Color c = particleImages[i].color;
            c.a = 0.55f * (0.4f + 0.6f * Mathf.Abs(Mathf.Sin(now * 2f + particlePhase[i])));
            particleImages[i].color = c;
        }
    }

    /// <summary>모드 정보는 왼쪽에서, 캐릭터는 오른쪽에서 살며시 밀려 들어온다.</summary>
    private void ApplyContent(float t)
    {
        float e = EaseOutCubic(t);
        infoGroup.alpha = e;
        info.anchoredPosition = new Vector2(140f - (1f - e) * 60f, 250f);
        characterGroup.alpha = e;
        RectTransform charArea = (RectTransform)characterGroup.transform;
        charArea.anchoredPosition = new Vector2(-620f + (1f - e) * 80f, 90f);
        float s = Mathf.Lerp(1.06f, 1f, e);
        charArea.localScale = new Vector3(s, s, 1f);
        titleAccent.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(0f, 90f, e), 4f);
    }

    private void SetHidden()
    {
        IsBusy = false;
        rootGroup.alpha = 0f;
        rootGroup.blocksRaycasts = false;
        progress = 0f;
        if (characterCopy != null)
            Destroy(characterCopy);
        characterCopy = null;
    }

    private RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = gameObject.layer;
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private Image NewImage(string name, Transform parent, Sprite sprite, Color color)
    {
        RectTransform rt = NewRect(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private TMP_Text NewText(string name, Transform parent, string text, float size, Vector2 pos, Vector2 box)
    {
        RectTransform rt = NewRect(name, parent);
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.BottomLeft;
        t.color = Color.white;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }

    private static float EaseOutCubic(float t)
    {
        float x = 1f - t;
        return 1f - x * x * x;
    }
}
