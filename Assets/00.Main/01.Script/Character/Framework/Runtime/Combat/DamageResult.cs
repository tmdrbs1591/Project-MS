namespace ProjectMS.CharacterSystem
{
    public enum DamageRequestStatus
    {
        Rejected = 0,
        Queued = 1,
        Applied = 2
    }

    public enum DamageRejectionReason
    {
        None = 0,
        InvalidAmount = 1,
        InvalidTarget = 2,
        NotDamageable = 3,
        SelfDamageBlocked = 4,
        FriendlyDamageBlocked = 5,
        AlreadyDestroying = 6
    }

    public readonly struct DamageResult
    {
        private DamageResult(
            DamageRequestStatus status,
            DamageRejectionReason rejectionReason,
            float requestedDamage,
            float appliedDamage,
            float remainingHealth,
            bool destroyed)
        {
            Status = status;
            RejectionReason = rejectionReason;
            RequestedDamage = requestedDamage;
            AppliedDamage = appliedDamage;
            RemainingHealth = remainingHealth;
            Destroyed = destroyed;
        }

        public DamageRequestStatus Status { get; }
        public DamageRejectionReason RejectionReason { get; }
        public float RequestedDamage { get; }
        public float AppliedDamage { get; }
        public float RemainingHealth { get; }
        public bool Destroyed { get; }
        public bool Accepted => Status != DamageRequestStatus.Rejected;

        public static DamageResult Rejected(float requestedDamage, DamageRejectionReason reason)
        {
            return new DamageResult(DamageRequestStatus.Rejected, reason, requestedDamage, 0f, 0f, false);
        }

        public static DamageResult Queued(float requestedDamage)
        {
            return new DamageResult(DamageRequestStatus.Queued, DamageRejectionReason.None, requestedDamage, 0f, 0f, false);
        }

        public static DamageResult Applied(float requestedDamage, float appliedDamage, float remainingHealth, bool destroyed)
        {
            return new DamageResult(
                DamageRequestStatus.Applied,
                DamageRejectionReason.None,
                requestedDamage,
                appliedDamage,
                remainingHealth,
                destroyed);
        }
    }
}
