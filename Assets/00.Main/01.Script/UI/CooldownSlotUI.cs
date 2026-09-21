using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectMS.CharacterSystem;

/// <summary>
/// 스킬 하나(평타/Q/E/대시/궁)의 쿨타임을 보여주는 슬롯 UI다.
///
/// [씬 설정]
///   - 아이콘 Image 위에 쿨타임 오버레이 Image를 겹쳐 놓는다.
///   - 오버레이 Image의 Image Type을 Filled(Radial 360 또는 Vertical)로 설정하고
///     Fill Origin/Method를 원하는 방향으로 맞춘다.
///   - 남은 시간을 숫자로도 보여주고 싶으면 TMP_Text를 자식으로 두고 연결한다(선택).
///   - iconImage는 CooldownHUD.Bind()가 캐릭터의 CharacterDefinition.GetIcon(actionType)
///     값으로 자동으로 채워준다 — 여기 미리 스프라이트를 넣어놔도 캐릭터가 바뀌면 덮어써진다.
///   - 게이지형 궁극기(gaugeFillImage)는 cooldownOverlay와 별개의 Image다. Image Type을
///     Filled로 설정하되, 채워질수록(0→1) "준비됨"을 뜻하므로 cooldownOverlay와 반대 방향
///     느낌으로 헷갈리지 않게 색을 다르게 주는 걸 권장한다. 궁극기가 쿨타임형인 캐릭터는
///     이 필드를 비워둬도 된다(안 쓰임).
///   - 충전형 스킬(예: Zipper Q)은 chargeCountText에 남은 충전 수를 숫자로 보여주고,
///     cooldownOverlay/remainingText로 "다음 충전까지"를 보여준다(UpdateCharges).
///     충전이 1개 이상 남아 있으면(=쓸 수 있으면) 오버레이를 연하게, 0개면 원래 진하기로
///     그린다(usableOverlayAlphaScale). chargeCountText는 충전형 슬롯에만 연결하면 된다.
/// </summary>
public class CooldownSlotUI : MonoBehaviour
{
    [SerializeField] private CharacterActionType actionType;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TMP_Text remainingText;
    [SerializeField] private Image gaugeFillImage;
    [SerializeField] private TMP_Text chargeCountText;
    [Tooltip("충전형 스킬에서 충전이 1개 이상 남아(쓸 수 있는 상태) 재충전 중일 때, 쿨타임 오버레이의 " +
             "투명도에 곱하는 값. 0이면 안 보이고 1이면 충전 0개일 때와 똑같이 진하다.")]
    [Range(0f, 1f)] [SerializeField] private float usableOverlayAlphaScale = 0.4f;

    private Color overlayBaseColor;
    private bool overlayColorCached;

    public CharacterActionType ActionType => actionType;

    /// <summary>시간 기반 쿨타임 표시. 게이지형 궁극기 슬롯에는 UpdateGauge()를 대신 쓴다.</summary>
    public void UpdateCooldown(float remaining, float total)
    {
        bool onCooldown = total > 0f && remaining > 0f;

        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = onCooldown ? remaining / total : 0f;
        SetOverlayAlphaScale(1f);

        if (remainingText != null)
            remainingText.text = onCooldown ? Mathf.CeilToInt(remaining).ToString() : string.Empty;

        // 쿨타임 모드로 갱신 중이면 게이지 바/충전 숫자는 관여하지 않는 상태이므로 비워서 잔상이 안 남게 한다.
        if (gaugeFillImage != null)
            gaugeFillImage.fillAmount = 0f;
        if (chargeCountText != null)
            chargeCountText.text = string.Empty;
        SetChargeCountActive(false); // 충전형이 아닌 스킬이면 숫자 오브젝트는 꺼둔다.
    }

    /// <summary>충전형 스킬 표시. currentCharges는 남은 충전 수, rechargeRemaining/Total은 다음
    /// 충전까지 남은 시간/총 시간이다. 충전이 가득 차 있으면 오버레이 없이 숫자만 보인다.</summary>
    public void UpdateCharges(int currentCharges, int maxCharges, float rechargeRemaining, float rechargeTotal)
    {
        // 음수 = 잔탄이 아직 초기화되지 않은 상태(사망 직후 등). 다음 리셋에서 가득 채워지므로 가득 찬 걸로 본다.
        int charges = currentCharges < 0
            ? Mathf.Max(0, maxCharges)
            : Mathf.Clamp(currentCharges, 0, Mathf.Max(0, maxCharges));
        bool recharging = charges < maxCharges && rechargeTotal > 0f && rechargeRemaining > 0f;

        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = recharging ? Mathf.Clamp01(rechargeRemaining / rechargeTotal) : 0f;
        // 쓸 수 있으면(1개 이상) 연하게, 못 쓰면(0개) 진하게.
        SetOverlayAlphaScale(charges > 0 ? usableOverlayAlphaScale : 1f);

        if (remainingText != null)
            remainingText.text = recharging ? Mathf.CeilToInt(rechargeRemaining).ToString() : string.Empty;

        // 충전형 스킬이면 숫자 오브젝트가 항상 켜져 있어야 한다(에디터에서 꺼둔 상태여도 켠다).
        SetChargeCountActive(true);
        if (chargeCountText != null)
            chargeCountText.text = charges.ToString();

        if (gaugeFillImage != null)
            gaugeFillImage.fillAmount = 0f;
    }

    private void SetOverlayAlphaScale(float scale)
    {
        if (cooldownOverlay == null)
            return;

        // 인스펙터에서 정해둔 오버레이 색을 기준으로 알파만 곱한다(처음 한 번만 원본을 기억해둔다).
        if (!overlayColorCached)
        {
            overlayBaseColor = cooldownOverlay.color;
            overlayColorCached = true;
        }

        Color color = overlayBaseColor;
        color.a *= scale;
        cooldownOverlay.color = color;
    }

    /// <summary>게이지형 궁극기 표시. current/max 비율만큼 gaugeFillImage가 차오른다(0=빈 상태,
    /// 1=가득 참). cooldownOverlay를 "남은 시간만큼 줄어드는" 방식으로 재활용하던 이전 방식과
    /// 달리, 실제로 게이지가 차오르는 걸 그대로 보여준다.</summary>
    public void UpdateGauge(float current, float max)
    {
        if (gaugeFillImage != null)
            gaugeFillImage.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        // 게이지 모드로 갱신 중이면 쿨타임 오버레이/텍스트/충전 숫자는 관여하지 않는 상태이므로 비운다.
        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = 0f;
        SetOverlayAlphaScale(1f);
        if (remainingText != null)
            remainingText.text = string.Empty;
        if (chargeCountText != null)
            chargeCountText.text = string.Empty;
        SetChargeCountActive(false);
    }

    private void SetChargeCountActive(bool active)
    {
        if (chargeCountText == null)
            return;

        // 숫자 텍스트가 슬롯 루트와 같은 오브젝트면 SetActive(false)가 슬롯 전체를 숨기게 되므로
        // 그 경우엔 컴포넌트만 켜고 끈다.
        if (chargeCountText.gameObject == gameObject)
        {
            chargeCountText.enabled = active;
            return;
        }

        if (chargeCountText.gameObject.activeSelf != active)
            chargeCountText.gameObject.SetActive(active);
    }

    /// <summary>이 슬롯의 아이콘을 바꾼다. null이면 빈 채로 둔다(그 액션을 안 쓰는 캐릭터 등).</summary>
    public void SetIcon(Sprite icon)
    {
        if (iconImage == null)
            return;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
    }
}
