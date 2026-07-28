using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    /// <summary>
    /// Shared Mode 캐릭터의 공통 네트워크 경계다.
    /// 자식 캐릭터는 스킬/패시브 훅과 protected 공통 API만 사용한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(NetworkObject))]
    public abstract class CharacterBase : NetworkBehaviour
    {
        [Header("Required")]
        [SerializeField] private CharacterDefinition definition;
        [SerializeField] private CharacterVisualController visual;

        [Header("Gameplay Sockets")]
        [SerializeField] private CharacterSockets sockets = new CharacterSockets();

        [Header("Local Input")]
        [Tooltip("비워두면 로컬 캐릭터에서 Camera.main을 사용합니다.")]
        [SerializeField] private Camera inputCamera;

        [Networked] private float NetHealth { get; set; }
        [Networked] private NetworkBool NetDead { get; set; }
        [Networked] private NetworkBool NetGameplayLocked { get; set; }
        [Networked] private int NetFacing { get; set; }
        [Networked] private float NetMoveInput { get; set; }
        [Networked] private NetworkBool NetGrounded { get; set; }
        [Networked] private Vector2 NetVelocity { get; set; }
        [Networked] private float NetAimAngle { get; set; }
        [Networked] private int NetAimDirection { get; set; }
        [Networked] private CharacterActionType NetAction { get; set; }
        [Networked] private int NetActionSequence { get; set; }
        [Networked] private int NetJumpSequence { get; set; }
        [Networked] private int NetLandSequence { get; set; }
        [Networked] private int NetDamageSequence { get; set; }

        private Rigidbody2D rigidbody2D;
        private Collider2D collider2D;
        private CharacterInputHandler input;
        private CharacterMovementHandler movement;
        private CharacterCooldownHandler cooldowns;
        private CharacterHealth health;

        private int lastRenderedActionSequence;
        private int lastRenderedJumpSequence;
        private int lastRenderedLandSequence;
        private int lastRenderedDamageSequence;
        private bool lastRenderedDead;
        private CharacterInputSnapshot lastInput;

        public CharacterDefinition Definition => definition;
        public CharacterVisualController Visual => visual;
        public float CurrentHealth => NetHealth;
        public float MaxHealth => definition != null ? definition.MaxHealth : 0f;
        public float CurrentHealthPercent => health != null ? health.Normalized : 0f;
        public bool IsDead => NetDead;
        public bool IsLocalPlayer => Object != null && Object.HasInputAuthority;
        public CharacterCooldownHandler Cooldowns => cooldowns;

        protected Rigidbody2D Rigidbody => rigidbody2D;
        protected CharacterMovementHandler Movement => movement;
        protected Vector2 AimDirection => DirectionFromAngle(NetAimAngle);
        protected float AimAngle => NetAimAngle;
        protected Vector2 AimWorldPosition => lastInput.AimWorldPosition;
        protected Transform AttackOrigin => sockets.ResolveAttackOrigin(transform);
        protected Transform ProjectileOrigin => sockets.ResolveProjectileOrigin(transform);
        protected Transform EffectOrigin => sockets.ResolveEffectOrigin(transform);
        protected Transform WeaponRoot => sockets.WeaponRoot;

        protected virtual void Awake()
        {
            rigidbody2D = GetComponent<Rigidbody2D>();
            collider2D = GetComponent<Collider2D>();

            if (definition == null)
            {
                Debug.LogError($"[{GetType().Name}] Character Definition이 필요합니다.", this);
                enabled = false;
                return;
            }

            if (visual == null)
                visual = GetComponentInChildren<CharacterVisualController>(true);

            input = new CharacterInputHandler(definition);
            movement = new CharacterMovementHandler(rigidbody2D, collider2D, definition);
            cooldowns = new CharacterCooldownHandler(definition);
            health = new CharacterHealth(definition.MaxHealth, () => NetHealth, value => NetHealth = value);

            movement.Jumped += HandleJumped;
            movement.Landed += HandleLanded;
        }

        public override void Spawned()
        {
            if (Object.HasStateAuthority)
            {
                health.Initialize();
                NetDead = false;
                NetFacing = 1;
                NetAimDirection = 1;
            }

            lastRenderedActionSequence = NetActionSequence;
            lastRenderedJumpSequence = NetJumpSequence;
            lastRenderedLandSequence = NetLandSequence;
            lastRenderedDamageSequence = NetDamageSequence;
            lastRenderedDead = NetDead;
            OnCharacterSpawned();
        }

        protected virtual void OnDestroy()
        {
            if (movement == null)
                return;

            movement.Jumped -= HandleJumped;
            movement.Landed -= HandleLanded;
        }

        private void Update()
        {
            if (Object == null || !Object.HasInputAuthority || input == null)
                return;

            Camera cameraToUse = inputCamera != null ? inputCamera : Camera.main;
            input.CaptureFrame(cameraToUse, transform.position);
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || input == null)
                return;

            lastInput = input.ConsumeTick();
            UpdateAim(lastInput.AimWorldPosition);

            CharacterInputSnapshot movementInput = NetDead || NetGameplayLocked
                ? default
                : lastInput;

            movement.Tick(movementInput, Runner.DeltaTime);
            cooldowns.Tick(Runner.DeltaTime);

            NetFacing = movement.FacingDirection;
            NetMoveInput = movement.MoveInput;
            NetGrounded = movement.IsGrounded;
            NetVelocity = rigidbody2D.linearVelocity;

            if (!NetDead && !NetGameplayLocked)
            {
                HandleActionInputs(lastInput);
                OnPassiveTick(Runner.DeltaTime);
            }
        }

        public override void Render()
        {
            if (visual == null)
                return;

            if (!Object.HasStateAuthority)
                movement.ApplyRemoteVisualState(NetFacing, NetMoveInput, NetGrounded, NetVelocity);

            CharacterVisualState state = new CharacterVisualState(
                Time.deltaTime,
                NetGrounded,
                NetMoveInput,
                NetVelocity,
                NetFacing,
                NetAimDirection,
                NetAimAngle,
                NetDead);

            visual.ApplyState(state);
            DetectVisualEvents();
        }

        public void RequestDamage(float amount, PlayerRef attacker)
        {
            if (Object == null || amount <= 0f)
                return;

            if (Object.HasStateAuthority)
                ApplyDamage(amount, attacker);
            else
                Rpc_RequestDamage(amount, attacker);
        }

        public void RequestHeal(float amount)
        {
            if (Object == null || amount <= 0f)
                return;

            if (Object.HasStateAuthority)
                health.Heal(amount);
            else
                Rpc_RequestHeal(amount);
        }

        public void SetGameplayLocked(bool locked)
        {
            if (Object != null && Object.HasStateAuthority)
                NetGameplayLocked = locked;
        }

        public void ResetCharacter(Vector2 position)
        {
            if (Object == null || !Object.HasStateAuthority)
                return;

            health.FullHeal();
            NetDead = false;
            cooldowns.ResetAll();
            movement.Reset(position);
            OnResetCharacter();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void Rpc_RequestDamage(float amount, PlayerRef attacker)
        { 
            ApplyDamage(amount, attacker);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)] 
        private void Rpc_RequestHeal(float amount)
        { 
            health.Heal(amount);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_PlayActionEffect(CharacterActionType action, Vector2 position, float angle)
        {
            visual?.SpawnActionEffect(action, position, angle);
        }

        protected void DealDamage(CharacterBase target, float amount)
        {
            if (target == null || target == this || amount <= 0f)
                return;

            PlayerRef attacker = Object != null ? Object.InputAuthority : PlayerRef.None;
            target.RequestDamage(amount, attacker);
            OnDamageDealt(target, amount);
        }

        protected void Heal(float amount)
        {
            if (Object != null && Object.HasStateAuthority)
                health.Heal(amount);
        }

        protected void StartDefaultDash()
        {
            movement.StartDefaultDash();
        }

        protected void StartDash(Vector2 direction, float power, float duration)
        {
            movement.StartDash(direction, power, duration);
        }

        protected void SpawnProjectile(
            CharacterProjectile projectilePrefab,
            Vector2 position,
            Vector2 direction,
            float speed,
            float damage,
            LayerMask targetLayer)
        {
            if (projectilePrefab == null || Runner == null || Object == null || !Object.HasStateAuthority)
                return;

            Vector2 normalized = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : new Vector2(NetFacing >= 0 ? 1f : -1f, 0f);

            Runner.Spawn(
                projectilePrefab,
                position,
                Quaternion.identity,
                Object.InputAuthority,
                (_, spawnedObject) =>
                {
                    CharacterProjectile projectile = spawnedObject.GetComponent<CharacterProjectile>();
                    projectile?.Initialize(normalized, speed, damage, targetLayer, Object.InputAuthority);
                });
        }

        protected void PlayActionEffect(CharacterActionType action, Vector2 position, float angle)
        {
            if (Object != null && Object.HasStateAuthority)
                Rpc_PlayActionEffect(action, position, angle);
        }

        protected List<CharacterBase> FindEnemiesInCircle(Vector2 center, float radius, LayerMask targetLayer)
        {
            return CharacterCombatQuery2D.Circle(this, center, radius, targetLayer);
        }

        protected List<CharacterBase> FindEnemiesInBox(
            Vector2 center,
            Vector2 size,
            float angle,
            LayerMask targetLayer)
        {
            return CharacterCombatQuery2D.Box(this, center, size, angle, targetLayer);
        }

        protected List<CharacterBase> FindEnemiesInLine(
            Vector2 origin,
            Vector2 direction,
            float distance,
            float width,
            LayerMask targetLayer)
        {
            return CharacterCombatQuery2D.Line(this, origin, direction, distance, width, targetLayer);
        }

        protected List<CharacterBase> FindEnemiesInArc(
            Vector2 origin,
            Vector2 direction,
            float radius,
            float angle,
            LayerMask targetLayer)
        {
            return CharacterCombatQuery2D.Arc(this, origin, direction, radius, angle, targetLayer);
        }

        private void HandleActionInputs(CharacterInputSnapshot snapshot)
        {
            TryExecute(CharacterActionType.BasicAttack, snapshot.BasicAttackPressed);
            TryExecute(CharacterActionType.SkillQ, snapshot.SkillQPressed);
            TryExecute(CharacterActionType.SkillE, snapshot.SkillEPressed);
            TryExecute(CharacterActionType.Dash, snapshot.DashPressed);
            TryExecute(CharacterActionType.Ultimate, snapshot.UltimatePressed);
        }

        private void TryExecute(CharacterActionType action, bool pressed)
        {
            if (!pressed || !cooldowns.CanUse(action))
                return;

            CharacterActionContext context = CreateActionContext(action);
            bool executed = action switch
            {
                CharacterActionType.BasicAttack => OnBasicAttack(context),
                CharacterActionType.SkillQ => OnSkillQ(context),
                CharacterActionType.SkillE => OnSkillE(context),
                CharacterActionType.Dash => OnDash(context),
                CharacterActionType.Ultimate => OnUltimate(context),
                _ => false
            };

            if (!executed)
                return;

            cooldowns.Start(action);
            NetAction = action;
            NetActionSequence++;
            OnSkillExecuted(action);
        }

        private CharacterActionContext CreateActionContext(CharacterActionType action)
        {
            return new CharacterActionContext(
                action,
                AttackOrigin.position,
                AimDirection,
                lastInput.AimWorldPosition,
                definition.GetDamage(action));
        }

        private void UpdateAim(Vector2 aimWorldPosition)
        {
            Vector2 direction = aimWorldPosition - (Vector2)AttackOrigin.position;
            if (direction.sqrMagnitude < 0.0001f)
                direction = new Vector2(NetFacing >= 0 ? 1f : -1f, 0f);

            NetAimAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            NetAimDirection = direction.x >= 0f ? 1 : -1;
        }

        private void ApplyDamage(float amount, PlayerRef attacker)
        {
            float before = health.Current;
            float applied = health.ApplyDamage(amount);
            if (applied <= 0f)
                return;

            NetDamageSequence++;
            CharacterDamageInfo info = new CharacterDamageInfo(amount, applied, attacker);
            OnDamaged(info);
            OnHealthChanged(before, health.Current);

            if (health.IsDead && !NetDead)
            {
                NetDead = true;
                rigidbody2D.linearVelocity = Vector2.zero;
                OnDied(attacker);
            }
        }

        private void HandleJumped()
        {
            NetJumpSequence++;
            OnJumped();
        }

        private void HandleLanded()
        {
            NetLandSequence++;
            OnLanded();
        }

        private void DetectVisualEvents()
        {
            if (lastRenderedActionSequence != NetActionSequence)
            {
                lastRenderedActionSequence = NetActionSequence;
                visual.PlayAction(NetAction);
            }

            if (lastRenderedJumpSequence != NetJumpSequence)
            {
                lastRenderedJumpSequence = NetJumpSequence;
                visual.PlayJump();
            }

            if (lastRenderedLandSequence != NetLandSequence)
            {
                lastRenderedLandSequence = NetLandSequence;
                visual.PlayLanded();
            }

            if (lastRenderedDamageSequence != NetDamageSequence)
            {
                lastRenderedDamageSequence = NetDamageSequence;
                visual.PlayDamaged();
            }

            bool dead = NetDead;
            if (lastRenderedDead != dead)
            {
                lastRenderedDead = dead;
                if (dead)
                    visual.PlayDead();
                else
                    visual.PlayRevived();
            }
        }

        private static Vector2 DirectionFromAngle(float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        protected virtual bool OnBasicAttack(CharacterActionContext context) => false;
        protected virtual bool OnSkillQ(CharacterActionContext context) => false;
        protected virtual bool OnSkillE(CharacterActionContext context) => false;
        protected virtual bool OnDash(CharacterActionContext context)
        {
            StartDefaultDash();
            return true;
        }

        protected virtual bool OnUltimate(CharacterActionContext context) => false;
        protected virtual void OnPassiveTick(float deltaTime) { }
        protected virtual void OnDamaged(CharacterDamageInfo damage) { }
        protected virtual void OnDamageDealt(CharacterBase target, float requestedDamage) { }
        protected virtual void OnDied(PlayerRef attacker) { }
        protected virtual void OnJumped() { }
        protected virtual void OnLanded() { }
        protected virtual void OnSkillExecuted(CharacterActionType action) { }
        protected virtual void OnHealthChanged(float previous, float current) { }
        protected virtual void OnCharacterSpawned() { }
        protected virtual void OnResetCharacter() { }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (visual == null)
                visual = GetComponentInChildren<CharacterVisualController>(true);
        }
#endif
    }
}
