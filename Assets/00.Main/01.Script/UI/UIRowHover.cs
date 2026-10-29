using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 설정 줄 같은 목록 줄에 마우스를 올리면 바탕이 은은하게 밝아지고, 왼쪽 강조 막대가 나타난다.
/// </summary>
public class UIRowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image accent;
    [SerializeField] private float idleAlpha = 0.035f;
    [SerializeField] private float hoverAlpha = 0.09f;

    private bool hovered;
    private float t;

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) => hovered = false;

    private void OnDisable()
    {
        hovered = false;
        t = 0f;
        Apply();
    }

    private void Update()
    {
        t = Mathf.MoveTowards(t, hovered ? 1f : 0f, Time.unscaledDeltaTime * 8f);
        Apply();
    }

    private void Apply()
    {
        if (background != null)
        {
            Color c = background.color;
            c.a = Mathf.Lerp(idleAlpha, hoverAlpha, t);
            background.color = c;
        }
        if (accent != null)
        {
            Color c = accent.color;
            c.a = t;
            accent.color = c;
            accent.rectTransform.localScale = new Vector3(1f, Mathf.Lerp(0.4f, 1f, t), 1f);
        }
    }
}
