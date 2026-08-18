namespace ProjectMS.CharacterSystem
{
    public static class OwnedEntityDurabilityRules
    {
        public static bool UsesHealth(OwnedEntityLifetimeMode mode)
        {
            return mode == OwnedEntityLifetimeMode.Health ||
                   mode == OwnedEntityLifetimeMode.HealthOrDuration;
        }

        public static bool UsesDuration(OwnedEntityLifetimeMode mode)
        {
            return mode == OwnedEntityLifetimeMode.Duration ||
                   mode == OwnedEntityLifetimeMode.HealthOrDuration;
        }

        public static bool CanReceiveDamage(
            OwnedEntityLifetimeMode mode,
            OwnedEntityDamageRelation relation,
            bool allowSelfDamage,
            bool allowFriendlyDamage,
            float amount)
        {
            if (!UsesHealth(mode) || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount))
                return false;

            switch (relation)
            {
                case OwnedEntityDamageRelation.Self:
                    return allowSelfDamage;
                case OwnedEntityDamageRelation.Friendly:
                    return allowFriendlyDamage;
                default:
                    return true;
            }
        }

        public static OwnedEntityDestroyReason ResolveDestructionReason(
            bool healthDepleted,
            bool lifetimeExpired)
        {
            if (healthDepleted)
                return OwnedEntityDestroyReason.HealthDepleted;
            if (lifetimeExpired)
                return OwnedEntityDestroyReason.LifetimeExpired;
            return OwnedEntityDestroyReason.None;
        }
    }
}
