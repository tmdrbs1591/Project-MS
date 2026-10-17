using System;
using System.Collections;
using System.Collections.Generic;
using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 라운드 종료 후(MatchPhase.AugmentSelect) 뜨는 증강 선택 UI. 로컬 캐릭터가 매치 시작 시
/// 고른 팩들의 증강 폴(CharacterBase.GetAugmentPool)에서 3개를 무작위로 뽑아 제시한다.
///
/// [픽 수]
///   - 라운드 승자는 1개, 패자는 2개를 고른다(CharacterBase.AugmentPicksRemaining, MatchManager가 배정).
///
/// [연출 흐름]
///   1) Intro: 패널이 켜지면 패널 애니메이터(카드팩 개봉 + 카드 등장)가 재생된다. 이동안 클릭 불가.
///   2) 애니메이터가 끝나면 끄고(계속 값을 덮어쓰지 않도록) 카드 슬롯이 직접 움직인다(호버 등).
///   3) 고르면 증강은 즉시 지급하고, 고른 카드는 튀어 오르며, 나머지는 작아지며 사라진다.
///   4) 남은 픽이 있으면 새 3장을 펼쳐 보여주고, 없으면 "상대가 고르는 중" 대기 문구를 띄운다.
///   - 제한시간이 끝나면 연출을 건너뛰고 남은 픽을 맨 앞 카드로 즉시 채운다.
///
/// [규칙]
///   - 초기 배정 시엔 같은 픽에 이미 다른 슬롯에 나온 팩과 겹치지 않는 후보를 우선 뽑는다.
///   - 리롤은 "그 슬롯이 리롤 전 갖고 있던 팩"만 제외한다(그마저 없으면 팩 제약 없이 허용).
///   - 이번 픽에 한 번이라도 등장했던 증강과 이미 최대 중첩인 증강은 후보에서 제외한다.
///
/// 선택은 로컬 캐릭터 자신에게만 적용되며(내 StateAuthority), 증강 값 자체가 [Networked] 라
/// 상대에게 자동으로 보인다. 이 스크립트의 연출은 전부 로컬 화면 전용이다.
///
/// [씬 설정]
///   - panel: 증강 선택 구간에만 켜지는 패널(애니메이터가 붙어 있으면 첫 등장 연출로 쓴다).
///   - timerFillImage: Filled Image. timerText(선택): 남은 초.
///   - titleText(선택): "증강을 선택하세요 (1/2)"처럼 진행도를 표시. waitingText(선택): 대기 문구.
/// </summary>
public class AugmentSelectUI : MonoBehaviour
{
    private enum State { Hidden, Intro, Ready, PickAnimating, Waiting }

    [SerializeField] private GameObject panel;
    [SerializeField] private Image timerFillImage;
    [SerializeField] private AugmentChoiceSlot[] slots;

