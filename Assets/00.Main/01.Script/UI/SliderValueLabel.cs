using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 슬라이더 값(0~1)을 "75%"처럼 글자로 보여준다. SetValueWithoutNotify로 바뀐 값도 따라가게 매 프레임 읽는다.
/// </summary>
public class SliderValueLabel : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text label;
    [SerializeField] private string format = "{0}%";

    private int shown = -1;

    private void Update()
    {
        if (slider == null || label == null)
            return;
        int value = Mathf.RoundToInt(Mathf.InverseLerp(slider.minValue, slider.maxValue, slider.value) * 100f);
        if (value == shown)
            return;
        shown = value;
        label.text = string.Format(format, value);
    }
}
