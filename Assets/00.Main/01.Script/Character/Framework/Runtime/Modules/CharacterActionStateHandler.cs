using System;

namespace ProjectMS.CharacterSystem
{
    internal interface ICharacterActionStateStore
    {
        bool GetEnabled(CharacterActionType action);
        void SetEnabled(CharacterActionType action, bool enabled);
        int GetCharges(CharacterActionType action);
        void SetCharges(CharacterActionType action, int charges);
        float GetCooldownDurationOverride(CharacterActionType action);
        void SetCooldownDurationOverride(CharacterActionType action, float seconds);
        bool GetAutoCooldown(CharacterActionType action);
        void SetAutoCooldown(CharacterActionType action, bool enabled);
        bool IsCooldownRunning(CharacterActionType action);
        void StartCooldown(CharacterActionType action, float seconds);
        void ClearCooldown(CharacterActionType action);
        float GetCooldownRemaining(CharacterActionType action);
    }

    public sealed class CharacterActionStateHandler
    {
        private readonly ICharacterActionStateStore store;
        private readonly Func<CharacterActionType, float> defaultCooldown;

        internal CharacterActionStateHandler(
            ICharacterActionStateStore store,
            Func<CharacterActionType, float> defaultCooldown)
        {
            this.store = store;
            this.defaultCooldown = defaultCooldown;
        }

        public void Initialize()
        {
            foreach (CharacterActionType action in Enum.GetValues(typeof(CharacterActionType)))
            {
                if (!IsAction(action))
                    continue;

                store.SetEnabled(action, true);
                store.SetCharges(action, -1);
                store.SetCooldownDurationOverride(action, -1f);
                store.SetAutoCooldown(action, true);
                store.ClearCooldown(action);
            }
        }
        public bool CanUse(CharacterActionType action)
        {
            return IsAction(action) &&
                store.GetEnabled(action) &&
                (store.GetCharges(action) < 0 || store.GetCharges(action) > 0) &&
                !store.IsCooldownRunning(action);
        }

        public void SetEnabled(CharacterActionType action, bool enabled)
        {
            if (IsAction(action))
                store.SetEnabled(action, enabled);
        }

        public bool IsEnabled(CharacterActionType action)
        {
            return IsAction(action) && store.GetEnabled(action);
        }

        public void SetCharges(CharacterActionType action, int charges)
        {
            if (IsAction(action))
                store.SetCharges(action, Math.Max(-1, charges));
        }

        public void AddCharges(CharacterActionType action, int amount)
        {
            if (!IsAction(action))
                return;

            int charges = store.GetCharges(action);
            if (charges >= 0)
                SetCharges(action, charges + amount);
        }

        public int GetCharges(CharacterActionType action)
        {
            return IsAction(action) ? store.GetCharges(action) : 0;
        }

        public void SetCooldownDuration(CharacterActionType action, float seconds)
        {
            if (IsAction(action))
                store.SetCooldownDurationOverride(action, Math.Max(0f, seconds));
        }

        public void ResetCooldownDuration(CharacterActionType action)
        {
            if (IsAction(action))
                store.SetCooldownDurationOverride(action, -1f);
        }

        public float GetCooldownDuration(CharacterActionType action)
        {
            if (!IsAction(action))
                return 0f;

            float value = store.GetCooldownDurationOverride(action);
            return value >= 0f ? value : Math.Max(0f, GetDefaultCooldown(action));
        }

        public void StartCooldown(CharacterActionType action)
        {
            StartCooldown(action, GetCooldownDuration(action));
        }

        public void StartCooldown(CharacterActionType action, float seconds)
        {
            if (IsAction(action))
                store.StartCooldown(action, Math.Max(0f, seconds));
        }

        public void ClearCooldown(CharacterActionType action)
        {
            if (IsAction(action))
                store.ClearCooldown(action);
        }

        public void ClearAllCooldowns()
        {
            foreach (CharacterActionType action in Enum.GetValues(typeof(CharacterActionType)))
            {
                if (IsAction(action))
                    store.ClearCooldown(action);
            }
        }

        public float GetCooldownRemaining(CharacterActionType action)
        {
            return IsAction(action) ? Math.Max(0f, store.GetCooldownRemaining(action)) : 0f;
        }

        public void SetAutoCooldown(CharacterActionType action, bool enabled)
        {
            if (IsAction(action))
                store.SetAutoCooldown(action, enabled);
        }

        public bool ShouldStartCooldownAutomatically(CharacterActionType action)
        {
            return IsAction(action) && store.GetAutoCooldown(action);
        }

        public void ConsumeCharge(CharacterActionType action)
        {
            if (!IsAction(action))
                return;

            int charges = store.GetCharges(action);
            if (charges > 0)
                store.SetCharges(action, charges - 1);
        }

        private float GetDefaultCooldown(CharacterActionType action)
        {
            return defaultCooldown != null ? defaultCooldown(action) : 0f;
        }

        private static bool IsAction(CharacterActionType action)
        {
            return action != CharacterActionType.None;
        }
    }
}