    [Header("텍스트 (선택)")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private string titleFormat = "증강을 선택하세요";
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text waitingText;
    [SerializeField] private string waitingMessage = "상대가 증강을 고르는 중...";

    [Header("타이머 강조")]
    [Tooltip("남은 시간이 이 값(초) 이하가 되면 타이머가 빨갛게 깜빡인다.")]
    [Min(0f)] [SerializeField] private float urgentSeconds = 3f;
    [SerializeField] private Color urgentColor = new Color(1f, 0.3f, 0.3f);

    [Header("연출 시간")]
    [Tooltip("패널 애니메이터의 첫 등장 연출 길이. 0이면 애니메이터의 클립 길이를 그대로 쓴다.")]
    [Min(0f)] [SerializeField] private float introDurationOverride;
    [Tooltip("두 번째 픽 카드들이 하나씩 펼쳐지는 간격(초).")]
    [Min(0f)] [SerializeField] private float revealStagger = 0.08f;
    [Min(0f)] [SerializeField] private float pickAnimationDuration = 0.45f;

    [Header("사운드 (선택)")]
    [SerializeField] private AudioClip introClip;
    [SerializeField] private AudioClip revealClip;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip pickClip;
    [SerializeField] private AudioClip rerollClip;
    [Range(0f, 1f)] [SerializeField] private float hoverVolume = 0.4f;

    private List<AugmentPoolEntry> pool;
    private int rolledForPicksRemaining = -1;
    private readonly HashSet<AugmentType> shownTypesThisPick = new HashSet<AugmentType>();

    private State state = State.Hidden;
    private Animator panelAnimator;
    private float introEndsAt;
    private int picksAtStart;
    private Color timerBaseColor = Color.white;
    private Coroutine pickRoutine;

    private void Awake()
    {
        if (panel != null)
            panelAnimator = panel.GetComponent<Animator>();
        if (timerFillImage != null)
            timerBaseColor = timerFillImage.color;

        if (slots != null)
        {
            foreach (AugmentChoiceSlot slot in slots)
            {
                if (slot != null)
                    slot.HoverEntered += _ => PlaySound(hoverClip, hoverVolume);
            }
        }

        SetPanelActive(false);
    }

    private void Update()
    {
        MatchManager match = MatchManager.Instance;
        bool inSelectPhase = match != null && match.Phase == MatchPhase.AugmentSelect;
        if (!inSelectPhase)
        {
            Hide();
            return;
        }

        CharacterBase local = CharacterBase.LocalPlayer;
        int picksRemaining = local != null ? local.AugmentPicksRemaining : 0;
        if (pool == null)
            pool = local != null ? local.GetAugmentPool() : new List<AugmentPoolEntry>();

        float total = match.GetPhaseTotalDuration();
        float remaining = Mathf.Max(0f, match.GetPhaseTimeRemaining());

        if (state == State.Hidden && picksRemaining > 0)
            BeginIntro(picksRemaining);

        // 제한시간 초과: 연출을 건너뛰고 남은 픽을 즉시 채운다(맨 앞 카드 자동 선택).
        if (remaining <= 0f && picksRemaining > 0)
        {
            FlushRemainingPicks(local);
            picksRemaining = local != null ? local.AugmentPicksRemaining : 0;
        }

        if (state == State.Intro && Time.unscaledTime >= introEndsAt)
            FinishIntro();

        if (picksRemaining <= 0 && state != State.Hidden && state != State.PickAnimating && state != State.Waiting)
            EnterWaiting();

        UpdateTimer(total, remaining);
        UpdateTitle(picksRemaining);
    }

    // ───────────────────────── 상태 전환 ─────────────────────────

    private void BeginIntro(int picksRemaining)
    {
        picksAtStart = picksRemaining;
        state = State.Intro;

        foreach (AugmentChoiceSlot slot in slots)
            slot.ReleaseToAnimator();

        // 애니메이터가 패널이 켜질 때 처음부터 재생되도록 다시 켜고, 슬로우모션 영향을 받지 않게 한다.
        if (panelAnimator != null)
        {
            panelAnimator.enabled = true;
            panelAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        SetPanelActive(true);
        SetWaiting(false);
        RollNewChoices(picksRemaining);
        SetSlotsInteractable(false);

        introEndsAt = Time.unscaledTime + ResolveIntroDuration();
        PlaySound(introClip);
    }

    private void FinishIntro()
    {
        // 애니메이터가 끝난 모습 그대로 멈춰 두고(끄기), 이후 움직임은 카드 슬롯이 맡는다.
        if (panelAnimator != null)
            panelAnimator.enabled = false;

        foreach (AugmentChoiceSlot slot in ActiveSlots())
            slot.TakeOver();

        ApplySlotContentVisibility();
        SetSlotsInteractable(true);
        state = State.Ready;
    }

    private void EnterWaiting()
    {
        state = State.Waiting;
        foreach (AugmentChoiceSlot slot in slots)
            slot.SetSlotActive(false);
        SetWaiting(true);
    }

    private void Hide()
    {
        if (state == State.Hidden)
            return;

        if (pickRoutine != null)
        {
            StopCoroutine(pickRoutine);
            pickRoutine = null;
        }

        state = State.Hidden;
        pool = null;
        rolledForPicksRemaining = -1;
        shownTypesThisPick.Clear();
        SetWaiting(false);
        SetPanelActive(false);
    }

    // ───────────────────────── 선택 / 리롤 ─────────────────────────

    private void OnPick(AugmentChoiceSlot picked)
    {
        if (state != State.Ready)
            return;

        CharacterBase local = CharacterBase.LocalPlayer;
        if (local == null || local.AugmentPicksRemaining <= 0)
            return;

        // 지급은 즉시(제한시간 경계와 겹쳐도 확실히 반영되게), 연출은 그 뒤에 보여준다.
        local.GrantAugment(picked.CurrentType);
        PlaySound(pickClip);
        pickRoutine = StartCoroutine(PickSequence(picked));
    }

    private IEnumerator PickSequence(AugmentChoiceSlot picked)
    {
        state = State.PickAnimating;
        SetSlotsInteractable(false);

        foreach (AugmentChoiceSlot slot in ActiveSlots())
        {
            if (slot == picked)
                slot.PlayPicked();
            else
                slot.PlayDismissed();
        }

        yield return WaitUnscaled(pickAnimationDuration);

        CharacterBase local = CharacterBase.LocalPlayer;
        int picksRemaining = local != null ? local.AugmentPicksRemaining : 0;
        pickRoutine = null;

        if (picksRemaining <= 0)
        {
            EnterWaiting();
            yield break;
        }

        // 다음 픽: 새 3장을 하나씩 펼쳐 보여준다.
        RollNewChoices(picksRemaining);
        int order = 0;
        foreach (AugmentChoiceSlot slot in ActiveSlots())
        {
            slot.PlayReveal(revealStagger * order);
            order++;
        }
        PlaySound(revealClip);

        yield return WaitUnscaled(revealStagger * order + 0.25f);
        SetSlotsInteractable(true);
        state = State.Ready;
    }

    private void OnReroll(AugmentChoiceSlot slot)
    {
        if (state != State.Ready || slot.IsAnimating)
            return;

        CharacterBase local = CharacterBase.LocalPlayer;
        if (local == null || local.AugmentPicksRemaining <= 0)
            return;
        if (!slot.TryConsumeReroll())
            return;

        int index = Array.IndexOf(slots, slot);
        if (index < 0)
            return;

        PlaySound(rerollClip);
        slot.PlayFlipSwap(() => AssignRandom(index, resetReroll: false));
    }

    // 제한시간 초과: 남은 픽을 맨 앞 카드로 즉시 지급한다(연출 없음). 픽이 2개 남은 경우 새로 굴려서 이어 채운다.
    private void FlushRemainingPicks(CharacterBase local)
    {
        if (local == null)
            return;

        if (pickRoutine != null)
        {
            StopCoroutine(pickRoutine);
            pickRoutine = null;
        }

        int guard = 4;
        while (local.AugmentPicksRemaining > 0 && guard-- > 0)
        {
            if (local.AugmentPicksRemaining != rolledForPicksRemaining)
                RollNewChoices(local.AugmentPicksRemaining);

            AugmentChoiceSlot first = null;
            foreach (AugmentChoiceSlot slot in slots)
            {
                if (slot.gameObject.activeSelf)
                {
                    first = slot;
                    break;
                }
            }

            if (first == null)
                break;

            int before = local.AugmentPicksRemaining;
            local.GrantAugment(first.CurrentType);
            if (local.AugmentPicksRemaining == before)
                break; // 지급이 즉시 반영되지 않으면 다음 프레임에 다시 시도한다.
        }

        if (local.AugmentPicksRemaining <= 0)
            EnterWaiting();
    }

    // ───────────────────────── 카드 배정 ─────────────────────────

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

        List<AugmentPoolEntry> notShown = pool.FindAll(entry =>
            entry.Data != null &&
            !shownTypesThisPick.Contains(entry.Data.type) &&
            (local == null || local.GetAugmentStack(entry.Data.type) < Mathf.Max(1, entry.Data.maxStack)));

        List<AugmentPoolEntry> candidates;
        if (isReroll)
        {
            AugmentPackData ownPack = slot.gameObject.activeSelf ? slot.CurrentEntry.SourcePack : null;
            candidates = notShown.FindAll(entry => entry.SourcePack == null || entry.SourcePack != ownPack);
            if (candidates.Count == 0)
                candidates = notShown;
        }
        else
        {
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

    // 애니메이터가 첫 등장 중 슬롯을 전부 켜므로, 끝난 뒤 실제로 내용이 있는 슬롯만 남긴다.
    private void ApplySlotContentVisibility()
    {
        foreach (AugmentChoiceSlot slot in slots)
        {
            bool hasContent = slot.CurrentEntry.Data != null && shownTypesThisPick.Contains(slot.CurrentType);
            if (slot.gameObject.activeSelf != hasContent)
                slot.SetSlotActive(hasContent);
        }
    }

    // ───────────────────────── 표시 ─────────────────────────

    private void UpdateTimer(float total, float remaining)
    {
        bool urgent = remaining > 0f && remaining <= urgentSeconds;
        float pulse = urgent ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12f) : 0f;

        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = total > 0f ? remaining / total : 0f;
            timerFillImage.color = urgent ? Color.Lerp(timerBaseColor, urgentColor, 0.6f + 0.4f * pulse) : timerBaseColor;
        }

        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(remaining).ToString();
            timerText.color = urgent ? Color.Lerp(Color.white, urgentColor, 0.6f + 0.4f * pulse) : Color.white;
        }
    }

