using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// 3판 2선승 매치의 승패를 관리하는 네트워크 매니저다. (Fusion 2 / Shared 모드)
///
/// [권한]
///   - 게임 씬 로드 후 Shared 모드 마스터 클라이언트가 한 번 스폰한다(PlayerSpawner 참고).
///     스폰한 클라가 이 오브젝트의 StateAuthority 가 되며, 라운드/점수 판정은 그 클라에서만 수행한다.
///   - 판정에 필요한 "누가 죽었는가"는 CharacterBase.Health.IsDead(=[Networked] 체력)를
///     직접 읽어서 확인한다. 체력은 이미 모든 클라에 동기화되어 있으므로 별도의 사망 보고
///     RPC 없이 StateAuthority 가 매 틱 폴링하는 것만으로 충분하다.
///
/// [두 캐릭터 구분]
///   - PlayerRef 를 따로 저장하지 않고, CharacterBase.All 을 PlayerId 오름차순으로 정렬해
///     "Player1 / Player2" 를 그때그때 다시 구한다. 정렬 기준(PlayerId)이 모든 클라에서 동일하므로
///     같은 결과가 나온다.
///
/// [씬 설정]
///   - NetworkObject 가 붙은 빈 프리팹을 만들고 이 스크립트를 붙인다.
///   - PlayerSpawner 의 Match Manager Prefab 필드에 연결한다.
/// </summary>
public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }

    public const int WinsRequired = 2;

    [Header("타이밍")]
    [Tooltip("매치 시작 직후 증강 팩 선택 제한시간.")]
    [SerializeField] private float packSelectDuration = 45f;
    [SerializeField] private float roundEndDisplayDuration = 3f;
    [Tooltip("증강 선택 최대 대기시간. 양쪽 다 고르면 이 시간이 남았어도 즉시 다음 라운드로 넘어간다. " +
             "한쪽이 고르지 않고 버틸 때(자리비움/연결끊김 등)의 안전장치용 상한선일 뿐이다.")]
    [SerializeField] private float augmentSelectDuration = 5f;
    [Tooltip("라운드 리셋 직후 사망 판정을 잠깐 쉬는 시간. 상대 클라의 부활 RPC가 도착하기 전에 " +
             "'아직 죽어있음'으로 오판해 곧바로 다음 라운드가 끝나버리는 것을 막는다.")]
    [SerializeField] private float roundStartGraceDuration = 1f;

    [Networked] public MatchPhase Phase { get; private set; }
    [Networked] public int Player1Wins { get; private set; }
    [Networked] public int Player2Wins { get; private set; }
    [Networked] public int RoundNumber { get; private set; }
    [Networked] public PlayerRef LastRoundWinner { get; private set; }
    [Networked] private TickTimer PhaseTimer { get; set; }

    public int TotalWins => Player1Wins + Player2Wins;

    private PlayerSpawner playerSpawner;

    public override void Spawned()
    {
        Instance = this;
        playerSpawner = FindObjectOfType<PlayerSpawner>();

        if (Object.HasStateAuthority)
        {
            Phase = MatchPhase.PackSelect;
            Player1Wins = 0;
            Player2Wins = 0;
            RoundNumber = 1;
            PhaseTimer = TickTimer.CreateFromSeconds(Runner, packSelectDuration);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
            Instance = null;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        switch (Phase)
        {
            case MatchPhase.PackSelect:
                TickPackSelect();
                break;

            case MatchPhase.Fighting:
                TickFighting();
                break;

            case MatchPhase.RoundEnd:
                if (PhaseTimer.Expired(Runner))
                    OnRoundEndExpired();
                break;

            case MatchPhase.AugmentSelect:
                TickAugmentSelect();
                break;

            case MatchPhase.MatchEnd:
                // 결과 화면을 띄운 채 대기. 로비 복귀 등은 UI 쪽에서 별도로 처리한다.
                break;
        }
    }

    /// <summary>내 승수/상대 승수 표시용. player 가 어느 쪽인지 몰라도 되게 PlayerRef 로 조회한다.</summary>
    public int GetWinsFor(PlayerRef player)
    {
        if (!TryGetOrderedCharacters(out CharacterBase p1, out CharacterBase p2))
            return 0;

        if (p1.Object.InputAuthority == player)
            return Player1Wins;
        if (p2.Object.InputAuthority == player)
            return Player2Wins;
        return 0;
    }

    /// <summary>player 가 Player1(스폰 기준 왼쪽)인지 여부. UI에서 좌우 표시 순서를 맞추는 데 쓴다.</summary>
    public bool IsPlayer1(PlayerRef player)
    {
        if (!TryGetOrderedCharacters(out CharacterBase p1, out _))
            return true;

        return p1.Object.InputAuthority == player;
    }

    /// <summary>현재 페이즈(PackSelect/AugmentSelect 등)의 남은 시간(초). 선택 UI 타이머 표시용 공용 접근자.</summary>
    public float GetPhaseTimeRemaining() => PhaseTimer.RemainingTime(Runner) ?? 0f;

    /// <summary>현재 페이즈의 전체 제한시간(초). Fill Image 타이머처럼 비율(남은시간/전체시간)이
    /// 필요한 UI에서 GetPhaseTimeRemaining()과 함께 쓴다. 해당 없는 페이즈면 0.</summary>
    public float GetPhaseTotalDuration()
    {
        switch (Phase)
        {
            case MatchPhase.PackSelect:
                return packSelectDuration;
            case MatchPhase.AugmentSelect:
                return augmentSelectDuration;
            default:
                return 0f;
        }
    }

    /// <summary>양쪽 다 팩 선택을 확정하면 즉시 전투로 넘어간다. packSelectDuration 은 상대가
    /// 고르지 않고 버티는 경우(자리비움/연결끊김 등)를 위한 최대 대기시간이며,
    /// 시간 초과 시 아직 안 고른 쪽은 해금된 팩 중 무작위로 자동 확정된다.</summary>
    private void TickPackSelect()
    {
        if (!TryGetOrderedCharacters(out CharacterBase p1, out CharacterBase p2))
            return;

        bool bothFinalized = p1.HasFinalizedPackSelection && p2.HasFinalizedPackSelection;
        bool timedOut = PhaseTimer.Expired(Runner);

        if (!bothFinalized && !timedOut)
            return;

        if (timedOut)
        {
            if (!p1.HasFinalizedPackSelection)
                p1.AutoFinalizePackSelection();
            if (!p2.HasFinalizedPackSelection)
                p2.AutoFinalizePackSelection();
        }

        Debug.Log($"[MatchManager] PackSelect → Fighting 전환 (timedOut={timedOut})");
        Phase = MatchPhase.Fighting;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, roundStartGraceDuration);
    }

    private void TickFighting()
    {
        // 라운드 리셋 직후 잠깐은 사망 판정을 쉰다(리셋 RPC가 아직 상대 클라에 도착 전일 수 있음).
        if (!PhaseTimer.ExpiredOrNotRunning(Runner))
            return;

        if (!TryGetOrderedCharacters(out CharacterBase p1, out CharacterBase p2))
            return;

        // 동시 사망(더블 KO)은 드문 경우라 p1 을 패자로 처리한다(무승부 처리는 하지 않음).
        CharacterBase loser = p1.Health.IsDead ? p1 : (p2.Health.IsDead ? p2 : null);
        if (loser == null)
            return;

        CharacterBase winner = loser == p1 ? p2 : p1;

        if (winner == p1)
            Player1Wins++;
        else
            Player2Wins++;

        LastRoundWinner = winner.Object.InputAuthority;
        Phase = MatchPhase.RoundEnd;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, roundEndDisplayDuration);
    }

    private void OnRoundEndExpired()
    {
        if (Player1Wins >= WinsRequired || Player2Wins >= WinsRequired)
        {
            Phase = MatchPhase.MatchEnd;
            return;
        }

        Phase = MatchPhase.AugmentSelect;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, augmentSelectDuration);
    }

    /// <summary>양쪽 다 증강을 고르면 즉시 다음 라운드로 넘어간다. augmentSelectDuration 은
    /// 상대가 고르지 않고 버티는 경우(자리비움/연결끊김 등)를 위한 최대 대기시간일 뿐이다.</summary>
    private void TickAugmentSelect()
    {
        if (!TryGetOrderedCharacters(out CharacterBase p1, out CharacterBase p2))
            return;

        bool bothPicked = p1.HasPickedAugmentForRound(RoundNumber) && p2.HasPickedAugmentForRound(RoundNumber);
        bool timedOut = PhaseTimer.Expired(Runner);

        if (bothPicked || timedOut)
            OnAugmentSelectExpired();
    }

    private void OnAugmentSelectExpired()
    {
        ResetRoundCharacters();
        RoundNumber++;
        Phase = MatchPhase.Fighting;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, roundStartGraceDuration);
    }

    private void ResetRoundCharacters()
    {
        if (!TryGetOrderedCharacters(out CharacterBase p1, out CharacterBase p2))
            return;

        p1.RequestRoundReset(GetSpawnPosition(p1));
        p2.RequestRoundReset(GetSpawnPosition(p2));
    }

    private Vector3 GetSpawnPosition(CharacterBase character)
    {
        if (playerSpawner != null)
            return playerSpawner.GetSpawnPosition(character.Object.InputAuthority.PlayerId);

        return character.transform.position;
    }

    private static bool TryGetOrderedCharacters(out CharacterBase p1, out CharacterBase p2)
    {
        p1 = null;
        p2 = null;

        if (CharacterBase.All.Count < 2)
            return false;

        List<CharacterBase> ordered = new List<CharacterBase>(CharacterBase.All);
        ordered.Sort((a, b) => a.Object.InputAuthority.PlayerId.CompareTo(b.Object.InputAuthority.PlayerId));
        p1 = ordered[0];
        p2 = ordered[1];
        return true;
    }
}
