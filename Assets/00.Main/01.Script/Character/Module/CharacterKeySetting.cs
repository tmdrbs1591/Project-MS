using System;
using UnityEngine.InputSystem;

/// <summary>
/// 캐릭터가 사용할 키 설정 데이터다.
///
/// [역할]
///   - 이동, 점프, 평타, 스킬, 대시, 궁극기 키를 한 곳에서 관리한다.
///   - 나중에 설정 UI에서 키를 바꾸면 CharacterBase.SetKeySetting으로 이 값을 넘긴다.
///   - 저장 방식은 담당하지 않는다. 저장은 별도의 KeySettingManager나 PlayerPrefs/JSON에서 처리한다.
///
/// [사용 예]
///   - 1P는 WASD + Q/E/R
///   - 2P는 방향키 + 다른 스킬 키
///   - 설정 UI에서 변경한 값을 현재 플레이어 캐릭터에 적용
///
/// 이 클래스는 MonoBehaviour가 아니므로 프리팹에 붙이지 않는다.
/// </summary>
[Serializable]
public class CharacterKeySetting
{
    public Key moveLeft = Key.A;
    public Key moveRight = Key.D;
    public Key jump = Key.Space;
    public CharacterMouseButton basicAttack = CharacterMouseButton.Left;
    public Key skillQ = Key.Q;
    public Key skillE = Key.E;
    public Key dash = Key.LeftShift;
    public Key ultimate = Key.R;
}

public enum CharacterMouseButton
{
    Left,
    Right,
    Middle
}