    private void UpdateTitle(int picksRemaining)
    {
        if (titleText == null)
            return;

        if (state == State.Waiting || picksAtStart <= 1)
        {
            titleText.text = titleFormat;
            return;
        }

        int current = Mathf.Clamp(picksAtStart - picksRemaining + (state == State.PickAnimating ? 0 : 1), 1, picksAtStart);
        titleText.text = $"{titleFormat} ({current}/{picksAtStart})";
    }

    private float ResolveIntroDuration()
    {
        if (introDurationOverride > 0f)
            return introDurationOverride;

        float length = 0f;
        if (panelAnimator != null && panelAnimator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in panelAnimator.runtimeAnimatorController.animationClips)
                length = Mathf.Max(length, clip.length);
        }

        return length > 0f ? length + 0.05f : 0.3f;
    }

    // ───────────────────────── 유틸 ─────────────────────────

    private IEnumerable<AugmentChoiceSlot> ActiveSlots()
    {
        if (slots == null)
            yield break;
        foreach (AugmentChoiceSlot slot in slots)
        {
            if (slot != null && slot.gameObject.activeSelf)
                yield return slot;
        }
    }

    private void SetSlotsInteractable(bool value)
    {
        if (slots == null)
            return;
        foreach (AugmentChoiceSlot slot in slots)
        {
            if (slot != null)
                slot.SetInteractable(value);
        }
    }

    private void SetWaiting(bool waiting)
    {
        if (waitingText != null)
        {
            waitingText.text = waitingMessage;
            waitingText.gameObject.SetActive(waiting);
        }
    }

    private void SetPanelActive(bool active)
    {
        if (panel != null && panel.activeSelf != active)
            panel.SetActive(active);
    }

    private static IEnumerator WaitUnscaled(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
            SoundManager.Instance?.PlaySfx(clip, volume);
    }
}
