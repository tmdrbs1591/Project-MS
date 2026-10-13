using System;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    /// <summary>캐릭터 선택 UI에 보여줄 스킬 하나의 이름과 설명.</summary>
    [Serializable]
    public struct CharacterSkillInfo
    {
        public string name;
        [TextArea(2, 4)] public string description;
    }
}
