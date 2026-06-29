using System;

/// <summary>
/// 캐릭터 행동별 쿨타임 수치 데이터다.
///
/// [역할]
///   - 평타, Q, E, 대시, 궁극기 쿨타임을 관리한다.
///   - CharacterCooldownHandler가 이 값을 사용해서 스킬 사용 가능 여부를 판단한다.
///   - 캐릭터마다 쿨타임이 다르면 Playable 캐릭터 Inspector에서 값을 조정한다.
///
/// 이 클래스는 MonoBehaviour가 아니며 CharacterBase의 Inspector에서 데이터로 사용된다.
/// </summary>
[Serializable]
public class CharacterCooldownData
{
    public float basicAttack = 0.35f;
    public float skillQ = 3f;
    public float skillE = 5f;
    public float dash = 1f;
    public float ultimate = 20f;

    public float GetCooldown(CharacterActionType actionType)
    {
        return actionType switch
        {
            CharacterActionType.BasicAttack => basicAttack,
            CharacterActionType.SkillQ => skillQ,
            CharacterActionType.SkillE => skillE,
            CharacterActionType.Dash => dash,
            CharacterActionType.Ultimate => ultimate,
            _ => 0f
        };
    }
}
