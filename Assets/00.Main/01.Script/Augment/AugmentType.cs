/// <summary>
/// 라운드 종료 후 선택 가능한 증강 종류다. CharacterBase의 중첩 저장소(NetAugmentStacks)에
/// 값의 위치(= 이 enum의 정수값)로 저장되므로, 반드시 0부터 연속된 정수여야 하고
/// AugmentTypeCount(CharacterBase.ProjectIntegration.cs)와 개수가 일치해야 한다.
///
/// 실제 수치(퍼센트 등)는 여기 없다 — AugmentData 에셋의 percentValue/secondaryPercentValue/radius에
/// 있다. 이 enum은 "어떤 증강인지"만 구분하고, 효과 적용은 CharacterBase의 배율 프로퍼티
/// (AttackMultiplier, MaxHealthMultiplier 등)와 캐릭터 스크립트(투사체 거동류)에서 처리한다.
/// </summary>
public enum AugmentType
{
    /// <summary>AUG_001 대형 탄약집 — 공격력 +30%/스택, 최대 3스택.</summary>
    LargeAmmoPouch = 0,

    /// <summary>AUG_002 방탄복 — 최대 체력 +30%/스택, 최대 3스택.</summary>
    BulletproofVest = 1,

    /// <summary>AUG_003 신속의 신발 — 이동 속도 +20%/스택, 최대 3스택.</summary>
    SwiftBoots = 2,

    /// <summary>AUG_004 과충전 탄창 — 최대 탄약 수 +50%/스택, 최대 2스택.</summary>
    OverchargedMagazine = 3,

    /// <summary>AUG_005 고속 재장전 — 재장전 시간 -25%/스택, 최대 2스택.</summary>
    RapidReload = 4,

    /// <summary>AUG_006 갈래 마법 — 기본기 발사 시 투사체 1발 추가(50% 피해)/스택, 최대 2스택.</summary>
    ForkedMagic = 5,

    /// <summary>AUG_007 바운스 마법 — 기본기 투사체가 벽에 1회 튕김/스택(스택 수 = 튕기는 횟수), 최대 2스택.</summary>
    BouncingMagic = 6,

    /// <summary>AUG_008 추진력 강화 — 대시 쿨타임 -30%/스택, 최대 2스택.</summary>
    DashBooster = 7,

    /// <summary>AUG_009 터보 차지 — 궁극기 게이지 충전율 +40%/스택, 최대 2스택.</summary>
    TurboCharge = 8,

    /// <summary>AUG_010 폭발 마법 — 기본기 투사체가 벽/바닥 명중 시 폭발(투사체 피해의 50%), 최대 1스택.</summary>
    ExplosiveMagic = 9,

    /// <summary>AUG_011 유리 대포 — 공격력 +50%, 최대 체력 -30%, 최대 1스택.</summary>
    GlassCannon = 10,

    /// <summary>AUG_012 버서커 — 체력 30% 이하일 때 공격력 +60%, 최대 1스택.</summary>
    Berserker = 11,

    /// <summary>AUG_013 반사 — 피격 시 받은 데미지의 20%를 주변 범위에 반사, 최대 1스택.</summary>
    Reflect = 12
}
