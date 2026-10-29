using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 좌우 넘기기용 둥근 화살표 버튼 연출.
/// 평소엔 화살표가 가리키는 방향으로 살짝 까딱거려 "넘길 수 있다"는 걸 알려주고,
/// 마우스를 올리면 버튼이 커지며 테두리와 주변 빛이 밝아진다. 누르면 화살표가 그쪽으로 톡 튄다.
/// </summary>
public class UIArrowButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Tooltip("화살표 아이콘(까딱거리는 부분).")]
    [SerializeField] private RectTransform icon;
    [SerializeField] private Image ring;
    [SerializeField] private Image glow;
    [Tooltip("화살표가 가리키는 방향(-1 왼쪽, 1 오른쪽).")]
    [SerializeField] private int direction = 1;
    [SerializeField] private float hoverScale = 1.12f;
    [SerializeField] private float nudgeDistance = 4f;
    [SerializeField] private Color ringColor = new Color(0.72f, 0.82f, 1f, 0.55f);
    [SerializeField] private Color ringHoverColor = new Color(0.85f, 0.75f, 1f, 1f);

    private bool hovered;
    private float hoverT;
    private float pressTime = -10f;
    private Vector2 iconBase;

    private void Awake()
    {
        if (icon != null)
            iconBase = icon.anchoredPosition;
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) => hovered = false;
    public void OnPointerDown(PointerEventData eventData) => pressTime = Time.unscaledTime;

    private void OnDisable()
    {
        hovered = false;
        hoverT = 0f;
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        float k = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
        hoverT = Mathf.Lerp(hoverT, hovered ? 1f : 0f, k);

        float press = Mathf.Clamp01(1f - (now - pressTime) / 0.18f);
        float s = Mathf.Lerp(1f, hoverScale, hoverT) * (1f - press * 0.08f);
        transform.localScale = new Vector3(s, s, 1f);

        if (icon != null)
        {
            // 평소엔 같은 방향으로 두 번 톡톡 → 쉬기를 반복, 누르면 크게 한 번 튄다.
            float beat = Mathf.Repeat(now, 1.6f);
            float idle = beat < 0.5f ? Mathf.Abs(Mathf.Sin(beat / 0.5f * Mathf.PI * 2f)) : 0f;
            float offset = (idle * nudgeDistance * (1f - hoverT) + hoverT * nudgeDistance * 0.8f + press * nudgeDistance * 2f) * direction;
            icon.anchoredPosition = iconBase + new Vector2(offset, 0f);
        }

        if (ring != null)
            ring.color = Color.Lerp(ringColor, ringHoverColor, hoverT);
        if (glow != null)
        {
            Color c = ringHoverColor;
            c.a = Mathf.Lerp(0.15f, 0.55f, hoverT);
            glow.color = c;
        }
    }
}
