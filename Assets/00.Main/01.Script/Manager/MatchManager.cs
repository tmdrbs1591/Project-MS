using System.Collections.Generic;
using Fusion;
using ProjectMS.CharacterSystem;
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

    /// <summary>라운드 승자가 이번 AugmentSelect 구간에 고를 수 있는 증강 개수.</summary>
    private const int WinnerAugmentPicks = 1;

    /// <summary>라운드 패자가 이번 AugmentSelect 구간에 고를 수 있는 증강 개수.</summary>
    private const int LoserAugmentPicks = 2;

    [Header("타이밍")]
    [Tooltip("매치 시작 직후 증강 팩 선택 제한시간.")]
    [SerializeField] private float packSelectDuration = 45f;
    [SerializeField] private float roundEndDisplayDuration = 3f;
    [Tooltip("증강 선택 최대 대기시간. 양쪽 다 고르면 이 시간이 남았어도 즉시 다음 라운드로 넘어간다. " +
             "한쪽이 고르지 않고 버틸 때(자리비움/연결끊김 등)의 안전장치용 상한선일 뿐이다.")]
    [SerializeField] private float augmentSelectDuration = 20f;
    [Tooltip("AugmentSelect 진입 직후 짧게 쉬는 시간. Rpc_BeginAugmentSelect가 상대 클라에 " +
             "도착하기 전에 '둘 다 이미 픽을 다 씀(0개 남음)'으로 오판해 곧바로 다음 라운드로 " +
             "넘어가버리는 것을 막는다.")]
    [SerializeField] private float augmentSelectGraceDuration = 1f;
    [Tooltip("라운드 리셋 직후 사망 판정을 잠깐 쉬는 시간. 상대 클라의 부활 RPC가 도착하기 전에 " +
             "'아직 죽어있음'으로 오판해 곧바로 다음 라운드가 끝나버리는 것을 막는다.")]
    [SerializeField] private float roundStartGraceDuration = 1f;

    [Networked] public MatchPhase Phase { get; private set; }
    [Networked] public int Player1Wins { get; private set; }
    [Networked] public int Player2Wins { get; private set; }
    [Networked] public int RoundNumber { get; private set; }
    [Networked] public PlayerRef LastRoundWinner { get; private set; }
    [Networked] private TickTimer PhaseTimer { get; set; }
    /// <summary>재대결할 때마다 1씩 오른다. 시작 연출(VS, ME 마커)이 새 매치를 알아채는 데 쓴다.</summary>
    [Networked] public int MatchNumber { get; private set; }
    [Networked] public NetworkBool Player1WantsRematch { get; private set; }
    [Networked] public NetworkBool Player2WantsRematch { get; private set; }

    public int TotalWins => Player1Wins + Player2Wins;

    private PlayerSpawner playerSpawner;

    public override void Spawned()
    {
        Instance = this;
        playerSpawner = FindObjectOfType<PlayerSpawner>();

        if (Object.HasStateAuthority)
        {
            Player1Wins = 0;
            Player2Wins = 0;
            RoundNumber = 1;
            MatchNumber = 1;
            Phase = MatchPhase.Fighting;
            PhaseTimer = TickTimer.CreateFromSeconds(Runner, roundStartGraceDuration);
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
                // 결과 화면을 띄운 채 대기. 양쪽이 재대결을 원하면 새 매치를 시작한다.
                // 로비 복귀는 UI 쪽에서 별도로 처리한다.
                TickMatchEnd();
                break;
        }
    }

    /// <summary>내 승수/상대 승수 표시용. player 가 어느 쪽인지 몰라도 되게 PlayerRef 로 조회한다.</summary>
    public int GetWinsFor(PlayerRef player)
    {
        if (!TryGetOrderedCharacters(out CharacterBase p1, out CharacterBase p2))
            return 0;

        if (p1.MatchPlayer == player)
            return Player1Wins;
        if (p2.MatchPlayer == player)
            return Player2Wins;
        return 0;
    }

    /// <summary>player 가 재대결을 요청했는지. AI 봇은 항상 원한다.</summary>
    public bool WantsRematch(PlayerRef player)
    {
        if (!TryGetOrderedCharacters(out CharacterBase p1, out CharacterBase p2))
            return false;

        if (p1.MatchPlayer == player)
            return p1.IsBot || Player1WantsRematch;
        if (p2.MatchPlayer == player)
            return p2.IsBot || Player2WantsRematch;
        return false;
    }

    /// <summary>결과 화면의 재대결 버튼에서 호출. 내 요청을 매치 매니저 권한자에게 보낸다.</summary>
    public void RequestRematch()
    {
        if (Object == null || Phase != MatchPhase.MatchEnd)
            return;

        Rpc_RequestRematch();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_RequestRematch(RpcInfo info = default)
    {
        if (Phase != MatchPhase.MatchEnd || !TryGetOrderedCharacters(out CharacterBase p1, out CharacterBase p2))
            return;

        // 보낸 사람 본인의 표만 받는다(오프라인 AI 전에서는 로컬 호출이라 Source가 비어 있을 수 있음).
        PlayerRef sender = info.Source == PlayerRef.None ? Runner.LocalPlayer : info.Source;
        if (p1.MatchPlayer == sender)
            Player1WantsRematch = true;
        else if (p2.MatchPlayer == sender)
            Player2WantsRematch = true;
    }

    private void TickMatchEnd()
    {
        if (!TryGetOrderedCharacters(out CharacterBase p1, out CharacterBase p2))
            return;

        if (WantsRematch(p1.MatchPlayer) && WantsRematch(p2.MatchPlayer))
            StartRematch(p1, p2);
    }

    private void StartRematch(CharacterBase p1, CharacterBase p2)
    {
        // 라운드 리셋과 같은 순서: 1라운드 맵을 먼저 준비해야 그 맵의 스폰 위치를 구할 수 있다.
        MapManager.Instance?.PrepareMapForRound(Runner, 1);

        Rpc_BroadcastMatchReset(p1.MatchPlayer, GetSpawnPosition(p1), p2.MatchPlayer, GetSpawnPosition(p2));

        Player1Wins = 0;
        Player2Wins = 0;
        RoundNumber = 1;
        LastRoundWinner = PlayerRef.None;
        Player1WantsRematch = false;
        Player2WantsRematch = false;
        MatchNumber++;
        Phase = MatchPhase.Fighting;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, roundStartGraceDuration);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_BroadcastMatchReset(PlayerRef owner1, Vector2 pos1, PlayerRef owner2, Vector2 pos2)
    {
        foreach (CharacterBase ch in CharacterBase.All)
        {
            if (!ch.Object.HasStateAuthority)
                continue;

            if (ch.MatchPlayer == owner1)
                ch.ResetForNewMatch(pos1);
            else if (ch.MatchPlayer == owner2)
                ch.ResetForNewMatch(pos2);
        }
    }

    /// <summary>player 가 Player1(스폰 기준 왼쪽)인지 여부. UI에서 좌우 표시 순서를 맞추는 데 쓴다.</summary>
    public bool IsPlayer1(PlayerRef player)
    {
        if (!TryGetOrderedCharacters(out CharacterBase p1, out _))
            return true;

        return p1.MatchPlayer == player;
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

    // 카드팩 기능 비활성화 — CharacterBase 관련 코드 제거됨
    private void TickPackSelect() { }

    private void TickFighting()
    {
        if (PracticeSession.IsActive)
        {
            TickPractice();
            return;
        }

        if (waitingForPracticeOpponent)
        {
            // 사라지는 중인 연습 봇을 상대로 착각하지 않게 사람 캐릭터만 센다.
            int humans = 0;
            foreach (CharacterBase ch in CharacterBase.All)
            {
                if (ch != null && ch.Object != null && !ch.IsBot)
                    humans++;
            }
            if (humans < 2)
                return;

            // 상대가 들어왔다: 새 매치로 시작(MatchNumber가 오르면 양쪽에서 시작 연출이 나온다).
            waitingForPracticeOpponent = false;
            MatchNumber++;
            PhaseTimer = TickTimer.CreateFromSeconds(Runner, roundStartGraceDuration);
            return;
        }

        // 라운드 리셋 직후 잠깐은 사망 판정을 쉰다(리셋 RPC가 아직 상대 클라에 도착 전일 수 있음).
        if (!PhaseTimer.ExpiredOrNotRunning(Runner))
            return;

        if (!TryGetOrderedCharacters(out CharacterBase p1, out CharacterBase p2))
            return;

        // 동시 사망(더블 KO)은 드문 경우라 p1 을 패자로 처리한다(무승부 처리는 하지 않음).
        CharacterBase loser = p1.IsDead ? p1 : (p2.IsDead ? p2 : null);
        if (loser == null)
            return;

        CharacterBase winner = loser == p1 ? p2 : p1;

        if (winner == p1)
            Player1Wins++;
        else
            Player2Wins++;

        LastRoundWinner = winner.MatchPlayer;
        Phase = MatchPhase.RoundEnd;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, roundEndDisplayDuration);
    }

    // 연습장: 라운드/매치가 끝나지 않는다. 쓰러진 캐릭터는 잠시 뒤 제자리(스폰 위치)에서 다시 일어난다.
    // 연습장은 오프라인(Single)이라 모든 캐릭터의 권한이 여기 있으므로 RPC 없이 바로 리셋한다.
    private readonly Dictionary<CharacterBase, float> practiceDeadSince = new Dictionary<CharacterBase, float>();
    // 매칭하면서 연습하던 방장이 대전으로 바뀐 직후, 상대 캐릭터가 들어올 때까지 기다리는 중인지(권한자 전용).
    private bool waitingForPracticeOpponent;

    /// <summary>연습장(매칭 세션 안)에서 기다리던 방장 쪽에서, 상대가 들어온 순간 호출(NetworkLauncher).
    /// 매치 상태와 내 캐릭터를 처음으로 되돌리고, 상대 캐릭터가 나타나면 새 매치를 시작한다.</summary>
    public void BeginMatchFromPractice()
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        practiceDeadSince.Clear();
        Player1Wins = 0;
        Player2Wins = 0;
        RoundNumber = 1;
        LastRoundWinner = PlayerRef.None;
        Player1WantsRematch = false;
        Player2WantsRematch = false;
        Phase = MatchPhase.Fighting;
        waitingForPracticeOpponent = true;

        foreach (CharacterBase ch in CharacterBase.All)
        {
            if (ch != null && ch.Object != null && ch.Object.HasStateAuthority && !ch.IsBot)
                ch.ResetForNewMatch(GetSpawnPosition(ch));
        }
    }
    private const float PracticeRespawnDelay = 1.2f;

    private void TickPractice()
    {
        foreach (CharacterBase ch in CharacterBase.All)
        {
            if (ch == null || ch.Object == null || !ch.Object.HasStateAuthority)
                continue;

            if (!ch.IsDead)
            {
                practiceDeadSince.Remove(ch);
                continue;
            }

            if (!practiceDeadSince.TryGetValue(ch, out float since))
            {
                practiceDeadSince[ch] = Runner.SimulationTime;
                continue;
            }

            if (Runner.SimulationTime - since >= PracticeRespawnDelay)
            {
                practiceDeadSince.Remove(ch);
                ch.ResetCharacter(GetSpawnPosition(ch));
            }
        }
    }

    private void OnRoundEndExpired()
    {
        if (Player1Wins >= WinsRequired || Player2Wins >= WinsRequired)
        {
            Phase = MatchPhase.MatchEnd;
            return;
        }

        // PackSelect 플로우는 아직 임시 비활성화 상태 — AugmentSelect로 바로 진입한다.
        BeginAugmentSelect();
    }

    private void BeginAugmentSelect()
    {
        if (!TryGetOrderedCharacters(out CharacterBase p1, out CharacterBase p2))
            return;

        PlayerRef winner = LastRoundWinner;
        PlayerRef loser = p1.MatchPlayer == winner
            ? p2.MatchPlayer
            : p1.MatchPlayer;

        Rpc_BeginAugmentSelect(winner, loser);

        Phase = MatchPhase.AugmentSelect;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, augmentSelectDuration);
    }

    private void TickAugmentSelect()
    {
        if (!TryGetOrderedCharacters(out CharacterBase p1, out CharacterBase p2))
            return;

        // Rpc_BeginAugmentSelect가 상대 클라에 도착하기 전에는 픽 배정이 아직 반영 안 됐을 수
        // 있으므로(둘 다 0개 남음으로 보임), 그레이스 타임 동안은 "둘 다 다 골랐음" 판정을 쉰다.
        float elapsed = augmentSelectDuration - GetPhaseTimeRemaining();
        bool pastGrace = elapsed >= augmentSelectGraceDuration;
        bool bothDone = p1.AugmentPicksRemaining <= 0 && p2.AugmentPicksRemaining <= 0;

        if (pastGrace && bothDone)
        {
            AdvanceToNextRound();
            return;
        }

        if (PhaseTimer.Expired(Runner))
        {
            // 시간 초과 — 아직 못 고른 쪽은 무작위 증강으로 강제 완료시킨다.
            Rpc_ForceFinishAugmentPicks();
            AdvanceToNextRound();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_BeginAugmentSelect(PlayerRef winner, PlayerRef loser)
    {
        foreach (CharacterBase ch in CharacterBase.All)
        {
            if (!ch.Object.HasStateAuthority)
                continue;

            if (ch.MatchPlayer == winner)
                ch.SetAugmentPicksRemaining(WinnerAugmentPicks);
            else if (ch.MatchPlayer == loser)
                ch.SetAugmentPicksRemaining(LoserAugmentPicks);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_ForceFinishAugmentPicks()
    {
        foreach (CharacterBase ch in CharacterBase.All)
        {
            if (ch.Object.HasStateAuthority)
                ch.AutoFinishAugmentPicks();
        }
    }

    private void AdvanceToNextRound()
    {
        // 캐릭터를 옮길 스폰 위치는 "다음 라운드 맵"의 스폰 지점이어야 하므로, 위치를 구하기 전에
        // 그 맵부터 준비한다(맵 회전이 꺼져 있으면 아무 것도 안 함).
        MapManager.Instance?.PrepareMapForRound(Runner, RoundNumber + 1);

        ResetRoundCharacters();
        RoundNumber++;
        Phase = MatchPhase.Fighting;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, roundStartGraceDuration);
    }

    private void ResetRoundCharacters()
    {
        if (!TryGetOrderedCharacters(out CharacterBase p1, out CharacterBase p2))
            return;

        Vector2 pos1 = GetSpawnPosition(p1);
        Vector2 pos2 = GetSpawnPosition(p2);

        // StateAuthority → All 브로드캐스트: 각 클라이언트가 자기 캐릭터를 직접 리셋한다.
        // CharacterBase 쪽 RPC(StateAuthority 라우팅)보다 Shared 모드에서 안정적이다.
        Rpc_BroadcastReset(p1.MatchPlayer, pos1, p2.MatchPlayer, pos2);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_BroadcastReset(PlayerRef owner1, Vector2 pos1, PlayerRef owner2, Vector2 pos2)
    {
        foreach (CharacterBase ch in CharacterBase.All)
        {
            if (!ch.Object.HasStateAuthority)
                continue;

            if (ch.MatchPlayer == owner1)
                ch.ResetCharacter(pos1);
            else if (ch.MatchPlayer == owner2)
                ch.ResetCharacter(pos2);
        }
    }

    private Vector2 GetSpawnPosition(CharacterBase character)
    {
        if (playerSpawner != null)
            return playerSpawner.GetSpawnPosition(character.MatchPlayer.PlayerId);

        return character.transform.position;
    }

    private static bool TryGetOrderedCharacters(out CharacterBase p1, out CharacterBase p2)
    {
        p1 = null;
        p2 = null;

        if (CharacterBase.All.Count < 2)
            return false;

        List<CharacterBase> ordered = new List<CharacterBase>(CharacterBase.All);
        ordered.Sort((a, b) => a.MatchPlayer.PlayerId.CompareTo(b.MatchPlayer.PlayerId));
        p1 = ordered[0];
        p2 = ordered[1];
        return true;
    }
}
