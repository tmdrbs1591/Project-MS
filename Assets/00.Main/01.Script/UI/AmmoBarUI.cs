using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 머리 위(체력바 위)에 뜨는 탄창 바다. 남은 탄 / 최대 탄 비율만큼 fillImage가 채워진다.
/// WorldHealthBarUI/ReloadSliderUI와 같은 이유로 이벤트 대신 매 프레임 폴링한다 — 잔탄과 재장전
/// 상태는 [Networked] 값을 읽는 것이라, 내 캐릭터든 상대 캐릭터든 똑같이 최신 값이 보인다.
///
/// [표시 규칙]
///   - 평소: fillAmount = 남은 탄 / 탄창 크기.
///   - 재장전 중(showReloadProgress): fillAmount = 재장전 진행도(0→1). 재장전이 시작되면 바가 비었다가
///     시간에 맞춰 다시 차오른다. 끄면 재장전 중에도 남은 탄 기준으로만 보여준다.
///   - 탄창이 없는 캐릭터/상태(예: 저격 모드)에서는 바 전체를 투명하게 숨긴다.
///   - showForOpponent를 끄면 내 캐릭터에만 보인다(기본은 상대에게도 보임).
///   - 재장전 중: 바가 reloadingColor로 바뀌어 깜빡이고, reloadLabel("재장전")이 보인다.
///   - 탄이 0인데 재장전 전: 바 테두리(이 오브젝트의 Image)가 emptyColor로 깜빡인다.
///   - 재장전 완료: 바가 하얗게 번쩍이며 살짝 커졌다 돌아온다. 내 캐릭터면 reloadCompleteClip 재생.
///
/// [씬/프리팹 설정]
///   - 캐릭터 프리팹의 머리 위 월드 캔버스 아래에, 체력바처럼 프레임 + Filled Image(Horizontal)를 만든다.
///   - 이 스크립트는 프레임(바 전체를 감싸는 오브젝트)에 붙이고 fillImage에 Filled Image를 연결한다.
///     숨길 때 이 오브젝트에 붙은 CanvasGroup의 알파를 쓰므로(없으면 자동 추가) 스크립트 자신을
///     SetActive(false)해서 Update가 멈추는 일이 없다.
///   - character는 비워두면 부모의 CharacterBase를 자동으로 찾는다.
/// </summary>
public class AmmoBarUI : MonoBehaviour
{
    [SerializeField] private CharacterBase character;
    [SerializeField] private Image fillImage;
    [Tooltip("켜면 재장전 중에 바가 재장전 진행도(0→1)로 차오른다. 끄면 재장전 중에도 남은 탄 기준.")]
    [SerializeField] private bool showReloadProgress = true;
    [Tooltip("끄면 내 캐릭터에만 표시한다. 켜면 상대 캐릭터의 탄창도 보인다.")]
    [SerializeField] private bool showForOpponent = true;
    [Tooltip("값이 바뀔 때 바가 따라가는 속도. 0이면 즉시 반영. 클수록 빠르다.")]
    [Min(0f)] [SerializeField] private float smoothSpeed = 14f;

    [Header("재장전 강조")]
    [Tooltip("재장전 중에만 보이는 글자(예: \"재장전\"). 비워두면 생략.")]
    [SerializeField] private TMP_Text reloadLabel;
    [SerializeField] private Color reloadingColor = new Color(1f, 0.62f, 0.15f, 1f);
    [SerializeField] private Color emptyColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color completeFlashColor = Color.white;
    [Min(0f)] [SerializeField] private float blinkSpeed = 10f;
    [Min(0f)] [SerializeField] private float completeFlashDuration = 0.25f;
    [Min(1f)] [SerializeField] private float completePopScale = 1.25f;
    [Tooltip("내 캐릭터의 재장전이 끝났을 때 재생할 효과음. 비워두면 소리 없음.")]
    [SerializeField] private AudioClip reloadCompleteClip;

    private CanvasGroup group;
    private float displayed = 1f;
    private Color normalColor;
    private Image frameImage;
    private Color frameColor;
    private Vector3 baseScale;
    private bool wasReloading;
    private float flashTimer;

    private void Awake()
    {
        if (character == null)
            character = GetComponentInParent<CharacterBase>();

        group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();

        normalColor = fillImage != null ? fillImage.color : Color.white;
        baseScale = transform.localScale;
        frameImage = GetComponent<Image>();
        if (frameImage == fillImage)
            frameImage = null;
        if (frameImage != null)
            frameColor = frameImage.color;
        if (reloadLabel != null)
            reloadLabel.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        int current = 0;
        int max = 0;
        bool available = character != null && character.Object != null && fillImage != null &&
                         character.TryGetAmmo(out current, out max);

        if (available && !showForOpponent && !character.IsLocalPlayer)
            available = false;

        group.alpha = available ? 1f : 0f;
        if (!available)
        {
            wasReloading = false;
            SetLabelVisible(false);
            return;
        }

        bool reloading = character.IsReloading;
        if (wasReloading && !reloading)
            OnReloadCompleted();
        wasReloading = reloading;

        UpdateEmphasis(reloading, current);

        float target = showReloadProgress && reloading
            ? character.ReloadProgress
            : (float)current / max;

        displayed = smoothSpeed > 0f
            ? Mathf.Lerp(displayed, target, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime))
            : target;

        fillImage.fillAmount = displayed;
    }

    private void OnReloadCompleted()
    {
        flashTimer = completeFlashDuration;
        // 재장전 완료 직후 바가 0에서 천천히 차오르지 않고 바로 가득 찬 상태로 번쩍이게 한다.
        displayed = 1f;
        if (character.IsLocalPlayer && reloadCompleteClip != null)
            SoundManager.Instance?.PlaySfx(reloadCompleteClip);
    }

    private void UpdateEmphasis(bool reloading, int current)
    {
        float blink = 0.5f + 0.5f * Mathf.Sin(Time.time * blinkSpeed);
        Color color = normalColor;

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            float t = completeFlashDuration > 0f ? Mathf.Clamp01(flashTimer / completeFlashDuration) : 0f;
            color = Color.Lerp(normalColor, completeFlashColor, t);
            transform.localScale = baseScale * Mathf.Lerp(1f, completePopScale, t);
        }
        else
        {
            transform.localScale = baseScale;
            if (reloading)
                color = Color.Lerp(reloadingColor, Color.white, blink * 0.35f);
        }

        // 탄이 바닥나면 채움이 0이라 안 보이므로 테두리를 깜빡여서 알린다.
        if (frameImage != null)
            frameImage.color = !reloading && current <= 0 ? Color.Lerp(frameColor, emptyColor, blink) : frameColor;

        fillImage.color = color;
        SetLabelVisible(reloading);
        if (reloading && reloadLabel != null)
            reloadLabel.alpha = Mathf.Lerp(0.55f, 1f, blink);
    }

    private void SetLabelVisible(bool visible)
    {
        if (reloadLabel != null && reloadLabel.gameObject.activeSelf != visible)
            reloadLabel.gameObject.SetActive(visible);
    }
}
