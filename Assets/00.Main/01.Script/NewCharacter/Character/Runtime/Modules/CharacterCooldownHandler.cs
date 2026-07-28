using System.Collections.Generic;

namespace ProjectMS.CharacterSystem
{
    public sealed class CharacterCooldownHandler
    {
        private readonly CharacterDefinition definition;
        private readonly Dictionary<CharacterActionType, float> timers = new Dictionary<CharacterActionType, float>();

        public CharacterCooldownHandler(CharacterDefinition definition)
        {
            this.definition = definition;
        }

        public void Tick(float deltaTime)
        {
            if (timers.Count == 0)
                return;

            CharacterActionType[] actions = new CharacterActionType[timers.Count];
            timers.Keys.CopyTo(actions, 0);

            foreach (CharacterActionType action in actions)
                timers[action] = System.Math.Max(0f, timers[action] - deltaTime);
        }

        public bool CanUse(CharacterActionType action)
        {
            return GetRemaining(action) <= 0f;
        }

        public void Start(CharacterActionType action)
        {
            timers[action] = definition.GetCooldown(action);
        }

        public float GetRemaining(CharacterActionType action)
        {
            return timers.TryGetValue(action, out float remaining) ? remaining : 0f;
        }

        public float GetDuration(CharacterActionType action)
        {
            return definition.GetCooldown(action);
        }

        public void ResetAll()
        {
            timers.Clear();
        }
    }
}
