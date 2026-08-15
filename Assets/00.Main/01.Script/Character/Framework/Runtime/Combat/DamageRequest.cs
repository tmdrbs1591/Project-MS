using Fusion;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    public readonly struct DamageRequest
    {
        public DamageRequest(
            float amount,
            PlayerRef attacker,
            NetworkId sourceObjectId,
            int attackerTeamId,
            CharacterDamageSource source,
            int skillId = 0,
            Vector2 hitPosition = default,
            Vector2 hitDirection = default)
        {
            Amount = amount;
            Attacker = attacker;
            SourceObjectId = sourceObjectId;
            AttackerTeamId = attackerTeamId;
            Source = source;
            SkillId = skillId;
            HitPosition = hitPosition;
            HitDirection = hitDirection;
        }

        public float Amount { get; }
        public PlayerRef Attacker { get; }
        public NetworkId SourceObjectId { get; }
        public int AttackerTeamId { get; }
        public CharacterDamageSource Source { get; }
        public int SkillId { get; }
        public Vector2 HitPosition { get; }
        public Vector2 HitDirection { get; }

        public DamageRequest WithAmount(float amount)
        {
            return new DamageRequest(
                amount,
                Attacker,
                SourceObjectId,
                AttackerTeamId,
                Source,
                SkillId,
                HitPosition,
                HitDirection);
        }
    }
}
