namespace ProjectMS.CharacterSystem
{
    public enum ProjectileDespawnReason
    {
        HitCharacter = 0,
        HitWall = 1,
        LifetimeExpired = 2,
        Manual = 3,
        HitOwnedEntity = 4
    }
}
