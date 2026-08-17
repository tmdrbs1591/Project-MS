using Fusion;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    public readonly struct CharacterDamageInfo
    {
        public CharacterDamageInfo(float requestedDamage, float appliedDamage, PlayerRef attacker)
            : this(
                new DamageRequest(
                    requestedDamage,
                    attacker,
                    default,
                    attacker != PlayerRef.None ? attacker.PlayerId : -1,
                    CharacterDamageSource.Direct),
                appliedDamage)
        {
        }

        public CharacterDamageInfo(DamageRequest request, float appliedDamage)
        {
            RequestedDamage = request.Amount;
            AppliedDamage = appliedDamage;
            Attacker = request.Attacker;
            SourceObjectId = request.SourceObjectId;
            AttackerTeamId = request.AttackerTeamId;
            Source = request.Source;
            SkillId = request.SkillId;
            HitPosition = request.HitPosition;
            HitDirection = request.HitDirection;
        }

        public float RequestedDamage { get; }
        public float AppliedDamage { get; }
        public PlayerRef Attacker { get; }
        public NetworkId SourceObjectId { get; }
        public int AttackerTeamId { get; }
        public CharacterDamageSource Source { get; }
        public int SkillId { get; }
        public Vector2 HitPosition { get; }
        public Vector2 HitDirection { get; }
    }
}
