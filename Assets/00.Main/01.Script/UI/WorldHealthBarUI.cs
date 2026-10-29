using ProjectMS.CharacterSystem;
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
/// [연출]
///   - 색: 내 캐릭터는 ownColor, 나머지(상대/봇)는 enemyColor.
///   - 맞으면 흰 잔상(trailImage)이 잠깐 머물렀다가 천천히 따라 줄어들고, 바 전체가 아주 살짝 흔들린다.
///   - 체력이 lowHealthRatio 아래로 떨어지면 채움 색이 은은하게 깜빡인다.
///
/// [씬/프리팹 설정]
///   - 캐릭터 프리팹 루트(머리 위치 근처, 예: Circle) 밑에 Canvas(Render Mode: World Space)를 자식으로 추가한다.
///   - 캐릭터가 방향전환 시 루트 scale.x가 ±1로 뒤집히므로, 체력바가 좌우 반전되지 않도록
///     이 Canvas에 CounterFlip 컴포넌트를 함께 붙인다.
///   - Canvas 밑에 Slider 또는 Filled Image를 만들어 fillImage에 연결한다(trailImage는 선택).
/// </summary>
public class WorldHealthBarUI : MonoBehaviour
{
    [SerializeField] private CharacterBase character;
    [SerializeField] private Image fillImage;
    [Tooltip("깎인 만큼 잠깐 남아 있다가 천천히 줄어드는 흰 잔상 바(fillImage 뒤, Filled Horizontal). 선택 사항.")]
    [SerializeField] private Image trailImage;

    [Header("색")]
    [SerializeField] private Color ownColor = new Color(0.35f, 0.92f, 0.55f, 1f);
    [SerializeField] private Color enemyColor = new Color(1f, 0.33f, 0.36f, 1f);
    [Range(0f, 1f)] [SerializeField] private float lowHealthRatio = 0.3f;

    [Header("움직임")]
    [Min(0f)] [SerializeField] private float trailDelay = 0.3f;
    [Min(0.1f)] [SerializeField] private float trailSpeed = 1.1f;
    [Tooltip("맞았을 때 흔들리는 폭(월드 단위). 0이면 안 흔들린다.")]
    [Min(0f)] [SerializeField] private float hitShake = 0.03f;

    private float trailFill = 1f;
    private float lastFill = 1f;
    private float lastHitTime = -10f;
    private Vector2 basePosition;
    private RectTransform rect;

    private void Awake()
    {
        if (character == null)
            character = GetComponentInParent<CharacterBase>();
        rect = transform as RectTransform;
        if (rect != null)
            basePosition = rect.anchoredPosition;
    }

    private void LateUpdate()
    {
        if (character == null || character.Object == null || fillImage == null)
            return;

        float max = character.MaxHealth;
        float fill = max > 0f ? character.CurrentHealth / max : 0f;
        fillImage.fillAmount = fill;

        float now = Time.unscaledTime;
        if (fill < lastFill - 0.0001f)
            lastHitTime = now;
        lastFill = fill;

        Color baseColor = character == CharacterBase.LocalPlayer ? ownColor : enemyColor;
        if (fill <= lowHealthRatio && fill > 0f)
            baseColor = Color.Lerp(baseColor, Color.white, 0.35f * (0.5f + 0.5f * Mathf.Sin(now * 8f)));
        fillImage.color = baseColor;

        if (trailImage != null)
        {
            if (fill > trailFill)
                trailFill = fill;
            else if (now - lastHitTime > trailDelay)
                trailFill = Mathf.MoveTowards(trailFill, fill, trailSpeed * Time.unscaledDeltaTime);
            trailImage.fillAmount = trailFill;
        }

        if (rect != null && hitShake > 0f)
        {
            float shake = Mathf.Clamp01(1f - (now - lastHitTime) / 0.18f);
            rect.anchoredPosition = basePosition + new Vector2(Mathf.Sin(now * 70f) * hitShake * shake, 0f);
        }
    }
}
