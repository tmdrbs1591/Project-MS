using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AugmentPackDetailUI에서 팩에 포함된 증강 하나를 보여주는 읽기 전용 행(아이콘/제목/설명).
/// </summary>
public class AugmentInfoRow : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    public void Bind(AugmentData augment)
    {
        if (augment == null)
            return;

        if (iconImage != null)
            iconImage.sprite = augment.icon;

        if (titleText != null)
            titleText.text = augment.title;

        if (descriptionText != null)
            descriptionText.text = augment.description;
    }
}
