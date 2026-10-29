using System.Collections;
using System.Collections.Generic;
using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 진입 시 표시되는 "VS" 연출. 좌(Player1)/우(Player2) 캐릭터 이름·그림을 채우고,
/// 연출이 나오는 동안 양쪽 캐릭터 조작을 전부 막는다.
///
/// [흐름] (시간은 연출 시작 기준, 모두 실제 시간)
///   0.00  대각선 빛줄기 두 줄이 화면을 "촥" 가르며 지나간다.
///   p1Enter / p2Enter  양쪽 캐릭터가 자기 쪽 화면 밖에서 돌진해 들어와 "쿵" 착지(화면 흔들림 + 빛 터짐).
///   namesTime  이름들이 아래에서 차례로 떠오른다.
///   mapTime    맵 액자가 위에서 툭 떨어지며 들어오고, 슬롯머신이 돌기 시작한다.
///   (맵 결정)  액자가 한 번 튀고 테두리 잔상이 퍼지며, 두 캐릭터가 "준비" 하듯 통 튄다.
///   flashDelay 이후(맵 결정 + 여유 시간이 더 늦으면 그때)
///              캐릭터가 서로를 향해 살짝 돌진 → 하얀 플래시와 함께 VS 화면이 확 커지며 사라지고
///              "FIGHT!"가 크게 박힌다. 박히는 순간 조작이 풀린다.
///
/// [트리거]
///   - 이 스크립트가 붙은 오브젝트(=VS 연출 루트)가 SetActive(true) 되는 순간(OnEnable)에
///     시작된다. BattleStartVfxController가 켜는 Vfx_UI_BattleStart_Vs에 붙인다.
///   - 각 클라가 자기 화면에서 독립적으로 재생한다(동기화 불필요). 조작 잠금(CharacterBase.SetGameplayLocked)은
///     StateAuthority가 있는 캐릭터에만 실제로 적용되므로, 각 클라는 결과적으로 자기 캐릭터만 잠근다.
///
/// [씬 설정]
///   - player1Text / player2Text: 좌/우 캐릭터 이름(CharacterDefinition.DisplayName).
///   - player1Root / player2Root: 캐릭터 그림과 이름을 묶은 부모. 이게 화면 밖에서 돌진해 들어온다.
///   - mapFrameParts: 맵 액자를 이루는 UI들(마스크 창, 테두리). 같이 떨어지고 같이 튄다.
///   - slotMachine: 맵 슬롯머신. Spin On Enable은 끄고 이 스크립트가 때맞춰 돌린다.
///   - flashObject: 마지막에 켜는 플래시 파티클. loopingParticle: 배경 파티클(마지막에 지운다).
/// </summary>
[DefaultExecutionOrder(1001)] // 카메라 줌에 맞춰 루트를 키우는 ScaleWithCameraZoom(1000) 다음에 캔버스 크기를 되돌린다.
public class VsIntroUI : MonoBehaviour
{
    [Header("VS 텍스트")]
    [SerializeField] private TMP_Text player1Text;
    [SerializeField] private TMP_Text player2Text;

    [Header("캐릭터 스프라이트")]
    [SerializeField] private Image player1Image;
    [SerializeField] private Image player1Shadow;
    [SerializeField] private Image player2Image;
    [SerializeField] private Image player2Shadow;

    [Header("스프라이트 크기 조절")]
    [Min(0f)][SerializeField] private float characterImageSizeMultiplier = 1.3684f;

    [System.Serializable]
    private class Portrait
    {
        public CharacterDefinition definition;
        [Tooltip("캐릭터 선택 카드와 같은 그림(팔/총 포함) + 캐릭터별 움직임(CharacterCardAnimator, Always On).")]
        public GameObject prefab;
        [Tooltip("착지할 때 같이 나는 캐릭터 선택 소리.")]
        public AudioClip pickSound;
    }

    [Header("캐릭터 그림 (선택 카드와 같은 움직이는 그림)")]
    [Tooltip("캐릭터마다 쓸 그림. 없는 캐릭터는 위의 한 장짜리 스프라이트를 그대로 쓴다.")]
    [SerializeField] private Portrait[] portraits;
    [SerializeField] private float portraitScale = 1.3f;
    [SerializeField] private Vector2 portraitOffset = Vector2.zero;
    [Tooltip("캐릭터와 같이 움직이는 진한 그림자(원래 VS 연출의 검은 그림자) 색.")]
    [SerializeField] private Color portraitShadowColor = new Color(0f, 0f, 0f, 1f);
    [Tooltip("그림자가 캐릭터 바깥쪽으로 비켜나는 거리(화면 픽셀).")]
    [SerializeField] private float portraitShadowOffset = 29f;

