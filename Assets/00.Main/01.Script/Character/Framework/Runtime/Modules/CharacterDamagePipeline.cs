using System;

namespace ProjectMS.CharacterSystem
{
    /// <summary>
    /// Applies the common outgoing-damage order without depending on Unity or Fusion.
    /// </summary>
    public sealed class CharacterDamagePipeline
    {
        private readonly Func<float, CharacterDamageSource, float> modifyDamage;
        private readonly Action<float> requestDamage;
        private readonly Action<float> notifyDamageDealt;

        public CharacterDamagePipeline(
            Func<float, CharacterDamageSource, float> modifyDamage,
            Action<float> requestDamage,
            Action<float> notifyDamageDealt)
        {
            this.modifyDamage = modifyDamage;
            this.requestDamage = requestDamage;
            this.notifyDamageDealt = notifyDamageDealt;
        }

        public void Apply(float amount, CharacterDamageSource source)
        {
            if (!IsFinitePositive(amount) || requestDamage == null)
                return;

            float finalDamage = modifyDamage != null ? modifyDamage(amount, source) : amount;
            if (!IsFinitePositive(finalDamage))
                return;

            requestDamage(finalDamage);
            if (notifyDamageDealt != null)
                notifyDamageDealt(finalDamage);
        }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
