using ProjectMS.CharacterSystem;
using UnityEngine;

/// <summary>
/// 상대(원격) 플레이어의 스킬 쿨타임을 보여주는 HUD다.
///
/// CooldownHUD는 CharacterBase.Spawned() 시점에 Bind()를 명시적으로 호출받는 방식인데,
/// 이건 "로컬 플레이어 자기 자신"이라 스폰 즉시 값을 신뢰할 수 있어서 성립하는 방식이다.
/// 상대 캐릭터는 원격 오브젝트라 리플리케이션 타이밍 문제가 있을 수 있으므로,
/// PlayerCornerHUD/WorldHealthBarUI와 같은 이유로 Bind 대신 매 프레임 폴링해서
/// "로컬이 아닌 캐릭터"를 찾는 방식을 쓴다.
///
/// [씬 설정]
///   - CooldownHUD와 동일하게 CooldownSlotUI를 슬롯 수만큼(평타/Q/E/대시/궁) 만들어
///     slots 배열에 연결한다. 위치만 다르게(예: 화면 반대편/상대 쪽 UI 영역) 배치하면 된다.
///   - 씬에는 하나만 존재하면 된다.
/// </summary>
public class OpponentCooldownHUD : MonoBehaviour
{
    [SerializeField] private CooldownSlotUI[] slots;

    private CharacterBase character;
    private CharacterDefinition boundDefinition;

    private void LateUpdate()
    {
        UpdateCharacterRef();

        if (character == null || character.Cooldowns == null)
            return;

        // Bind() 호출 시점이 따로 없으므로, Definition 참조가 바뀌었는지로 캐릭터 교체를 감지해서
        // 아이콘을 갱신한다(상대가 바뀌는 경우는 실제로 없지만 재접속/재스폰 대비).
        if (character.Definition != boundDefinition)
        {
            boundDefinition = character.Definition;
            foreach (CooldownSlotUI slot in slots)
                slot.SetIcon(boundDefinition != null ? boundDefinition.GetIcon(slot.ActionType) : null);
        }

        foreach (CooldownSlotUI slot in slots)
        {
            // CooldownHUD와 동일: 게이지형 궁극기는 실제로 차오르는 Filled 이미지로 보여준다.
            if (slot.ActionType == CharacterActionType.Ultimate && character.IsUltimateGaugeMode)
            {
                slot.UpdateGauge(character.UltimateGaugeCurrent, character.UltimateGaugeMax);
                continue;
            }

            float remaining = character.Cooldowns.GetRemaining(slot.ActionType);
            float total = character.Cooldowns.GetDuration(slot.ActionType);
            slot.UpdateCooldown(remaining, total);
        }
    }

    private void UpdateCharacterRef()
    {
        CharacterBase found = null;

        foreach (CharacterBase candidate in CharacterBase.All)
        {
            if (candidate.Object == null || candidate.IsLocalPlayer)
                continue;

            found = candidate;
            break;
        }

        if (found == character)
            return;

        character = found;

        if (character == null)
        {
            boundDefinition = null;
            foreach (CooldownSlotUI slot in slots)
            {
                slot.SetIcon(null);
                slot.UpdateCooldown(0f, 0f);
            }
        }
    }
}
