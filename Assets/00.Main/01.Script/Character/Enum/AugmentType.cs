/// <summary>
/// 라운드 종료 후 선택 가능한 증강 종류다. CharacterBase 의 비트마스크(AugmentFlags)에
/// 값의 위치(1 &lt;&lt; (int)AugmentType)로 저장되므로 32개까지 추가할 수 있다.
///
/// 새 증강을 추가하면 여기에 항목을 추가하고, 그 효과는 해당 스킬을 아는
/// 캐릭터 스크립트(예: SlimeCharacter)에서 HasAugment(...) 로 분기해 구현한다.
/// </summary>
public enum AugmentType
{
    ATK,
    ATK2,
    ATK3,
    DEF,
    DEF2,
    SPD,
    SPD2,

    /// <summary>Q스킬이 조준 방향 대신 8방향으로 동시에 나간다.</summary>
    QSkillOctoDirection,

    /// <summary>E스킬(수류탄)이 원래 1번에서 추가로 2번 더, 총 3연발로 나간다.</summary>
    ESkillTripleThrow
}
