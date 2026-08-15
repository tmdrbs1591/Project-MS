namespace ProjectMS.CharacterSystem
{
    public enum OwnedEntityLifetimeMode
    {
        Manual = 0,
        Health = 1,
        Duration = 2,
        HealthOrDuration = 3
    }

    public enum OwnedEntityDamageRelation
    {
        Self = 0,
        Friendly = 1,
        Enemy = 2
    }

    public enum OwnedEntityOverflowPolicy
    {
        RejectNew = 0,
        DestroyOldest = 1,
        DestroyNewest = 2,
        Unlimited = 3
    }

    public enum OwnedEntityOwnerExitPolicy
    {
        Destroy = 0,
        ExpireNormally = 1,
        TransferStateAuthority = 2
    }

    public enum OwnedEntityDestroyReason
    {
        None = 0,
        HealthDepleted = 1,
        LifetimeExpired = 2,
        LimitExceeded = 3,
        OwnerDied = 4,
        OwnerDespawned = 5,
        OwnerDisconnected = 6,
        SkillTriggered = 7,
        Manual = 8
    }

    public enum OwnedEntitySpawnFailureReason
    {
        None = 0,
        InvalidPrefab = 1,
        InvalidGroup = 2,
        InvalidCount = 3,
        AuthorityUnavailable = 4,
        CountLimitReached = 5,
        UnsupportedPolicy = 6,
        SpawnFailed = 7,
        RegistrationFailed = 8
    }
}
