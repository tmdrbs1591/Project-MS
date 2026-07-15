using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 라운드 종료 후(MatchPhase.AugmentSelect) 뜨는 증강 선택 UI. 로컬 캐릭터가 매치 시작 시
/// 고른 팩들의 증강 폴(CharacterBase.GetAugmentPool)에서 3개를 무작위로 뽑아 제시한다.
/// 선택은 로컬 캐릭터 자신에게만 적용되며(내 StateAuthority), 상대에게 알리거나 동기화할
/// 필요가 없다 — 증강 값 자체가 [Networked] 라 자동으로 보인다.
///
/// [씬 설정]
///   - 게임 씬의 Canvas 하위에 빈 오브젝트를 만들고 이 스크립트를 붙인다.
///   - panel: 평소엔 꺼져 있다가 증강 선택 구간에만 켜지는 패널.
///   - timerFillImage: Image Type을 Filled로 설정. 남은시간/전체시간 비율만큼 fillAmount가 줄어든다.
///   - slots: AugmentChoiceSlot 3개를 인스펙터에서 연결.
///   - 씬에는 하나만 존재해야 한다(싱글턴 아님, MatchResultUI와 달리 참조가 필요 없어 static 불필요).
/// </summary>
public class AugmentSelectUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Image timerFillImage;
    [SerializeField] private AugmentChoiceSlot[] slots;

    // 한 라운드에 한 번만 고를 수 있다. 다음 AugmentSelect 구간이 오면 다시 풀린다.
    private bool pickedThisRound;
    private List<AugmentPoolEntry> pool;

    private void Awake()
    {
        SetPanelActive(false);
    }

    private void Update()
    {
        MatchManager match = MatchManager.Instance;
        bool inSelectPhase = match != null && match.Phase == MatchPhase.AugmentSelect;

        if (!inSelectPhase)
        {
            ResetForNextRound();
            SetPanelActive(false);
            return;
        }

        if (pool == null)
            RollNewChoices();

        SetPanelActive(!pickedThisRound);

        if (timerFillImage != null)
        {
            float total = match.GetPhaseTotalDuration();
            float remaining = Mathf.Max(0f, match.GetPhaseTimeRemaining());
            timerFillImage.fillAmount = total > 0f ? remaining / total : 0f;
        }
    }

    private void RollNewChoices()
    {
        CharacterBase local = CharacterBase.LocalPlayer;
        pool = local != null ? local.GetAugmentPool() : new List<AugmentPoolEntry>();

        if (slots == null)
            return;

        foreach (AugmentChoiceSlot slot in slots)
            slot.SetSlotActive(false);

        for (int i = 0; i < slots.Length; i++)
            AssignRandom(i, resetReroll: true);
    }

    private void AssignRandom(int slotIndex, bool resetReroll)
    {
        if (slots == null || slotIndex >= slots.Length)
            return;

        AugmentChoiceSlot slot = slots[slotIndex];

        // 이미 이번 라운드에 다른 슬롯에 배정된 증강은 제외한다(중복 노출 방지).
        List<AugmentType> excluded = new List<AugmentType>();
        for (int i = 0; i < slots.Length; i++)
        {
            if (i != slotIndex && slots[i].gameObject.activeSelf)
                excluded.Add(slots[i].CurrentType);
        }

        List<AugmentPoolEntry> candidates = pool.FindAll(entry => entry.Data != null && !excluded.Contains(entry.Data.type));
        if (candidates.Count == 0)
        {
            slot.SetSlotActive(false);
            return;
        }

        AugmentPoolEntry chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        slot.SetSlotActive(true);
        if (resetReroll)
            slot.ResetRerollState();
        slot.Bind(chosen, OnPick, OnReroll);
    }

    private void OnPick(AugmentChoiceSlot slot)
    {
        if (pickedThisRound)
            return;

        CharacterBase local = CharacterBase.LocalPlayer;
        if (local == null)
            return;

        local.GrantAugment(slot.CurrentType);
        pickedThisRound = true;
    }

    private void OnReroll(AugmentChoiceSlot slot)
    {
        if (pickedThisRound)
            return;

        if (!slot.TryConsumeReroll())
            return;

        int index = Array.IndexOf(slots, slot);
        if (index >= 0)
            AssignRandom(index, resetReroll: false);
    }

    private void ResetForNextRound()
    {
        pickedThisRound = false;
        pool = null;
    }

    private void SetPanelActive(bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}
