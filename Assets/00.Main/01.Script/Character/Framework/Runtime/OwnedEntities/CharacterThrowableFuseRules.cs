namespace ProjectMS.CharacterSystem
{
    public enum CharacterThrowableFuseStartMode
    {
        OnSpawn = 0,
        OnGroundContact = 1,
        Manual = 2
    }

    public enum CharacterThrowableFuseTrigger
    {
        Spawn = 0,
        GroundContact = 1,
        Manual = 2
    }

    public static class CharacterThrowableFuseRules
    {
        public static bool CanStart(
            CharacterThrowableFuseStartMode mode,
            CharacterThrowableFuseTrigger trigger)
        {
            return (mode == CharacterThrowableFuseStartMode.OnSpawn &&
                    trigger == CharacterThrowableFuseTrigger.Spawn) ||
                   (mode == CharacterThrowableFuseStartMode.OnGroundContact &&
                    trigger == CharacterThrowableFuseTrigger.GroundContact) ||
                   (mode == CharacterThrowableFuseStartMode.Manual &&
                    trigger == CharacterThrowableFuseTrigger.Manual);
        }
    }
}
