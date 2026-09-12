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
/// </summary>
public class CooldownSlotUI : MonoBehaviour
{
    [SerializeField] private CharacterActionType actionType;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TMP_Text remainingText;
    [SerializeField] private Image gaugeFillImage;

    public CharacterActionType ActionType => actionType;

    /// <summary>시간 기반 쿨타임 표시. 게이지형 궁극기 슬롯에는 UpdateGauge()를 대신 쓴다.</summary>
    public void UpdateCooldown(float remaining, float total)
    {
        bool onCooldown = total > 0f && remaining > 0f;

        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = onCooldown ? remaining / total : 0f;

        if (remainingText != null)
            remainingText.text = onCooldown ? Mathf.CeilToInt(remaining).ToString() : string.Empty;

        // 쿨타임 모드로 갱신 중이면 게이지 바는 관여하지 않는 상태이므로 비워서 잔상이 안 남게 한다.
        if (gaugeFillImage != null)
            gaugeFillImage.fillAmount = 0f;
    }

    /// <summary>게이지형 궁극기 표시. current/max 비율만큼 gaugeFillImage가 차오른다(0=빈 상태,
    /// 1=가득 참). cooldownOverlay를 "남은 시간만큼 줄어드는" 방식으로 재활용하던 이전 방식과
    /// 달리, 실제로 게이지가 차오르는 걸 그대로 보여준다.</summary>
    public void UpdateGauge(float current, float max)
    {
        if (gaugeFillImage != null)
            gaugeFillImage.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        // 게이지 모드로 갱신 중이면 쿨타임 오버레이/텍스트는 관여하지 않는 상태이므로 비운다.
        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = 0f;
        if (remainingText != null)
            remainingText.text = string.Empty;
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