    [Header("연출 대상")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform player1Root;
    [SerializeField] private RectTransform player2Root;
    [Tooltip("캐릭터 아래 이름 글자들(위에서부터). 착지 뒤 차례로 떠오른다.")]
    [SerializeField] private RectTransform[] player1Names;
    [SerializeField] private RectTransform[] player2Names;
    [SerializeField] private RectTransform[] mapFrameParts;
    [Tooltip("맵 결정 때 테두리 잔상으로 복제해 퍼뜨릴 액자 테두리 이미지.")]
    [SerializeField] private Image mapFrameBorder;
    [SerializeField] private MapSlotMachine slotMachine;

    [Header("타이밍 (초)")]
    [SerializeField] private float p1Enter = 0.12f;
    [SerializeField] private float p2Enter = 0.55f;
    [SerializeField] private float enterDuration = 0.13f;
    [SerializeField] private float namesTime = 0.95f;
    [SerializeField] private float mapTime = 1.15f;
    [Tooltip("맵이 정해진 뒤 마무리 연출까지 최소 여유.")]
    [SerializeField] private float afterResultHold = 1.0f;

    [Header("플래시 연출")]
    [Tooltip("연출이 시작되고 플래시가 터지기까지 최소 대기 시간(초). 맵이 늦게 정해지면 그만큼 미뤄진다.")]
    [SerializeField] private float flashDelay = 4.8f;
    [SerializeField] private GameObject flashObject;
    [Tooltip("대기하는 동안 재생 중인 배경 파티클. 플래시가 켜지는 순간 지운다.")]
    [SerializeField] private ParticleSystem loopingParticle;

    [Header("FIGHT")]
    [SerializeField] private string fightText = "FIGHT!";
    [SerializeField] private TMP_FontAsset fightFont;
    [SerializeField] private Material fightMaterial;
    [SerializeField] private Color fightColor = new Color(1f, 0.92f, 0.55f, 1f);
    [SerializeField] private float fightSize = 190f;

    [Header("연출 재료")]
    [SerializeField] private Sprite softCircle;

    [Header("VS 로고 / 맵 연출")]
    [Tooltip("가운데 VS 글자(UI). 2P가 착지하는 순간 크게 날아와 박힌다.")]
    [SerializeField] private RectTransform vsLogo;
    [Tooltip("맵 액자 뒤 빛(9-슬라이스 부드러운 테두리 빛).")]
    [SerializeField] private Sprite frameGlowSprite;
    [Tooltip("맵이 정해지면 액자 뒤에 띄울 후광 이펙트 프리팹(파티클). 액자 가운데에 화면을 향해 사방으로 뻗게 놓는다.")]
    [SerializeField] private GameObject haloPrefab;
    [SerializeField] private float haloScale = 4f;
    [Tooltip("후광 진하기 배율(파티클 시작 알파에 곱한다).")]
    [SerializeField] private float haloIntensity = 3f;
    [Tooltip("후광 빛줄기 개수 배율(초당 생성 수에 곱한다).")]
    [SerializeField] private float haloDensity = 2.5f;
    [Tooltip("VS 글자에 붙은 전기 파티클들. 처음엔 꺼 두었다가 VS가 박히는 순간 켠다.")]
    [SerializeField] private ParticleSystem[] vsSparks;
    [Tooltip("액자 이미지 크기 중 실제로 보이는 테두리 비율(투명 여백 제외).")]
    [SerializeField] private Vector2 frameVisibleRatio = new Vector2(0.88f, 0.74f);
    [SerializeField] private Color spinGlowColor = new Color(0.45f, 0.85f, 1f, 1f);
    [SerializeField] private Color decideGlowColor = new Color(1f, 0.82f, 0.35f, 1f);

    [Header("화면 연출")]
    [SerializeField] private Color player1Color = new Color(0.3f, 0.72f, 1f, 1f);
    [SerializeField] private Color player2Color = new Color(1f, 0.36f, 0.42f, 1f);
    [Tooltip("캐릭터 뒤 거대 실루엣의 진하기.")]
    [Range(0f, 1f)] [SerializeField] private float silhouetteAlpha = 0.22f;
    [Tooltip("위아래 영화 같은 검은 띠 두께.")]
    [SerializeField] private float letterboxHeight = 70f;

    [Header("사운드")]
    [SerializeField] private AudioClip whooshClip;
    [SerializeField] private AudioClip impactClip;
    [SerializeField] private AudioClip dropClip;
    [SerializeField] private AudioClip boomClip;
    [Tooltip("붐 소리에서 '쾅' 터지는 순간까지의 시간(앞에 빨아들이는 소리가 붙어 있다).")]
    [SerializeField] private float boomLead = 1f;
    [SerializeField] private AudioClip fightClip;
    [SerializeField] private AudioClip slamClip;
    [Tooltip("맵이 도는 동안 깔리는 드럼롤.")]
    [SerializeField] private AudioClip rollClip;
    [Tooltip("맵이 정해지는 순간.")]
    [SerializeField] private AudioClip decideClip;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.8f;

    [Header("조작 잠금")]
    [Tooltip("플래시가 터진 뒤 조작이 풀리기까지 추가 대기 시간(초). FIGHT가 박히는 순간에 맞춘다.")]
    [SerializeField] private float controlUnlockDelay = 0.3f;

    private Coroutine introRoutine;
    private bool controlsUnlocked;
    private bool captured;

    private void LateUpdate()
    {
        if (canvasHolder == null || canvasHolder.parent == null)
            return;
        Vector3 parentScale = canvasHolder.parent.lossyScale;
        canvasHolder.localScale = new Vector3(
            1f / Mathf.Max(0.0001f, parentScale.x),
            1f / Mathf.Max(0.0001f, parentScale.y),
            1f / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z)));
    }
    private readonly List<RectTransform> stageParts = new List<RectTransform>();
    private readonly Dictionary<RectTransform, Vector2> basePos = new Dictionary<RectTransform, Vector2>();
    private readonly Dictionary<RectTransform, Vector3> baseScale = new Dictionary<RectTransform, Vector3>();
    private RectTransform fx;
    private Transform canvasHolder;
    private RectTransform backdrop;
    private readonly RectTransform[] letterbox = new RectTransform[2];
    private readonly RectTransform[] sides = new RectTransform[2];
    private readonly Image[] sideGlows = new Image[2];
    private readonly GameObject[] silhouettes = new GameObject[2];
    private readonly float[] sideShownAt = { -1f, -1f };

    private sealed class Ember
    {
        public RectTransform rt;
        public Image img;
        public Vector2 velocity;
        public float life;
        public float age;
    }

    private readonly List<Ember> embers = new List<Ember>();
    private Image frameGlow;
    private GameObject halo;
    private bool frameSpinning;
    private bool blowingOut;
    private float decidedAt = -1f;
    private float nextEmberAt;
    private CanvasGroup fightGroup;
    private TextMeshProUGUI fightLabel;

    private void OnEnable()
    {
        Capture();
        ResetVisuals();

        if (flashObject != null)
            flashObject.SetActive(false);

        controlsUnlocked = false;
        CharacterBase.IntroLocked = true;
        SetAllCharactersLocked(true);

        if (introRoutine != null)
            StopCoroutine(introRoutine);
        introRoutine = StartCoroutine(IntroSequence());
    }

    private void OnDisable()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        // 연출 도중에 오브젝트가 꺼져도 조작 불능 상태로 남지 않도록 방어적으로 풀어준다.
        CharacterBase.IntroLocked = false;
        SetAllCharactersLocked(false);
    }

    private void Update()
    {
        // 캐릭터 스폰/동기화 타이밍이 화면마다 다를 수 있어 매 프레임 폴링한다
        // (PlayerCornerHUD와 동일한 이유).
        UpdateCharacterProfile();
        if (captured)
        {
            UpdateBackdrop();
            UpdateFrameFx();
        }

        // 연출 도중에 늦게 등록된 캐릭터(AI 봇 등)도 잠금에서 빠지지 않도록, 잠금 구간 동안은 계속 건다.
        if (introRoutine != null && !controlsUnlocked)
            SetAllCharactersLocked(true);
    }

    private void UpdateCharacterProfile()
    {
        MatchManager match = MatchManager.Instance;

        foreach (CharacterBase character in CharacterBase.All)
        {
            if (character == null || character.Object == null)
                continue;

            bool isP1 = match != null && match.IsPlayer1(character.MatchPlayer);
            string displayName = character.Definition != null ? character.Definition.DisplayName : string.Empty;
            Sprite displaySprite = character.Definition != null ? character.Definition.CharacterMainSprite : null;

            TMP_Text playerText = isP1 ? player1Text : player2Text;
            Image playerImage = isP1 ? player1Image : player2Image;
            Image playerShadow = isP1 ? player1Shadow : player2Shadow;

            if (playerText != null)
                playerText.text = displayName;

            if (EnsurePortrait(isP1, character.Definition, playerImage, playerShadow))
                continue;

            if (playerImage != null && playerShadow != null)
            {
                if (displaySprite == null || displaySprite.pixelsPerUnit <= 0f)
                    return;

                playerImage.sprite = displaySprite;
                playerShadow.sprite = displaySprite;

                playerImage.SetNativeSize();
                playerImage.rectTransform.sizeDelta *= characterImageSizeMultiplier;

                playerShadow.rectTransform.sizeDelta = playerImage.rectTransform.sizeDelta;
            }
        }
    }

    private readonly GameObject[] portraitInstances = new GameObject[2];
    private readonly CharacterDefinition[] portraitDefinitions = new CharacterDefinition[2];
    private readonly AudioClip[] pickSounds = new AudioClip[2];

    /// <summary>이 캐릭터용 움직이는 그림이 있으면 한 장짜리 스프라이트 대신 띄운다. 띄웠으면 true.</summary>
    private bool EnsurePortrait(bool isP1, CharacterDefinition definition, Image image, Image shadow)
    {
        int side = isP1 ? 0 : 1;
        if (definition == null || image == null)
            return false;
        if (portraitDefinitions[side] == definition)
            return portraitInstances[side] != null;

        portraitDefinitions[side] = definition;
        if (portraitInstances[side] != null)
            Destroy(portraitInstances[side]);
        portraitInstances[side] = null;
        pickSounds[side] = null;

        Portrait entry = null;
        if (portraits != null)
            foreach (Portrait p in portraits)
                if (p != null && p.definition == definition)
                    entry = p;
        if (entry == null || entry.prefab == null)
        {
            image.enabled = true;
            if (shadow != null) shadow.enabled = true;
            return false;
        }

        GameObject go = Instantiate(entry.prefab, image.rectTransform.parent);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = image.rectTransform.anchoredPosition + portraitOffset;
        // 오른쪽 캐릭터는 원래 스프라이트처럼 좌우를 뒤집어 가운데를 보게 한다.
        float flip = Mathf.Sign(image.rectTransform.localScale.x);
        rt.localScale = new Vector3(portraitScale * flip, portraitScale, 1f);
        rt.SetSiblingIndex(image.rectTransform.GetSiblingIndex());
        image.enabled = false;
        if (shadow != null) shadow.enabled = false;
        ApplyPortraitShadow(go);
        portraitInstances[side] = go;
        pickSounds[side] = entry.pickSound;
        BuildSilhouette(side, entry.prefab, flip);
        return true;
    }

    /// <summary>그림의 그림자 묶음(몸·팔·총과 같이 움직임)을 원래 VS처럼 진한 검은색으로, 바깥쪽으로 비켜 놓는다.
    /// 오른쪽 캐릭터는 그림이 좌우로 뒤집혀 있어서 같은 값이 저절로 바깥쪽이 된다.</summary>
    private void ApplyPortraitShadow(GameObject portrait)
    {
        Transform character = portrait.transform.Find("Character");
        Transform shadowGroup = character != null ? character.Find("Shadow") : null;
        if (shadowGroup == null)
            return;
        float scale = Mathf.Abs(portrait.transform.localScale.x * character.localScale.x);
        ((RectTransform)shadowGroup).anchoredPosition = new Vector2(-portraitShadowOffset / Mathf.Max(0.01f, scale), 0f);
        foreach (Graphic g in shadowGroup.GetComponentsInChildren<Graphic>(true))
            g.color = portraitShadowColor;
    }

    private static void SetAllCharactersLocked(bool locked)
    {
        foreach (CharacterBase character in CharacterBase.All)
        {
            character?.SetGameplayLocked(locked);
        }
    }

    // ---------------- 준비 ----------------

    private void Capture()
    {
        if (captured || canvasGroup == null)
            return;
        captured = true;

        // Screen Space - Camera 캔버스는 부모 크기를 무시하고 자기 크기를 정하는데, 이 연출 루트는 카메라 줌에 맞춰
        // 커지므로(ScaleWithCameraZoom) 실제 대전(카메라가 줌아웃된 상태)에서는 UI까지 같이 커져 화면 밖으로 잘렸다.
        // 캔버스를 받침 오브젝트에 옮겨 담고, 받침이 부모 크기를 매 프레임 상쇄하게 한다(LateUpdate).
        Transform canvasT = canvasGroup.transform;
        canvasHolder = new GameObject("Canvas Holder").transform;
        canvasHolder.gameObject.layer = canvasT.gameObject.layer;
        canvasHolder.SetParent(canvasT.parent, false);
        canvasT.SetParent(canvasHolder, false);

        foreach (Transform child in canvasGroup.transform)
            stageParts.Add((RectTransform)child);
        foreach (RectTransform rt in stageParts)
            Remember(rt);
        Remember(player1Root);
        Remember(player2Root);
        foreach (RectTransform rt in player1Names) Remember(rt);
        foreach (RectTransform rt in player2Names) Remember(rt);
        foreach (RectTransform rt in mapFrameParts) Remember(rt);
        Remember(vsLogo);

        // 맵 액자 뒤: 정해지면 도는 빛줄기, 그 위에 액자 테두리 빛.
        if (mapFrameParts.Length > 0)
        {
            RectTransform frame = mapFrameParts[0];
            int index = frame.GetSiblingIndex();
            if (frameGlowSprite != null)
            {
                frameGlow = NewImage("Map Glow", canvasGroup.transform, frameGlowSprite, Color.clear);
                frameGlow.type = Image.Type.Sliced;
                frameGlow.rectTransform.anchoredPosition = basePos[frame];
                // 액자 이미지에는 투명 여백이 있어서, 실제로 보이는 테두리 크기(가로 88%, 세로 74%)에 맞춘다.
                frameGlow.rectTransform.sizeDelta = Vector2.Scale(Vector2.Scale(frame.sizeDelta, baseScale[frame]), frameVisibleRatio) + new Vector2(70f, 70f);
                frameGlow.transform.SetSiblingIndex(frame.GetSiblingIndex());
                stageParts.Add(frameGlow.rectTransform);
                Remember(frameGlow.rectTransform);
            }
        }

        // 캐릭터 뒤 배경 장식(쪽마다 색 판, 거대한 실루엣, 빛, 불씨). 캐릭터들보다 뒤에 깐다.
        backdrop = NewRect("Intro Backdrop", canvasGroup.transform);
        Stretch(backdrop);
        backdrop.SetAsFirstSibling();
        stageParts.Add(backdrop);
        Remember(backdrop);
        BuildSidePanel(0);
        BuildSidePanel(1);

        // 효과용 레이어(빛줄기, 빛 터짐, 잔상)와 FIGHT 글자는 코드로 만든다.
        fx = NewRect("Intro FX", canvasGroup.transform);
        Stretch(fx);

        // 위아래 검은 띠(영화처럼). FIGHT보다 아래, 나머지보다 위.
        for (int i = 0; i < 2; i++)
        {
            Image bar = NewImage(i == 0 ? "Letterbox Top" : "Letterbox Bottom", canvasGroup.transform, null, new Color(0.01f, 0.01f, 0.04f, 1f));
            RectTransform brt = bar.rectTransform;
            brt.anchorMin = new Vector2(0f, i == 0 ? 1f : 0f);
            brt.anchorMax = new Vector2(1f, i == 0 ? 1f : 0f);
            brt.pivot = new Vector2(0.5f, i == 0 ? 1f : 0f);
            brt.sizeDelta = new Vector2(400f, letterboxHeight);
            letterbox[i] = brt;
        }

        RectTransform fightRoot = NewRect("Fight", canvasGroup.transform);
        Stretch(fightRoot);
        fightGroup = fightRoot.gameObject.AddComponent<CanvasGroup>();
        fightGroup.ignoreParentGroups = true;
        fightGroup.blocksRaycasts = false;
        fightLabel = NewRect("Label", fightRoot).gameObject.AddComponent<TextMeshProUGUI>();
        fightLabel.rectTransform.sizeDelta = new Vector2(1600f, 400f);
        fightLabel.alignment = TextAlignmentOptions.Center;
        fightLabel.enableWordWrapping = false;
        fightLabel.raycastTarget = false;
        if (fightFont != null) fightLabel.font = fightFont;
        if (fightMaterial != null) fightLabel.fontSharedMaterial = fightMaterial;
        fightLabel.fontStyle = FontStyles.Italic;
        fightLabel.fontSize = fightSize;
        fightLabel.color = Color.white;
        fightLabel.enableVertexGradient = true;
        fightLabel.colorGradient = new VertexGradient(Color.white, Color.white, fightColor, fightColor);
        fightLabel.text = fightText;
        Remember(fightLabel.rectTransform);
    }

    private RectTransform RootOf(int side) => side == 0 ? player1Root : player2Root;
    private Color ColorOf(int side) => side == 0 ? player1Color : player2Color;

    /// <summary>쪽마다 비스듬한 색 판(안쪽 가장자리에 밝은 선) + 캐릭터 뒤 빛. 캐릭터가 들어올 때 같이 밀려 들어온다.</summary>
    private void BuildSidePanel(int side)
    {
        RectTransform root = RootOf(side);
        if (root == null)
            return;
        float dir = SideOf(root);
        Color color = ColorOf(side);

        RectTransform container = NewRect("Side " + (side + 1), backdrop);
        container.localEulerAngles = new Vector3(0f, 0f, -12f);
        sides[side] = container;

        Image panel = NewImage("Panel", container, null, new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.7f, 0.55f));
        panel.rectTransform.sizeDelta = new Vector2(1100f, 2600f);
        panel.rectTransform.anchoredPosition = new Vector2(dir * 820f, 0f);
        Image edge = NewImage("Edge", container, null, new Color(1f, 1f, 1f, 0.7f));
        edge.rectTransform.sizeDelta = new Vector2(8f, 2600f);
        edge.rectTransform.anchoredPosition = new Vector2(dir * 270f, 0f);
        Image edgeGlow = NewImage("Edge Glow", container, softCircle, new Color(color.r, color.g, color.b, 0.5f));
        edgeGlow.rectTransform.sizeDelta = new Vector2(120f, 2600f);
        edgeGlow.rectTransform.anchoredPosition = new Vector2(dir * 270f, 0f);

        if (softCircle != null)
        {
            Image glow = NewImage("Glow", backdrop, softCircle, new Color(color.r, color.g, color.b, 0f));
            glow.rectTransform.anchoredPosition = CanvasPositionOf(root);
            glow.rectTransform.sizeDelta = new Vector2(900f, 900f);
            sideGlows[side] = glow;
        }
    }

    // 캔버스 중심 기준 위치(캐릭터 묶음의 원래 자리).
    private Vector2 CanvasPositionOf(RectTransform rt)
    {
        Vector3 local = canvasGroup.transform.InverseTransformPoint(rt.position);
        if (basePos.ContainsKey(rt))
            local -= (Vector3)(rt.anchoredPosition - basePos[rt]);
        return local;
    }

    /// <summary>캐릭터 뒤에 같은 그림을 크고 흐리게 깐다(쪽 색으로 물든 거대한 실루엣).</summary>
    private void BuildSilhouette(int side, GameObject prefab, float flip)
    {
        if (silhouettes[side] != null)
            Destroy(silhouettes[side]);
        if (prefab == null || sides[side] == null)
            return;

        // 꺼진 받침 아래에서 만들어, 움직임 스크립트가 깨어나기 전에 실루엣용으로 바꿔 둔다.
        // 몸·팔·총 움직임은 캐릭터와 똑같이 따라 하고, 색을 바꾸는 효과(전기 오라, 총구 섬광 등)는 빠진다.
        var holder = new GameObject("Silhouette Holder");
        holder.SetActive(false);
        GameObject go = Instantiate(prefab, holder.transform);
        foreach (CharacterCardAnimator anim in go.GetComponentsInChildren<CharacterCardAnimator>(true))
            anim.MakeSilhouette();
        go.transform.SetParent(backdrop, false);
        Destroy(holder);
        go.transform.SetSiblingIndex(sides[side].GetSiblingIndex() + 1);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = CanvasPositionOf(RootOf(side)) + new Vector2(SideOf(RootOf(side)) * 120f, 120f);
        rt.localScale = new Vector3(portraitScale * 2.6f * flip, portraitScale * 2.6f, 1f);
        Color color = ColorOf(side);
        foreach (Graphic g in go.GetComponentsInChildren<Graphic>(true))
        {
            g.material = null;
            g.color = new Color(color.r, color.g, color.b, silhouetteAlpha);
        }
        silhouettes[side] = go;
    }

    private void UpdateBackdrop()
    {
        float now = Time.unscaledTime;
        for (int side = 0; side < 2; side++)
        {
            if (sideGlows[side] != null)
            {
                float shown = sideShownAt[side] < 0f ? 0f : Mathf.Clamp01((now - sideShownAt[side]) / 0.2f);
                float pulse = 0.35f + 0.12f * Mathf.Sin(now * 3f + side * 1.7f);
                Color c = ColorOf(side); c.a = pulse * shown;
                sideGlows[side].color = c;
            }
            if (silhouettes[side] != null)
            {
                var rt = (RectTransform)silhouettes[side].transform;
                rt.anchoredPosition += new Vector2(-SideOf(RootOf(side)) * 6f * Time.unscaledDeltaTime, 0f);
                bool visible = sideShownAt[side] >= 0f;
                if (silhouettes[side].activeSelf != visible)
                    silhouettes[side].SetActive(visible);
            }
        }

        // 불씨: 아래에서 위로 천천히 떠오르는 작은 빛 알갱이.
        if (introRoutine != null && softCircle != null && now >= nextEmberAt)
        {
            nextEmberAt = now + 0.045f;
            Image img = NewImage("Ember", backdrop, softCircle, Color.white);
            float x = Random.Range(-980f, 980f);
            Color c = x < 0f ? player1Color : player2Color;
            if (SideOf(player1Root) > 0f) c = x < 0f ? player2Color : player1Color;
            img.color = Color.Lerp(c, Color.white, 0.4f);
            img.rectTransform.anchoredPosition = new Vector2(x, -600f);
            img.rectTransform.sizeDelta = Vector2.one * Random.Range(10f, 26f);
            embers.Add(new Ember { rt = img.rectTransform, img = img, velocity = new Vector2(Random.Range(-30f, 30f), Random.Range(160f, 360f)), life = Random.Range(1.6f, 3f) });
        }
        for (int i = embers.Count - 1; i >= 0; i--)
        {
            Ember e = embers[i];
            e.age += Time.unscaledDeltaTime;
            if (e.rt == null || e.age >= e.life)
            {
                if (e.rt != null) Destroy(e.rt.gameObject);
                embers.RemoveAt(i);
                continue;
            }
            e.rt.anchoredPosition += e.velocity * Time.unscaledDeltaTime;
            float t = e.age / e.life;
            Color c = e.img.color; c.a = Mathf.Sin(t * Mathf.PI) * 0.8f; e.img.color = c;
        }
    }

    /// <summary>맵 액자: 도는 동안엔 빛이 빠르게 맥동하고 액자가 미세하게 떨린다. 정해지면 금빛으로 바뀌고 뒤에서 빛줄기가 돈다.</summary>
    private void UpdateFrameFx()
    {
        float now = Time.unscaledTime;
        if (frameSpinning)
        {
            foreach (RectTransform rt in mapFrameParts)
                rt.anchoredPosition = basePos[rt] + Random.insideUnitCircle * 2.5f;
            if (frameGlow != null)
            {
                Color c = spinGlowColor; c.a = 0.55f + 0.35f * Mathf.Sin(now * 22f);
                frameGlow.color = c;
            }
        }
        else if (decidedAt >= 0f)
        {
            float t = now - decidedAt;
            if (frameGlow != null)
            {
                // 번쩍 하얗게 → 금빛으로 → 은은하게 남는다(후광은 이펙트 프리팹이 맡는다).
                Color c = Color.Lerp(Color.white, decideGlowColor, Mathf.Clamp01(t / 0.35f));
                c.a = Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(t / 0.6f));
                frameGlow.color = c;
            }
        }
    }

    /// <summary>맵 액자 뒤에 후광 이펙트를 띄운다. 원래 위로 뻗는 빛줄기라, 화면을 향하게 돌리고 퍼짐 각을 넓혀 사방으로 뻗게 한다.</summary>
    private void SpawnHalo()
    {
        if (haloPrefab == null || mapFrameParts.Length == 0)
            return;
        Camera cam = Camera.main;
        if (halo != null)
            Destroy(halo);

        Vector3 at = mapFrameParts[0].position;
        at.z = transform.position.z;
        halo = Instantiate(haloPrefab, at, cam != null ? Quaternion.LookRotation(-cam.transform.forward) : Quaternion.identity, transform);
        halo.transform.localScale = Vector3.one * haloScale;
        foreach (ParticleSystem ps in halo.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.ShapeModule shape = ps.shape;
            if (ps.GetComponent<ParticleSystemRenderer>().renderMode == ParticleSystemRenderMode.Stretch)
                shape.angle = 82f;
            ParticleSystem.MainModule main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            // 원래는 상자 뒤에 은은하게 깔리는 용도라 연하다 → 더 진하고 촘촘하게.
            Color c = main.startColor.color;
            c.a = Mathf.Clamp01(c.a * haloIntensity);
            main.startColor = c;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTimeMultiplier *= haloDensity;
            // VS 캔버스(UI 레이어) 바로 아래에 그려서 배경 파티클 위, 맵 액자 뒤에 오게 한다.
            var rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.sortingLayerName = "UI";
            rend.sortingOrder = 790;
        }
    }

    private void StartVsSparks()
    {
        if (vsSparks == null)
            return;
        foreach (ParticleSystem ps in vsSparks)
        {
            if (ps == null)
                continue;
            ps.gameObject.SetActive(true);
            ps.Play(true);
        }
    }

    private IEnumerator VsSlam()
    {
        if (vsLogo == null)
            yield break;
        Play(slamClip, 1f);
        StartVsSparks();
        TwoPlayerCamera.Instance?.Shake(0.3f, 0.25f);
        StartCoroutine(ScreenFlash(0.55f, 0.18f));
        Vector2 at = basePos[vsLogo];
        StartCoroutine(Burst(at, new Color(0.7f, 0.9f, 1f, 1f), 5.5f, 0.5f));
        StartCoroutine(Burst(at, Color.white, 2.2f, 0.25f));
        Vector3 scale = baseScale[vsLogo];
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.14f)
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            vsLogo.localScale = scale * Mathf.Lerp(3.2f, 1f, e);
            vsLogo.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-14f, 0f, e));
            yield return null;
        }
        vsLogo.localScale = scale;
        vsLogo.localEulerAngles = Vector3.zero;
        yield return Squash(vsLogo, 0.12f, 0.2f);
        // 박힌 뒤엔 심장 뛰듯 살짝살짝.
        while (!blowingOut && vsLogo != null)
        {
            float k = 1f + Mathf.Max(0f, Mathf.Sin(Time.unscaledTime * 6f)) * 0.04f;
            vsLogo.localScale = scale * k;
            yield return null;
        }
    }

    private IEnumerator SlideSideIn(int side)
    {
        RectTransform container = sides[side];
        if (container == null)
            yield break;
        sideShownAt[side] = Time.unscaledTime;
        float dir = SideOf(RootOf(side));
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.16f)
        {
            float e = 1f - Mathf.Pow(1f - t, 4f);
            container.anchoredPosition = new Vector2(dir * 1500f * (1f - e), 0f);
            yield return null;
        }
        container.anchoredPosition = Vector2.zero;
    }

    private IEnumerator Letterbox(bool show, float duration)
    {
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            float h = letterboxHeight * (show ? e : 1f - e);
            foreach (RectTransform bar in letterbox)
                if (bar != null) bar.sizeDelta = new Vector2(bar.sizeDelta.x, h);
            yield return null;
        }
        // 프레임이 한 번 크게 끊겨도 마지막 값에 정확히 도착하게.
        foreach (RectTransform bar in letterbox)
            if (bar != null) bar.sizeDelta = new Vector2(bar.sizeDelta.x, show ? letterboxHeight : 0f);
    }

    private void Remember(RectTransform rt)
    {
        if (rt == null || basePos.ContainsKey(rt))
            return;
        basePos[rt] = rt.anchoredPosition;
        baseScale[rt] = rt.localScale;
    }

    private void ResetVisuals()
    {
        if (!captured)
            return;

        foreach (KeyValuePair<RectTransform, Vector2> pair in basePos)
        {
            pair.Key.anchoredPosition = pair.Value;
            pair.Key.localScale = baseScale[pair.Key];
        }
        canvasGroup.alpha = 1f;
        fightGroup.alpha = 0f;
        foreach (Transform child in fx)
            Destroy(child.gameObject);

        if (loopingParticle != null)
            loopingParticle.gameObject.SetActive(true);

        for (int side = 0; side < 2; side++)
        {
            sideShownAt[side] = -1f;
            if (sides[side] != null)
                sides[side].anchoredPosition = new Vector2(SideOf(RootOf(side)) * 1500f, 0f);
        }
        foreach (RectTransform bar in letterbox)
            if (bar != null) bar.sizeDelta = new Vector2(bar.sizeDelta.x, 0f);
        foreach (Ember e in embers)
            if (e.rt != null) Destroy(e.rt.gameObject);
        embers.Clear();
        frameSpinning = false;
        blowingOut = false;
        decidedAt = -1f;
        if (vsLogo != null) vsLogo.localScale = Vector3.zero;
        if (halo != null) Destroy(halo);
        if (vsSparks != null)
            foreach (ParticleSystem ps in vsSparks)
                if (ps != null) { ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); ps.gameObject.SetActive(false); }
        if (frameGlow != null) frameGlow.color = Color.clear;

        // 첫 화면: 캐릭터는 화면 밖, 이름과 맵 액자는 숨김.
        SetOffscreen(player1Root);
        SetOffscreen(player2Root);
        foreach (RectTransform rt in player1Names) SetHidden(rt);
        foreach (RectTransform rt in player2Names) SetHidden(rt);
        foreach (RectTransform rt in mapFrameParts) rt.localScale = Vector3.zero;
    }

    private void SetOffscreen(RectTransform rt)
    {
        if (rt == null)
            return;
        rt.anchoredPosition = basePos[rt] + new Vector2(SideOf(rt) * 1300f, 0f);
    }

    private static void SetHidden(RectTransform rt)
    {
        if (rt == null)
            return;
        if (!rt.TryGetComponent(out CanvasGroup g))
            g = rt.gameObject.AddComponent<CanvasGroup>();
        g.alpha = 0f;
    }

    // 화면 왼쪽에 있는 쪽은 왼쪽에서, 오른쪽은 오른쪽에서 들어온다.
    private float SideOf(RectTransform rt)
    {
        Vector3 local = canvasGroup.transform.InverseTransformPoint(rt.position);
        if (basePos.ContainsKey(rt))
            local -= (Vector3)(rt.anchoredPosition - basePos[rt]);
        return local.x < 0f ? -1f : 1f;
    }

    // ---------------- 흐름 ----------------

    private IEnumerator IntroSequence()
    {
        // 로딩 화면이 떠 있는 동안은 시작하지 않는다(아무도 못 본다).
        while (SceneTransition.Instance != null && SceneTransition.Instance.IsBusy)
            yield return null;

        float start = Time.unscaledTime;
        System.Func<float> now = () => Time.unscaledTime - start;

        StartCoroutine(Letterbox(true, 0.25f));
        StartCoroutine(Slash(0f, -18f, 70f, 0.9f));
        StartCoroutine(Slash(0.06f, -18f, 22f, 0.55f));
        Play(whooshClip, 0.7f);

        yield return WaitUntilTime(now, p1Enter);
        StartCoroutine(DashIn(player1Root));
        yield return WaitUntilTime(now, p2Enter);
        StartCoroutine(DashIn(player2Root));

        yield return WaitUntilTime(now, namesTime);
        StartCoroutine(RevealNames(player1Names));
        StartCoroutine(RevealNames(player2Names));

        yield return WaitUntilTime(now, mapTime);
        yield return DropMapFrame();
        if (slotMachine != null)
            slotMachine.Spin();
        frameSpinning = true;
        Play(rollClip, 0.75f);

        // 맵이 정해질 때까지 기다렸다가 결정 연출.
        yield return null;
        while (slotMachine != null && slotMachine.IsSpinning)
            yield return null;
        frameSpinning = false;
        foreach (RectTransform rt in mapFrameParts)
            rt.anchoredPosition = basePos[rt];
        float resultAt = now();
        StartCoroutine(MapDecided());

        float outroAt = Mathf.Max(flashDelay, resultAt + afterResultHold);
        // 붐 소리는 빨아들이는 소리가 앞에 붙어 있어서, 터지는 순간이 플래시에 맞도록 미리 튼다.
        yield return WaitUntilTime(now, outroAt - boomLead);
        Play(boomClip, 1f);
        StartCoroutine(Lunge(player1Root));
        StartCoroutine(Lunge(player2Root));
        yield return WaitUntilTime(now, outroAt);

        // 하얀 플래시 → VS 화면이 확 커지며 사라진다.
        if (flashObject != null)
            flashObject.SetActive(true);
        if (loopingParticle != null)
        {
            loopingParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            loopingParticle.Clear(true);
        }
        TwoPlayerCamera.Instance?.Shake(0.25f, 0.3f);
        yield return BlowOut();

        // FIGHT!
        yield return WaitUntilTime(now, outroAt + controlUnlockDelay);
        controlsUnlocked = true;
        CharacterBase.IntroLocked = false;
        SetAllCharactersLocked(false);
        yield return FightSlam();

        introRoutine = null;
        gameObject.SetActive(false);
    }

    private static IEnumerator WaitUntilTime(System.Func<float> now, float t)
    {
        while (now() < t)
            yield return null;
    }

    private void Play(AudioClip clip, float scale) => SoundManager.Instance?.PlaySfx(clip, volume * scale, 0.03f);

    // ---------------- 조각 연출 ----------------

    private IEnumerator Slash(float delay, float angle, float thickness, float alpha)
    {
        float t0 = Time.unscaledTime + delay;
        while (Time.unscaledTime < t0)
            yield return null;

        Image bar = NewImage("Slash", fx, null, new Color(1f, 1f, 1f, alpha));
        bar.rectTransform.sizeDelta = new Vector2(3200f, thickness);
        bar.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        float yOff = thickness > 40f ? 60f : -90f;
        const float dur = 0.22f;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / dur)
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            bar.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-2600f, 2600f, e), yOff);
            bar.rectTransform.sizeDelta = new Vector2(3200f, thickness * (1f - t * 0.6f));
            Color c = bar.color; c.a = alpha * (1f - t * t); bar.color = c;
            yield return null;
        }
        Destroy(bar.gameObject);
    }

    private IEnumerator DashIn(RectTransform root)
    {
        if (root == null)
            yield break;
        int side = root == player1Root ? 0 : 1;
        float dir = SideOf(root);
        Play(whooshClip, 0.5f);
        StartCoroutine(SlideSideIn(side));
        StartCoroutine(SpeedLines(basePos[root].y, dir));

        // 팍! 하고 꽂힌다: 아주 빠르게 미끄러져 들어와 딱 멈춘다(가로로 길게 늘어난 채로).
        Vector2 target = basePos[root];
        Vector2 from = root.anchoredPosition;
        Vector3 scale = baseScale[root];
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / enterDuration)
        {
            float e = 1f - Mathf.Pow(1f - t, 4f);
            root.anchoredPosition = Vector2.LerpUnclamped(from, target, e);
            root.localScale = new Vector3(scale.x * 1.35f, scale.y * 0.82f, scale.z);
            yield return null;
        }
        root.anchoredPosition = target;
        root.localScale = scale;

        // 착지: 번쩍 + 쿵 + 흔들림 + 빛 터짐 + 캐릭터 고유 소리, 그리고 아주 짧게 멈칫(히트스톱).
        Play(impactClip, 1f);
        if (pickSounds[side] != null)
            SoundManager.Instance?.PlaySfx(pickSounds[side], volume * 0.8f);
        TwoPlayerCamera.Instance?.Shake(0.22f, 0.2f);
        StartCoroutine(ScreenFlash(0.45f, 0.14f));
        StartCoroutine(Burst(target, new Color(0.8f, 0.88f, 1f, 0.95f), 4f, 0.45f));
        StartCoroutine(Burst(target, new Color(1f, 1f, 1f, 0.9f), 1.6f, 0.2f));
        root.localScale = new Vector3(scale.x * 0.78f, scale.y * 1.22f, scale.z);
        // 두 번째로 들어온 쪽이 착지하면 가운데 VS가 쾅 박힌다.
        if (root == player2Root)
            StartCoroutine(VsSlamAfter(0.12f));
        yield return Wait(0.05f);
        yield return Squash(root, 0.2f, 0.26f);
    }

    private IEnumerator VsSlamAfter(float delay)
    {
        yield return Wait(delay);
        yield return VsSlam();
    }

    private IEnumerator SpeedLines(float y, float fromSide)
    {
        for (int i = 0; i < 9; i++)
        {
            StartCoroutine(SpeedLine(y + Random.Range(-230f, 230f), fromSide, Random.Range(0.6f, 1.4f)));
            yield return null;
        }
    }

    private IEnumerator SpeedLine(float y, float fromSide, float thickness)
    {
        Image line = NewImage("Speed Line", fx, null, new Color(1f, 1f, 1f, 0.75f));
        float length = Random.Range(380f, 820f);
        line.rectTransform.sizeDelta = new Vector2(length, 5f * thickness);
        float start = fromSide * 1300f;
        float end = fromSide * Random.Range(120f, 420f);
        const float dur = 0.2f;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / dur)
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            line.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(start, end, e), y);
            Color c = line.color; c.a = 0.75f * (1f - t); line.color = c;
            yield return null;
        }
        Destroy(line.gameObject);
    }

    private IEnumerator ScreenFlash(float alpha, float duration)
    {
        Image flash = NewImage("Screen Flash", fx, null, new Color(1f, 1f, 1f, alpha));
        Stretch(flash.rectTransform);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
        {
            Color c = flash.color; c.a = alpha * (1f - t) * (1f - t); flash.color = c;
            yield return null;
        }
        Destroy(flash.gameObject);
    }

    private IEnumerator Squash(RectTransform root, float amount, float duration)
    {
        Vector3 scale = baseScale[root];
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
        {
            float k = Mathf.Sin(t * Mathf.PI) * (1f - t) * amount * 2f;
            root.localScale = new Vector3(scale.x * (1f + k), scale.y * (1f - k), scale.z);
            yield return null;
        }
        root.localScale = scale;
    }

    private IEnumerator Burst(Vector2 at, Color color, float size, float duration)
    {
        if (softCircle == null)
            yield break;
        Image img = NewImage("Burst", fx, softCircle, color);
        img.rectTransform.anchoredPosition = at;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            img.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(80f, 260f * size, e);
            Color c = color; c.a = color.a * (1f - t); img.color = c;
            yield return null;
        }
        Destroy(img.gameObject);
    }

    private IEnumerator RevealNames(RectTransform[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            StartCoroutine(RiseIn(names[i]));
            yield return Wait(0.08f);
        }
    }

    private IEnumerator RiseIn(RectTransform rt)
    {
        if (rt == null)
            yield break;
        CanvasGroup g = rt.GetComponent<CanvasGroup>();
        Vector2 target = basePos[rt];
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.3f)
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            rt.anchoredPosition = target + new Vector2(0f, -26f * (1f - e));
            if (g != null) g.alpha = e;
            yield return null;
        }
        rt.anchoredPosition = target;
        if (g != null) g.alpha = 1f;
    }

    private IEnumerator DropMapFrame()
    {
        Play(dropClip, 0.8f);
        const float dur = 0.32f;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / dur)
        {
            // 크게 떨어지며 들어와 톡 튀고 자리 잡는다.
            const float c1 = 1.6f;
            float x = t - 1f;
            float e = 1f + (c1 + 1f) * x * x * x + c1 * x * x;
            float s = Mathf.LerpUnclamped(1.35f, 1f, e);
            float a = Mathf.Clamp01(t * 3f);
            foreach (RectTransform rt in mapFrameParts)
            {
                rt.localScale = baseScale[rt] * (s * a + (1f - a) * 1.35f) * Mathf.Clamp01(t * 6f);
                rt.anchoredPosition = basePos[rt] + new Vector2(0f, 60f * (1f - e));
            }
            yield return null;
        }
        foreach (RectTransform rt in mapFrameParts)
        {
            rt.localScale = baseScale[rt];
            rt.anchoredPosition = basePos[rt];
        }
        TwoPlayerCamera.Instance?.Shake(0.06f, 0.1f);
    }

    private IEnumerator MapDecided()
    {
        // 테두리 잔상이 바깥으로 퍼진다.
        if (mapFrameBorder != null)
        {
            Image echo = NewImage("Frame Echo", fx, mapFrameBorder.sprite, new Color(1f, 0.95f, 0.75f, 0.9f));
            echo.type = mapFrameBorder.type;
            echo.rectTransform.anchoredPosition = mapFrameBorder.rectTransform.anchoredPosition;
            echo.rectTransform.sizeDelta = mapFrameBorder.rectTransform.sizeDelta;
            StartCoroutine(Echo(echo));
        }
        StartCoroutine(Burst(mapFrameParts.Length > 0 ? basePos[mapFrameParts[0]] : Vector2.zero, new Color(1f, 0.95f, 0.8f, 0.55f), 5f, 0.55f));

        decidedAt = Time.unscaledTime;
        SpawnHalo();
        Play(decideClip, 1f);
        TwoPlayerCamera.Instance?.Shake(0.28f, 0.25f);
        StartCoroutine(ScreenFlash(0.35f, 0.2f));

        // 액자가 크게 한 번 박힌다.
        StartCoroutine(PunchAll(mapFrameParts, 0.16f, 0.32f));
        yield return Wait(0.12f);
        // 두 캐릭터가 "준비" 하듯 통 튄다.
        if (player1Root != null) StartCoroutine(Hop(player1Root));
        yield return Wait(0.06f);
        if (player2Root != null) StartCoroutine(Hop(player2Root));
    }

    private IEnumerator Echo(Image echo)
    {
        Vector3 s0 = echo.rectTransform.localScale;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.5f)
        {
            float e = 1f - Mathf.Pow(1f - t, 2f);
            echo.rectTransform.localScale = s0 * Mathf.Lerp(1f, 1.14f, e);
            Color c = echo.color; c.a = 0.9f * (1f - t); echo.color = c;
            yield return null;
        }
        Destroy(echo.gameObject);
    }

    private IEnumerator PunchAll(RectTransform[] parts, float amount, float duration)
    {
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
        {
            float k = 1f + Mathf.Sin(t * Mathf.PI) * (1f - t * 0.5f) * amount;
            foreach (RectTransform rt in parts)
                rt.localScale = baseScale[rt] * k;
            yield return null;
        }
        foreach (RectTransform rt in parts)
            rt.localScale = baseScale[rt];
    }

    private IEnumerator Hop(RectTransform root)
    {
        Vector2 p = basePos[root];
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.3f)
        {
            root.anchoredPosition = p + new Vector2(0f, Mathf.Sin(t * Mathf.PI) * 30f);
            yield return null;
        }
        root.anchoredPosition = p;
        yield return Squash(root, 0.1f, 0.18f);
    }

    private IEnumerator Lunge(RectTransform root)
    {
        if (root == null)
            yield break;
        Vector2 p = basePos[root];
        float dir = -SideOf(root);
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.3f)
        {
            // 뒤로 살짝 움츠렸다가 앞으로 확.
            float k = t < 0.55f ? -Mathf.Sin(t / 0.55f * Mathf.PI * 0.5f) * 0.4f : Mathf.Lerp(-0.4f, 1f, (t - 0.55f) / 0.45f);
            root.anchoredPosition = p + new Vector2(dir * 70f * k, 0f);
            yield return null;
        }
    }

    private IEnumerator BlowOut()
    {
        blowingOut = true;
        if (halo != null)
            Destroy(halo);
        StartCoroutine(Letterbox(false, 0.25f));
        const float dur = 0.28f;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / dur)
        {
            float e = t * t;
            float k = Mathf.Lerp(1f, 1.35f, 1f - (1f - t) * (1f - t));
            foreach (RectTransform rt in stageParts)
            {
                rt.localScale = baseScale[rt] * k;
                rt.anchoredPosition = basePos[rt] * k;
            }
            canvasGroup.alpha = 1f - e;
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }

    private IEnumerator FightSlam()
    {
        Play(fightClip, 1f);
        TwoPlayerCamera.Instance?.Shake(0.18f, 0.22f);
        RectTransform rt = fightLabel.rectTransform;
        StartCoroutine(Burst(Vector2.zero, new Color(1f, 0.9f, 0.6f, 0.6f), 6f, 0.5f));

        // 크게 날아와 박힌다.
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.18f)
        {
            float e = 1f - Mathf.Pow(1f - t, 3f);
            rt.localScale = Vector3.one * Mathf.Lerp(2.6f, 1f, e);
            fightGroup.alpha = Mathf.Clamp01(t * 2.5f);
            yield return null;
        }
        rt.localScale = Vector3.one;
        fightGroup.alpha = 1f;
        yield return Squash(rt, 0.06f, 0.16f);

        yield return Wait(0.45f);

        // 앞으로 튀어나오며 사라진다.
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.28f)
        {
            rt.localScale = Vector3.one * Mathf.Lerp(1f, 1.35f, t);
            fightGroup.alpha = 1f - t * t;
            yield return null;
        }
        fightGroup.alpha = 0f;
    }

    private static IEnumerator Wait(float seconds)
    {
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end)
            yield return null;
    }

    // ---------------- 만들기 ----------------

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
    {
        Image img = NewRect(name, parent).gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }
}
