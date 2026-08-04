using ProjectMS.CharacterSystem;
using UnityEngine;

/// <summary>
/// 화면 하단에 고정된 스킬 쿨타임 HUD다. 로컬 플레이어(내 캐릭터) 한 명에만 연결되며,
/// 상대 캐릭터의 쿨타임은 표시하지 않는다.
///
/// [연결 흐름]
///   - CharacterBase.Spawned()에서 자신이 로컬 플레이어면 CooldownHUD.Instance.Bind(this)를 호출한다.
///   - CharacterBase.Despawned()에서 Unbind()로 해제한다.
///
/// [씬 설정]
///   - 게임 씬의 Screen Space Canvas 하위에 빈 오브젝트를 만들고 이 스크립트를 붙인다.
///   - 하위에 CooldownSlotUI를 행동 종류 수만큼(평타/Q/E/대시/궁) 만들어 slots 배열에 연결한다.
///   - 씬에는 하나만 존재해야 한다(싱글턴).
/// </summary>
public class CooldownHUD : MonoBehaviour
{
    public static CooldownHUD Instance { get; private set; }

    [SerializeField] private CooldownSlotUI[] slots;

    private CharacterBase character;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Bind(CharacterBase target)
    {
        character = target;
    }

    public void Unbind()
    {
        character = null;
    }

    private void Update()
    {
        if (character == null || character.Cooldowns == null)
            return;

        foreach (CooldownSlotUI slot in slots)
        {
            float remaining = character.Cooldowns.GetRemaining(slot.ActionType);
            float total = character.Cooldowns.GetDuration(slot.ActionType);
            slot.UpdateCooldown(remaining, total);
        }
    }
}
