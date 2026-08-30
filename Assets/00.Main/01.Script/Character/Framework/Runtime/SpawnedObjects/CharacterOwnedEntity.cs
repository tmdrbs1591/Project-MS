using Fusion;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    /// <summary>
    /// 캐릭터 스킬이 생성한 네트워크 오브젝트의 공통 소유권, 피해, 수명, 제거 경계다.
    /// 맵 Structure와는 별개의 계층이다.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public abstract class CharacterOwnedEntity : NetworkBehaviour, IDamageable, IStateAuthorityChanged, IPlayerLeft
    {
        [Header("Durability")]
        [SerializeField] private OwnedEntityLifetimeMode lifetimeMode = OwnedEntityLifetimeMode.Manual;
        [Min(0f)] [SerializeField] private float maxHealth = 1f;
        [Min(0f)] [SerializeField] private float duration;
        [SerializeField] private bool allowSelfDamage;
        [SerializeField] private bool allowFriendlyDamage;

        [Header("Owner lifecycle")]
        [SerializeField] private bool destroyWhenOwnerDies = true;

        [Networked] private NetworkId NetOwnerCharacterId { get; set; }
        [Networked] private PlayerRef NetOwnerPlayer { get; set; }
        [Networked] private int NetOwnerTeamId { get; set; }
        [Networked] private OwnedEntityGroupId NetGroup { get; set; }
        [Networked] private int NetCreationSequence { get; set; }
        [Networked] private OwnedEntityOwnerExitPolicy NetOwnerExitPolicy { get; set; }
        [Networked] private NetworkBool NetActive { get; set; }
        [Networked] private NetworkBool NetDestroying { get; set; }
        [Networked] private float NetHealth { get; set; }
        [Networked] private TickTimer NetLifetimeTimer { get; set; }
        [Networked] private TickTimer NetActionIntervalTimer { get; set; }
        [Networked] private TickTimer NetDespawnTimer { get; set; }
        [Networked] private OwnedEntityDestroyReason NetDestroyReason { get; set; }

        private Vector2 initialVelocity;
        private NetworkId cachedOwnerCharacterId;
        private OwnedEntityGroupId cachedGroup;
        private CharacterBase cachedOwner;
        private bool pendingOwnerDisconnect;
        private bool renderedDestroying;

        public NetworkId OwnerCharacterId => cachedOwnerCharacterId.IsValid
            ? cachedOwnerCharacterId
            : NetOwnerCharacterId;
        public PlayerRef DamageOwner => NetOwnerPlayer;
        public int DamageTeamId => NetOwnerTeamId;
        public OwnedEntityGroupId Group => cachedGroup.IsValid ? cachedGroup : NetGroup;
        public int CreationSequence => NetCreationSequence;
        public OwnedEntityOwnerExitPolicy OwnerExitPolicy => NetOwnerExitPolicy;
        public OwnedEntityLifetimeMode LifetimeMode => lifetimeMode;
        public float MaxHealth => OwnedEntityDurabilityRules.UsesHealth(lifetimeMode) ? Mathf.Max(0f, maxHealth) : 0f;
        public float CurrentHealth => NetHealth;
        public float RemainingLifetime => NetLifetimeTimer.RemainingTime(Runner) ?? 0f;
        public bool IsActive => NetActive;
        public bool IsDestroying => NetDestroying;
        public bool DestroyWhenOwnerDies => destroyWhenOwnerDies;
        public OwnedEntityDestroyReason DestroyReason => NetDestroyReason;
        public bool AllowSelfDamage => allowSelfDamage;
        public bool AllowFriendlyDamage => allowFriendlyDamage;
        internal virtual bool CanExpireAfterOwnerExit => OwnedEntityDurabilityRules.UsesDuration(lifetimeMode);
        protected CharacterBase OwnerCharacter => ResolveOwner();

        internal void InitializeOwnedEntity(
            NetworkId ownerCharacterId,
            PlayerRef ownerPlayer,
            int ownerTeamId,
            in OwnedEntitySpawnRequest request,
            int creationSequence)
        {
            cachedOwnerCharacterId = ownerCharacterId;
            cachedGroup = request.Group;
            NetOwnerCharacterId = ownerCharacterId;
            NetOwnerPlayer = ownerPlayer;
            NetOwnerTeamId = ownerTeamId;
            NetGroup = request.Group;
            NetCreationSequence = creationSequence;
            NetOwnerExitPolicy = request.OwnerExitPolicy;
            initialVelocity = request.InitialVelocity;

            // Shared Mode에서 소유 플레이어가 이탈해도 공통 정책이 제거 사유를 결정한다.
            Object.Flags &= ~NetworkObjectFlags.DestroyWhenStateAuthorityLeaves;
            Object.Flags |= NetworkObjectFlags.AllowStateAuthorityOverride;
        }

        public override void Spawned()
        {
            pendingOwnerDisconnect = false;
            renderedDestroying = false;
            if (!cachedOwnerCharacterId.IsValid)
                cachedOwnerCharacterId = NetOwnerCharacterId;
            if (!cachedGroup.IsValid)
                cachedGroup = NetGroup;

            if (Object.HasStateAuthority)
            {
                NetDestroying = false;
                NetDestroyReason = OwnedEntityDestroyReason.None;
                NetHealth = OwnedEntityDurabilityRules.UsesHealth(lifetimeMode)
                    ? Mathf.Max(0f, maxHealth)
                    : 0f;
                NetLifetimeTimer = OwnedEntityDurabilityRules.UsesDuration(lifetimeMode)
                    ? TickTimer.CreateFromSeconds(Runner, Mathf.Max(0f, duration))
                    : TickTimer.None;
                NetActive = true;

                if (initialVelocity.sqrMagnitude > 0f && TryGetComponent(out Rigidbody2D body))
                    body.linearVelocity = initialVelocity;

                OnOwnedEntitySpawnedAuthority();
            }

            ResolveOwner()?.RegisterOwnedEntityFromSpawn(this);
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority)
                return;
            if (NetDestroying)
            {
                if (NetDespawnTimer.Expired(Runner) && Runner != null && Object != null && Object.IsValid)
                    Runner.Despawn(Object);
                return;
            }

            bool healthDepleted = OwnedEntityDurabilityRules.UsesHealth(lifetimeMode) && NetHealth <= 0f;
            bool lifetimeExpired = OwnedEntityDurabilityRules.UsesDuration(lifetimeMode) &&
                                   NetLifetimeTimer.Expired(Runner);
            OwnedEntityDestroyReason reason = OwnedEntityDurabilityRules.ResolveDestructionReason(
                healthDepleted,
                lifetimeExpired);
            if (reason != OwnedEntityDestroyReason.None)
                RequestDestroy(reason);
        }

        public override void Render()
        {
            if (!renderedDestroying && NetDestroying)
            {
                renderedDestroying = true;
                OnOwnedEntityDestroyedRendered(NetDestroyReason);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            cachedOwner?.UnregisterOwnedEntity(this);
            cachedOwner = null;
        }

        public void StateAuthorityChanged()
        {
            if (Object == null || Runner == null || IsDestroying)
                return;

            if (pendingOwnerDisconnect && Object.HasStateAuthority)
            {
                pendingOwnerDisconnect = false;
                cachedOwner = null;
                cachedOwnerCharacterId = default;
                NetOwnerCharacterId = default;
                NetOwnerPlayer = PlayerRef.None;
                if (OwnerExitPolicy == OwnedEntityOwnerExitPolicy.Destroy)
                    RequestDestroy(OwnedEntityDestroyReason.OwnerDisconnected);
                return;
            }
        }

        public void PlayerLeft(PlayerRef player)
        {
            if (Object == null || Runner == null || IsDestroying ||
                !Runner.IsSharedModeMasterClient || Object.StateAuthority != player)
            {
                return;
            }

            pendingOwnerDisconnect = true;
            Object.RequestStateAuthority();
        }

        public bool CanReceiveDamage(DamageRequest request)
        {
            if (IsDestroying)
                return false;

            OwnedEntityDamageRelation relation = ResolveDamageRelation(request);
            return CanReceiveDamageInCurrentState(request) &&
                   OwnedEntityDurabilityRules.CanReceiveDamage(
                       lifetimeMode,
                       relation,
                       allowSelfDamage,
                       allowFriendlyDamage,
                       request.Amount);
        }

        public DamageResult RequestDamage(DamageRequest request)
        {
            if (!CanReceiveDamage(request))
                return DamageResult.Rejected(request.Amount, ResolveDamageRejection(request));

            if (Object.HasStateAuthority)
                return ApplyDamageAuthority(request);

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
                ApplyDamageAuthority(request);
        }

        internal bool RequestDestroy(OwnedEntityDestroyReason reason)
        {
            if (Object == null || !Object.HasStateAuthority || NetDestroying || reason == OwnedEntityDestroyReason.None)
                return false;

            NetDestroying = true;
            NetActive = false;
            NetDestroyReason = reason;
            NetDespawnTimer = TickTimer.CreateFromTicks(Runner, 1);
            OnOwnedEntityDestroyed(reason);

            CharacterBase owner = ResolveOwner();
            owner?.NotifyOwnedEntityDestroyed(this, reason);

            return true;
        }

        protected void SetOwnedEntityActive(bool active)
        {
            if (Object != null && Object.HasStateAuthority && !NetDestroying)
                NetActive = active;
        }

        protected virtual bool CanReceiveDamageInCurrentState(DamageRequest request) => IsActive;
        protected virtual float ModifyIncomingDamage(DamageRequest request, float damage) => damage;
        protected virtual void OnDamageReceived(DamageRequest request, DamageResult result) { }
        protected virtual void OnOwnedEntityHealthChanged(float previous, float current) { }
        protected virtual void OnOwnedEntitySpawnedAuthority() { }
        protected virtual void OnOwnedEntityDestroyed(OwnedEntityDestroyReason reason) { }
        protected virtual void OnOwnedEntityDestroyedRendered(OwnedEntityDestroyReason reason) { }

        /// <summary>소유 캐릭터의 공통 보정, 실제 적용량 확인, 게이지·콜백 흐름을 사용해 피해를 준다.</summary>
        protected void DealDamage(
            IDamageable target,
            float amount,
            CharacterDamageSource source = CharacterDamageSource.Area)
        {
            ResolveOwner()?.DealOwnedEntityDamage(target, amount, source);
        }

        protected System.Collections.Generic.List<IDamageable> FindDamageablesInCircle(
            Vector2 center,
            float radius,
            LayerMask targetLayer)
        {
            return CharacterCombatQuery2D.DamageablesInCircle(ResolveOwner(), center, radius, targetLayer);
        }

        /// <summary>
        /// State Authority에서 활성 상태일 때 반복 실행 간격이 지났는지 확인한다.
        /// true를 반환한 순간 다음 간격을 시작한다.
        /// 포탑 공격이나 장판의 주기 효과처럼 일정 시간마다 한 번 실행할 때 사용한다.
        /// </summary>
        protected bool TryUseInterval(float seconds)
        {
            if (Object == null || Runner == null || !Object.HasStateAuthority ||
                !IsActive || IsDestroying)
            {
                return false;
            }

            if (NetActionIntervalTimer.IsRunning && !NetActionIntervalTimer.Expired(Runner))
                return false;

            NetActionIntervalTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0f, seconds));
            return true;
        }

        /// <summary>범위 안에서 처음 찾은 공격 가능한 대상을 반환한다. 대상이 없으면 null이다.</summary>
        protected IDamageable FindFirstDamageableInCircle(
            Vector2 center,
            float radius,
            LayerMask targetLayer)
        {
            System.Collections.Generic.List<IDamageable> targets =
                FindDamageablesInCircle(center, radius, targetLayer);
            return targets.Count > 0 ? targets[0] : null;
        }

        protected System.Collections.Generic.List<IDamageable> FindDamageablesInBox(
            Vector2 center,
            Vector2 size,
            float angle,
            LayerMask targetLayer)
        {
            return CharacterCombatQuery2D.DamageablesInBox(ResolveOwner(), center, size, angle, targetLayer);
        }

        private DamageResult ApplyDamageAuthority(DamageRequest request)
        {
            float modifiedDamage = ModifyIncomingDamage(request, request.Amount);
            if (modifiedDamage <= 0f || float.IsNaN(modifiedDamage) || float.IsInfinity(modifiedDamage))
                return DamageResult.Rejected(request.Amount, DamageRejectionReason.InvalidAmount);

            float previous = NetHealth;
            float applied = Mathf.Min(previous, modifiedDamage);
            NetHealth = Mathf.Max(0f, previous - applied);
            bool destroyed = NetHealth <= 0f;
            DamageResult result = DamageResult.Applied(request.Amount, applied, NetHealth, destroyed);

            OnDamageReceived(request, result);
            OnOwnedEntityHealthChanged(previous, NetHealth);
            ConfirmOwnedEntityDamage(request, result);
            if (destroyed)
                RequestDestroy(OwnedEntityDestroyReason.HealthDepleted);
            return result;
        }

        private OwnedEntityDamageRelation ResolveDamageRelation(DamageRequest request)
        {
            if ((request.SourceObjectId.IsValid && request.SourceObjectId == NetOwnerCharacterId) ||
                (request.Attacker != PlayerRef.None && request.Attacker == NetOwnerPlayer))
            {
                return OwnedEntityDamageRelation.Self;
            }

            if (request.AttackerTeamId >= 0 && request.AttackerTeamId == NetOwnerTeamId)
                return OwnedEntityDamageRelation.Friendly;
            return OwnedEntityDamageRelation.Enemy;
        }

        private DamageRejectionReason ResolveDamageRejection(DamageRequest request)
        {
            if (NetDestroying)
                return DamageRejectionReason.AlreadyDestroying;
            if (request.Amount <= 0f || float.IsNaN(request.Amount) || float.IsInfinity(request.Amount))
                return DamageRejectionReason.InvalidAmount;
            if (!OwnedEntityDurabilityRules.UsesHealth(lifetimeMode) || !CanReceiveDamageInCurrentState(request))
                return DamageRejectionReason.NotDamageable;

            OwnedEntityDamageRelation relation = ResolveDamageRelation(request);
            if (relation == OwnedEntityDamageRelation.Self && !allowSelfDamage)
                return DamageRejectionReason.SelfDamageBlocked;
            if (relation == OwnedEntityDamageRelation.Friendly && !allowFriendlyDamage)
                return DamageRejectionReason.FriendlyDamageBlocked;
            return DamageRejectionReason.InvalidTarget;
        }

        private CharacterBase ResolveOwner()
        {
            if (cachedOwner != null)
                return cachedOwner;

            NetworkId ownerCharacterId = OwnerCharacterId;
            if (Runner == null || !ownerCharacterId.IsValid ||
                !Runner.TryFindObject(ownerCharacterId, out NetworkObject ownerObject))
            {
                return null;
            }

            cachedOwner = ownerObject.GetComponent<CharacterBase>();
            return cachedOwner;
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

        private void ConfirmOwnedEntityDamage(DamageRequest request, DamageResult result)
        {
            if (result.AppliedDamage <= 0f || !request.SourceObjectId.IsValid || Runner == null ||
                !Runner.TryFindObject(request.SourceObjectId, out NetworkObject sourceObject))
            {
                return;
            }

            CharacterBase source = sourceObject.GetComponent<CharacterBase>();
            source?.ConfirmOwnedEntityDamage(Object.Id, result.AppliedDamage, request.Source);
        }

    }
}
