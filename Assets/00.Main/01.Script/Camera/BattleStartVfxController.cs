using UnityEngine;

/// <summary>
/// "전투 시작" 연출(Vfx_UI_BattleStart / Vfx_UI_BattleStart_Vs)의 재생 시점을 제어한다.
/// 원래 두 VFX는 씬에 항상 켜진 채(Play On Awake)로 배치되어 있어 씬 로드 즉시
/// (팩 선택 페이즈 시작과 동시에) 재생됐다. 이를 팩 선택이 끝나고 실제 전투(Fighting)가
/// 시작되는 시점으로 미루기 위한 스크립트다.
///
/// [씬 설정]
///   - Main Camera에 이 스크립트를 붙인다.
///   - battleStartVfx / battleStartVfxVs: Main Camera 자식의 Vfx_UI_BattleStart, Vfx_UI_BattleStart_Vs
///     오브젝트를 그대로 연결한다(둘 다 씬에서 활성 상태로 둬도 된다 — Awake에서 꺼버린다).
///   - MatchManager.Phase는 [Networked]라 모든 클라에서 동일하게 관찰되므로, 폴링으로
///     PackSelect → Fighting 전환(=매치 최초 전투 시작)을 감지해 한 번만 재생한다.
/// </summary>
public class BattleStartVfxController : MonoBehaviour
{
    [SerializeField] private GameObject battleStartVfx;
    [SerializeField] private GameObject battleStartVfxVs;
    [Tooltip("VFX와 같이 재생할 효과음. 비워두면 소리 없이 VFX만 재생된다.")]
    [SerializeField] private AudioClip battleStartClip;

    private MatchPhase lastPhase = MatchPhase.PackSelect;
    private bool hasPlayed;
    private int playedMatchNumber;

    private void Awake()
    {
        SetVfxActive(false);
    }

    private void Update()
    {
        MatchManager match = MatchManager.Instance;
        if (match == null)
            return;

        if (hasPlayed)
        {
            // 재대결로 새 매치가 시작되면(MatchNumber 증가) 시작 연출을 처음부터 다시 튼다.
            if (match.MatchNumber != playedMatchNumber && match.Phase == MatchPhase.Fighting)
                Play(match);
            return;
        }

        if (lastPhase == MatchPhase.PackSelect && match.Phase == MatchPhase.Fighting)
        {
            // 연습장은 VS 연출 없이 바로 시작한다.
            if (PracticeSession.IsActive)
            {
                hasPlayed = true;
                playedMatchNumber = match.MatchNumber;
            }
            else
                Play(match);
        }

        lastPhase = match.Phase;
    }

    private void Play(MatchManager match)
    {
        // 이미 켜져 있던 오브젝트도 OnEnable(VsIntroUI 등)이 다시 돌도록 껐다 켠다.
        SetVfxActive(false);
        SetVfxActive(true);
        SoundManager.Instance?.PlaySfx(battleStartClip);
        hasPlayed = true;
        playedMatchNumber = match.MatchNumber;
    }

    private void SetVfxActive(bool active)
    {
        if (battleStartVfx != null)
            battleStartVfx.SetActive(active);

        if (battleStartVfxVs != null)
            battleStartVfxVs.SetActive(active);
    }
}
