using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectMS.CharacterSystem
{
    /// <summary>
    /// Unity Input System의 프레임 입력을 수집하고 짧은 버튼 입력을 다음 Fusion 틱까지 보관한다.
    /// 네트워크 상태를 직접 변경하지 않는다.
    /// </summary>
    public sealed class CharacterInputHandler
    {
        private readonly CharacterDefinition definition;

        private float moveDirection;
        private bool jumpHeld;
        private bool jumpPressed;
        private bool basicAttackPressed;
        private bool skillQPressed;
        private bool skillEPressed;
        private bool dashPressed;
        private bool ultimatePressed;
        private Vector2 aimWorldPosition;

        public CharacterInputHandler(CharacterDefinition definition)
        {
            this.definition = definition;
        }

        public void CaptureFrame(Camera targetCamera, Vector3 characterPosition)
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            moveDirection = 0f;
            if (keyboard != null)
            {
                if (IsHeld(keyboard, definition.MoveLeft))
                    moveDirection -= 1f;
                if (IsHeld(keyboard, definition.MoveRight))
                    moveDirection += 1f;

                jumpHeld = IsHeld(keyboard, definition.Jump);
                jumpPressed |= WasPressed(keyboard, definition.Jump);
                skillQPressed |= WasPressed(keyboard, definition.SkillQ);
                skillEPressed |= WasPressed(keyboard, definition.SkillE);
                dashPressed |= WasPressed(keyboard, definition.Dash);
                ultimatePressed |= WasPressed(keyboard, definition.Ultimate);
            }

            if (mouse != null)
                basicAttackPressed |= mouse.leftButton.wasPressedThisFrame;

            aimWorldPosition = ReadMouseWorldPosition(targetCamera, characterPosition, mouse);
        }

        public CharacterInputSnapshot ConsumeTick()
        {
            CharacterInputSnapshot snapshot = new CharacterInputSnapshot
            {
                MoveDirection = moveDirection,
                JumpPressed = jumpPressed,
                JumpHeld = jumpHeld,
                BasicAttackPressed = basicAttackPressed,
                SkillQPressed = skillQPressed,
                SkillEPressed = skillEPressed,
                DashPressed = dashPressed,
                UltimatePressed = ultimatePressed,
                AimWorldPosition = aimWorldPosition
            };

            jumpPressed = false;
            basicAttackPressed = false;
            skillQPressed = false;
            skillEPressed = false;
            dashPressed = false;
            ultimatePressed = false;
            return snapshot;
        }

        private static bool IsHeld(Keyboard keyboard, Key key)
        {
            return key != Key.None && keyboard[key].isPressed;
        }

        private static bool WasPressed(Keyboard keyboard, Key key)
        {
            return key != Key.None && keyboard[key].wasPressedThisFrame;
        }

        private static Vector2 ReadMouseWorldPosition(Camera targetCamera, Vector3 fallback, Mouse mouse)
        {
            if (targetCamera == null || mouse == null)
                return fallback;

            Vector3 screen = mouse.position.ReadValue();
            screen.z = Mathf.Abs(targetCamera.transform.position.z - fallback.z);
            return targetCamera.ScreenToWorldPoint(screen);
        }
    }
}
