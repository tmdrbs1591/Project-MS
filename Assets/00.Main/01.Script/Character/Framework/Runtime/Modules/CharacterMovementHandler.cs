using System;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    public sealed class CharacterMovementHandler
    {
        private readonly Rigidbody2D rigidbody;
        private readonly Collider2D collider;
        private readonly CharacterDefinition definition;

        private float jumpBufferTimer;
        private float coyoteTimer;
        private float dashTimer;
        private Vector2 dashVelocity;
        private float originalGravityScale;
        private bool playerJumping;
        private float autoHopTimer;

        public CharacterMovementHandler(
            Rigidbody2D rigidbody,
            Collider2D collider,
            CharacterDefinition definition)
        {
            this.rigidbody = rigidbody;
            this.collider = collider;
            this.definition = definition;

            originalGravityScale = rigidbody.gravityScale;
            rigidbody.freezeRotation = true;
            CurrentVelocity = rigidbody.linearVelocity;
        }

        public event Action Jumped;
        public event Action Landed;
        public event Action AutoHopped;

        public bool IsGrounded { get; private set; }
        public bool IsDashing { get; private set; }
        public bool MovementEnabled { get; private set; } = true;
        public float MovementSpeedMultiplier { get; private set; } = 1f;
        /// <summary>슬로우(MovementSpeedMultiplier)와는 별개로 곱해지는 지속형 배율(예: 신속의
        /// 신발 증강). 슬로우가 걸려도 증강 배율은 유지되고, 둘 다 곱해서 최종 속도가 나온다.</summary>
        public float BaseSpeedMultiplier { get; private set; } = 1f;
        public int FacingDirection { get; private set; } = 1;
        public float MoveInput { get; private set; }
        public Vector2 CurrentVelocity { get; private set; }

        public void Tick(CharacterInputSnapshot input, float deltaTime)
        {
            bool wasGrounded = IsGrounded;
            CheckGround();

            MoveInput = MovementEnabled ? Mathf.Clamp(input.MoveDirection, -1f, 1f) : 0f;
            if (MoveInput > 0.01f)
                FacingDirection = 1;
            else if (MoveInput < -0.01f)
                FacingDirection = -1;

            TickDash(deltaTime);
            Move(deltaTime);
            if (MovementEnabled)
            {
                Jump(input.JumpPressed, deltaTime);
                TickAutoHop(deltaTime);
            }
            ApplyBetterGravity(input.JumpHeld, deltaTime);
            ClampSpeed();

            CurrentVelocity = rigidbody.linearVelocity;
            if (!wasGrounded && IsGrounded)
            {
                playerJumping = false;
                Landed?.Invoke();
            }
        }

        public void StartDefaultDash()
        {
            StartDash(new Vector2(FacingDirection, 0f), definition.DefaultDashPower, definition.DefaultDashDuration);
        }

        public void SetMovementEnabled(bool enabled)
        {
            MovementEnabled = enabled;
            if (!enabled)
            {
                CancelDash();
                rigidbody.linearVelocity = new Vector2(0f, rigidbody.linearVelocity.y);
                jumpBufferTimer = 0f;
                autoHopTimer = 0f;
            }
        }

        public void SetMovementSpeedMultiplier(float multiplier)
        {
            MovementSpeedMultiplier = Mathf.Max(0f, multiplier);
        }

        public void SetBaseSpeedMultiplier(float multiplier)
        {
            BaseSpeedMultiplier = Mathf.Max(0f, multiplier);
        }

        public void CancelDash()
        {
            if (!IsDashing)
                return;

            IsDashing = false;
            dashTimer = 0f;
            rigidbody.gravityScale = originalGravityScale;
        }

        public void StartDash(Vector2 direction, float power, float duration)
        {
            if (!MovementEnabled)
                return;

            ForceVelocityOverride(direction, power, duration, new Vector2(FacingDirection, 0f));
        }

        /// <summary>피격 시 강제로 밀려나는 넉백. StartDash와 물리 구현은 동일하지만
        /// MovementEnabled 가드를 건너뛴다 — 맞는 건 자기 의지와 무관하게 항상 적용돼야 한다.
        /// 방향을 못 정할 수 없는 경우(원점 벡터 등)엔 현재 바라보는 방향의 반대(후방)로 민다.</summary>
        public void ApplyKnockback(Vector2 direction, float power, float duration)
        {
            ForceVelocityOverride(direction, power, duration, new Vector2(-FacingDirection, 0f));
        }

        private void ForceVelocityOverride(Vector2 direction, float power, float duration, Vector2 fallbackDirection)
        {
            Vector2 normalized = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : fallbackDirection.normalized;

            dashVelocity = normalized * Mathf.Max(0f, power);
            dashTimer = Mathf.Max(0.01f, duration);
            IsDashing = true;
            rigidbody.gravityScale = 0f;
        }

        public void ApplyRemoteVisualState(int facing, float moveInput, bool grounded, Vector2 velocity)
        {
            FacingDirection = facing >= 0 ? 1 : -1;
            MoveInput = moveInput;
            IsGrounded = grounded;
            CurrentVelocity = velocity;
        }

        public void Reset(Vector2 position)
        {
            rigidbody.position = position;
            rigidbody.linearVelocity = Vector2.zero;
            rigidbody.gravityScale = originalGravityScale;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            dashTimer = 0f;
            autoHopTimer = 0f;
            playerJumping = false;
            IsDashing = false;
            IsGrounded = false;
            CurrentVelocity = Vector2.zero;
        }

        private void CheckGround()
        {
            Bounds bounds = collider.bounds;
            Vector2 origin = new Vector2(bounds.center.x, bounds.min.y - 0.025f);
            Vector2 size = new Vector2(bounds.size.x * 0.9f, 0.05f);
            RaycastHit2D hit = Physics2D.BoxCast(
                origin,
                size,
                0f,
                Vector2.down,
                definition.GroundCheckDistance,
                definition.GroundLayer);

            IsGrounded = hit.collider != null && rigidbody.linearVelocity.y <= 0.01f;
        }

        private void Move(float deltaTime)
        {
            if (IsDashing)
                return;

            float targetX = MovementEnabled
                ? MoveInput * definition.MoveSpeed * BaseSpeedMultiplier * MovementSpeedMultiplier
                : 0f;
            float acceleration = IsGrounded ? definition.GroundAcceleration : definition.AirAcceleration;
            float nextX = Mathf.MoveTowards(rigidbody.linearVelocity.x, targetX, acceleration * deltaTime);
            rigidbody.linearVelocity = new Vector2(nextX, rigidbody.linearVelocity.y);
        }

        private void Jump(bool jumpPressed, float deltaTime)
        {
            coyoteTimer = IsGrounded ? definition.CoyoteTime : coyoteTimer - deltaTime;
            jumpBufferTimer = jumpPressed ? definition.JumpBufferTime : jumpBufferTimer - deltaTime;

            if (jumpBufferTimer <= 0f || coyoteTimer <= 0f)
                return;

            rigidbody.linearVelocity = new Vector2(rigidbody.linearVelocity.x, 0f);
            rigidbody.AddForce(Vector2.up * definition.JumpForce, ForceMode2D.Impulse);
            IsGrounded = false;
            playerJumping = true;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            autoHopTimer = 0f;
            Jumped?.Invoke();
        }

        private void TickAutoHop(float deltaTime)
        {
            if (!definition.AutoHop || !IsGrounded || Mathf.Abs(MoveInput) < definition.AutoHopMoveThreshold || IsDashing)
            {
                autoHopTimer = 0f;
                return;
            }

            autoHopTimer += deltaTime;
            if (autoHopTimer < definition.AutoHopInterval)
                return;

            autoHopTimer = 0f;
            rigidbody.linearVelocity = new Vector2(rigidbody.linearVelocity.x, 0f);
            rigidbody.AddForce(Vector2.up * definition.AutoHopForce, ForceMode2D.Impulse);
            IsGrounded = false;
            AutoHopped?.Invoke();
        }

        private void TickDash(float deltaTime)
        {
            if (!IsDashing)
                return;

            rigidbody.linearVelocity = dashVelocity;
            dashTimer -= deltaTime;
            if (dashTimer > 0f)
                return;

            IsDashing = false;
            rigidbody.gravityScale = originalGravityScale;
        }

        private void ApplyBetterGravity(bool jumpHeld, float deltaTime)
        {
            if (IsDashing)
                return;

            if (rigidbody.linearVelocity.y < 0f)
            {
                rigidbody.linearVelocity += Vector2.up * Physics2D.gravity.y
                    * (definition.FallGravityMultiplier - 1f) * deltaTime;
            }
            else if (rigidbody.linearVelocity.y > 0f && !jumpHeld && playerJumping)
            {
                rigidbody.linearVelocity += Vector2.up * Physics2D.gravity.y
                    * (definition.LowJumpMultiplier - 1f) * deltaTime;
            }
        }

        private void ClampSpeed()
        {
            if (IsDashing)
                return;

            float maxSpeed = definition.MaxSpeed;
            if (maxSpeed <= 0f || rigidbody.linearVelocity.sqrMagnitude <= maxSpeed * maxSpeed)
                return;

            rigidbody.linearVelocity = rigidbody.linearVelocity.normalized * maxSpeed;
        }
    }
}
