using Fusion;
using ProjectMS.CharacterSystem;
using UnityEngine;

/// <summary>
/// 맵 밖으로 떨어졌을 때 걸리는 낙사 존이다. 최대 체력의 일정 비율만큼 데미지를 주고
/// 위로 크게 튕겨올린다. (Fusion 2 / Shared 모드)
///
/// [동작]
///   - [Networked] 상태가 전혀 없어서(소비/재생성 같은 공유 상태가 없음) NetworkObject가
///     필요 없다 — 그냥 트리거 콜라이더를 가진 일반 MonoBehaviour다.
///   - "내 플레이어"가 들어왔을 때만 반응한다(character.Object.HasStateAuthority로 판정 —
///     Map/ItemBase.cs가 쓰는 것과 같은 중복 방지 패턴). 각 클라가 자기 캐릭터에 대해서만
///     처리하면 되므로 이걸로 충분하고, RPC나 권한 이전이 필요 없다.
///   - 데미지: character.MaxHealth * fallDamagePercent, 공격자 없음(PlayerRef.None) —
///     PushableStructure의 낙하 데미지 처리와 동일한 관례.
///   - 튕겨오르기: character.ApplyMapBounce(bounceVelocity, bounceDuration) — 전투 피격
///     넉백과는 별개의 API(CharacterBase.ApplyMapBounce 참고). 즉발 임펄스가 아니라
///     "bounceDuration초 동안 bounceVelocity 속도를 중력 없이 유지"하는 방식이라, 속도를
///     극단적으로 안 올려도 체공시간만큼 자연스럽게 높이 올라간다(순간이동처럼 보이는 걸 방지).
///
/// [필요한 것]
///   - 맵 아래(낙사 지점)에 빈 오브젝트 + Collider2D(Is Trigger 켜기).
///   - 플레이어 프리팹에 "Player" 태그.
/// </summary>
public class OutOfBoundsZone : MonoBehaviour
{
    [Tooltip("최대 체력 대비 데미지 비율(0.1 = 10%).")]
    [Range(0f, 1f)] [SerializeField] private float fallDamagePercent = 0.1f;

    [Tooltip("튕겨오르는 속도. 너무 크게 잡으면 순간이동처럼 보이니, 높이는 이 값보다는 " +
             "Bounce Duration으로 조절하는 걸 추천.")]
    [Min(0f)] [SerializeField] private float bounceVelocity = 12f;

    [Tooltip("bounceVelocity를 중력 없이 유지하는 시간(초). 이 값을 늘리면 속도는 그대로 " +
             "둔 채로 더 높이 튀어오른다.")]
    [Min(0f)] [SerializeField] private float bounceDuration = 0.5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        CharacterBase character = other.GetComponentInParent<CharacterBase>();
        if (character == null || character.Object == null || !character.Object.HasStateAuthority)
            return;

        if (fallDamagePercent > 0f)
            character.RequestDamage(character.MaxHealth * fallDamagePercent, PlayerRef.None);

        if (bounceVelocity > 0f && bounceDuration > 0f)
            character.ApplyMapBounce(bounceVelocity, bounceDuration);
    }
}
