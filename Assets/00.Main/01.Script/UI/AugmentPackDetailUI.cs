using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 팩 관리 UI(AugmentPackManageUI) 옆에 상시 떠 있는 상세 패널.
/// 팩 이름/설명/이미지와, 그 팩에 포함된 증강들(아이콘/제목/설명)을 보여준다.
/// 패널 자체는 항상 켜져 있고, 행을 누르거나 관리 UI가 열릴 때 Show()로 내용만 갱신된다.
///
/// [씬 설정]
///   - augmentListContainer / augmentRowPrefab: 팩에 포함된 증강마다 하나씩 생성되는 읽기 전용 행.
/// </summary>
public class AugmentPackDetailUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Transform augmentListContainer;
    [SerializeField] private AugmentInfoRow augmentRowPrefab;

    private readonly List<AugmentInfoRow> spawnedRows = new List<AugmentInfoRow>();

    public void Show(AugmentPackData pack)
    {
        if (pack == null)
            return;

        if (titleText != null)
            titleText.text = pack.displayName;

        if (descriptionText != null)
            descriptionText.text = pack.description;

        if (iconImage != null)
            iconImage.sprite = pack.icon;

        BuildAugmentRows(pack);
    }

    private void BuildAugmentRows(AugmentPackData pack)
    {
        foreach (AugmentInfoRow row in spawnedRows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }
        spawnedRows.Clear();

        if (augmentRowPrefab == null || augmentListContainer == null)
            return;

        foreach (AugmentData augment in pack.augments)
        {
            if (augment == null)
                continue;

            AugmentInfoRow row = Instantiate(augmentRowPrefab, augmentListContainer);
            row.Bind(augment);
            spawnedRows.Add(row);
        }
    }
}
