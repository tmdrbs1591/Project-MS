using System;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    public sealed class CharacterHealth
    {
        private float maxHealth;
        private readonly Func<float> getter;
        private readonly Action<float> setter;

        public CharacterHealth(float maxHealth, Func<float> getter, Action<float> setter)
        {
            this.maxHealth = maxHealth;
            this.getter = getter;
            this.setter = setter;
        }

        public float Current => getter();
        public float Max => maxHealth;
        public float Normalized => maxHealth > 0f ? Current / maxHealth : 0f;
        public bool IsDead => Current <= 0f;

        public void Initialize()
        {
            setter(maxHealth);
        }

        /// <summary>최대 체력을 갱신한다(예: 라운드 사이 체력 증강을 새로 얻었을 때). 그 자체로는
        /// 현재 체력을 안 건드리니, 곧바로 채워야 하면(라운드 시작 리셋 등) FullHeal()을 같이 호출한다.</summary>
        public void SetMaxHealth(float newMaxHealth)
        {
            maxHealth = Mathf.Max(1f, newMaxHealth);
        }

        public float ApplyDamage(float requestedDamage)
        {
            if (!IsFinitePositive(requestedDamage) || IsDead)
                return 0f;

            float before = Current;
            float next = Mathf.Clamp(before - requestedDamage, 0f, maxHealth);
            setter(next);
            return before - next;
        }

        public float Heal(float requestedAmount, bool allowRevive = false)
        {
            if (!IsFinitePositive(requestedAmount) || (IsDead && !allowRevive))
                return 0f;

            float before = Current;
            float next = Mathf.Clamp(before + requestedAmount, 0f, maxHealth);
            setter(next);
            return next - before;
        }

        public void FullHeal()
        {
            setter(maxHealth);
        }

        private static bool IsFinitePositive(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
