using System;
using System.Collections.Generic;

namespace ProjectMS.CharacterSystem
{
    /// <summary>
    /// Advances and invokes local scheduled callbacks. Network authority belongs
    /// to the future owner that chooses when to call this handler's methods.
    /// </summary>
    public sealed class CharacterTimerHandler
    {
        private sealed class PendingTimer
        {
            public int Id;
            public float RemainingSeconds;
            public Action Callback;
            public bool Cancelled;
        }

        private readonly Dictionary<int, PendingTimer> pendingTimers =
            new Dictionary<int, PendingTimer>();
        private int nextId = 1;

        public CharacterTimerHandle Schedule(float seconds, Action callback)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            int id = AllocateId();
            pendingTimers.Add(id, new PendingTimer
            {
                Id = id,
                RemainingSeconds = Math.Max(0f, seconds),
                Callback = callback
            });
            return new CharacterTimerHandle(id);
        }

        public bool Cancel(CharacterTimerHandle handle)
        {
            if (!handle.IsValid)
                return false;

            PendingTimer timer;
            if (!pendingTimers.TryGetValue(handle.Id, out timer))
                return false;

            timer.Cancelled = true;
            pendingTimers.Remove(handle.Id);
            return true;
        }

        public void Tick(float deltaTime)
        {
            float elapsedSeconds = Math.Max(0f, deltaTime);
            List<PendingTimer> expiredTimers = new List<PendingTimer>();
            foreach (PendingTimer timer in pendingTimers.Values)
            {
                timer.RemainingSeconds -= elapsedSeconds;
                if (timer.RemainingSeconds <= 0f)
                    expiredTimers.Add(timer);
            }

            expiredTimers.Sort((left, right) => left.Id.CompareTo(right.Id));

            foreach (PendingTimer timer in expiredTimers)
            {
                if (timer.Cancelled)
                    continue;

                timer.Cancelled = true;
                pendingTimers.Remove(timer.Id);
                timer.Callback();
            }
        }

        public void CancelAll()
        {
            foreach (PendingTimer timer in pendingTimers.Values)
                timer.Cancelled = true;

            pendingTimers.Clear();
        }

        private int AllocateId()
        {
            int candidate = nextId;
            do
            {
                if (candidate <= 0)
                    candidate = 1;

                if (!pendingTimers.ContainsKey(candidate))
                {
                    nextId = candidate == int.MaxValue ? 1 : candidate + 1;
                    return candidate;
                }

                candidate = candidate == int.MaxValue ? 1 : candidate + 1;
            }
            while (candidate != nextId);

            throw new InvalidOperationException("No timer handles are available.");
        }
    }
}
