using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 현재 키 설정을 기준으로 키보드와 마우스 입력을 읽는 모듈이다.
///
/// [역할]
///   - AD 이동, Space 점프, 마우스 평타, Q/E/Shift/R 스킬 입력을 읽는다.
///   - 키가 바뀌어도 CharacterBase나 캐릭터별 스킬 코드는 수정하지 않게 만든다.
///   - 읽은 결과는 CharacterInputState로 반환한다.
///
/// [주의]
///   - 이 클래스는 MonoBehaviour가 아니므로 프리팹에 붙이지 않는다.
///   - CharacterBase가 내부에서 생성해서 사용한다.
/// </summary>
public class CharacterInputHandler
{
    public CharacterInputState CurrentState { get; private set; }

    private CharacterKeySetting keySetting;
    private bool jumpPendingFixed;

    public CharacterInputHandler(CharacterKeySetting keySetting)
    {
        this.keySetting = keySetting;
    }

    public void SetKeySetting(CharacterKeySetting newKeySetting)
    {
        keySetting = newKeySetting;
    }

    // FixedUpdate에서 호출. 누적된 점프 입력을 소비하고 올바른 상태를 반환한다.
    public CharacterInputState ReadFixed()
    {
        CharacterInputState state = CurrentState;
        state.JumpPressed = jumpPendingFixed;
        jumpPendingFixed = false;
        return state;
    }

    public CharacterInputState Read()
    {
        CharacterInputState state = new CharacterInputState();

        if (IsKeyPressed(keySetting.moveLeft))
            state.MoveDirection -= 1f;

        if (IsKeyPressed(keySetting.moveRight))
            state.MoveDirection += 1f;

        bool jumpThisFrame = WasKeyPressedThisFrame(keySetting.jump);
        if (jumpThisFrame)
            jumpPendingFixed = true;

        state.JumpPressed = jumpThisFrame;
        state.JumpHeld = IsKeyPressed(keySetting.jump);
        state.BasicAttackPressed = WasMousePressedThisFrame(keySetting.basicAttack);
        state.MouseWorldPosition = ReadMouseWorldPosition();
        state.SkillQPressed = WasKeyPressedThisFrame(keySetting.skillQ);
        state.SkillEPressed = WasKeyPressedThisFrame(keySetting.skillE);
        state.DashPressed = WasKeyPressedThisFrame(keySetting.dash);
        state.UltimatePressed = WasKeyPressedThisFrame(keySetting.ultimate);

        CurrentState = state;
        return state;
    }

    private bool IsKeyPressed(Key key)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && key != Key.None && keyboard[key].isPressed;
    }

    private bool WasKeyPressedThisFrame(Key key)
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && key != Key.None && keyboard[key].wasPressedThisFrame;
    }

    private Vector2 ReadMouseWorldPosition()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || Camera.main == null)
            return Vector2.zero;

        Vector3 screenPos = mouse.position.ReadValue();
        screenPos.z = -Camera.main.transform.position.z;
        return Camera.main.ScreenToWorldPoint(screenPos);
    }

    private bool WasMousePressedThisFrame(CharacterMouseButton button)
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return false;

        return button switch
        {
            CharacterMouseButton.Left => mouse.leftButton.wasPressedThisFrame,
            CharacterMouseButton.Right => mouse.rightButton.wasPressedThisFrame,
            CharacterMouseButton.Middle => mouse.middleButton.wasPressedThisFrame,
            _ => false
        };
    }
}
