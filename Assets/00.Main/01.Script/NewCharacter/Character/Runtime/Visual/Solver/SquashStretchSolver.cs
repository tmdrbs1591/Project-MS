using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    internal sealed class SquashStretchSolver
    {
        private readonly Transform body;
        private readonly CharacterVisualProfile profile;
        private readonly Vector3 baseLocalPosition;
        private readonly Vector3 baseLocalScale;
        private readonly float halfHeight;

        private bool initializedGroundState;
        private bool wasGrounded;
        private float stretch;
        private float stretchVelocity;

        public SquashStretchSolver(
            Transform body,
            SpriteRenderer bodyRenderer,
            CharacterVisualProfile profile)
        {
            this.body = body;
            this.profile = profile;
            baseLocalPosition = body != null ? body.localPosition : Vector3.zero;
            baseLocalScale = body != null ? body.localScale : Vector3.one;

            if (bodyRenderer != null)
            {
                halfHeight = bodyRenderer.sprite != null
                    ? bodyRenderer.sprite.bounds.extents.y
                    : bodyRenderer.bounds.extents.y;
            }
        }

        public void PlayJump()
        {
            stretchVelocity += profile.JumpStretch * 60f;
        }

        public void PlayHit()
        {
            stretchVelocity -= profile.HitRecoilStretch * 60f;
        }

        public void Tick(CharacterVisualState state)
        {
            if (body == null)
                return;

            if (!initializedGroundState)
            {
                initializedGroundState = true;
                wasGrounded = state.Grounded;
            }

            if (state.Grounded && !wasGrounded)
            {
                float impact = Mathf.Abs(state.Velocity.y);
                float amount = Mathf.Clamp01(impact / 18f) * profile.LandSquash;
                stretchVelocity -= amount * 60f;
            }

            float deltaTime = Mathf.Min(state.DeltaTime, 0.0333f);
            float force = -profile.Stiffness * stretch - profile.Damping * stretchVelocity;
            stretchVelocity += force * deltaTime;
            stretch += stretchVelocity * deltaTime;
            stretchVelocity = Mathf.Clamp(stretchVelocity, -50f, 50f);
            stretch = Mathf.Clamp(stretch, -0.6f, 0.8f);

            float airStretch = state.Grounded
                ? 0f
                : Mathf.Clamp(state.Velocity.y * profile.AirStretchFactor, -0.25f, 0.5f);

            float idleWobble = 0f;
            if (state.Grounded && Mathf.Abs(state.MoveInput) < 0.01f && !state.Dead)
                idleWobble = Mathf.Sin(Time.time * profile.IdleWobbleSpeed) * profile.IdleWobbleAmount;

            float finalStretch = stretch + airStretch + idleWobble;
            float scaleY = Mathf.Max(0.2f, 1f + finalStretch);
            float scaleX = Mathf.Max(0.2f, 1f - finalStretch * profile.HorizontalCompensation);
            int direction = state.FacingDirection >= 0 ? 1 : -1;

            body.localScale = new Vector3(
                Mathf.Abs(baseLocalScale.x) * scaleX * direction,
                baseLocalScale.y * scaleY,
                baseLocalScale.z);

            if (profile.AnchorBodyToBottom && halfHeight > 0f)
            {
                float offsetY = halfHeight * (scaleY - 1f);
                body.localPosition = baseLocalPosition + new Vector3(0f, offsetY, 0f);
            }

            wasGrounded = state.Grounded;
        }
    }
}
