using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectMS.CharacterSystem
{
    [CreateAssetMenu(menuName = "Project MS/Character/Definition", fileName = "CharacterDefinition")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "New Character";

        [Header("Stats")]
        [Min(1f)] [SerializeField] private float maxHealth = 100f;
        [Min(0f)] [SerializeField] private float basicAttackDamage = 10f;
        [Min(0f)] [SerializeField] private float skillQDamage = 20f;
        [Min(0f)] [SerializeField] private float skillEDamage = 20f;
        [Min(0f)] [SerializeField] private float ultimateDamage = 50f;

        [Header("Cooldowns")]
        [Min(0f)] [SerializeField] private float basicAttackCooldown = 0.3f;
        [Min(0f)] [SerializeField] private float skillQCooldown = 5f;
        [Min(0f)] [SerializeField] private float skillECooldown = 7f;
        [Min(0f)] [SerializeField] private float dashCooldown = 3f;
        [Min(0f)] [SerializeField] private float ultimateCooldown = 30f;

        [Header("Movement")]
        [Min(0f)] [SerializeField] private float moveSpeed = 5f;
        [Min(0f)] [SerializeField] private float groundAcceleration = 45f;
        [Min(0f)] [SerializeField] private float airAcceleration = 28f;
        [Min(0f)] [SerializeField] private float jumpForce = 8f;
        [Min(0f)] [SerializeField] private float maxSpeed = 18f;
        [Min(0f)] [SerializeField] private float groundCheckDistance = 0.08f;
        [SerializeField] private LayerMask groundLayer;
        [Min(0f)] [SerializeField] private float coyoteTime = 0.1f;
        [Min(0f)] [SerializeField] private float jumpBufferTime = 0.12f;
        [Min(1f)] [SerializeField] private float fallGravityMultiplier = 2.2f;
        [Min(1f)] [SerializeField] private float lowJumpMultiplier = 2f;

        [Header("Dash")]
        [Min(0f)] [SerializeField] private float defaultDashPower = 14f;
        [Min(0.01f)] [SerializeField] private float defaultDashDuration = 0.16f;

        [Header("Auto Hop")]
        [SerializeField] private bool autoHop = true;
        [Min(0.01f)] [SerializeField] private float autoHopInterval = 0.1f;
        [Min(0f)] [SerializeField] private float autoHopForce = 5f;
        [Min(0f)] [SerializeField] private float autoHopMoveThreshold = 0.05f;

        [Header("Input")]
        [SerializeField] private Key moveLeft = Key.A;
        [SerializeField] private Key moveRight = Key.D;
        [SerializeField] private Key jump = Key.Space;
        [SerializeField] private Key skillQ = Key.Q;
        [SerializeField] private Key skillE = Key.E;
        [SerializeField] private Key dash = Key.LeftShift;
        [SerializeField] private Key ultimate = Key.R;

        public string DisplayName => displayName;
        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float GroundAcceleration => groundAcceleration;
        public float AirAcceleration => airAcceleration;
        public float JumpForce => jumpForce;
        public float MaxSpeed => maxSpeed;
        public float GroundCheckDistance => groundCheckDistance;
        public LayerMask GroundLayer => groundLayer;
        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;
        public float FallGravityMultiplier => fallGravityMultiplier;
        public float LowJumpMultiplier => lowJumpMultiplier;
        public float DefaultDashPower => defaultDashPower;
        public float DefaultDashDuration => defaultDashDuration;
        public bool AutoHop => autoHop;
        public float AutoHopInterval => autoHopInterval;
        public float AutoHopForce => autoHopForce;
        public float AutoHopMoveThreshold => autoHopMoveThreshold;
        public Key MoveLeft => moveLeft;
        public Key MoveRight => moveRight;
        public Key Jump => jump;
        public Key SkillQ => skillQ;
        public Key SkillE => skillE;
        public Key Dash => dash;
        public Key Ultimate => ultimate;

        public float GetDamage(CharacterActionType action)
        {
            return action switch
            {
                CharacterActionType.BasicAttack => basicAttackDamage,
                CharacterActionType.SkillQ => skillQDamage,
                CharacterActionType.SkillE => skillEDamage,
                CharacterActionType.Ultimate => ultimateDamage,
                _ => 0f
            };
        }

        public float GetCooldown(CharacterActionType action)
        {
            return action switch
            {
                CharacterActionType.BasicAttack => basicAttackCooldown,
                CharacterActionType.SkillQ => skillQCooldown,
                CharacterActionType.SkillE => skillECooldown,
                CharacterActionType.Dash => dashCooldown,
                CharacterActionType.Ultimate => ultimateCooldown,
                _ => 0f
            };
        }
    }
}
