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
    public abstract partial class CharacterBase : NetworkBehaviour, ICharacterActionStateStore, ICharacterStatusStateStore, IDamageable
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

        [Header("Common Ultimate Effect")]
        [Tooltip("궁극기 사용 성공 시 공통으로 재생되는 반짝임 파티클. 캐릭터 프리팹에 미리 배치해두고 연결한다(비워두면 재생 안 함).")]
        [SerializeField] private ParticleSystem ultimateFlashEffect;

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

        private new Rigidbody2D rigidbody2D;
        private new Collider2D collider2D;
        private CharacterInputHandler input;
        private CharacterMovementHandler movement;
        private CharacterCooldownHandler cooldowns;
        private CharacterActionStateHandler actionState;
        private CharacterStatusHandler status;
        private CharacterTimerHandler timers;
        private CharacterHealth health;
        private CharacterOwnedEntityRegistry ownedEntityRegistry;

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
        public float MaxHealth => definition != null ? definition.MaxHealth * MaxHealthMultiplier : 0f;
        public float CurrentHealthPercent => health != null ? health.Normalized : 0f;
        public bool IsDead => NetDead;
        public bool IsLocalPlayer => Object != null && Object.HasInputAuthority;
        public PlayerRef DamageOwner => ResolveDamageOwner();
        public int DamageTeamId => ResolveDamageTeamId();
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
        public Vector2 AimDirection => DirectionFromAngle(NetAimAngle);
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
            ownedEntityRegistry = new CharacterOwnedEntityRegistry();

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
            if (hasState)
                DestroyOwnedEntitiesForOwnerExit(OwnedEntityDestroyReason.OwnerDespawned);
            timers?.CancelAll();
            ownedEntityRegistry?.Clear();
            input?.ClearGameplayInput();
            OnCharacterDespawned();
            UnregisterProjectIntegration();
        }

        protected virtual void OnDestroy()
        {
            UnregisterProjectIntegration();
            timers?.CancelAll();
            ownedEntityRegistry?.Clear();
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

            if (IsProjectInputLocked)
                input.ClearGameplayInput();
            lastInput = input.ConsumeTick();
            UpdateAim(lastInput.AimWorldPosition);
            status.Tick();
            movement.SetMovementEnabled(NetMovementEnabled);
            movement.SetMovementSpeedMultiplier(status.MovementSpeedMultiplier);
            movement.SetBaseSpeedMultiplier(MoveSpeedMultiplier);

            bool gameplayLocked = NetDead || NetGameplayLocked || IsProjectGameplayLocked ||
                                  IsProjectInputLocked || IsHitstunned;
            CharacterInputSnapshot movementInput = gameplayLocked
                ? default
                : lastInput;

            movement.Tick(movementInput, Runner.DeltaTime);
            timers.Tick(Runner.DeltaTime);
            ownedEntityRegistry?.Prune();

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
            if (Object == null || !IsFinitePositive(amount))
                return;

            if (Object.HasStateAuthority)
                ApplyDamage(new DamageRequest(
                    amount,
                    attacker,
                    default,
                    attacker != PlayerRef.None ? attacker.PlayerId : -1,
                    CharacterDamageSource.Direct));
            else
                Rpc_RequestLegacyDamage(amount, attacker);
        }

        public bool CanReceiveDamage(DamageRequest request)
        {
            if (Object == null || NetDead || !IsFinitePositive(request.Amount))
                return false;

            if (request.SourceObjectId.IsValid && request.SourceObjectId == Object.Id)
                return false;

            return request.AttackerTeamId < 0 || DamageTeamId < 0 ||
                   request.AttackerTeamId != DamageTeamId;
        }

        public DamageResult RequestDamage(DamageRequest request)
        {
            if (!CanReceiveDamage(request))
                return DamageResult.Rejected(request.Amount, DamageRejectionReason.InvalidTarget);

            if (Object.HasStateAuthority)
                return ApplyDamage(request);

            Rpc_RequestDamage(
                request.Amount,
                request.Attacker,
                request.SourceObjectId,
                request.AttackerTeamId,
                request.Source,
                request.SkillId,
                request.HitPosition,
                request.HitDirection);
            return DamageResult.Queued(request.Amount);
        }

        public void RequestHeal(float amount)
        {
            if (Object == null || !IsFinitePositive(amount))
                return;

            if (Object.HasStateAuthority)
                ApplyHeal(amount);
            else
                Rpc_RequestHeal(amount);
        }

        public void SetGameplayLocked(bool locked)
        {
            if (Object != null && Object.HasStateAuthority)
                NetGameplayLocked = locked;
        }

        /// <summary>맵 오브젝트(낙사존 등)에 의한 상승. 전투 피격 넉백(내부 ApplyKnockback)과는
        /// 별개의 매커니즘 — 스킬에 맞아서 밀리는 게 아니라 환경 요소에 튕겨오르는 것임을
        /// 호출부에서 바로 구분할 수 있게 이름을 나눠뒀다. velocity 동안 duration초만큼
        /// 중력 없이 그 속도를 유지하다가(체공시간으로 높이를 번다) 정상 중력으로 돌아온다.</summary>
        public void ApplyMapBounce(float velocity, float duration)
        {
            if (Object == null || !Object.HasStateAuthority)
                return;

            movement.ApplyMapBounce(velocity, duration);
        }

        public void ResetCharacter(Vector2 position)
        {
            if (Object == null || !Object.HasStateAuthority)
                return;

            float previousHealth = health.Current;
            // 라운드 사이 체력 증강(방탄복/유리 대포)이 새로 생겼을 수 있으니 라운드 시작마다 최대
            // 체력을 다시 계산해서 반영한 뒤 채운다.
            health.SetMaxHealth(MaxHealth);
            health.FullHeal();
            if (!Mathf.Approximately(previousHealth, health.Current))
                OnHealthChanged(previousHealth, health.Current);
            NetDead = false;
            ResetCommonState();
            movement.Reset(position);
            OnResetCharacter();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void Rpc_RequestLegacyDamage(float amount, PlayerRef attacker, RpcInfo info = default)
        {
            if (attacker == PlayerRef.None || info.Source != attacker)
                return;
            ApplyDamage(new DamageRequest(
                amount,
                attacker,
                default,
                attacker != PlayerRef.None ? attacker.PlayerId : -1,
                CharacterDamageSource.Direct));
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void Rpc_RequestDamage(
            float amount,
            PlayerRef attacker,
            NetworkId sourceObjectId,
            int attackerTeamId,
            CharacterDamageSource source,
            int skillId,
            Vector2 hitPosition,
            Vector2 hitDirection,
            RpcInfo info = default)
        {
            DamageRequest request = new DamageRequest(
                amount,
                attacker,
                sourceObjectId,
                attackerTeamId,
                source,
                skillId,
                hitPosition,
                hitDirection);
            if (IsValidDamageRpcSource(request, info.Source) && CanReceiveDamage(request))
                ApplyDamage(request);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)] 
        private void Rpc_RequestHeal(float amount, RpcInfo info = default)
        {
            if (info.Source == DamageOwner && IsFinitePositive(amount))
                ApplyHeal(amount);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void Rpc_RequestSlow(
            float ratio,
            float duration,
            NetworkId sourceObjectId,
            PlayerRef attacker,
            int attackerTeamId,
            RpcInfo info = default)
        {
            DamageRequest sourceRequest = new DamageRequest(
                1f,
                attacker,
                sourceObjectId,
                attackerTeamId,
                CharacterDamageSource.Direct);
            if (IsFiniteNonNegative(ratio) && IsFiniteNonNegative(duration) &&
                IsValidDamageRpcSource(sourceRequest, info.Source) && CanReceiveDamage(sourceRequest))
            {
                ApplySlowAuthority(ratio, duration);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_PlayActionEffect(CharacterActionType action, Vector2 position, float angle)
        {
            visual?.SpawnActionEffect(action, position, angle);
        }

        // 궁극기 사용 성공 시 공통 반짝임 파티클(ultimateFlashEffect)을 전체 클라이언트에서 재생
        private void PlayUltimateFlashEffect()
        {
            if (Object != null && Object.HasStateAuthority)
                Rpc_PlayUltimateFlashEffect();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void Rpc_PlayUltimateFlashEffect()
        {
            ultimateFlashEffect?.Play();
        }

        protected void DealDamage(CharacterBase target, float amount)
        {
            if (target == null || target == this || amount <= 0f)
                return;

            PlayerRef attacker = DamageOwner;
            DealDamageThroughPipeline(target, amount, attacker, CharacterDamageSource.Direct);
        }

        internal void DealProjectileDamage(CharacterBase target, float amount)
        {
            if (target == null || target == this || amount <= 0f)
                return;

            PlayerRef attacker = DamageOwner;
            DealDamageThroughPipeline(target, amount, attacker, CharacterDamageSource.Projectile);
        }

        internal void DealProjectileDamage(IDamageable target, float amount)
        {
            DealDamageToDamageable(target, amount, CharacterDamageSource.Projectile);
        }

        protected void DealDamage(
            IDamageable target,
            float amount,
            CharacterDamageSource source = CharacterDamageSource.Direct)
        {
            DealDamageToDamageable(target, amount, source);
        }

        /// <summary>투사체가 벽/바닥에서 폭발할 때(예: 폭발 마법 증강) 호출된다. 반경 안의
        /// IDamageable 전부에게 데미지를 준다 — 어떤 캐릭터의 투사체든 공용으로 쓸 수 있게
        /// 프레임워크 레벨에 둔다. 공격력 증강 배율은 DealDamage 경로에서 다시 적용되므로
        /// 여기 넘기는 damage는 "이미 계산된 폭발 피해량"이어야 한다(중복 곱산 주의).</summary>
        internal void DetonateProjectileExplosion(Vector2 position, float damage, float radius, LayerMask targetLayer)
        {
            if (!HasStateAuthority || damage <= 0f || Runner == null || Runner.IsResimulation)
                return;

            foreach (IDamageable target in FindDamageablesInCircle(position, radius, targetLayer))
                DealDamage(target, damage, CharacterDamageSource.Projectile);
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
            // 공격력 증강(대형 탄약집/유리 대포/버서커)을 캐릭터별 ModifyOutgoingDamage보다 먼저
            // 적용한다 — 이게 여러 호출부(DealDamage/DealProjectileDamage/DealDamageToDamageable)의
            // 유일한 합류점이라 여기 한 곳에서만 곱한다.
            float augmentedAmount = amount * AttackMultiplier;

            DamageRequest request = new DamageRequest(
                augmentedAmount,
                attacker,
                Object != null ? Object.Id : default,
                DamageTeamId,
                source);
            if (!target.CanReceiveDamage(request))
                return;

            CharacterDamagePipeline pipeline = new CharacterDamagePipeline(
                (damage, damageSource) => ModifyOutgoingDamage(target, damage, damageSource),
                damage => target.RequestDamage(request.WithAmount(damage)),
                null);
            pipeline.Apply(augmentedAmount, source);
        }

        private void DealDamageToDamageable(
            IDamageable target,
            float amount,
            CharacterDamageSource source)
        {
            if (target == null || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
                return;
            if (Object == null)
                return;

            if (target is CharacterBase characterTarget)
            {
                if (characterTarget == this)
                    return;
                // 배율은 DealDamageThroughPipeline 안에서 한 번만 적용된다 — 원본 amount를 그대로 넘긴다.
                DealDamageThroughPipeline(characterTarget, amount, DamageOwner, source);
                return;
            }

            // 캐릭터가 아닌 IDamageable(설치물 등)은 이 경로가 유일한 합류점이라 여기서 적용한다.
            float augmentedAmount = amount * AttackMultiplier;

            DamageRequest request = new DamageRequest(
                augmentedAmount,
                DamageOwner,
                Object.Id,
                DamageTeamId,
                source);
            if (!target.CanReceiveDamage(request))
                return;

            float finalDamage = ModifyOutgoingDamage(target, augmentedAmount, source);
            request = request.WithAmount(finalDamage);
            if (!target.CanReceiveDamage(request))
                return;

            // State Authority가 실제 적용량을 확정한 뒤 ConfirmOwnedEntityDamage로 통보한다.
            // Queued 응답에서는 게이지나 적중 콜백을 선반영하지 않는다.
            target.RequestDamage(request);
        }

        internal void ConfirmOwnedEntityDamage(
            NetworkId targetId,
            float appliedDamage,
            CharacterDamageSource source)
        {
            if (Object == null || !IsFinitePositive(appliedDamage))
                return;

            if (Object.HasStateAuthority)
                ApplyOwnedEntityDamageConfirmation(targetId, appliedDamage, source);
            else
                Rpc_ConfirmOwnedEntityDamage(targetId, appliedDamage, source);
        }

        internal void ConfirmCharacterDamage(
            NetworkId targetId,
            float appliedDamage,
            CharacterDamageSource source)
        {
            if (Object == null || !IsFinitePositive(appliedDamage))
                return;

            if (Object.HasStateAuthority)
                ApplyCharacterDamageConfirmation(targetId, appliedDamage, source);
            else
                Rpc_ConfirmCharacterDamage(targetId, appliedDamage, source);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void Rpc_ConfirmCharacterDamage(
            NetworkId targetId,
            float appliedDamage,
            CharacterDamageSource source,
            RpcInfo info = default)
        {
            if (IsValidDamageConfirmationSource(targetId, info.Source))
                ApplyCharacterDamageConfirmation(targetId, appliedDamage, source);
        }

        private void ApplyCharacterDamageConfirmation(
            NetworkId targetId,
            float appliedDamage,
            CharacterDamageSource source)
        {
            if (!HasStateAuthority || !IsFinitePositive(appliedDamage) || Runner == null ||
                !Runner.TryFindObject(targetId, out NetworkObject targetObject))
            {
                return;
            }

            CharacterBase target = targetObject.GetComponent<CharacterBase>();
            if (target == null)
                return;

            AddUltimateGaugeFromDamageDealt(appliedDamage);
            OnDamageDealt(target, appliedDamage);
            OnDamageableDealt(target, appliedDamage, source);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void Rpc_ConfirmOwnedEntityDamage(
            NetworkId targetId,
            float appliedDamage,
            CharacterDamageSource source,
            RpcInfo info = default)
        {
            if (IsValidDamageConfirmationSource(targetId, info.Source))
                ApplyOwnedEntityDamageConfirmation(targetId, appliedDamage, source);
        }

        private void ApplyOwnedEntityDamageConfirmation(
            NetworkId targetId,
            float appliedDamage,
            CharacterDamageSource source)
        {
            if (!HasStateAuthority || !IsFinitePositive(appliedDamage))
                return;

            AddUltimateGaugeFromDamageDealt(appliedDamage);
            OnOwnedEntityDamageDealt(targetId, appliedDamage, source);
            if (Runner != null && Runner.TryFindObject(targetId, out NetworkObject targetObject))
            {
                CharacterOwnedEntity target = targetObject.GetComponent<CharacterOwnedEntity>();
                if (target != null)
                    OnDamageableDealt(target, appliedDamage, source);
            }
        }

        /// <summary>게이지형 궁극기(IsUltimateGaugeMode)를 쓰는 캐릭터가 적에게 입힌 데미지만큼
        /// 게이지를 채운다. 게이지 모드가 아니면 아무 일도 안 한다.</summary>
        private void AddUltimateGaugeFromDamageDealt(float damage)
        {
            if (!HasStateAuthority || !IsUltimateGaugeMode || damage <= 0f)
                return;

            float next = NetUltimateGauge + damage * definition.UltimateGaugePerDamageDealt * UltimateGaugeRateMultiplier;
            NetUltimateGauge = Mathf.Clamp(next, 0f, definition.UltimateGaugeMax);
        }

        protected void Heal(float amount)
        {
            if (Object != null && Object.HasStateAuthority)
                ApplyHeal(amount);
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
            if (!HasStateAuthority || target == null || target.Object == null || target == this ||
                !IsFiniteNonNegative(slowRatio) || !IsFiniteNonNegative(duration))
                return;

            DamageRequest sourceRequest = new DamageRequest(
                1f,
                DamageOwner,
                Object.Id,
                DamageTeamId,
                CharacterDamageSource.Direct);
            if (!target.CanReceiveDamage(sourceRequest))
                return;

            if (target.HasStateAuthority)
                target.ApplySlowAuthority(slowRatio, duration);
            else
                target.Rpc_RequestSlow(slowRatio, duration, Object.Id, DamageOwner, DamageTeamId);
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

        protected CharacterProjectile SpawnProjectile(
            CharacterProjectile projectilePrefab,
            Vector2 position,
            Vector2 direction,
            float speed,
            float damage,
            LayerMask targetLayer,
            int skillId = 0)
        {
            if (projectilePrefab == null || Runner == null || Object == null || !Object.HasStateAuthority ||
                projectilePrefab.GetComponent<NetworkObject>() == null ||
                projectilePrefab.GetComponents<NetworkTRSP>().Length != 1)
                return null;

            Vector2 normalized = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : new Vector2(NetFacing >= 0 ? 1f : -1f, 0f);

            CharacterProjectile spawnedProjectile = Runner.Spawn(
                projectilePrefab,
                position,
                Quaternion.identity,
                Object.InputAuthority,
                (_, spawnedObject) =>
                {
                    CharacterProjectile projectile = spawnedObject.GetComponent<CharacterProjectile>();
                    projectile?.Initialize(
                        normalized,
                        speed,
                        damage,
                        targetLayer,
                        DamageOwner,
                        Object.Id,
                        DamageTeamId,
                        skillId);
                });
            return spawnedProjectile;
        }

        protected OwnedEntitySpawnResult<T> SpawnOwnedEntity<T>(
            T prefab,
            in OwnedEntitySpawnRequest request,
            Action<T> initialize = null)
            where T : CharacterOwnedEntity
        {
            if (prefab == null || prefab.GetComponent<NetworkObject>() == null)
                return OwnedEntitySpawnResult<T>.Failed(OwnedEntitySpawnFailureReason.InvalidPrefab);
            if (Runner == null || Object == null || !Object.HasStateAuthority)
                return OwnedEntitySpawnResult<T>.Failed(OwnedEntitySpawnFailureReason.AuthorityUnavailable);
            if (request.OwnerExitPolicy == OwnedEntityOwnerExitPolicy.ExpireNormally &&
                !prefab.CanExpireAfterOwnerExit)
            {
                return OwnedEntitySpawnResult<T>.Failed(OwnedEntitySpawnFailureReason.UnsupportedPolicy);
            }
            if (request.InitialVelocity.sqrMagnitude > 0f)
            {
                Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
                if (body == null || body.bodyType != RigidbodyType2D.Dynamic ||
                    prefab.GetComponent<Fusion.Addons.Physics.NetworkRigidbody2D>() == null)
                {
                    return OwnedEntitySpawnResult<T>.Failed(OwnedEntitySpawnFailureReason.InvalidPrefab);
                }
            }

            if (ownedEntityRegistry == null)
                ownedEntityRegistry = new CharacterOwnedEntityRegistry();

            if (!ownedEntityRegistry.TrySelectOverflowEntities(
                    request,
                    out IReadOnlyList<CharacterOwnedEntity> replacements,
                    out OwnedEntitySpawnFailureReason failureReason))
            {
                return OwnedEntitySpawnResult<T>.Failed(failureReason);
            }

            int creationSequence = ownedEntityRegistry.ReserveCreationSequence();
            OwnedEntitySpawnRequest spawnRequest = request;
            PlayerRef ownerPlayer = DamageOwner;
            NetworkObject spawnedObject = Runner.Spawn(
                prefab.gameObject,
                spawnRequest.Position,
                spawnRequest.Rotation,
                Object.InputAuthority,
                (_, networkObject) =>
                {
                    T entity = networkObject.GetComponent<T>();
                    entity?.InitializeOwnedEntity(
                        Object.Id,
                        ownerPlayer,
                        DamageTeamId,
                        spawnRequest,
                        creationSequence);
                    if (entity != null)
                        initialize?.Invoke(entity);
                });

            T spawnedEntity = spawnedObject != null ? spawnedObject.GetComponent<T>() : null;
            if (spawnedEntity == null)
            {
                if (spawnedObject != null && spawnedObject.IsValid)
                    Runner.Despawn(spawnedObject);
                return OwnedEntitySpawnResult<T>.Failed(OwnedEntitySpawnFailureReason.SpawnFailed);
            }

            if (!ownedEntityRegistry.Contains(spawnedEntity) && !ownedEntityRegistry.Register(spawnedEntity))
            {
                spawnedEntity.RequestDestroy(OwnedEntityDestroyReason.Manual);
                return OwnedEntitySpawnResult<T>.Failed(OwnedEntitySpawnFailureReason.RegistrationFailed);
            }

            foreach (CharacterOwnedEntity replacement in replacements)
            {
                if (replacement != null && !replacement.RequestDestroy(OwnedEntityDestroyReason.LimitExceeded))
                {
                    spawnedEntity.RequestDestroy(OwnedEntityDestroyReason.Manual);
                    return OwnedEntitySpawnResult<T>.Failed(OwnedEntitySpawnFailureReason.SpawnFailed);
                }
            }

            return OwnedEntitySpawnResult<T>.Succeeded(spawnedEntity);
        }

        /// <summary>
        /// 스킬 종류를 그룹으로 사용해 소유 오브젝트를 생성한다.
        /// OwnedEntityGroupId와 OwnedEntitySpawnRequest는 내부에서 구성한다.
        /// 생성에 실패하면 null을 반환한다. 실패 이유가 필요하면 상세 SpawnOwnedEntity 오버로드를 사용한다.
        /// </summary>
        protected T SpawnOwnedEntity<T>(
            T prefab,
            CharacterActionType action,
            Vector2 position,
            int maxCount = 1,
            bool replaceOldest = true,
            Vector2 initialVelocity = default,
            Action<T> initialize = null)
            where T : CharacterOwnedEntity
        {
            if (action == CharacterActionType.None)
                return null;

            OwnedEntitySpawnRequest request = new OwnedEntitySpawnRequest(
                position,
                Quaternion.identity,
                GetOwnedEntityGroup(action),
                maxCount,
                replaceOldest
                    ? OwnedEntityOverflowPolicy.DestroyOldest
                    : OwnedEntityOverflowPolicy.RejectNew,
                OwnedEntityOwnerExitPolicy.Destroy,
                initialVelocity);

            return SpawnOwnedEntity(prefab, request, initialize).Entity;
        }

        protected OwnedEntitySpawnResult<T> SpawnThrowable<T>(
            T prefab,
            in OwnedEntitySpawnRequest request,
            Action<T> initialize = null)
            where T : CharacterThrowable
        {
            Rigidbody2D body = prefab != null ? prefab.GetComponent<Rigidbody2D>() : null;
            Collider2D collider = prefab != null ? prefab.GetComponent<Collider2D>() : null;
            if (prefab == null || body == null || body.bodyType != RigidbodyType2D.Dynamic ||
                collider == null || collider.isTrigger ||
                prefab.GetComponent<Fusion.Addons.Physics.NetworkRigidbody2D>() == null ||
                prefab.GetComponents<NetworkTRSP>().Length != 1 ||
                !prefab.HasValidFuseConfiguration)
            {
                return OwnedEntitySpawnResult<T>.Failed(OwnedEntitySpawnFailureReason.InvalidPrefab);
            }

            return SpawnOwnedEntity(prefab, request, initialize);
        }

        /// <summary>
        /// direction과 speed로 초기 속도를 계산해 물리 투척체를 생성한다.
        /// 스킬 종류별 개수 제한과 초과 처리 방식을 적용한다.
        /// 생성에 실패하면 null을 반환한다.
        /// </summary>
        protected T SpawnThrowable<T>(
            T prefab,
            CharacterActionType action,
            Vector2 position,
            Vector2 direction,
            float speed,
            int maxCount = 1,
            bool replaceOldest = true,
            Action<T> initialize = null)
            where T : CharacterThrowable
        {
            if (action == CharacterActionType.None)
                return null;

            Vector2 normalized = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : new Vector2(NetFacing >= 0 ? 1f : -1f, 0f);

            OwnedEntitySpawnRequest request = new OwnedEntitySpawnRequest(
                position,
                Quaternion.identity,
                GetOwnedEntityGroup(action),
                maxCount,
                replaceOldest
                    ? OwnedEntityOverflowPolicy.DestroyOldest
                    : OwnedEntityOverflowPolicy.RejectNew,
                OwnedEntityOwnerExitPolicy.Destroy,
                normalized * Mathf.Max(0f, speed));

            return SpawnThrowable(prefab, request, initialize).Entity;
        }

        protected bool StartThrowableFuse(CharacterThrowable throwable)
        {
            return throwable != null && ownedEntityRegistry != null &&
                   ownedEntityRegistry.Contains(throwable) &&
                   throwable.TryStartFuse(CharacterThrowableFuseTrigger.Manual);
        }

        protected bool DestroyOwnedEntity(
            CharacterOwnedEntity entity,
            OwnedEntityDestroyReason reason)
        {
            if (entity == null || ownedEntityRegistry == null || !ownedEntityRegistry.Contains(entity))
                return false;
            return entity.RequestDestroy(reason);
        }

        /// <summary>스킬로 생성한 소유 오브젝트 하나를 제거한다.</summary>
        protected bool DestroyOwnedEntity(CharacterOwnedEntity entity)
        {
            return DestroyOwnedEntity(entity, OwnedEntityDestroyReason.SkillTriggered);
        }

        protected IReadOnlyList<T> GetOwnedEntities<T>(OwnedEntityGroupId group)
            where T : CharacterOwnedEntity
        {
            return ownedEntityRegistry != null
                ? ownedEntityRegistry.Get<T>(group)
                : new List<T>().AsReadOnly();
        }

        /// <summary>해당 행동이 생성한 소유 오브젝트를 조회한다.</summary>
        protected IReadOnlyList<T> GetOwnedEntities<T>(CharacterActionType action)
            where T : CharacterOwnedEntity
        {
            return action == CharacterActionType.None
                ? new List<T>().AsReadOnly()
                : GetOwnedEntities<T>(GetOwnedEntityGroup(action));
        }

        protected int DestroyOwnedEntities(
            OwnedEntityGroupId group,
            OwnedEntityDestroyReason reason)
        {
            IReadOnlyList<CharacterOwnedEntity> entities = GetOwnedEntities<CharacterOwnedEntity>(group);
            int destroyed = 0;
            foreach (CharacterOwnedEntity entity in entities)
            {
                if (entity.RequestDestroy(reason))
                    destroyed++;
            }

            return destroyed;
        }

        /// <summary>해당 행동이 생성한 소유 오브젝트를 모두 제거한다.</summary>
        protected int DestroyOwnedEntities(
            CharacterActionType action,
            OwnedEntityDestroyReason reason = OwnedEntityDestroyReason.SkillTriggered)
        {
            return action == CharacterActionType.None
                ? 0
                : DestroyOwnedEntities(GetOwnedEntityGroup(action), reason);
        }

        private static OwnedEntityGroupId GetOwnedEntityGroup(CharacterActionType action)
        {
            return new OwnedEntityGroupId((int)action);
        }

        internal void RegisterOwnedEntityFromSpawn(CharacterOwnedEntity entity)
        {
            if (ownedEntityRegistry == null)
                ownedEntityRegistry = new CharacterOwnedEntityRegistry();
            if (!ownedEntityRegistry.Contains(entity))
                ownedEntityRegistry.Register(entity);
        }

        internal void UnregisterOwnedEntity(CharacterOwnedEntity entity)
        {
            ownedEntityRegistry?.Unregister(entity);
        }

        internal void NotifyOwnedEntityDestroyed(
            CharacterOwnedEntity entity,
            OwnedEntityDestroyReason reason)
        {
            if (HasStateAuthority && entity != null)
                OnOwnedEntityDestroyed(entity, reason);
            ownedEntityRegistry?.Unregister(entity);
        }

        internal void DealOwnedEntityDamage(
            IDamageable target,
            float amount,
            CharacterDamageSource source)
        {
            DealDamageToDamageable(target, amount, source);
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

        protected List<IDamageable> FindDamageablesInCircle(
            Vector2 center,
            float radius,
            LayerMask targetLayer)
        {
            return CharacterCombatQuery2D.DamageablesInCircle(this, center, radius, targetLayer);
        }

        protected List<IDamageable> FindDamageablesInBox(
            Vector2 center,
            Vector2 size,
            float angle,
            LayerMask targetLayer)
        {
            return CharacterCombatQuery2D.DamageablesInBox(this, center, size, angle, targetLayer);
        }

        protected List<IDamageable> FindDamageablesInLine(
            Vector2 origin,
            Vector2 direction,
            float distance,
            float width,
            LayerMask targetLayer)
        {
            return CharacterCombatQuery2D.DamageablesInLine(
                this,
                origin,
                direction,
                distance,
                width,
                targetLayer);
        }

        protected List<IDamageable> FindDamageablesInArc(
            Vector2 origin,
            Vector2 direction,
            float radius,
            float angle,
            LayerMask targetLayer)
        {
            return CharacterCombatQuery2D.DamageablesInArc(
                this,
                origin,
                direction,
                radius,
                angle,
                targetLayer);
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

            if (action == CharacterActionType.Ultimate)
                PlayUltimateFlashEffect();

            actionState.ConsumeCharge(action);
            if (action == CharacterActionType.Ultimate && IsUltimateGaugeMode)
                NetUltimateGauge = 0f;
            else if (action == CharacterActionType.Dash && actionState.ShouldStartCooldownAutomatically(action))
                // 추진력 강화 증강(DashCooldownMultiplier)만 적용받는 유일한 액션이라 여기서 따로 계산한다.
                actionState.StartCooldown(action, actionState.GetCooldownDuration(action) * DashCooldownMultiplier);
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

        private DamageResult ApplyDamage(DamageRequest request)
        {
            if (!CanReceiveDamage(request))
                return DamageResult.Rejected(request.Amount, DamageRejectionReason.InvalidTarget);

            float before = health.Current;
            float applied = health.ApplyDamage(request.Amount);
            if (applied <= 0f)
                return DamageResult.Rejected(request.Amount, DamageRejectionReason.InvalidAmount);

            NetDamageSequence++;
            CharacterDamageInfo info = new CharacterDamageInfo(request, applied);
            OnDamaged(info);
            OnHealthChanged(before, health.Current);
            ApplyAugmentReflect(applied);

            if (health.IsDead && !NetDead)
            {
                NetDead = true;
                rigidbody2D.linearVelocity = Vector2.zero;
                ResetCommonState();
                DestroyOwnedEntitiesForOwnerDeath();
                OnDied(request.Attacker);
            }
            else
            {
                // 죽는 타격이 아닐 때만 넉백을 건다 — 어차피 죽는 순간 위의 ResetCommonState()가
                // 속도를 0으로 되돌리고 대시/넉백을 취소해버려서(그래야 사망 연출/카메라 포커스가
                // 제자리에서 안정적으로 잡힘), 죽는 타격에 걸어봐야 바로 지워진다.
                ApplyKnockback(request.Attacker);
            }

            DamageResult result = DamageResult.Applied(request.Amount, applied, health.Current, health.IsDead);
            ConfirmCharacterDamageSource(request, result);
            return result;
        }

        protected virtual int ResolveDamageTeamId()
        {
            PlayerRef owner = DamageOwner;
            return owner != PlayerRef.None
                ? owner.PlayerId
                : -1;
        }

        private PlayerRef ResolveDamageOwner()
        {
            if (Object == null)
                return PlayerRef.None;
            if (Runner != null && Runner.GameMode == GameMode.Shared)
                return Object.StateAuthority;
            return Object.InputAuthority;
        }

        private bool IsValidDamageRpcSource(DamageRequest request, PlayerRef rpcSource)
        {
            if (request.Attacker == PlayerRef.None || request.Attacker != rpcSource ||
                !request.SourceObjectId.IsValid || Runner == null ||
                !Runner.TryFindObject(request.SourceObjectId, out NetworkObject sourceObject))
            {
                return false;
            }

            CharacterBase sourceCharacter = sourceObject.GetComponent<CharacterBase>();
            return sourceCharacter != null && sourceCharacter.DamageOwner == rpcSource &&
                   sourceCharacter.DamageTeamId == request.AttackerTeamId;
        }

        private bool IsValidDamageConfirmationSource(NetworkId targetId, PlayerRef rpcSource)
        {
            return targetId.IsValid && rpcSource != PlayerRef.None && Runner != null &&
                   Runner.TryFindObject(targetId, out NetworkObject targetObject) &&
                   targetObject.StateAuthority == rpcSource;
        }

        private void ConfirmCharacterDamageSource(DamageRequest request, DamageResult result)
        {
            if (result.AppliedDamage <= 0f || !request.SourceObjectId.IsValid || Runner == null ||
                !Runner.TryFindObject(request.SourceObjectId, out NetworkObject sourceObject))
            {
                return;
            }

            CharacterBase source = sourceObject.GetComponent<CharacterBase>();
            source?.ConfirmCharacterDamage(Object.Id, result.AppliedDamage, request.Source);
        }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void DestroyOwnedEntitiesForOwnerDeath()
        {
            if (ownedEntityRegistry == null)
                return;

            IReadOnlyList<CharacterOwnedEntity> entities = ownedEntityRegistry.GetAll();
            foreach (CharacterOwnedEntity entity in entities)
            {
                if (entity.DestroyWhenOwnerDies)
                    entity.RequestDestroy(OwnedEntityDestroyReason.OwnerDied);
            }
        }

        private void DestroyOwnedEntitiesForOwnerExit(OwnedEntityDestroyReason reason)
        {
            if (ownedEntityRegistry == null)
                return;

            IReadOnlyList<CharacterOwnedEntity> entities = ownedEntityRegistry.GetAll();
            foreach (CharacterOwnedEntity entity in entities)
            {
                if (entity.OwnerExitPolicy == OwnedEntityOwnerExitPolicy.Destroy)
                    entity.RequestDestroy(reason);
            }
        }

        /// <summary>캐릭터별 고정 세기로 밀려나는 넉백 + 그 동안 이동/행동을 막는 히트스턴을 건다.
        /// 데미지량과 무관하게 항상 같은 세기다(예전엔 데미지 비례였으나 의도적으로 뺌 — 캐릭터마다
        /// 데미지 스케일이 달라서 큰 피해를 주는 캐릭터가 넉백까지 비정상적으로 세지는 문제가 있었음).</summary>
        private void ApplyKnockback(PlayerRef attacker)
        {
            float force = definition.KnockbackBaseForce;
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
            CharacterBase attackerCharacter = All.Find(c => c != null && c.DamageOwner == attacker);
            float horizontal = attackerCharacter != null
                ? Mathf.Sign(transform.position.x - attackerCharacter.transform.position.x)
                : -FacingDirection;

            return new Vector2(horizontal, definition.KnockbackUpwardBias).normalized;
        }

        private new bool HasStateAuthority => Object != null && Object.HasStateAuthority;

        private void ResetCommonState()
        {
            if (!HasStateAuthority)
                return;

            timers?.CancelAll();
            actionState?.Initialize();
            NetGameplayLocked = false;
            NetUltimateGauge = 0f;
            NetSlowRatio = 0f;
            NetSlowTimer = default;
            NetHitstunTimer = default;
            NetMovementEnabled = true;
            movement?.CancelDash();
            movement?.CancelMapBounce();
            movement?.SetMovementSpeedMultiplier(1f);
            movement?.SetMovementEnabled(true);
        }

        private void ApplySlowAuthority(float ratio, float duration)
        {
            if (HasStateAuthority)
                status.ApplySlow(ratio, duration);
        }

        private float ApplyHeal(float amount)
        {
            if (!HasStateAuthority || !IsFinitePositive(amount))
                return 0f;

            float before = health.Current;
            float applied = health.Heal(amount);
            if (applied > 0f)
                OnHealthChanged(before, health.Current);
            return applied;
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
        protected virtual float ModifyOutgoingDamage(
            IDamageable target,
            float damage,
            CharacterDamageSource source)
        {
            return target is CharacterBase characterTarget
                ? ModifyOutgoingDamage(characterTarget, damage, source)
                : damage;
        }
        protected virtual void OnDamageableDealt(
            IDamageable target,
            float appliedDamage,
            CharacterDamageSource source) { }
        protected virtual void OnOwnedEntityDamageDealt(
            NetworkId targetId,
            float appliedDamage,
            CharacterDamageSource source) { }
        protected virtual void OnOwnedEntityDestroyed(
            CharacterOwnedEntity entity,
            OwnedEntityDestroyReason reason) { }
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
        protected virtual void OnCharacterDespawned() { }
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
