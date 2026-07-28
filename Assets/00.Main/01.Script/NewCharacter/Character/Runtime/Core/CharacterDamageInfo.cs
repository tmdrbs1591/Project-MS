using Fusion;

namespace ProjectMS.CharacterSystem
{
    public readonly struct CharacterDamageInfo
    {
        public CharacterDamageInfo(float requestedDamage, float appliedDamage, PlayerRef attacker)
        {
            RequestedDamage = requestedDamage;
            AppliedDamage = appliedDamage;
            Attacker = attacker;
        }

        public float RequestedDamage { get; }
        public float AppliedDamage { get; }
        public PlayerRef Attacker { get; }
    }
}
