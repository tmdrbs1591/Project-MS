using ProjectMS.CharacterSystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터가 설치한 오브젝트(CharacterOwnedEntity, 예: SPARK Q의 전기 노드) 위에 떠 있는 체력바다.
/// WorldHealthBarUI(캐릭터용)와 같은 이유로 월드 스페이스 캔버스로 만들어 오브젝트 프리팹의
/// 자식으로 둔다.
///
/// [체력 값 읽기]
///   - CharacterOwnedEntity.CurrentHealth/MaxHealth는 [Networked] NetHealth를 그대로 읽으므로
///     StateAuthority든 원격 클라이언트든 항상 최신 값을 읽을 수 있다 — 별도 동기화 코드 없이
///     매 프레임 값을 읽어 채운다(WorldHealthBarUI와 동일한 이유로 이벤트 대신 폴링).
///   - lifetimeMode가 체력을 안 쓰는 오브젝트(Manual/Duration 전용)는 MaxHealth가 항상 0이라
///     visualRoot를 연결해두면 자동으로 숨겨진다.
///
/// [씬/프리팹 설정]
///   - 오브젝트 프리팹 위쪽에 Canvas(Render Mode: World Space)를 자식으로 추가한다.
///   - visualRoot에 그 Canvas(또는 표시할 루트)를 연결하면, 체력이 없는 상태일 때 자동으로 꺼진다.
///     비워두면 숨김 처리를 하지 않는다 — 이 스크립트 자신이 붙은 오브젝트는 절대 SetActive(false)
///     하지 않는다(그러면 LateUpdate가 멈춰서 다시 켜질 수 없게 된다).
///   - CharacterOwnedEntity.EffectAnchor(비어 있으면 오브젝트 루트)를 기준 위치로 따라간다.
///     오브젝트 루트가 바닥/설치 기준점이라 스프라이트 중심과 다르면, EffectAnchor용 자식을
///     따로 만들어 연결해두면 체력바 위치가 그걸 따라간다.
///   - 오브젝트가 회전하는 종류(예: SparkQNode가 벽/천장에 붙을 때 회전)라도 체력바는 매 프레임
///     회전을 원상태로 되돌려서 항상 정면을 본다.
///   - Canvas 밑에 Slider 또는 Filled Image를 만들어 fillImage에 연결한다.
/// </summary>
public class OwnedEntityHealthBarUI : MonoBehaviour
{
    [SerializeField] private CharacterOwnedEntity entity;
    [SerializeField] private Image fillImage;
    [Tooltip("EffectAnchor 위치에 추가로 더할 오프셋(월드 좌표, 회전 영향 없음).")]
    [SerializeField] private Vector2 worldOffset;
    [Tooltip("체력을 안 쓰는 오브젝트거나 대상이 없을 때 꺼서 숨길 루트(보통 Canvas). 비워두면 숨기지 않는다.")]
    [SerializeField] private GameObject visualRoot;

    private void Awake()
    {
        if (entity == null)
            entity = ResolveOwnedEntity();
    }

    private void LateUpdate()
    {
        bool available = entity != null && entity.Object != null && entity.Object.IsValid &&
                          entity.IsActive && entity.MaxHealth > 0f;

        if (visualRoot != null && visualRoot.activeSelf != available)
            visualRoot.SetActive(available);

        if (!available || fillImage == null)
            return;

        transform.position = (Vector2)entity.EffectAnchor.position + worldOffset;
        transform.rotation = Quaternion.identity; // 오브젝트가 회전해도 체력바는 항상 정면을 본다.

        fillImage.fillAmount = entity.CurrentHealth / entity.MaxHealth;
    }

    // 같은 오브젝트에 CharacterOwnedEntity 계열 컴포넌트가 여러 개 붙어 있을 수 있어서(예: 잘못
    // 남겨진 기반 클래스 컴포넌트) 가장 파생된 타입을 우선 선택한다.
    private CharacterOwnedEntity ResolveOwnedEntity()
    {
        CharacterOwnedEntity[] candidates = GetComponentsInParent<CharacterOwnedEntity>(true);
        if (candidates.Length == 0)
            return null;

        foreach (CharacterOwnedEntity candidate in candidates)
        {
            System.Type type = candidate.GetType();
            if (type != typeof(CharacterOwnedEntity) && type != typeof(CharacterDeployable))
                return candidate;
        }

        return candidates[0];
    }
}
