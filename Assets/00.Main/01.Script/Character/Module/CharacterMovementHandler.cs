using System;
using System.Collections;
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
/// [주의]
///   - 이 클래스는 MonoBehaviour가 아니므로 프리팹에 붙이지 않는다.
///   - 슬라임 통통점프처럼 특정 캐릭터만 쓰는 움직임은 SlimeAutoHop 같은 별도 스크립트가 담당한다.
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
    private Coroutine dashRoutine;
    private MonoBehaviour coroutineRunner;
    private Vector3 baseScale;

    public CharacterMovementHandler(Rigidbody2D rb, Collider2D col, Transform rootTransform, CharacterMovementData data)
    {
        this.rb = rb;
        this.col = col;
        this.rootTransform = rootTransform;
        this.data = data;

        rb.freezeRotation = true;
        originalGravityScale = rb.gravityScale;
        baseScale = rootTransform.localScale;
        CurrentVelocity = rb.linearVelocity;
    }

    public void TickFixed(CharacterInputState inputState)
    {
        CheckGround();
        UpdateMoveInput(inputState.MoveDirection);
        Move();
        Jump(inputState.JumpPressed);
        ApplyBetterGravity(inputState.JumpHeld);
        ClampSpeed();
        CurrentVelocity = rb.linearVelocity;
    }

    public void StartDefaultDash(MonoBehaviour runner)
    {
        StartDash(runner, new Vector2(FacingDirection, 0f), data.defaultDashPower, data.defaultDashDuration);
    }

    public void StartDash(MonoBehaviour runner, Vector2 direction, float power, float duration)
    {
        coroutineRunner = runner;

        if (dashRoutine != null)
            coroutineRunner.StopCoroutine(dashRoutine);

        dashRoutine = coroutineRunner.StartCoroutine(DashRoutine(direction.normalized, power, duration));
    }

    public void ApplyNetworkVisualState(int facingDirection, float moveInput, bool isGrounded, Vector2 velocity)
    {
        FacingDirection = facingDirection;
        MoveInput = moveInput;
        IsGrounded = isGrounded;
        CurrentVelocity = velocity;
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

    private void Move()
    {
        if (IsDashing)
            return;

        float targetX = MoveInput * data.moveSpeed;
        float acceleration = IsGrounded ? data.groundAcceleration : data.airAcceleration;
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, acceleration * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    private void Jump(bool jumpPressed)
    {
        if (IsGrounded)
            coyoteTimer = data.coyoteTime;
        else
            coyoteTimer -= Time.fixedDeltaTime;

        if (jumpPressed)
            jumpBufferTimer = data.jumpBufferTime;
        else
            jumpBufferTimer -= Time.fixedDeltaTime;

        if (jumpBufferTimer <= 0f || coyoteTimer <= 0f)
            return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * data.jumpForce, ForceMode2D.Impulse);

        playerJumping = true;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        Jumped?.Invoke();
    }

    private void ApplyBetterGravity(bool jumpHeld)
    {
        if (IsDashing)
            return;

        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y *
                                 (data.fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !jumpHeld && playerJumping)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y *
                                 (data.lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    private void ClampSpeed()
    {
        if (rb.linearVelocity.sqrMagnitude > data.maxSpeed * data.maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * data.maxSpeed;
    }

    private IEnumerator DashRoutine(Vector2 direction, float power, float duration)
    {
        IsDashing = true;
        rb.gravityScale = 0f;
        rb.linearVelocity = direction * power;

        yield return new WaitForSeconds(duration);

        rb.gravityScale = originalGravityScale;
        IsDashing = false;
        dashRoutine = null;
    }
}
