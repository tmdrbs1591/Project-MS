using UnityEngine;

/// <summary>
/// Photon으로 받은 원격 캐릭터 상태를 보관하는 데이터다.
///
/// [역할]
///   - 위치, 속도, 바라보는 방향, 이동 입력, 땅 착지 여부를 한 묶음으로 저장한다.
///   - CharacterBase가 여러 상태를 버퍼에 쌓아두고 과거 시점 기준으로 보간한다.
///   - 원격 플레이어가 끊겨 보이지 않도록 부드러운 표시를 돕는다.
///
/// 이 구조체는 네트워크 데이터 보관용이며 프리팹에 붙이지 않는다.
/// </summary>
public struct CharacterNetworkState
{
    public double Time;
    public Vector3 Position;
    public Vector2 Velocity;
    public int FacingDirection;
    public float MoveInput;
    public bool IsGrounded;
}
