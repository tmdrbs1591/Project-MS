using TMPro;
using UnityEngine;

/// <summary>
/// 지정한 동작에 묶인 키 이름을 텍스트로 보여준다(스킬 슬롯의 Q/E/R/SHIFT, 상호작용 F 안내 등).
/// 키 설정이 바뀌면 즉시 갱신된다. TMP_Text가 있는 오브젝트에 붙인다.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class KeyBindingLabel : MonoBehaviour
{
    [SerializeField] private BindableAction action;
    [Tooltip("{0} 자리에 키 이름이 들어간다. 비워두면 키 이름만.")]
    [SerializeField] private string format = string.Empty;

    private TMP_Text label;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        KeyBindings.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        KeyBindings.Changed -= Refresh;
    }

    private void Refresh()
    {
        if (label == null)
            label = GetComponent<TMP_Text>();

        string key = KeyBindings.GetKeyLabel(action);
        label.text = string.IsNullOrEmpty(format) ? key : string.Format(format, key);
    }
}
