using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 팩 관리 UI(AugmentPackManageUI)의 행 하나. 팩 이름과 해금 상태 텍스트("해금됨"/"해금 필요")를
/// 보여준다. 해금 여부는 읽기만 하며(이 UI에서 직접 바꾸는 액션 없음), 잠긴 팩도 상세 패널은
/// 항상 열어볼 수 있다(어떤 증강이 들었는지 미리보기 용도).
/// </summary>
public class AugmentPackRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text unlockStatusText;
    [SerializeField] private Button detailButton;

    private AugmentPackData pack;

    public void Bind(AugmentPackData pack, Action<AugmentPackData> onDetailRequested)
    {
        this.pack = pack;

        if (nameText != null)
            nameText.text = pack.displayName;

        if (unlockStatusText != null)
        {
            bool isUnlock = AugmentPackManager.IsUnlocked(pack);
            isUnlock = true; // 임시
            unlockStatusText.text = isUnlock ? "해금됨" : "해금 필요";
            unlockStatusText.color = isUnlock ? Color.green : Color.red;
        }

        if (detailButton != null)
        {
            detailButton.onClick.RemoveAllListeners();
            detailButton.onClick.AddListener(() => onDetailRequested?.Invoke(this.pack));
        }
    }
}
