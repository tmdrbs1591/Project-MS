using System;
using System.Collections.Generic;
using ProjectMS.CharacterSystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 라운드 종료 후(MatchPhase.AugmentSelect) 뜨는 증강 선택 UI. 로컬 캐릭터가 매치 시작 시
/// 고른 팩들의 증강 폴(CharacterBase.GetAugmentPool)에서 3개를 무작위로 뽑아 제시한다.
///
/// [픽 수]
///   - 라운드 승자는 1개, 패자는 2개를 고른다(CharacterBase.AugmentPicksRemaining, MatchManager가 배정).
///   - 패자가 첫 픽을 고르면 남은 픽 수가 줄어드는 것을 감지해 자동으로 다음 3택을 다시 띄운다.
///
/// [규칙]
///   - 초기 배정 시엔 같은 픽에 이미 다른 슬롯에 나온 팩과 겹치지 않는 후보를 우선 뽑는다.
///     고른 팩 수가 3개 이상이면 3개 슬롯이 서로 다른 팩에서 나온다(부족할 때만 같은 팩에서 또 뽑음).
///   - 리롤은 "그 슬롯이 리롤 전 갖고 있던 팩"만 제외한다(다른 슬롯에 떠 있는 팩과 겹치는 건
///     허용). 그마저도 후보가 없으면(안 나온 증강이 그 팩밖에 안 남은 경우) 팩 제약 없이 허용한다.
///   - 이번 픽에 한 번이라도 등장했던 증강은(리롤로 사라진 것 포함) 같은 픽 안에서는 다시 나오지
///     않는다. 이미 보유한 증강(CharacterBase.HasAugment)도 애초에 후보에서 제외된다.
///
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

    private List<AugmentPoolEntry> pool;

    // 마지막으로 슬롯을 채웠을 때의 AugmentPicksRemaining 값. 이 값이 바뀌면(픽을 하나 소비하면)
    // 다음 Update에서 새 3택을 다시 굴린다 — 패자의 2번째 픽이 이 방식으로 자동으로 뜬다.
    private int rolledForPicksRemaining = -1;

    // 이번 픽(3택 + 리롤)에 한 번이라도 슬롯에 떴던 증강. 다시 등장하지 않게 막는다.
    private readonly HashSet<AugmentType> shownTypesThisPick = new HashSet<AugmentType>();

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

        CharacterBase local = CharacterBase.LocalPlayer;
        int picksRemaining = local != null ? local.AugmentPicksRemaining : 0;

        if (pool == null)
            pool = local != null ? local.GetAugmentPool() : new List<AugmentPoolEntry>();

        if (picksRemaining > 0 && picksRemaining != rolledForPicksRemaining)
            RollNewChoices(picksRemaining);

        float total = match.GetPhaseTotalDuration();
        float remaining = Mathf.Max(0f, match.GetPhaseTimeRemaining());

        // 제한시간 안에 못 고르면 지금 제시된 슬롯 중 맨 앞(왼쪽)을 자동으로 선택한다.
        // 패자처럼 픽이 두 개 남아있어도, 첫 픽을 자동 선택하면 다음 프레임에 새 3택이
        // 뜨자마자 그 첫 번째도 이어서 자동 선택되어(remaining이 계속 0이므로) 남은 픽이
        // 모두 채워질 때까지 이어진다.
        if (remaining <= 0f && picksRemaining > 0)
            AutoPickFirstAvailable();

        SetPanelActive(picksRemaining > 0);

        if (timerFillImage != null)
            timerFillImage.fillAmount = total > 0f ? remaining / total : 0f;
    }

    /// <summary>제한시간 초과 시 지금 활성화된 슬롯 중 맨 앞(왼쪽) 것을 자동으로 선택한다.</summary>
    private void AutoPickFirstAvailable()
    {
        if (slots == null)
            return;

        foreach (AugmentChoiceSlot slot in slots)
        {
            if (slot.gameObject.activeSelf)
            {
                OnPick(slot);
                return;
            }
        }
    }

    private void RollNewChoices(int picksRemaining)
    {
        rolledForPicksRemaining = picksRemaining;
        shownTypesThisPick.Clear();

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

        CharacterBase local = CharacterBase.LocalPlayer;
        AugmentChoiceSlot slot = slots[slotIndex];
        bool isReroll = !resetReroll;

        // 이번 픽에 한 번이라도 나왔던 증강과, 이미 보유한 증강은 항상 제외한다.
        List<AugmentPoolEntry> notShown = pool.FindAll(entry =>
            entry.Data != null &&
            !shownTypesThisPick.Contains(entry.Data.type) &&
            (local == null || !local.HasAugment(entry.Data.type)));

        List<AugmentPoolEntry> candidates;

        if (isReroll)
        {
            // 리롤: 이 슬롯이 리롤 전 갖고 있던 팩만 제외한다. 다른 슬롯에 떠 있는 팩과 겹치는 건 허용.
            AugmentPackData ownPack = slot.gameObject.activeSelf ? slot.CurrentEntry.SourcePack : null;
            candidates = notShown.FindAll(entry => entry.SourcePack == null || entry.SourcePack != ownPack);

            // 그마저 없으면(안 나온 증강이 그 팩밖에 안 남은 경우) 팩 제약 없이 허용한다.
            if (candidates.Count == 0)
                candidates = notShown;
        }
        else
        {
            // 초기 배정: 다른 슬롯에 이미 나온 팩은 피해서 최대한 팩이 고루 나오게 한다.
            List<AugmentPackData> otherSlotPacks = new List<AugmentPackData>();
            for (int i = 0; i < slots.Length; i++)
            {
                if (i == slotIndex || !slots[i].gameObject.activeSelf)
                    continue;

                AugmentPackData otherPack = slots[i].CurrentEntry.SourcePack;
                if (otherPack != null && !otherSlotPacks.Contains(otherPack))
                    otherSlotPacks.Add(otherPack);
            }

            candidates = notShown.FindAll(entry => entry.SourcePack == null || !otherSlotPacks.Contains(entry.SourcePack));

            // 그마저 없으면(고른 팩이 3개 미만인 경우 등) 팩 제약 없이 허용한다.
            if (candidates.Count == 0)
                candidates = notShown;
        }

        if (candidates.Count == 0)
        {
            slot.SetSlotActive(false);
            return;
        }

        AugmentPoolEntry chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        shownTypesThisPick.Add(chosen.Data.type);

        slot.SetSlotActive(true);
        if (resetReroll)
            slot.ResetRerollState();
        slot.Bind(chosen, OnPick, OnReroll);
    }

    private void OnPick(AugmentChoiceSlot slot)
    {
        CharacterBase local = CharacterBase.LocalPlayer;
        if (local == null || local.AugmentPicksRemaining <= 0)
            return;

        local.GrantAugment(slot.CurrentType);
        // 남은 픽이 있으면(패자의 2번째 픽) 다음 Update에서 picksRemaining 변화가 감지되어
        // RollNewChoices가 자동으로 다시 호출된다.
    }

    private void OnReroll(AugmentChoiceSlot slot)
    {
        CharacterBase local = CharacterBase.LocalPlayer;
        if (local == null || local.AugmentPicksRemaining <= 0)
            return;

        if (!slot.TryConsumeReroll())
            return;

        int index = Array.IndexOf(slots, slot);
        if (index >= 0)
            AssignRandom(index, resetReroll: false);
    }

    private void ResetForNextRound()
    {
        pool = null;
        rolledForPicksRemaining = -1;
        shownTypesThisPick.Clear();
    }

    private void SetPanelActive(bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}
