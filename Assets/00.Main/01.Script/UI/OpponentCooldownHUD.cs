/// <summary>
/// P2(오른쪽 코너) 캐릭터의 스킬 쿨타임 HUD다. 이름은 "Opponent"지만 실제로는 "상대"가 아니라
/// **P2 자리의 캐릭터**를 보여준다 — 오른쪽 코너의 HP/승수(PlayerCornerHUD)와 항상 같은 캐릭터다.
/// 동작은 전부 PlayerSideCooldownHUD에 있고, 여기서는 "P2 자리"라는 것만 정한다.
/// (P1 자리는 CooldownHUD)
/// </summary>
public class OpponentCooldownHUD : PlayerSideCooldownHUD
{
    protected override bool IsPlayer1Side => false;
}
