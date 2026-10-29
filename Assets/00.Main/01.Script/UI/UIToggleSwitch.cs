using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 체크박스 Toggle을 스위치(알약 + 미끄러지는 동그라미)로 보이게 한다.
/// Toggle.isOn을 매 프레임 읽으므로 SetIsOnWithoutNotify로 바뀐 값도 따라간다.
/// </summary>
[RequireComponent(typeof(Toggle))]
public class UIToggleSwitch : MonoBehaviour
{
    [SerializeField] private Image track;
    [SerializeField] private RectTransform knob;
    [SerializeField] private Color onColor = new Color(0.35f, 0.62f, 1f, 1f);
    [SerializeField] private Color offColor = new Color(0.14f, 0.18f, 0.32f, 1f);
    [SerializeField] private float travel = 12f;
    [SerializeField] private AudioClip clickSound;
    [Range(0f, 1f)] [SerializeField] private float clickVolume = 0.5f;

    private Toggle toggle;
    private float t;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        t = toggle.isOn ? 1f : 0f;
        toggle.onValueChanged.AddListener(_ => SoundManager.Instance?.PlaySfx(clickSound, clickVolume, 0.04f));
        Apply();
    }

    private void Update()
    {
        t = Mathf.MoveTowards(t, toggle.isOn ? 1f : 0f, Time.unscaledDeltaTime * 8f);
        Apply();
    }

    private void Apply()
    {
        float e = t * t * (3f - 2f * t);
        if (knob != null)
            knob.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, e), 0f);
        if (track != null)
        {
            Color c = Color.Lerp(offColor, onColor, e);
            if (!toggle.IsInteractable())
                c.a *= 0.45f;
            track.color = c;
        }
    }
}
