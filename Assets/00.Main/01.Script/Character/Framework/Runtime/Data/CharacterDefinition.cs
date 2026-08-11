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

        [Header("Knockback")]
        [Tooltip("피해량과 무관하게 항상 적용되는 기본 넉백 힘.")]
        [Min(0f)] [SerializeField] private float knockbackBaseForce = 3f;
        [Tooltip("받은 피해량 1당 추가되는 넉백 힘.")]
        [Min(0f)] [SerializeField] private float knockbackForcePerDamage = 0.1f;
        [Tooltip("넉백이 지속되는 시간(초). 이 동안 이동/행동 입력이 안 먹는다(히트스턴).")]
        [Min(0.01f)] [SerializeField] private float knockbackDuration = 0.15f;
        [Tooltip("넉백 방향에서 위쪽으로 얼마나 띄울지(0=수평만, 1=거의 수직).")]
        [Range(0f, 1f)] [SerializeField] private float knockbackUpwardBias = 0.35f;

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

        [Header("Ultimate Gauge")]
        [Tooltip("켜면 궁극기가 쿨타임 대신 게이지로 동작한다 — 게이지가 가득 차야 쓸 수 있고, 쓰면 0으로 리셋된다. Ultimate Cooldown 값은 이때 무시된다.")]
        [SerializeField] private bool ultimateUsesGauge;
        [Tooltip("게이지 최대치(=100%에 해당하는 값).")]
        [Min(1f)] [SerializeField] private float ultimateGaugeMax = 100f;
        [Tooltip("적에게 데미지 1을 입힐 때마다 차는 게이지 양.")]
        [Min(0f)] [SerializeField] private float ultimateGaugePerDamageDealt = 1f;

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
        public float KnockbackBaseForce => knockbackBaseForce;
        public float KnockbackForcePerDamage => knockbackForcePerDamage;
        public float KnockbackDuration => knockbackDuration;
        public float KnockbackUpwardBias => knockbackUpwardBias;
        public bool UltimateUsesGauge => ultimateUsesGauge;
        public float UltimateGaugeMax => ultimateGaugeMax;
        public float UltimateGaugePerDamageDealt => ultimateGaugePerDamageDealt;

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
