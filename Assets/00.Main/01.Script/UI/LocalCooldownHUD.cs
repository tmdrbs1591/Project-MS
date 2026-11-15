using ProjectMS.CharacterSystem;

/// <summary>
/// 내 캐릭터의 스킬 쿨타임만 보여주는 HUD(스킬 UI 타입 B — 화면 아래 가운데).
/// 슬롯 구성/표시 방식은 CooldownHUD(P1)·OpponentCooldownHUD(P2)와 같고, 캐릭터를 P1/P2 대신
/// "내가 조종하는 캐릭터"(입력 권한이 있는 캐릭터)로 고른다는 점만 다르다.
/// A/B 중 무엇을 켤지는 HudLayoutSwitcher가 정한다.
/// </summary>
public class LocalCooldownHUD : PlayerSideCooldownHUD
{
    protected override bool IsPlayer1Side => true; // 쓰지 않음(FindCharacter를 바꿨다)

    protected override CharacterBase FindCharacter()
    {
        foreach (CharacterBase candidate in CharacterBase.All)
        {
            if (candidate != null && candidate.Object != null && candidate.IsLocalPlayer)
                return candidate;
        }
        return null;
    }
}
