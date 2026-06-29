using UnityEngine;
using Map;

/// <summary>
/// 회복 아이템 예시. (Fusion 2)
///
/// [역할]
///   - ItemBase 를 상속받아 "내 플레이어" 접촉 시 최대 체력의 50%를 회복한다.
///   - 회복은 그 플레이어의 StateAuthority 에서 적용되도록 RequestHeal 로 요청한다.
///
/// [필요한 것]
///   - 오브젝트에 NetworkObject + Collider2D(Is Trigger)
///   - 플레이어 오브젝트에 "Player" 태그 + CharacterBase 상속 스크립트
/// </summary>
public class HealPack : ItemBase
{
    protected override void OnPickup(Collider2D player)
    {
        CharacterBase character = player.GetComponentInParent<CharacterBase>();
        if (character == null) return;

        character.RequestHealByMaxHealthPercent(50);
    }
}
