using System;
using UnityEngine;

namespace ProjectMS.CharacterSystem
{
    public sealed class CharacterHealth
    {
        private readonly float maxHealth;
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

        // 구 시스템 호환 접근자
        public float CurrentHealth => Current;
        public float MaxHealth => Max;

        public void Initialize()
        {
            setter(maxHealth);
        }

        public float ApplyDamage(float requestedDamage)
        {
            if (requestedDamage <= 0f || IsDead)
                return 0f;

            float before = Current;
            float next = Mathf.Clamp(before - requestedDamage, 0f, maxHealth);
            setter(next);
            return before - next;
        }

        public float Heal(float requestedAmount, bool allowRevive = false)
        {
            if (requestedAmount <= 0f || (IsDead && !allowRevive))
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
    }
}
