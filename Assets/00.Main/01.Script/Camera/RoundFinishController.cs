using System.Collections;
using ProjectMS.CharacterSystem;
using UnityEngine;

/// <summary>
/// 라운드가 끝나는(=누군가 죽는) 순간의 "피니시" 연출: 히트스톱 → 슬로우모션 → 정상 속도 복귀,
/// 그리고 그 동안 카메라를 죽은 캐릭터로 줌인/포커스한다.
///
/// [트리거]
///   - MatchManager.Phase 가 Fighting → RoundEnd 로 바뀌는 걸 매 프레임 감지한다([Networked]라
///     모든 클라에서 동일하게 관찰됨). RoundEnd 에 들어온 뒤에도 죽은 캐릭터(CharacterBase.IsDead)가
///     아직 안 보이면(NetDead 동기화가 Phase 보다 한두 프레임 늦게 도착하는 경우) 매 프레임
///     다시 찾아본다 — RoundEnd 인 동안은 계속 재시도하다가 찾으면 그때 1회 재생한다.
///
/// [로컬 전용 연출]
///   - Time.timeScale 을 직접 건드리지만 이건 각 클라이언트의 로컬 렌더링/애니메이션에만
///     영향을 준다. 라운드 타이머(MatchManager.PhaseTimer)는 Fusion 네트워크 틱 기반이라
///     Time.timeScale 과 무관하게 흐르므로, 이 연출 때문에 두 클라의 라운드 진행이
///     어긋나지는 않는다.
///   - 각 클라가 자기 화면에서 독립적으로 재생한다(동기화 불필요 — BattleStartVfxController와 동일 패턴).
///
/// [씬 설정]
///   - TwoPlayerCamera 와 같은 오브젝트(Main Camera)에 붙인다.
/// </summary>
[RequireComponent(typeof(TwoPlayerCamera))]
public class RoundFinishController : MonoBehaviour
{
    public static RoundFinishController Instance { get; private set; }

    /// <summary>지금 히트스톱/슬로우모션/줌 연출이 재생 중인지. MatchResultUI가 이 값을 보고
    /// 연출이 끝날 때까지 결과 배너를 띄우는 걸 미룬다.</summary>
    public bool IsPlaying => finishRoutine != null;

    [Header("히트스톱")]
    [Tooltip("완전 정지(타임스케일 0) 유지 시간(실시간 초).")]
    [SerializeField] private float hitStopDuration = 0.08f;

    [Header("슬로우모션")]
    [Tooltip("히트스톱 직후의 타임스케일.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float slowMotionScale = 0.2f;
    [Tooltip("슬로우모션 유지 시간(실시간 초).")]
    [SerializeField] private float slowMotionDuration = 1f;
    [Tooltip("슬로우모션에서 정상 속도(1)로 되돌아오는 데 걸리는 시간(실시간 초).")]
    [SerializeField] private float recoverDuration = 0.3f;

    [Header("카메라 포커스")]
    [Tooltip("죽은 캐릭터를 비출 orthographicSize (작을수록 확대).")]
    [SerializeField] private float focusZoomSize = 4f;

    [Header("사운드")]
    [Tooltip("히트스톱이 걸리는 순간 같이 재생할 효과음. 비워두면 소리 없이 연출만 재생된다.")]
    [SerializeField] private AudioClip koClip;

    [Header("카메라 셰이크")]
    [Tooltip("KO 순간 화면을 흔드는 세기(월드 단위). 0이면 흔들지 않는다.")]
    [SerializeField] private float koShakeMagnitude = 0.3f;
    [SerializeField] private float koShakeDuration = 0.25f;

    private TwoPlayerCamera twoPlayerCamera;
    private MatchPhase lastObservedPhase = MatchPhase.Fighting;
    private bool playedThisRoundEnd;
    private Coroutine finishRoutine;

    private void Awake()
    {
        Instance = this;
        twoPlayerCamera = GetComponent<TwoPlayerCamera>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        MatchManager match = MatchManager.Instance;
        MatchPhase phase = match != null ? match.Phase : MatchPhase.Fighting;

        if (phase != MatchPhase.RoundEnd)
        {
            // RoundEnd 를 벗어났는데 연출이 아직 끝나지 않았다면(제한시간을 아주 짧게 설정한
            // 경우 등) 다음 라운드/페이즈로 밀려 들어가지 않게 즉시 정리한다.
            if (finishRoutine != null)
                StopFinishRoutine();

            playedThisRoundEnd = false;
            lastObservedPhase = phase;
            return;
        }

        if (!playedThisRoundEnd)
        {
            CharacterBase loser = FindDeadCharacter();
            if (loser != null)
            {
                playedThisRoundEnd = true;
                finishRoutine = StartCoroutine(FinishRoutine(loser.transform));
            }
        }

        lastObservedPhase = phase;
    }

    private void OnDisable()
    {
        if (finishRoutine != null)
            StopFinishRoutine();
    }

    private static CharacterBase FindDeadCharacter()
    {
        foreach (CharacterBase character in CharacterBase.All)
        {
            if (character != null && character.IsDead)
                return character;
        }

        return null;
    }

    private IEnumerator FinishRoutine(Transform loserTransform)
    {
        twoPlayerCamera.SetFocusOverride(loserTransform, focusZoomSize);
        SoundManager.Instance?.PlaySfx(koClip);

        if (koShakeMagnitude > 0f)
            twoPlayerCamera.Shake(koShakeMagnitude, koShakeDuration);

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopDuration);

        Time.timeScale = slowMotionScale;
        yield return new WaitForSecondsRealtime(slowMotionDuration);

        float elapsed = 0f;
        while (elapsed < recoverDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(slowMotionScale, 1f, elapsed / recoverDuration);
            yield return null;
        }

        Time.timeScale = 1f;
        twoPlayerCamera.ClearFocusOverride();
        finishRoutine = null;
    }

    /// <summary>연출을 중단하고 타임스케일/카메라 포커스를 안전하게 원상복구한다.</summary>
    private void StopFinishRoutine()
    {
        StopCoroutine(finishRoutine);
        finishRoutine = null;
        Time.timeScale = 1f;
        twoPlayerCamera.ClearFocusOverride();
    }
}
