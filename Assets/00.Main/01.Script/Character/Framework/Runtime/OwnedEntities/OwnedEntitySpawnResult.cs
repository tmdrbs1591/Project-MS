namespace ProjectMS.CharacterSystem
{
    public readonly struct OwnedEntitySpawnResult<T> where T : CharacterOwnedEntity
    {
        private OwnedEntitySpawnResult(T entity, OwnedEntitySpawnFailureReason failureReason)
        {
            Entity = entity;
            FailureReason = failureReason;
        }

        public bool Success => Entity != null && FailureReason == OwnedEntitySpawnFailureReason.None;
        public T Entity { get; }
        public OwnedEntitySpawnFailureReason FailureReason { get; }

        public static OwnedEntitySpawnResult<T> Succeeded(T entity)
        {
            return new OwnedEntitySpawnResult<T>(entity, OwnedEntitySpawnFailureReason.None);
        }

        public static OwnedEntitySpawnResult<T> Failed(OwnedEntitySpawnFailureReason reason)
        {
            return new OwnedEntitySpawnResult<T>(null, reason);
        }
    }
}
