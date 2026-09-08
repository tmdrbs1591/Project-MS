using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    /// <summary>입력 봉인과 조준 변환에 공통으로 사용하는 규칙을 제공한다.</summary>
    public static class CharacterControlRules
    {
        public const int SealSlotCount = 7;
        public const int InputSequenceCount = 6;

        public static bool Contains(CharacterControlType mask, CharacterControlType control)
        {
            return control != CharacterControlType.None && (mask & control) == control;
        }

        public static CharacterInputSnapshot FilterInput(
            CharacterInputSnapshot input,
            CharacterControlType sealedControls)
        {
            if (Contains(sealedControls, CharacterControlType.Movement))
                input.MoveDirection = 0f;

            if (Contains(sealedControls, CharacterControlType.Jump))
            {
                input.JumpPressed = false;
                input.JumpHeld = false;
            }

            if (Contains(sealedControls, CharacterControlType.BasicAttack))
                input.BasicAttackPressed = false;
            if (Contains(sealedControls, CharacterControlType.SkillQ))
                input.SkillQPressed = false;
            if (Contains(sealedControls, CharacterControlType.SkillE))
                input.SkillEPressed = false;
            if (Contains(sealedControls, CharacterControlType.Dash))
                input.DashPressed = false;
            if (Contains(sealedControls, CharacterControlType.Ultimate))
                input.UltimatePressed = false;

            return input;
        }

        public static bool ShouldReplaceTimedEffect(float? currentRemaining, float requestedDuration)
        {
            return IsFinitePositive(requestedDuration) &&
                   (!currentRemaining.HasValue || requestedDuration >= currentRemaining.Value);
        }

        public static Vector2 TransformAim(
            Vector2 origin,
            Vector2 rawAimWorldPosition,
            bool invert,
            float angleOffsetDegrees)
        {
            Vector2 direction = rawAimWorldPosition - origin;
            if (direction.sqrMagnitude < 0.0001f)
                return rawAimWorldPosition;

            if (invert)
                direction = -direction;

            if (!Mathf.Approximately(angleOffsetDegrees, 0f))
            {
                float radians = angleOffsetDegrees * Mathf.Deg2Rad;
                float sin = Mathf.Sin(radians);
                float cos = Mathf.Cos(radians);
                direction = new Vector2(
                    direction.x * cos - direction.y * sin,
                    direction.x * sin + direction.y * cos);
            }

            return origin + direction;
        }

        public static int GetSealSlot(CharacterControlType control)
        {
            return control switch
            {
                CharacterControlType.Movement => 0,
                CharacterControlType.Jump => 1,
                CharacterControlType.BasicAttack => 2,
                CharacterControlType.SkillQ => 3,
                CharacterControlType.SkillE => 4,
                CharacterControlType.Dash => 5,
                CharacterControlType.Ultimate => 6,
                _ => -1
            };
        }

        public static CharacterControlType GetControlForSlot(int slot)
        {
            return slot switch
            {
                0 => CharacterControlType.Movement,
                1 => CharacterControlType.Jump,
                2 => CharacterControlType.BasicAttack,
                3 => CharacterControlType.SkillQ,
                4 => CharacterControlType.SkillE,
                5 => CharacterControlType.Dash,
                6 => CharacterControlType.Ultimate,
                _ => CharacterControlType.None
            };
        }

        public static bool IsDefinedInput(CharacterInputType input)
        {
            int index = (int)input;
            return index >= 0 && index < InputSequenceCount;
        }

        public static bool IsFinitePositive(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static bool IsFinite(Vector2 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                   !float.IsNaN(value.y) && !float.IsInfinity(value.y);
        }
    }
}
