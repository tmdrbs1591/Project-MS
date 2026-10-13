using Fusion;

namespace ProjectMS.CharacterSystem
{
    /// <summary>CharacterBase.HitRendered로 전달되는 피격 연출 정보. Amount는 지난 렌더 이후 받은 피해 합계.</summary>
    public readonly struct CharacterHitRenderInfo
    {
        public readonly float Amount;
        public readonly CharacterDamageSource Source;
        public readonly PlayerRef Attacker;

        public CharacterHitRenderInfo(float amount, CharacterDamageSource source, PlayerRef attacker)
        {
            Amount = amount;
            Source = source;
            Attacker = attacker;
        }

        /// <summary>도트/낙사처럼 "타격"이 아닌 피해인지. 히트스톱·명중음은 이런 피해에 걸지 않는다.</summary>
        public bool IsNonImpact => Source == CharacterDamageSource.Periodic || Source == CharacterDamageSource.Environment;
    }
}
