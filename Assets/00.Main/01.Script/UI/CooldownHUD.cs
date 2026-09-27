/// <summary>
/// P1(왼쪽 코너) 캐릭터의 스킬 쿨타임 HUD다. 로컬 플레이어(나)가 아니라 **P1 자리의 캐릭터**를
/// 보여준다 — 왼쪽 코너의 HP/승수(PlayerCornerHUD)와 항상 같은 캐릭터다.
/// 동작은 전부 PlayerSideCooldownHUD에 있고, 여기서는 "P1 자리"라는 것만 정한다.
/// (P2 자리는 OpponentCooldownHUD)
/// </summary>
public class CooldownHUD : PlayerSideCooldownHUD
{
    protected override bool IsPlayer1Side => true;
}
