using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PackSelectUI의 행 하나. 팩 이름과 "이번 매치에 쓸지" 선택 토글을 보여준다.
/// (로비의 AugmentPackRow는 해금 여부를, 이 행은 매치용 선택 여부를 다룬다는 점이 다르다.)
/// </summary>
public class PackSelectRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Toggle selectToggle;

    public AugmentPackData Pack { get; private set; }

    private Action<AugmentPackData, bool> onToggleChanged;

    public void Bind(AugmentPackData pack, Action<AugmentPackData, bool> onToggleChanged)
    {
        Pack = pack;
        this.onToggleChanged = onToggleChanged;

        if (nameText != null)
            nameText.text = pack.displayName;

        if (selectToggle != null)
        {
            selectToggle.onValueChanged.RemoveAllListeners();
            selectToggle.SetIsOnWithoutNotify(false);
            selectToggle.interactable = true;
            selectToggle.onValueChanged.AddListener(OnValueChanged);
        }
    }

    public void SetOnWithoutNotify(bool isOn)
    {
        if (selectToggle != null)
            selectToggle.SetIsOnWithoutNotify(isOn);
    }

    public void SetInteractable(bool interactable)
    {
        if (selectToggle != null)
            selectToggle.interactable = interactable;
    }

    private void OnValueChanged(bool isOn)
    {
        onToggleChanged?.Invoke(Pack, isOn);
    }
}
