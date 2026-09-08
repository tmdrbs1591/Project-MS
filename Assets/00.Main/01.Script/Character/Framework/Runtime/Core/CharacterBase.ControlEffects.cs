using Fusion;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    /// <summary>매핑 입력 관찰과 조작·조준·커서 방해를 처리한다.</summary>
    public abstract partial class CharacterBase
    {
        private const int ControlSealSlotCount = 7;
        private const int ObservedInputSlotCount = 6;

        [Networked, Capacity(ControlSealSlotCount)]
        private NetworkArray<TickTimer> NetControlSealTimers => default;
        [Networked, Capacity(ObservedInputSlotCount)]
        private NetworkArray<int> NetObservedInputSequences => default;
        [Networked] private float NetObservedMoveDirection { get; set; }
        [Networked] private NetworkBool NetObservedJumpHeld { get; set; }

        [Networked] private TickTimer NetAimInversionTimer { get; set; }
        [Networked] private TickTimer NetAimAngleOffsetTimer { get; set; }
        [Networked] private float NetAimAngleOffsetDegrees { get; set; }

        [Networked] private TickTimer NetCrosshairVisibilityTimer { get; set; }
        [Networked] private int NetCrosshairVisibilityMode { get; set; }
        [Networked] private TickTimer NetSystemCursorVisibilityTimer { get; set; }
        [Networked] private int NetSystemCursorVisibilityMode { get; set; }
        [Networked] private TickTimer NetCrosshairOffsetTimer { get; set; }
        [Networked] private Vector2 NetCrosshairOffset { get; set; }

        private CharacterInputSequenceTracker inputSequenceTracker;
        private CharacterAimCursorPresenter aimCursorPresenter;

        /// <summary>대상의 지정한 조작을 duration 초 동안 막는다.</summary>
        protected void ApplyControlSeal(
            CharacterBase target,
            CharacterControlType controls,
            float duration)
        {
            if (!CanRequestHostileControlEffect(target, duration) ||
                (controls & CharacterControlType.All) == CharacterControlType.None)
            {
                return;
            }

            CharacterControlType sanitized = controls & CharacterControlType.All;
            if (target.HasStateAuthority)
                target.ApplyControlSealAuthority(sanitized, duration);
            else
                target.Rpc_RequestControlSeal(sanitized, duration, Object.Id, DamageOwner, DamageTeamId);
        }

        /// <summary>대상의 실제 조준 방향을 duration 초 동안 반대로 바꾼다.</summary>
        protected void ApplyAimInversion(CharacterBase target, float duration)
        {
            if (!CanRequestHostileControlEffect(target, duration))
                return;

            if (target.HasStateAuthority)
                target.ApplyAimInversionAuthority(duration);
            else
                target.Rpc_RequestAimInversion(duration, Object.Id, DamageOwner, DamageTeamId);
        }

        /// <summary>대상의 실제 조준 방향에 지정한 각도를 duration 초 동안 더한다.</summary>
        protected void ApplyAimAngleOffset(CharacterBase target, float degrees, float duration)
        {
            if (!CanRequestHostileControlEffect(target, duration) || !IsFiniteNumber(degrees))
                return;

            if (target.HasStateAuthority)
                target.ApplyAimAngleOffsetAuthority(degrees, duration);
            else
                target.Rpc_RequestAimAngleOffset(degrees, duration, Object.Id, DamageOwner, DamageTeamId);
        }

        /// <summary>대상의 조준선을 duration 초 동안 표시하거나 숨긴다.</summary>
        protected void SetCrosshairVisible(CharacterBase target, bool visible, float duration)
        {
            RequestCrosshairState(target, visible ? 2 : 1, duration, CursorEffectKind.CrosshairVisibility);
        }

        /// <summary>대상의 시스템 마우스 커서를 duration 초 동안 표시하거나 숨긴다.</summary>
        protected void SetSystemCursorVisible(CharacterBase target, bool visible, float duration)
        {
            RequestCrosshairState(target, visible ? 2 : 1, duration, CursorEffectKind.SystemCursorVisibility);
        }

        /// <summary>대상의 조준선 화면 위치를 duration 초 동안 픽셀 단위로 옮긴다.</summary>
        protected void SetCrosshairOffset(CharacterBase target, Vector2 screenOffset, float duration)
        {
            if (!CanRequestHostileControlEffect(target, duration) || !CharacterControlRules.IsFinite(screenOffset))
                return;

            if (target.HasStateAuthority)
                target.ApplyCrosshairOffsetAuthority(screenOffset, duration);
            else
                target.Rpc_RequestCrosshairOffset(screenOffset, duration, Object.Id, DamageOwner, DamageTeamId);
        }

        /// <summary>대상의 기본 커서를 숨기고 조준선을 duration 초 동안 표시한다.</summary>
        protected void ReplaceCursorWithCrosshair(CharacterBase target, float duration)
        {
            SetCrosshairVisible(target, true, duration);
            SetSystemCursorVisible(target, false, duration);
        }

        /// <summary>대상의 매핑된 버튼 입력이 지난 확인 이후 새로 들어왔는지 확인한다.</summary>
        protected bool WasInputPressed(CharacterBase target, CharacterInputType inputType)
        {
            if (target == null || !CharacterControlRules.IsDefinedInput(inputType))
                return false;

            inputSequenceTracker ??= new CharacterInputSequenceTracker();
            return inputSequenceTracker.WasPressed(
                target.GetInstanceID(),
                inputType,
                target.NetObservedInputSequences.Get((int)inputType));
        }

        /// <summary>대상이 지정한 매핑 입력을 누르고 있는지 확인한다. 현재는 Jump만 유지 상태를 제공한다.</summary>
        protected bool IsInputHeld(CharacterBase target, CharacterInputType inputType)
        {
            return target != null && inputType == CharacterInputType.Jump && target.NetObservedJumpHeld;
        }

        /// <summary>대상이 입력한 수평 이동 방향을 -1부터 1 사이로 반환한다.</summary>
        protected float GetObservedMoveDirection(CharacterBase target)
        {
            return target != null ? Mathf.Clamp(target.NetObservedMoveDirection, -1f, 1f) : 0f;
        }

        private void InitializeControlEffects()
        {
            inputSequenceTracker ??= new CharacterInputSequenceTracker();
            if (!IsLocalPlayer || aimCursorPresenter != null)
                return;

            aimCursorPresenter = new CharacterAimCursorPresenter();
        }

        private void DisposeControlEffects()
        {
            inputSequenceTracker?.Clear();
            aimCursorPresenter?.Dispose();
            aimCursorPresenter = null;
        }

        private void RecordObservedInput(CharacterInputSnapshot snapshot)
        {
            if (!HasStateAuthority)
                return;

            NetObservedMoveDirection = Mathf.Clamp(snapshot.MoveDirection, -1f, 1f);
            NetObservedJumpHeld = snapshot.JumpHeld;
            IncrementObservedInput(CharacterInputType.Jump, snapshot.JumpPressed);
            IncrementObservedInput(CharacterInputType.BasicAttack, snapshot.BasicAttackPressed);
            IncrementObservedInput(CharacterInputType.SkillQ, snapshot.SkillQPressed);
            IncrementObservedInput(CharacterInputType.SkillE, snapshot.SkillEPressed);
            IncrementObservedInput(CharacterInputType.Dash, snapshot.DashPressed);
            IncrementObservedInput(CharacterInputType.Ultimate, snapshot.UltimatePressed);
        }

        private CharacterInputSnapshot ApplyControlEffects(CharacterInputSnapshot snapshot)
        {
            Vector2 origin = AttackOrigin.position;
            snapshot.AimWorldPosition = CharacterControlRules.TransformAim(
                origin,
                snapshot.AimWorldPosition,
                IsTimerActive(NetAimInversionTimer),
                IsTimerActive(NetAimAngleOffsetTimer) ? NetAimAngleOffsetDegrees : 0f);

            return CharacterControlRules.FilterInput(snapshot, GetActiveControlSeals());
        }

        private CharacterControlType GetActiveControlSeals()
        {
            CharacterControlType result = CharacterControlType.None;
            if (Runner == null)
                return result;

            for (int slot = 0; slot < ControlSealSlotCount; slot++)
            {
                if (IsTimerActive(NetControlSealTimers.Get(slot)))
                    result |= CharacterControlRules.GetControlForSlot(slot);
            }

            return result;
        }

        private void RenderAimCursor()
        {
            if (!IsLocalPlayer)
                return;

            InitializeControlEffects();
            bool hasCrosshairOverride = IsTimerActive(NetCrosshairVisibilityTimer);
            bool crosshairVisible = hasCrosshairOverride && NetCrosshairVisibilityMode == 2;
            bool hasCursorOverride = IsTimerActive(NetSystemCursorVisibilityTimer);
            bool cursorVisible = !hasCursorOverride || NetSystemCursorVisibilityMode == 2;
            Vector2 offset = IsTimerActive(NetCrosshairOffsetTimer) ? NetCrosshairOffset : Vector2.zero;

            aimCursorPresenter?.Apply(
                offset,
                hasCrosshairOverride,
                crosshairVisible,
                hasCursorOverride,
                cursorVisible);
        }

        private void ResetControlEffectsAuthority()
        {
            inputSequenceTracker?.Clear();
            if (!HasStateAuthority)
                return;

            for (int i = 0; i < ControlSealSlotCount; i++)
                NetControlSealTimers.Set(i, default);
            for (int i = 0; i < ObservedInputSlotCount; i++)
                NetObservedInputSequences.Set(i, 0);

            NetObservedMoveDirection = 0f;
            NetObservedJumpHeld = false;
            NetAimInversionTimer = default;
            NetAimAngleOffsetTimer = default;
            NetAimAngleOffsetDegrees = 0f;
            NetCrosshairVisibilityTimer = default;
            NetCrosshairVisibilityMode = 0;
            NetSystemCursorVisibilityTimer = default;
            NetSystemCursorVisibilityMode = 0;
            NetCrosshairOffsetTimer = default;
            NetCrosshairOffset = Vector2.zero;
        }

        private void IncrementObservedInput(CharacterInputType inputType, bool pressed)
        {
            if (!pressed)
                return;

            int index = (int)inputType;
            NetObservedInputSequences.Set(index, NetObservedInputSequences.Get(index) + 1);
        }

        private bool CanRequestHostileControlEffect(CharacterBase target, float duration)
        {
            if (!HasStateAuthority || target == null || target == this || target.Object == null ||
                !CharacterControlRules.IsFinitePositive(duration))
            {
                return false;
            }

            return target.CanReceiveDamage(CreateControlEffectSourceRequest());
        }

        private DamageRequest CreateControlEffectSourceRequest()
        {
            return new DamageRequest(
                1f,
                DamageOwner,
                Object.Id,
                DamageTeamId,
                CharacterDamageSource.Direct);
        }

        private bool IsValidControlEffectRpc(
            NetworkId sourceObjectId,
            PlayerRef attacker,
            int attackerTeamId,
            PlayerRef rpcSource)
        {
            DamageRequest request = new DamageRequest(
                1f,
                attacker,
                sourceObjectId,
                attackerTeamId,
                CharacterDamageSource.Direct);
            return IsValidDamageRpcSource(request, rpcSource) && CanReceiveDamage(request);
        }

        private void ApplyControlSealAuthority(CharacterControlType controls, float duration)
        {
            if (!HasStateAuthority || !CharacterControlRules.IsFinitePositive(duration))
                return;

            for (int slot = 0; slot < ControlSealSlotCount; slot++)
            {
                CharacterControlType control = CharacterControlRules.GetControlForSlot(slot);
                if (!CharacterControlRules.Contains(controls, control))
                    continue;

                TickTimer current = NetControlSealTimers.Get(slot);
                if (ShouldReplace(current, duration))
                    NetControlSealTimers.Set(slot, TickTimer.CreateFromSeconds(Runner, duration));
            }
        }

        private void ApplyAimInversionAuthority(float duration)
        {
            if (HasStateAuthority && ShouldReplace(NetAimInversionTimer, duration))
                NetAimInversionTimer = TickTimer.CreateFromSeconds(Runner, duration);
        }

        private void ApplyAimAngleOffsetAuthority(float degrees, float duration)
        {
            if (!HasStateAuthority || !IsFiniteNumber(degrees) || !ShouldReplace(NetAimAngleOffsetTimer, duration))
                return;

            NetAimAngleOffsetDegrees = degrees;
            NetAimAngleOffsetTimer = TickTimer.CreateFromSeconds(Runner, duration);
        }

        private void ApplyCrosshairVisibilityAuthority(int mode, float duration)
        {
            if (!HasStateAuthority || !ShouldReplace(NetCrosshairVisibilityTimer, duration))
                return;

            NetCrosshairVisibilityMode = mode;
            NetCrosshairVisibilityTimer = TickTimer.CreateFromSeconds(Runner, duration);
        }

        private void ApplySystemCursorVisibilityAuthority(int mode, float duration)
        {
            if (!HasStateAuthority || !ShouldReplace(NetSystemCursorVisibilityTimer, duration))
                return;

            NetSystemCursorVisibilityMode = mode;
            NetSystemCursorVisibilityTimer = TickTimer.CreateFromSeconds(Runner, duration);
        }

        private void ApplyCrosshairOffsetAuthority(Vector2 offset, float duration)
        {
            if (!HasStateAuthority || !CharacterControlRules.IsFinite(offset) ||
                !ShouldReplace(NetCrosshairOffsetTimer, duration))
            {
                return;
            }

            NetCrosshairOffset = offset;
            NetCrosshairOffsetTimer = TickTimer.CreateFromSeconds(Runner, duration);
        }

        private void RequestCrosshairState(
            CharacterBase target,
            int mode,
            float duration,
            CursorEffectKind kind)
        {
            if (!CanRequestHostileControlEffect(target, duration))
                return;

            if (target.HasStateAuthority)
            {
                if (kind == CursorEffectKind.CrosshairVisibility)
                    target.ApplyCrosshairVisibilityAuthority(mode, duration);
                else
                    target.ApplySystemCursorVisibilityAuthority(mode, duration);
                return;
            }

            target.Rpc_RequestCursorVisibility(
                mode,
                duration,
                kind,
                Object.Id,
                DamageOwner,
                DamageTeamId);
        }

        private bool ShouldReplace(TickTimer current, float duration)
        {
            float? remaining = Runner != null && current.IsRunning && !current.Expired(Runner)
                ? current.RemainingTime(Runner)
                : null;
            return CharacterControlRules.ShouldReplaceTimedEffect(remaining, duration);
        }

        private bool IsTimerActive(TickTimer timer)
        {
            return Runner != null && timer.IsRunning && !timer.Expired(Runner);
        }

        private static bool IsFiniteNumber(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void Rpc_RequestControlSeal(
            CharacterControlType controls,
            float duration,
            NetworkId sourceObjectId,
            PlayerRef attacker,
            int attackerTeamId,
            RpcInfo info = default)
        {
            if (IsValidControlEffectRpc(sourceObjectId, attacker, attackerTeamId, info.Source))
                ApplyControlSealAuthority(controls & CharacterControlType.All, duration);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void Rpc_RequestAimInversion(
            float duration,
            NetworkId sourceObjectId,
            PlayerRef attacker,
            int attackerTeamId,
            RpcInfo info = default)
        {
            if (IsValidControlEffectRpc(sourceObjectId, attacker, attackerTeamId, info.Source))
                ApplyAimInversionAuthority(duration);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void Rpc_RequestAimAngleOffset(
            float degrees,
            float duration,
            NetworkId sourceObjectId,
            PlayerRef attacker,
            int attackerTeamId,
            RpcInfo info = default)
        {
            if (IsValidControlEffectRpc(sourceObjectId, attacker, attackerTeamId, info.Source))
                ApplyAimAngleOffsetAuthority(degrees, duration);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void Rpc_RequestCursorVisibility(
            int mode,
            float duration,
            CursorEffectKind kind,
            NetworkId sourceObjectId,
            PlayerRef attacker,
            int attackerTeamId,
            RpcInfo info = default)
        {
            if ((mode != 1 && mode != 2) ||
                !IsValidControlEffectRpc(sourceObjectId, attacker, attackerTeamId, info.Source))
            {
                return;
            }

            if (kind == CursorEffectKind.CrosshairVisibility)
                ApplyCrosshairVisibilityAuthority(mode, duration);
            else if (kind == CursorEffectKind.SystemCursorVisibility)
                ApplySystemCursorVisibilityAuthority(mode, duration);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void Rpc_RequestCrosshairOffset(
            Vector2 offset,
            float duration,
            NetworkId sourceObjectId,
            PlayerRef attacker,
            int attackerTeamId,
            RpcInfo info = default)
        {
            if (IsValidControlEffectRpc(sourceObjectId, attacker, attackerTeamId, info.Source))
                ApplyCrosshairOffsetAuthority(offset, duration);
        }

        private enum CursorEffectKind
        {
            CrosshairVisibility = 0,
            SystemCursorVisibility = 1
        }
    }
}
