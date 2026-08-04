using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    public struct CharacterInputSnapshot
    {
        public float MoveDirection;
        public bool JumpPressed;
        public bool JumpHeld;
        public bool BasicAttackPressed;
        public bool SkillQPressed;
        public bool SkillEPressed;
        public bool DashPressed;
        public bool UltimatePressed;
        public Vector2 AimWorldPosition;
    }
}
