using System;
using UnityEngine;

/// <summary>
/// 체력 계산 로직. 실제 체력 값(현재 체력)은 CharacterBase 의 [Networked] 프로퍼티에
/// 저장되며, 이 클래스는 그 값을 읽고/쓰는 위임(delegate)을 통해 조작한다.
///
/// [Fusion 이전 메모]
///   - 현재 체력은 네트워크로 동기화되는 단일 진실 소스다.
///   - 따라서 TakeDamage/Heal 은 반드시 "그 캐릭터의 StateAuthority" 에서만 실행돼야 한다
///     (CharacterBase 가 RPC 로 권한 측에서 호출하도록 보장).
/// </summary>
public class CharacterHealth
{
    private readonly CharacterStat stat;
    private readonly Func<float> getHealth;
    private readonly Action<float> setHealth;

    public event Action<float, float> HealthChanged;
    public event Action Died;

    public CharacterHealth(CharacterStat stat, Func<float> getHealth, Action<float> setHealth)
    {
        this.stat = stat;
        this.getHealth = getHealth;
        this.setHealth = setHealth;
    }

    public float CurrentHealth => getHealth();
    public float MaxHealth => stat.MaxHealth;
    public bool IsDead => CurrentHealth <= 0f;

    /// <summary>StateAuthority 에서만 호출할 것.</summary>
    public void Initialize()
    {
        setHealth(stat.MaxHealth);
        HealthChanged?.Invoke(CurrentHealth, stat.MaxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f)
            return;

        float next = Mathf.Clamp(CurrentHealth - damage, 0f, stat.MaxHealth);
        setHealth(next);

        HealthChanged?.Invoke(next, stat.MaxHealth);

        if (next <= 0f)
            Died?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        float next = Mathf.Clamp(CurrentHealth + amount, 0f, stat.MaxHealth);
        setHealth(next);
        HealthChanged?.Invoke(next, stat.MaxHealth);
    }

    public void HealByMaxHealthPercent(float percent)
    {
        if (IsDead || percent <= 0f)
            return;

        // 기존 로직 유지: percent 가 0~1 비율이 아니라 "퍼센트 값" 으로 들어오는 호출부(50)를
        //   고려해 Max(percent,1) 형태를 그대로 둔다. (호출부와의 호환 유지)
        float healAmount = stat.MaxHealth * Mathf.Max(percent, 1f);
        Heal(healAmount);
    }

    public void FullHeal()
    {
        setHealth(stat.MaxHealth);
        HealthChanged?.Invoke(CurrentHealth, stat.MaxHealth);
    }
}
