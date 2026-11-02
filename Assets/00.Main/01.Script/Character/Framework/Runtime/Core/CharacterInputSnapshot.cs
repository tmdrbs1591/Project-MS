using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    public struct CharacterInputSnapshot
    {
        public float MoveDirection;
        public bool JumpPressed;
        public bool JumpHeld;
        public bool BasicAttackPressed;
        /// <summary>기본공격 버튼을 지금 누르고 있는지(꾹 눌러 모으는 공격용).</summary>
        public bool BasicAttackHeld;
        public bool SkillQPressed;
        public bool SkillEPressed;
        public bool DashPressed;
        public bool UltimatePressed;
        public bool ReloadPressed;
        public Vector2 AimWorldPosition;
    }
}
