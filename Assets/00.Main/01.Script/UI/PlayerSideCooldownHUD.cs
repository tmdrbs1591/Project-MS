using ProjectMS.CharacterSystem;
using UnityEngine;

/// <summary>
/// 좌/우 코너(P1/P2) 자리에 고정된 스킬 쿨타임 HUD의 공통 로직이다.
/// PlayerCornerHUD(HP/이름/승수)와 같은 기준으로 캐릭터를 고른다 — "나/상대"가 아니라
/// MatchManager의 P1/P2 순서(PlayerId가 작은 쪽이 P1)를 따라서, 한쪽 코너의 HP·승수·스킬이
/// 항상 같은 캐릭터를 가리키게 한다.
///
/// [캐릭터 찾기]
///   - Bind/Unbind 같은 이벤트 대신 매 프레임 폴링한다(PlayerCornerHUD/WorldHealthBarUI와
///     같은 이유: 원격 캐릭터의 스폰/리플리케이션 타이밍에 의존하지 않기 위함).
///   - 캐릭터의 Definition이 바뀌면(스폰/재스폰) 슬롯 아이콘을 다시 채운다.
///
/// [씬 설정]
///   - 이 클래스를 상속한 CooldownHUD(P1, 왼쪽) / OpponentCooldownHUD(P2, 오른쪽)를 각 코너에 붙인다.
///   - 하위에 CooldownSlotUI를 행동 종류 수만큼 만들어 slots 배열에 연결한다.
///
/// [슬롯 표시 방식]
///   - 게이지형 궁극기(CharacterDefinition.UltimateUsesGauge)는 차오르는 Filled 이미지(UpdateGauge).
///   - 충전형 스킬(예: Zipper Q)은 남은 충전 수 + 다음 충전까지의 오버레이(UpdateCharges).
///   - 그 외는 시간 기반 쿨타임(UpdateCooldown).
/// </summary>
public abstract class PlayerSideCooldownHUD : MonoBehaviour
{
    [SerializeField] private CooldownSlotUI[] slots;

    private CharacterBase character;
    private CharacterDefinition boundDefinition;

    /// <summary>true면 P1(왼쪽) 캐릭터, false면 P2(오른쪽) 캐릭터를 보여준다.</summary>
    protected abstract bool IsPlayer1Side { get; }

    private void LateUpdate()
    {
        if (slots == null)
            return;

        UpdateCharacterRef();

        if (character == null || character.Cooldowns == null)
            return;

        // 이벤트 시점이 없으므로 Definition 참조가 바뀌었는지로 캐릭터 교체를 감지해서 아이콘을 갱신한다.
        if (character.Definition != boundDefinition)
        {
            boundDefinition = character.Definition;
            foreach (CooldownSlotUI slot in slots)
                slot.SetIcon(boundDefinition != null ? boundDefinition.GetIcon(slot.ActionType) : null);
        }

        foreach (CooldownSlotUI slot in slots)
        {
            if (slot.ActionType == CharacterActionType.Ultimate && character.IsUltimateGaugeMode)
            {
                slot.UpdateGauge(character.UltimateGaugeCurrent, character.UltimateGaugeMax);
                continue;
            }

            int maxCharges = character.GetChargeCapacity(slot.ActionType);
            if (maxCharges > 0)
            {
                slot.UpdateCharges(
                    character.GetCurrentCharges(slot.ActionType),
                    maxCharges,
                    character.GetChargeRechargeRemaining(slot.ActionType),
                    character.GetChargeRechargeDuration(slot.ActionType));
                continue;
            }

            float remaining = character.Cooldowns.GetRemaining(slot.ActionType);
            float total = character.Cooldowns.GetDuration(slot.ActionType);
            slot.UpdateCooldown(remaining, total);
        }
    }

    private void UpdateCharacterRef()
    {
        CharacterBase found = FindCharacterForSide();
        if (found == character)
            return;

        character = found;

        if (character != null)
            return;

        // 대상이 사라지면(세션 종료 등) 아이콘/쿨타임 표시를 비운다.
        boundDefinition = null;
        foreach (CooldownSlotUI slot in slots)
        {
            slot.SetIcon(null);
            slot.UpdateCooldown(0f, 0f);
        }
    }

    // PlayerCornerHUD와 같은 기준: MatchManager의 P1/P2 순서. 봇은 MatchPlayer에 가상 번호가 들어 있다.
    private CharacterBase FindCharacterForSide()
    {
        MatchManager match = MatchManager.Instance;
        if (match == null)
            return null;

        foreach (CharacterBase candidate in CharacterBase.All)
        {
            if (candidate == null || candidate.Object == null)
                continue;

            if (match.IsPlayer1(candidate.MatchPlayer) == IsPlayer1Side)
                return candidate;
        }

        return null;
    }
}
