using UnityEngine;

[System.Serializable]
public class CharacterStatData
{
    [Header("Base Stat")]
    public float maxHealth;
    public float attackSpeed;
    public float bonusAttackDamage;
    public float moveSpeed;
    public int magazineCount;

    [Header("Skill Damage")]
    public float basicAttackDamage;
    public float skillQDamage;
    public float skillEDamage;
    public float ultimateDamage;
}