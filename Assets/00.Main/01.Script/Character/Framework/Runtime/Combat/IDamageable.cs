using Fusion;

namespace ProjectMS.CharacterSystem
{
    public interface IDamageable
    {
        PlayerRef DamageOwner { get; }
        int DamageTeamId { get; }
        bool CanReceiveDamage(DamageRequest request);
        DamageResult RequestDamage(DamageRequest request);
    }
}
