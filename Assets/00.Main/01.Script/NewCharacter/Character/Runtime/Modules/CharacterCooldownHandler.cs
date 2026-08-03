using System;
using System.Collections.Generic;

namespace ProjectMS.CharacterSystem
{
    public sealed class CharacterCooldownHandler
    {
        private readonly LocalActionStateStore store;
        private readonly CharacterActionStateHandler state;

        internal CharacterActionStateHandler State
        {
            get { return state; }
        }

        public CharacterCooldownHandler(CharacterDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException("definition");

            store = new LocalActionStateStore();
            state = new CharacterActionStateHandler(store, definition.GetCooldown);
            state.Initialize();
        }

        internal CharacterCooldownHandler(CharacterActionStateHandler state)
        {
            if (state == null)
                throw new ArgumentNullException("state");

            this.state = state;
        }

        public void Tick(float deltaTime)
        {
            if (store != null)
                store.Tick(deltaTime);
        }

        public bool CanUse(CharacterActionType action)
        {
            return state.CanUse(action);
        }

        public void Start(CharacterActionType action)
        {
            state.StartCooldown(action);
        }

        public float GetRemaining(CharacterActionType action)
        {
            return state.GetCooldownRemaining(action);
        }

        public float GetDuration(CharacterActionType action)
        {
            return state.GetCooldownDuration(action);
        }

        public void ResetAll()
        {
            state.ClearAllCooldowns();
        }

        private sealed class LocalActionStateStore : ICharacterActionStateStore
        {
            private readonly Dictionary<CharacterActionType, bool> enabled =
                new Dictionary<CharacterActionType, bool>();
            private readonly Dictionary<CharacterActionType, int> charges =
                new Dictionary<CharacterActionType, int>();
            private readonly Dictionary<CharacterActionType, float> cooldownDurationOverrides =
                new Dictionary<CharacterActionType, float>();
            private readonly Dictionary<CharacterActionType, bool> autoCooldown =
                new Dictionary<CharacterActionType, bool>();
            private readonly Dictionary<CharacterActionType, float> cooldownRemaining =
                new Dictionary<CharacterActionType, float>();

            public bool GetEnabled(CharacterActionType action)
            {
                bool value;
                return enabled.TryGetValue(action, out value) && value;
            }

            public void SetEnabled(CharacterActionType action, bool value)
            {
                enabled[action] = value;
            }

            public int GetCharges(CharacterActionType action)
            {
                int value;
                return charges.TryGetValue(action, out value) ? value : -1;
            }

            public void SetCharges(CharacterActionType action, int value)
            {
                charges[action] = value;
            }

            public float GetCooldownDurationOverride(CharacterActionType action)
            {
                float value;
                return cooldownDurationOverrides.TryGetValue(action, out value) ? value : -1f;
            }

            public void SetCooldownDurationOverride(CharacterActionType action, float seconds)
            {
                cooldownDurationOverrides[action] = seconds;
            }

            public bool GetAutoCooldown(CharacterActionType action)
            {
                bool value;
                return !autoCooldown.TryGetValue(action, out value) || value;
            }

            public void SetAutoCooldown(CharacterActionType action, bool value)
            {
                autoCooldown[action] = value;
            }

            public bool IsCooldownRunning(CharacterActionType action)
            {
                return GetCooldownRemaining(action) > 0f;
            }

            public void StartCooldown(CharacterActionType action, float seconds)
            {
                cooldownRemaining[action] = Math.Max(0f, seconds);
            }

            public void ClearCooldown(CharacterActionType action)
            {
                cooldownRemaining.Remove(action);
            }

            public float GetCooldownRemaining(CharacterActionType action)
            {
                float value;
                return cooldownRemaining.TryGetValue(action, out value) ? value : 0f;
            }

            public void Tick(float deltaTime)
            {
                if (cooldownRemaining.Count == 0)
                    return;

                CharacterActionType[] actions = new CharacterActionType[cooldownRemaining.Count];
                cooldownRemaining.Keys.CopyTo(actions, 0);

                foreach (CharacterActionType action in actions)
                    cooldownRemaining[action] = Math.Max(0f, cooldownRemaining[action] - deltaTime);
            }
        }
    }
}
