using System;

namespace ProjectMS.CharacterSystem
{
    public interface ICharacterStatusStateStore
    {
        float SlowRatio { get; set; }
        bool IsSlowRunning { get; }
        bool IsSlowExpired { get; }
        void StartSlow(float seconds);
        void ClearSlow();
    }

    public sealed class CharacterStatusHandler
    {
        private readonly ICharacterStatusStateStore store;

        public CharacterStatusHandler(ICharacterStatusStateStore store)
        {
            if (store == null)
                throw new ArgumentNullException("store");

            this.store = store;
        }

        public float MovementSpeedMultiplier
        {
            get { return 1f - Math.Max(0f, Math.Min(0.99f, store.SlowRatio)); }
        }

        public void ApplySlow(float ratio, float duration)
        {
            if (float.IsNaN(ratio) || float.IsInfinity(ratio) ||
                float.IsNaN(duration) || float.IsInfinity(duration))
                return;

            float clampedRatio = Math.Max(0f, Math.Min(0.99f, ratio));
            if (store.IsSlowRunning && clampedRatio < store.SlowRatio)
                return;

            store.SlowRatio = clampedRatio;
            store.StartSlow(Math.Max(0f, duration));
        }

        public void Tick()
        {
            if (store.IsSlowRunning && store.IsSlowExpired)
                store.ClearSlow();
        }
    }
}
