using System;
using UnityEngine;

/// <summary>
/// 캐릭터 이동, 점프, 대시, 땅 체크를 담당하는 공통 모듈이다.
///
/// [역할]
///   - CharacterBase에서 받은 입력 상태를 바탕으로 Rigidbody2D를 움직인다.
///   - 점프 버퍼와 코요테 타임을 처리해서 조작감을 부드럽게 만든다.
///   - 기본 대시 함수를 제공해서 자식 캐릭터가 Dash에서 쉽게 호출할 수 있게 한다.
///   - 방향 전환은 루트 Transform의 scale.x를 바꾸는 기존 프로젝트 방식을 따른다.
///
/// [Fusion 이전 메모]
///   - 모든 시뮬레이션은 CharacterBase.FixedUpdateNetwork() 에서 호출되며,
///     deltaTime 으로 Runner.DeltaTime 을 받는다(Unity 의 Time.fixedDeltaTime 대신).
///   - 대시는 코루틴 대신 틱 기반 타이머로 처리해 네트워크 시뮬레이션과 정확히 맞춘다.
///   - 이 클래스는 MonoBehaviour가 아니므로 프리팹에 붙이지 않는다.
/// </summary>
public class CharacterMovementHandler
{
    public event Action Jumped;

    public bool IsGrounded { get; private set; }
    public bool IsDashing { get; private set; }
    public int FacingDirection { get; private set; } = 1;
    public float MoveInput { get; private set; }
    public Vector2 CurrentVelocity { get; private set; }

    private readonly Rigidbody2D rb;
    private readonly Collider2D col;
    private readonly Transform rootTransform;
    private readonly CharacterMovementData data;

    private float jumpBufferTimer;
    private float coyoteTimer;
    private float originalGravityScale;
    private bool playerJumping;

    // 틱 기반 대시 상태
    private float dashTimer;
    private Vector2 dashVelocity;

    public CharacterMovementHandler(Rigidbody2D rb, Collider2D col, Transform rootTransform, CharacterMovementData data)
    {
        this.rb = rb;
        this.col = col;
        this.rootTransform = rootTransform;
        this.data = data;

        rb.freezeRotation = true;
        originalGravityScale = rb.gravityScale;
        CurrentVelocity = rb.linearVelocity;
    }

    /// <summary>네트워크 틱마다 호출. deltaTime 은 Runner.DeltaTime.</summary>
    public void TickFixed(CharacterInputState inputState, float deltaTime)
    {
        CheckGround();
        UpdateMoveInput(inputState.MoveDirection);
        TickDash(deltaTime);
        Move(deltaTime);
        Jump(inputState.JumpPressed, deltaTime);
        ApplyBetterGravity(inputState.JumpHeld, deltaTime);
        ClampSpeed();
        CurrentVelocity = rb.linearVelocity;
    }

    public void StartDefaultDash()
    {
        StartDash(new Vector2(FacingDirection, 0f), data.defaultDashPower, data.defaultDashDuration);
    }

    public void StartDash(Vector2 direction, float power, float duration)
    {
        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : new Vector2(FacingDirection, 0f);
        dashVelocity = dir * power;
        dashTimer = duration;
        IsDashing = true;
        rb.gravityScale = 0f;
    }

    public void ApplyNetworkVisualState(int facingDirection, float moveInput, bool isGrounded, Vector2 velocity)
    {
        FacingDirection = facingDirection;
        MoveInput = moveInput;
        IsGrounded = isGrounded;
        CurrentVelocity = velocity;
    }

    private void TickDash(float deltaTime)
    {
        if (!IsDashing)
            return;

        rb.linearVelocity = dashVelocity;
        dashTimer -= deltaTime;

        if (dashTimer <= 0f)
        {
            IsDashing = false;
            rb.gravityScale = originalGravityScale;
        }
    }

    private void UpdateMoveInput(float moveInput)
    {
        MoveInput = moveInput;

        if (MoveInput > 0.01f)
            FacingDirection = 1;
        else if (MoveInput < -0.01f)
            FacingDirection = -1;
    }

    private void CheckGround()
    {
        Bounds bounds = col.bounds;
        Vector2 size = new Vector2(bounds.size.x * 0.9f, 0.05f);
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y - 0.025f);

        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, data.groundCheckDistance, data.groundLayer);
        IsGrounded = hit.collider != null && rb.linearVelocity.y <= 0.01f;
    }

    private void Move(float deltaTime)
    {
        if (IsDashing)
            return;

        float targetX = MoveInput * data.moveSpeed;
        float acceleration = IsGrounded ? data.groundAcceleration : data.airAcceleration;
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, acceleration * deltaTime);

        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    private void Jump(bool jumpPressed, float deltaTime)
    {
        if (IsGrounded)
            coyoteTimer = data.coyoteTime;
        else
            coyoteTimer -= deltaTime;

        if (jumpPressed)
            jumpBufferTimer = data.jumpBufferTime;
        else
            jumpBufferTimer -= deltaTime;

        if (jumpBufferTimer <= 0f || coyoteTimer <= 0f)
            return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * data.jumpForce, ForceMode2D.Impulse);

        // 방금 땅을 떠났으므로 즉시 접지 해제한다.
        // (이 틱 뒤에 도는 자동 통통점프(NetworkHop)가 stale 한 IsGrounded 를 보고
        //  점프 속도를 0 으로 지워버려 점프가 약해지는 문제를 막는다.)
        IsGrounded = false;

        playerJumping = true;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        Jumped?.Invoke();
    }

    private void ApplyBetterGravity(bool jumpHeld, float deltaTime)
    {
        if (IsDashing)
            return;

        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y *
                                 (data.fallGravityMultiplier - 1f) * deltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !jumpHeld && playerJumping)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y *
                                 (data.lowJumpMultiplier - 1f) * deltaTime;
        }
    }

    private void ClampSpeed()
    {
        if (rb.linearVelocity.sqrMagnitude > data.maxSpeed * data.maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * data.maxSpeed;
    }
}
