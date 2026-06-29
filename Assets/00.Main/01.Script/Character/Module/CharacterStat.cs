public class CharacterStat
{
    public float MaxHealth { get; private set; }
    public float AttackSpeed { get; private set; }
    public float BonusAttackDamage { get; private set; }
    public float MoveSpeed { get; private set; }
    public int MagazineCount { get; private set; }

    private float basicAttackDamage;
    private float skillQDamage;
    private float skillEDamage;
    private float ultimateDamage;

    public CharacterStat(CharacterStatData data)
    {
        MaxHealth = data.maxHealth;
        AttackSpeed = data.attackSpeed;
        BonusAttackDamage = data.bonusAttackDamage;
        MoveSpeed = data.moveSpeed;
        MagazineCount = data.magazineCount;

        basicAttackDamage = data.basicAttackDamage;
        skillQDamage = data.skillQDamage;
        skillEDamage = data.skillEDamage;
        ultimateDamage = data.ultimateDamage;
    }

    public void AddBonusAttackDamage(float value)
    {
        BonusAttackDamage += value;
    }

    public void AddAttackSpeed(float value)
    {
        AttackSpeed += value;
    }

    public void AddMaxHealth(float value)
    {
        MaxHealth += value;
    }

    public void AddMoveSpeed(float value)
    {
        MoveSpeed += value;
    }

    public void AddMagazineCount(int value)
    {
        MagazineCount += value;
    }

    public float GetAttackDamage(CharacterActionType actionType)
    {
        float baseDamage = actionType switch
        {
            CharacterActionType.BasicAttack => basicAttackDamage,
            CharacterActionType.SkillQ => skillQDamage,
            CharacterActionType.SkillE => skillEDamage,
            CharacterActionType.Ultimate => ultimateDamage,
            _ => 0f
        };

        return baseDamage + BonusAttackDamage;
    }
}