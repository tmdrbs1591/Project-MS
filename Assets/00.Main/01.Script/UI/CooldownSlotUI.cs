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
/// </summary>
public class CooldownSlotUI : MonoBehaviour
{
    [SerializeField] private CharacterActionType actionType;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TMP_Text remainingText;

    public CharacterActionType ActionType => actionType;

    public void UpdateCooldown(float remaining, float total)
    {
        bool onCooldown = total > 0f && remaining > 0f;

        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = onCooldown ? remaining / total : 0f;

        if (remainingText != null)
            remainingText.text = onCooldown ? Mathf.CeilToInt(remaining).ToString() : string.Empty;
    }
}
