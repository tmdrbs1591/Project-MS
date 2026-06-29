using UnityEngine;

/// <summary>
/// 이번 프레임에 읽은 캐릭터 입력 상태다.
///
/// [역할]
///   - CharacterInputHandler가 키보드/마우스 입력을 읽어서 이 구조체에 담는다.
///   - CharacterBase는 이 값을 보고 어떤 행동을 실행할지 결정한다.
///   - CharacterMovementHandler는 이동 방향과 점프 입력만 받아 물리를 처리한다.
///
/// 입력 장치에 직접 접근하는 코드는 CharacterInputHandler 안에만 두는 것을 권장한다.
/// </summary>
public struct CharacterInputState
{
    public float MoveDirection;
    public bool JumpPressed;
    public bool JumpHeld;
    public bool BasicAttackPressed;
    public bool SkillQPressed;
    public bool SkillEPressed;
    public bool DashPressed;
    public bool UltimatePressed;
    public Vector2 MouseWorldPosition;
}
