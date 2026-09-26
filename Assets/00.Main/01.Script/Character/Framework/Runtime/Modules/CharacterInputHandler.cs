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
        private bool reloadPressed;
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
            {
                basicAttackPressed |= mouse.leftButton.wasPressedThisFrame;
                // 재장전: 휠을 굴리거나(위/아래 아무 쪽) 휠 버튼을 누르면.
                reloadPressed |= Mathf.Abs(mouse.scroll.ReadValue().y) > 0.01f || mouse.middleButton.wasPressedThisFrame;
            }

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
                ReloadPressed = reloadPressed,
                AimWorldPosition = aimWorldPosition
            };

            reloadPressed = false;
            jumpPressed = false;
            basicAttackPressed = false;
            skillQPressed = false;
            skillEPressed = false;
            dashPressed = false;
            ultimatePressed = false;
            return snapshot;
        }

        public void ClearGameplayInput()
        {
            moveDirection = 0f;
            jumpHeld = false;
            jumpPressed = false;
            basicAttackPressed = false;
            skillQPressed = false;
            skillEPressed = false;
            dashPressed = false;
            ultimatePressed = false;
            reloadPressed = false;
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
