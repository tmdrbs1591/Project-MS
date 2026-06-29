using System;

/// <summary>
/// 캐릭터 이동과 점프에 필요한 공통 수치 데이터다.
///
/// [역할]
///   - 이동 속도, 점프 힘, 지상/공중 가속도, 대시 수치 등을 관리한다.
///   - CharacterMovementHandler가 이 값을 사용해서 실제 Rigidbody2D를 움직인다.
///   - 캐릭터마다 조작감을 다르게 만들고 싶으면 캐릭터 Inspector에서 값을 조정한다.
///
/// [주의]
///   - 슬라임 통통점프처럼 특정 캐릭터만 쓰는 값은 여기에 넣지 않는다.
///   - 캐릭터 전용 특성은 SlimeAutoHop 같은 별도 스크립트로 분리한다.
/// </summary>
[Serializable]
public class CharacterMovementData
{
    public float moveSpeed = 7f;
    public float jumpForce = 14f;
    public float groundAcceleration = 60f;
    public float airAcceleration = 25f;
    public float fallGravityMultiplier = 2.2f;
    public float lowJumpMultiplier = 2.5f;
    public float jumpBufferTime = 0.15f;
    public float coyoteTime = 0.12f;
    public float maxSpeed = 18f;
    public float defaultDashPower = 18f;
    public float defaultDashDuration = 0.12f;
    public UnityEngine.LayerMask groundLayer = ~0;
    public float groundCheckDistance = 0.08f;
}
