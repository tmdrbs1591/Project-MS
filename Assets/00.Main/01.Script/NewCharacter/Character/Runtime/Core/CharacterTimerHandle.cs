namespace ProjectMS.CharacterSystem
{
    public readonly struct CharacterTimerHandle
    {
        internal CharacterTimerHandle(int id) => Id = id;

        internal int Id { get; }

        public bool IsValid => Id > 0;
    }
}
