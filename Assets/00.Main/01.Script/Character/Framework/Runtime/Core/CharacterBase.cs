using System;
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
    public abstract partial class CharacterBase : NetworkBehaviour, ICharacterActionStateStore, ICharacterStatusStateStore
    {
        private const int ActionSlotCount = 6;
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
        [Networked] private int NetAutoHopSequence { get; set; }
        [Networked] private int NetDamageSequence { get; set; }
        [Networked] private float NetUltimateGauge { get; set; }
        [Networked, Capacity(ActionSlotCount)]
        private NetworkArray<NetworkBool> NetActionEnabled => default;
        [Networked, Capacity(ActionSlotCount)]
        private NetworkArray<int> NetActionCharges => default;
        [Networked, Capacity(ActionSlotCount)]
        private NetworkArray<float> NetCooldownDurationOverrides => default;
        [Networked, Capacity(ActionSlotCount)]
        private NetworkArray<NetworkBool> NetAutoCooldown => default;
        [Networked, Capacity(ActionSlotCount)]
        private NetworkArray<TickTimer> NetCooldownTimers => default;
        [Networked] private NetworkBool NetMovementEnabled { get; set; }
        [Networked] private float NetSlowRatio { get; set; }
        [Networked] private TickTimer NetSlowTimer { get; set; }
        [Networked] private TickTimer NetHitstunTimer { get; set; }

        private Rigidbody2D rigidbody2D;
        private Collider2D collider2D;
        private CharacterInputHandler input;
        private CharacterMovementHandler movement;
        private CharacterCooldownHandler cooldowns;
        private CharacterActionStateHandler actionState;
        private CharacterStatusHandler status;
        private CharacterTimerHandler timers;
        private CharacterHealth health;

        private int lastRenderedActionSequence;
        private int lastRenderedJumpSequence;
        private int lastRenderedLandSequence;
        private int lastRenderedAutoHopSequence;
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
        public float SlowRatio => Mathf.Clamp(NetSlowRatio, 0f, 0.99f);
        public bool IsSlowed => SlowRatio > 0f && IsSlowRunning;
        public bool IsHitstunned => Runner != null && NetHitstunTimer.IsRunning && !NetHitstunTimer.Expired(Runner);
        public int FacingDirection => NetFacing >= 0 ? 1 : -1;
        public bool IsFacingRight => FacingDirection > 0;
        public bool IsFacingLeft => FacingDirection < 0;

        /// <summary>궁극기가 쿨타임 대신 게이지(데미지 등으로 충전)로 동작하는지. CooldownHUD가
        /// 이 값을 보고 궁극기 슬롯을 쿨타임/게이지 중 어느 쪽으로 표시할지 정한다.</summary>
        public bool IsUltimateGaugeMode => definition != null && definition.UltimateUsesGauge;
        public float UltimateGaugeCurrent => NetUltimateGauge;
        public float UltimateGaugeMax => definition != null ? definition.UltimateGaugeMax : 0f;

        protected Rigidbody2D Rigidbody => rigidbody2D;
        protected CharacterMovementHandler Movement => movement;
        protected Vector2 AimDirection => DirectionFromAngle(NetAimAngle);
        protected float AimAngle => NetAimAngle;
        protected Vector2 AimWorldPosition => lastInput.AimWorldPosition;
        protected Transform AttackOrigin => sockets.ResolveAttackOrigin(transform);
        protected Transform ProjectileOrigin => sockets.ResolveProjectileOrigin(transform);
        protected Transform EffectOrigin => sockets.ResolveEffectOrigin(transform);
        protected Transform WeaponRoot => sockets.WeaponRoot;
        protected bool IsMovementEnabled => NetMovementEnabled;

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
            actionState = new CharacterActionStateHandler(this, definition.GetCooldown);
            cooldowns = new CharacterCooldownHandler(actionState);
            status = new CharacterStatusHandler(this);
            timers = new CharacterTimerHandler();
            health = new CharacterHealth(definition.MaxHealth, () => NetHealth, value => NetHealth = value);

            movement.Jumped += HandleJumped;
            movement.Landed += HandleLanded;
            movement.AutoHopped += HandleAutoHopped;
        }

        public override void Spawned()
        {
            RegisterProjectIntegration();

            if (Object.HasStateAuthority)
            {
                health.Initialize();
                NetDead = false;
                NetFacing = 1;
                NetAimDirection = 1;
                ResetCommonState();
            }

            lastRenderedActionSequence = NetActionSequence;
            lastRenderedJumpSequence = NetJumpSequence;
            lastRenderedLandSequence = NetLandSequence;
            lastRenderedAutoHopSequence = NetAutoHopSequence;
            lastRenderedDamageSequence = NetDamageSequence;
            lastRenderedDead = NetDead;
            OnCharacterSpawned();
            BindProjectHud();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            UnregisterProjectIntegration();
        }

        protected virtual void OnDestroy()
        {
            UnregisterProjectIntegration();
            timers?.CancelAll();
            if (HasStateAuthority)
                ResetCommonState();

            if (movement == null)
                return;

            movement.Jumped -= HandleJumped;
            movement.Landed -= HandleLanded;
            movement.AutoHopped -= HandleAutoHopped;
        }

        private void Update()
        {
            if (Object == null || !Object.HasInputAuthority || input == null)
                return;

            if (IsProjectInputLocked)
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
            status.Tick();
            movement.SetMovementEnabled(NetMovementEnabled);
            movement.SetMovementSpeedMultiplier(status.MovementSpeedMultiplier);

            bool gameplayLocked = NetDead || NetGameplayLocked || IsProjectGameplayLocked || IsHitstunned;
            CharacterInputSnapshot movementInput = gameplayLocked
                ? default
                : lastInput;

            movement.Tick(movementInput, Runner.DeltaTime);
            timers.Tick(Runner.DeltaTime);

            NetFacing = movement.FacingDirection;
            NetMoveInput = movement.MoveInput;
            NetGrounded = movement.IsGrounded;
            NetVelocity = rigidbody2D.linearVelocity;

            if (!gameplayLocked)
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
            ResetCommonState();
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

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void Rpc_RequestSlow(float ratio, float duration)
        {
            ApplySlowAuthority(ratio, duration);
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
            DealDamageThroughPipeline(target, amount, attacker, CharacterDamageSource.Direct);
        }

        internal void DealProjectileDamage(CharacterBase target, float amount)
        {
            if (target == null || target == this || amount <= 0f)
                return;

            PlayerRef attacker = Object != null ? Object.InputAuthority : PlayerRef.None;
            DealDamageThroughPipeline(target, amount, attacker, CharacterDamageSource.Projectile);
        }

        internal void NotifyProjectileDespawned(
            CharacterProjectile projectile,
            ProjectileDespawnReason reason,
            CharacterBase hitTarget)
        {
            if (!HasStateAuthority)
                return;

            OnProjectileDespawned(projectile, reason, hitTarget);
        }

        protected void DespawnProjectile(CharacterProjectile projectile)
        {
            if (projectile != null)
                projectile.CompleteManually();
        }

        private void DealDamageThroughPipeline(
            CharacterBase target,
            float amount,
            PlayerRef attacker,
            CharacterDamageSource source)
        {
            CharacterDamagePipeline pipeline = new CharacterDamagePipeline(
                (damage, damageSource) => ModifyOutgoingDamage(target, damage, damageSource),
                damage => target.RequestDamage(damage, attacker),
                damage =>
                {
                    AddUltimateGaugeFromDamageDealt(damage);
                    OnDamageDealt(target, damage);
                });
            pipeline.Apply(amount, source);
        }

        /// <summary>게이지형 궁극기(IsUltimateGaugeMode)를 쓰는 캐릭터가 적에게 입힌 데미지만큼
        /// 게이지를 채운다. 게이지 모드가 아니면 아무 일도 안 한다.</summary>
        private void AddUltimateGaugeFromDamageDealt(float damage)
        {
            if (!HasStateAuthority || !IsUltimateGaugeMode || damage <= 0f)
                return;

            float next = NetUltimateGauge + damage * definition.UltimateGaugePerDamageDealt;
            NetUltimateGauge = Mathf.Clamp(next, 0f, definition.UltimateGaugeMax);
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

        protected void SetActionEnabled(CharacterActionType action, bool enabled)
        {
            if (HasStateAuthority)
                actionState.SetEnabled(action, enabled);
        }

        protected bool IsActionEnabled(CharacterActionType action)
        {
            return actionState != null && actionState.IsEnabled(action);
        }

        protected void SetActionCharges(CharacterActionType action, int charges)
        {
            if (HasStateAuthority)
                actionState.SetCharges(action, charges);
        }

        protected void AddActionCharges(CharacterActionType action, int amount)
        {
            if (HasStateAuthority)
                actionState.AddCharges(action, amount);
        }

        protected int GetActionCharges(CharacterActionType action)
        {
            return actionState != null ? actionState.GetCharges(action) : 0;
        }

        protected void SetCooldownDuration(CharacterActionType action, float seconds)
        {
            if (HasStateAuthority)
                actionState.SetCooldownDuration(action, seconds);
        }

        protected void ResetCooldownDuration(CharacterActionType action)
        {
            if (HasStateAuthority)
                actionState.ResetCooldownDuration(action);
        }

        protected void SetAutoCooldown(CharacterActionType action, bool enabled)
        {
            if (HasStateAuthority)
                actionState.SetAutoCooldown(action, enabled);
        }

        protected void StartCooldown(CharacterActionType action)
        {
            if (HasStateAuthority)
                actionState.StartCooldown(action);
        }

        protected void StartCooldown(CharacterActionType action, float seconds)
        {
            if (HasStateAuthority)
                actionState.StartCooldown(action, seconds);
        }

        protected void ClearCooldown(CharacterActionType action)
        {
            if (HasStateAuthority)
                actionState.ClearCooldown(action);
        }

        protected float GetCooldownRemaining(CharacterActionType action)
        {
            return actionState != null ? actionState.GetCooldownRemaining(action) : 0f;
        }

        protected void SetMovementEnabled(bool enabled)
        {
            if (!HasStateAuthority)
                return;

            NetMovementEnabled = enabled;
            movement.SetMovementEnabled(enabled);
        }

        protected void ApplySlow(CharacterBase target, float slowRatio, float duration)
        {
            if (target == null || target.Object == null)
                return;

            if (target.HasStateAuthority)
                target.ApplySlowAuthority(slowRatio, duration);
            else
                target.Rpc_RequestSlow(slowRatio, duration);
        }

        protected CharacterTimerHandle ScheduleTimer(float seconds, Action callback)
        {
            return HasStateAuthority && timers != null
                ? timers.Schedule(seconds, callback)
                : default;
        }

        protected bool CancelTimer(CharacterTimerHandle handle)
        {
            return HasStateAuthority && timers != null && timers.Cancel(handle);
        }

        protected bool IsBehindTarget(CharacterBase target, float rearArcAngle)
        {
            if (target == null)
                return false;

            Vector2 targetForward = target.FacingDirection >= 0 ? Vector2.right : Vector2.left;
            Vector2 targetToAttacker = ((Vector2)transform.position - (Vector2)target.transform.position).normalized;
            float rearHalfAngle = Mathf.Clamp(rearArcAngle, 0f, 360f) * 0.5f;
            return Vector2.Angle(-targetForward, targetToAttacker) <= rearHalfAngle;
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
                    projectile?.Initialize(normalized, speed, damage, targetLayer, Object.InputAuthority, Object.Id);
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
            if (!pressed || !actionState.CanUse(action))
                return;
            if (action == CharacterActionType.Dash && !NetMovementEnabled)
                return;
            if (action == CharacterActionType.Ultimate && IsUltimateGaugeMode && NetUltimateGauge < UltimateGaugeMax)
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

            actionState.ConsumeCharge(action);
            if (action == CharacterActionType.Ultimate && IsUltimateGaugeMode)
                NetUltimateGauge = 0f;
            else if (actionState.ShouldStartCooldownAutomatically(action))
                actionState.StartCooldown(action);
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
                ResetCommonState();
                OnDied(attacker);
            }
            else
            {
                // 죽는 타격이 아닐 때만 넉백을 건다 — 어차피 죽는 순간 위의 ResetCommonState()가
                // 속도를 0으로 되돌리고 대시/넉백을 취소해버려서(그래야 사망 연출/카메라 포커스가
                // 제자리에서 안정적으로 잡힘), 죽는 타격에 걸어봐야 바로 지워진다.
                ApplyKnockback(applied, attacker);
            }
        }

        /// <summary>피해량에 비례해 밀려나는 넉백 + 그 동안 이동/행동을 막는 히트스턴을 건다.</summary>
        private void ApplyKnockback(float damageApplied, PlayerRef attacker)
        {
            float force = definition.KnockbackBaseForce + damageApplied * definition.KnockbackForcePerDamage;
            if (force <= 0f)
                return;

            Vector2 direction = ResolveKnockbackDirection(attacker);
            float duration = definition.KnockbackDuration;

            movement.ApplyKnockback(direction, force, duration);
            NetHitstunTimer = TickTimer.CreateFromSeconds(Runner, duration);
        }

        /// <summary>공격자 위치 기준 좌우 방향 + 위쪽 편향을 섞은 넉백 방향. 공격자를 못 찾으면
        /// (투사체 소유자가 이미 디스폰된 경우 등) 피격자가 바라보던 방향의 반대(후방)로 민다.</summary>
        private Vector2 ResolveKnockbackDirection(PlayerRef attacker)
        {
            CharacterBase attackerCharacter = All.Find(c => c != null && c.Object != null && c.Object.InputAuthority == attacker);
            float horizontal = attackerCharacter != null
                ? Mathf.Sign(transform.position.x - attackerCharacter.transform.position.x)
                : -FacingDirection;

            return new Vector2(horizontal, definition.KnockbackUpwardBias).normalized;
        }

        private bool HasStateAuthority => Object != null && Object.HasStateAuthority;

        private void ResetCommonState()
        {
            if (!HasStateAuthority)
                return;

            timers?.CancelAll();
            actionState?.Initialize();
            NetUltimateGauge = 0f;
            NetSlowRatio = 0f;
            NetSlowTimer = default;
            NetHitstunTimer = default;
            NetMovementEnabled = true;
            movement?.CancelDash();
            movement?.SetMovementSpeedMultiplier(1f);
            movement?.SetMovementEnabled(true);
        }

        private void ApplySlowAuthority(float ratio, float duration)
        {
            if (HasStateAuthority)
                status.ApplySlow(ratio, duration);
        }

        bool ICharacterActionStateStore.GetEnabled(CharacterActionType action)
        {
            return IsActionSlot(action) && NetActionEnabled.Get(ActionIndex(action));
        }

        void ICharacterActionStateStore.SetEnabled(CharacterActionType action, bool enabled)
        {
            if (HasStateAuthority && IsActionSlot(action))
                NetActionEnabled.Set(ActionIndex(action), enabled);
        }

        int ICharacterActionStateStore.GetCharges(CharacterActionType action)
        {
            return IsActionSlot(action) ? NetActionCharges.Get(ActionIndex(action)) : 0;
        }

        void ICharacterActionStateStore.SetCharges(CharacterActionType action, int charges)
        {
            if (HasStateAuthority && IsActionSlot(action))
                NetActionCharges.Set(ActionIndex(action), Mathf.Max(-1, charges));
        }

        float ICharacterActionStateStore.GetCooldownDurationOverride(CharacterActionType action)
        {
            return IsActionSlot(action) ? NetCooldownDurationOverrides.Get(ActionIndex(action)) : -1f;
        }

        void ICharacterActionStateStore.SetCooldownDurationOverride(CharacterActionType action, float seconds)
        {
            if (HasStateAuthority && IsActionSlot(action))
                NetCooldownDurationOverrides.Set(ActionIndex(action), Mathf.Max(-1f, seconds));
        }

        bool ICharacterActionStateStore.GetAutoCooldown(CharacterActionType action)
        {
            return IsActionSlot(action) && NetAutoCooldown.Get(ActionIndex(action));
        }

        void ICharacterActionStateStore.SetAutoCooldown(CharacterActionType action, bool enabled)
        {
            if (HasStateAuthority && IsActionSlot(action))
                NetAutoCooldown.Set(ActionIndex(action), enabled);
        }

        bool ICharacterActionStateStore.IsCooldownRunning(CharacterActionType action)
        {
            return ((ICharacterActionStateStore)this).GetCooldownRemaining(action) > 0f;
        }

        void ICharacterActionStateStore.StartCooldown(CharacterActionType action, float seconds)
        {
            if (HasStateAuthority && IsActionSlot(action))
                NetCooldownTimers.Set(ActionIndex(action), TickTimer.CreateFromSeconds(Runner, Mathf.Max(0f, seconds)));
        }

        void ICharacterActionStateStore.ClearCooldown(CharacterActionType action)
        {
            if (HasStateAuthority && IsActionSlot(action))
                NetCooldownTimers.Set(ActionIndex(action), default);
        }

        float ICharacterActionStateStore.GetCooldownRemaining(CharacterActionType action)
        {
            if (Runner == null || !IsActionSlot(action))
                return 0f;

            return NetCooldownTimers.Get(ActionIndex(action)).RemainingTime(Runner) ?? 0f;
        }

        float ICharacterStatusStateStore.SlowRatio
        {
            get => SlowRatio;
            set
            {
                if (HasStateAuthority)
                    NetSlowRatio = Mathf.Clamp(value, 0f, 0.99f);
            }
        }

        // A configured TickTimer remains running after its remaining time reaches
        // zero.  Keep that distinction so CharacterStatusHandler can observe
        // "running and expired" once and clear the replicated slow state.
        bool ICharacterStatusStateStore.IsSlowRunning => Runner != null && NetSlowTimer.IsRunning;

        bool ICharacterStatusStateStore.IsSlowExpired => Runner != null &&
            NetSlowTimer.IsRunning && NetSlowTimer.Expired(Runner);

        void ICharacterStatusStateStore.StartSlow(float seconds)
        {
            if (HasStateAuthority)
                NetSlowTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0f, seconds));
        }

        void ICharacterStatusStateStore.ClearSlow()
        {
            if (!HasStateAuthority)
                return;

            NetSlowRatio = 0f;
            NetSlowTimer = default;
        }

        private bool IsSlowRunning
        {
            get
            {
                return Runner != null && NetSlowTimer.IsRunning && !NetSlowTimer.Expired(Runner);
            }
        }

        private static bool IsActionSlot(CharacterActionType action)
        {
            return action > CharacterActionType.None && (int)action < ActionSlotCount;
        }

        private static int ActionIndex(CharacterActionType action)
        {
            return (int)action;
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

        /// <summary>이동 중 자동으로 통통 튀는 연출(AutoHop)용. 진짜 점프(스페이스바)와 달리
        /// 스쿼시/애니메이션만 재생하고 사운드는 울리지 않는다 — HandleJumped와 분리된 이유.</summary>
        private void HandleAutoHopped()
        {
            NetAutoHopSequence++;
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

            if (lastRenderedAutoHopSequence != NetAutoHopSequence)
            {
                lastRenderedAutoHopSequence = NetAutoHopSequence;
                visual.PlayAutoHop();
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
        protected virtual float ModifyOutgoingDamage(
            CharacterBase target,
            float damage,
            CharacterDamageSource source) => damage;
        protected virtual void OnProjectileDespawned(
            CharacterProjectile projectile,
            ProjectileDespawnReason reason,
            CharacterBase hitTarget) { }
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
