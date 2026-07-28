using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    public readonly struct CharacterVisualState
    {
        public CharacterVisualState(
            float deltaTime,
            bool grounded,
            float moveInput,
            Vector2 velocity,
            int facingDirection,
            int aimDirection,
            float aimAngle,
            bool dead)
        {
            DeltaTime = deltaTime;
            Grounded = grounded;
            MoveInput = moveInput;
            Velocity = velocity;
            FacingDirection = facingDirection;
            AimDirection = aimDirection;
            AimAngle = aimAngle;
            Dead = dead;
        }

        public float DeltaTime { get; }
        public bool Grounded { get; }
        public float MoveInput { get; }
        public Vector2 Velocity { get; }
        public int FacingDirection { get; }
        public int AimDirection { get; }
        public float AimAngle { get; }
        public bool Dead { get; }
    }
}
