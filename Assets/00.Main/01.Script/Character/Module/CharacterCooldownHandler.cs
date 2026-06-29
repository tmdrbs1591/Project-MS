using System.Collections.Generic;

/// <summary>
/// 캐릭터 행동별 쿨타임을 계산하는 모듈이다.
///
/// [역할]
///   - 각 행동이 지금 사용 가능한지 확인한다.
///   - 행동을 사용하면 해당 행동의 남은 쿨타임을 시작한다.
///   - 매 프레임 남은 쿨타임을 줄인다.
///
/// [주의]
///   - 이 클래스는 MonoBehaviour가 아니므로 프리팹에 붙이지 않는다.
///   - 스킬 효과 자체는 여기서 실행하지 않고, CharacterBase와 자식 캐릭터가 실행한다.
/// </summary>
public class CharacterCooldownHandler
{
    private readonly CharacterCooldownData data;
    private readonly Dictionary<CharacterActionType, float> timers = new Dictionary<CharacterActionType, float>();

    public CharacterCooldownHandler(CharacterCooldownData data)
    {
        this.data = data;
    }

    public bool CanUse(CharacterActionType actionType)
    {
        return GetRemainingTime(actionType) <= 0f;
    }

    public void Start(CharacterActionType actionType)
    {
        timers[actionType] = data.GetCooldown(actionType);
    }

    public void Tick(float deltaTime)
    {
        CharacterActionType[] keys = new CharacterActionType[timers.Keys.Count];
        timers.Keys.CopyTo(keys, 0);

        foreach (CharacterActionType key in keys)
        {
            timers[key] -= deltaTime;
            if (timers[key] < 0f)
                timers[key] = 0f;
        }
    }

    public float GetRemainingTime(CharacterActionType actionType)
    {
        return timers.TryGetValue(actionType, out float remainingTime) ? remainingTime : 0f;
    }
}
