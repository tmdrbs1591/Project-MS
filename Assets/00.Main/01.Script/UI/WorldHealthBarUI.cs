using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 머리 위에 떠 있는 체력바다. 양쪽 플레이어 모두에게 보여야 하므로
/// World Space Canvas로 만들어 캐릭터 프리팹의 자식으로 둔다.
///
/// [체력 값 읽기]
///   - CharacterBase.Health.CurrentHealth/MaxHealth는 [Networked] NetHealth를 그대로 읽으므로
///     StateAuthority든 원격 클라이언트든 항상 최신 값을 읽을 수 있다.
///   - 따라서 이벤트 구독 대신 매 프레임 값을 읽어 채우는 방식으로 원격 동기화 타이밍 문제를 피한다.
///
/// [씬/프리팹 설정]
///   - 캐릭터 프리팹 루트(머리 위치 근처, 예: Circle) 밑에 Canvas(Render Mode: World Space)를 자식으로 추가한다.
///   - 캐릭터가 방향전환 시 루트 scale.x가 ±1로 뒤집히므로, 체력바가 좌우 반전되지 않도록
///     이 Canvas에 CounterFlip 컴포넌트를 함께 붙인다.
///   - Canvas 밑에 Slider 또는 Filled Image를 만들어 fillImage에 연결한다.
/// </summary>
public class WorldHealthBarUI : MonoBehaviour
{
    [SerializeField] private CharacterBase character;
    [SerializeField] private Image fillImage;

    private void Awake()
    {
        if (character == null)
            character = GetComponentInParent<CharacterBase>();
    }

    private void LateUpdate()
    {
        if (character == null || character.Health == null || fillImage == null)
            return;

        float max = character.Health.MaxHealth;
        fillImage.fillAmount = max > 0f ? character.Health.CurrentHealth / max : 0f;
    }
}
