using UnityEngine;
using Map;

/// <summary>
/// 아이템 예시다.
///
/// [역할]
///   - ItemBase를 상속받아 플레이어 접촉 시 최대 체력의 50%를 회복시킨다.
///   - 픽업 후 지정한 시간이 지나면 오브젝트가 다시 활성화된다.
///   - 새 아이템을 만들 때 이 파일을 참고해서 OnPickup 내용을 바꾸고 활용하면된다.
///
/// [필요한 것]
///   - 오브젝트에 Collider2D (Is Trigger 체크)
///   - 플레이어 오브젝트에 "Player" 태그 설정
///   - 플레이어 프리팹에 CharacterBase를 상속받은 캐릭터 스크립트 부착 (플레이어의 상태를 관여하기 때문)
/// </summary>
public class HealPack : ItemBase
{
    protected override void OnPickup(Collider2D player)
    {
        CharacterBase character = player.GetComponent<CharacterBase>();
        if (character == null) return;

        character.Heal(character.MaxHealth * 0.5f);
    }
}
