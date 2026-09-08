using System;

namespace ProjectMS.CharacterSystem
{
    /// <summary>일정 시간 동안 사용할 수 없게 막을 캐릭터 조작을 지정한다.</summary>
    [Flags]
    public enum CharacterControlType
    {
        None = 0,
        Movement = 1 << 0,
        Jump = 1 << 1,
        BasicAttack = 1 << 2,
        SkillQ = 1 << 3,
        SkillE = 1 << 4,
        Dash = 1 << 5,
        Ultimate = 1 << 6,
        AllActions = BasicAttack | SkillQ | SkillE | Dash | Ultimate,
        All = Movement | Jump | AllActions
    }
}
