using System;
using UnityEngine;

public class CharacterHealth
{
    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0f;

    private readonly CharacterStat stat;

    public event Action<float, float> HealthChanged;
    public event Action Died;

    public CharacterHealth(CharacterStat stat)
    {
        this.stat = stat;
        CurrentHealth = stat.MaxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        if (damage <= 0f)
            return;

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, stat.MaxHealth);

        HealthChanged?.Invoke(CurrentHealth, stat.MaxHealth);

        if (CurrentHealth <= 0f)
            Died?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead)
            return;

        if (amount <= 0f)
            return;

        CurrentHealth += amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, stat.MaxHealth);

        HealthChanged?.Invoke(CurrentHealth, stat.MaxHealth);
    }

    public void HealByMaxHealthPercent(float percent)
    {
        if (IsDead)
            return;

        if (percent <= 0f)
            return; 

        float healAmount = stat.MaxHealth * Mathf.Max(percent, 1);
        Heal(healAmount);
    }

    public void FullHeal()
    {
        CurrentHealth = stat.MaxHealth;
        HealthChanged?.Invoke(CurrentHealth, stat.MaxHealth);
    }
}