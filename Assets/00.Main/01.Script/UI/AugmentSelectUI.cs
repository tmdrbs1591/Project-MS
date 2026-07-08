using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 라운드 종료 후(MatchPhase.AugmentSelect) 뜨는 증강 선택 UI. 지금은 프로토타입이라
/// 두 개 중 하나만 고를 수 있다. 선택은 로컬 캐릭터 자신에게만 적용되며(내 StateAuthority),
/// 상대에게 알리거나 동기화할 필요가 없다 — 증강 값 자체가 [Networked] 라 자동으로 보인다.
///
/// [씬 설정]
///   - 게임 씬의 Canvas 하위에 빈 오브젝트를 만들고 이 스크립트를 붙인다.
///   - panel: 평소엔 꺼져 있다가 증강 선택 구간에만 켜지는 패널.
///   - qAugmentButton / eAugmentButton: 각 증강을 고르는 버튼. 텍스트는 버튼 자식에 직접 배치.
///   - 씬에는 하나만 존재해야 한다(싱글턴 아님, MatchResultUI와 달리 참조가 필요 없어 static 불필요).
/// </summary>
public class AugmentSelectUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button qAugmentButton;
    [SerializeField] private Button eAugmentButton;

    // 한 라운드에 한 번만 고를 수 있다. 다음 AugmentSelect 구간이 오면 다시 풀린다.
    private bool pickedThisRound;

    private void Awake()
    {
        SetPanelActive(false);

        if (qAugmentButton != null)
            qAugmentButton.onClick.AddListener(() => PickAugment(AugmentType.QSkillOctoDirection));

        if (eAugmentButton != null)
            eAugmentButton.onClick.AddListener(() => PickAugment(AugmentType.ESkillTripleThrow));
    }

    private void Update()
    {
        MatchManager match = MatchManager.Instance;
        bool inSelectPhase = match != null && match.Phase == MatchPhase.AugmentSelect;

        if (!inSelectPhase)
        {
            pickedThisRound = false;
            SetPanelActive(false);
            return;
        }

        SetPanelActive(!pickedThisRound);
    }

    private void PickAugment(AugmentType type)
    {
        if (pickedThisRound)
            return;

        CharacterBase local = CharacterBase.LocalPlayer;
        if (local == null)
            return;

        local.GrantAugment(type);
        pickedThisRound = true;
    }

    private void SetPanelActive(bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}
