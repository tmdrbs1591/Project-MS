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

    private MatchPhase lastPhase = MatchPhase.PackSelect;
    private bool hasPlayed;

    private void Awake()
    {
        SetVfxActive(false);
    }

    private void Update()
    {
        if (hasPlayed)
            return;

        MatchManager match = MatchManager.Instance;
        if (match == null)
            return;

        if (lastPhase == MatchPhase.PackSelect && match.Phase == MatchPhase.Fighting)
        {
            SetVfxActive(true);
            hasPlayed = true;
        }

        lastPhase = match.Phase;
    }

    private void SetVfxActive(bool active)
    {
        if (battleStartVfx != null)
            battleStartVfx.SetActive(active);

        if (battleStartVfxVs != null)
            battleStartVfxVs.SetActive(active);
    }
}
