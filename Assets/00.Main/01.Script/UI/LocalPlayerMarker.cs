using ProjectMS.CharacterSystem;
using UnityEngine;

/// <summary>
/// 전투가 시작될 때 "내 캐릭터가 어느 쪽인지" 잠깐 알려주는 마커(예: "ME" 글자 + 화살표)를 내
/// 캐릭터 머리 위에 띄우고 따라다니게 한 뒤, 일정 시간이 지나면 서서히 사라지게 한다.
///
/// [동작]
///   - BattleStartVfxController와 같은 방식으로 MatchManager.Phase가 Fighting이 되는 순간
///     (팩 선택이 끝나고 첫 전투 시작 = "전투 시작" 연출과 같은 시점)에 한 번 보여준다.
///     showEveryRound를 켜면 매 라운드 시작 때마다 보여준다.
///   - "내 캐릭터"는 CharacterBase.LocalPlayer(내가 InputAuthority를 가진 캐릭터)라서 상대 화면에는
///     상대 마커가 아니라 각자 자기 캐릭터에만 뜬다. 봇전에서도 사람 쪽 캐릭터에만 뜬다.
///   - 마커 위치는 캐릭터 위치 + worldOffset이고, 캐릭터가 좌우로 뒤집혀도 마커는 뒤집히지 않는다.
///   - 라운드가 끝나거나(Fighting이 아니게 되면) 내 캐릭터가 죽으면 즉시 숨긴다.
///
/// [씬 설정]
///   - 씬에 마커 오브젝트를 만든다(예: World Space Canvas 아래에 "ME" TMP 텍스트 + 화살표 Image,
///     또는 스프라이트/3D TMP). 캐릭터보다 앞에 그려지도록 Sorting Order를 높게 둔다.
///     스프라이트를 쓰면 Global Light 2D가 비추는 정렬 레이어(Default/Object 등)에 둔다.
///   - 이 스크립트를 아무 오브젝트(마커의 부모가 아닌 항상 켜져 있는 오브젝트)에 붙이고
///     markerRoot에 마커 오브젝트를 연결한다. 마커는 씬에서 켜둬도 된다(Awake에서 꺼버린다).
///   - 서서히 사라지게 하려면 마커 루트에 CanvasGroup을 달아 fadeGroup에 연결한다(선택).
/// </summary>
public class LocalPlayerMarker : MonoBehaviour
{
    [Header("Marker")]
    [Tooltip("ME 글자 + 화살표가 들어 있는 마커 루트. 이 스크립트가 붙은 오브젝트와 달라야 한다" +
             "(끄고 켜기 때문에 자기 자신이면 Update가 멈춘다).")]
    [SerializeField] private GameObject markerRoot;
    [Tooltip("서서히 사라지게 할 CanvasGroup(선택). 비워두면 시간이 되면 그냥 꺼진다.")]
    [SerializeField] private CanvasGroup fadeGroup;
    [Tooltip("캐릭터 위치에서 마커까지의 월드 좌표 오프셋. 머리 위로 띄우려면 y를 올린다.")]
    [SerializeField] private Vector2 worldOffset = new Vector2(0f, 1.6f);

    [Header("Start Timing (시작 연출 뒤에 띄우기)")]
    [Tooltip("전투가 시작된 뒤 최소 이 시간(초)이 지나야 마커를 띄운다. 'Fight!' 같은 시작 연출이 " +
             "끝난 뒤에 나오도록 그 연출 길이보다 조금 길게 맞춘다.")]
    [Min(0f)] [SerializeField] private float startDelay = 2.5f;
    [Tooltip("(선택) 시작 연출 오브젝트(예: Vfx_UI_BattleStart, Vfx_UI_BattleStart_Vs). 여기 연결하면 그 안의 " +
             "파티클이 전부 끝날 때까지 기다린다(위 Start Delay는 최소 대기 시간으로 함께 적용).")]
    [SerializeField] private GameObject[] waitForVfx;
    [Tooltip("연출 파티클이 끝나길 기다리는 최대 시간(초). 이 시간이 지나면 연출이 안 끝났어도 띄운다.")]
    [Min(0f)] [SerializeField] private float maxVfxWait = 8f;

    [Header("Timing")]
    [Min(0.1f)] [SerializeField] private float showDuration = 3f;
    [Tooltip("표시 시간의 마지막 이 시간(초) 동안 서서히 투명해진다. 0이면 페이드 없음.")]
    [Min(0f)] [SerializeField] private float fadeDuration = 0.4f;
    [Tooltip("켜면 매 라운드 시작 때마다 다시 보여준다. 끄면 매치의 첫 전투 시작 때 한 번만.")]
    [SerializeField] private bool showEveryRound;

    [Header("Bobbing")]
    [Tooltip("마커가 위아래로 통통 움직이는 폭(월드 단위). 0이면 가만히 있는다.")]
    [SerializeField] private float bobAmplitude = 0.12f;
    [SerializeField] private float bobSpeed = 6f;

    private MatchPhase lastPhase = MatchPhase.PackSelect;
    private bool hasShownOnce;
    private bool pending;   // 전투 시작은 감지했지만 아직 띄울 때가 아닌 상태(시작 연출 대기/내 캐릭터 대기)
    private float pendingElapsed;
    private bool showing;
    private float elapsed;
    private ParticleSystem[] vfxSystems = System.Array.Empty<ParticleSystem>();

    private void Awake()
    {
        SetVisible(false);
        CacheVfxSystems();
    }

    // 시작 연출 오브젝트 안의 파티클을 미리 모아둔다(연출은 꺼진 채로 시작하므로 비활성 포함).
    private void CacheVfxSystems()
    {
        if (waitForVfx == null || waitForVfx.Length == 0)
            return;

        var systems = new System.Collections.Generic.List<ParticleSystem>();
        foreach (GameObject vfx in waitForVfx)
        {
            if (vfx != null)
                systems.AddRange(vfx.GetComponentsInChildren<ParticleSystem>(true));
        }

        vfxSystems = systems.ToArray();
    }

    private bool IsStartVfxPlaying()
    {
        foreach (ParticleSystem system in vfxSystems)
        {
            if (system != null && system.IsAlive(false))
                return true;
        }

        return false;
    }

    private void Update()
    {
        MatchManager match = MatchManager.Instance;
        if (match != null)
        {
            if (lastPhase != MatchPhase.Fighting && match.Phase == MatchPhase.Fighting &&
                (!hasShownOnce || showEveryRound))
            {
                // 전투 시작을 감지한 순간 내 캐릭터가 아직 스폰/등록 전일 수 있다(특히 매치 매니저가
                // 캐릭터보다 먼저 관찰되는 클라이언트). 그때 바로 포기하면 다시는 안 뜨므로,
                // 캐릭터가 나타날 때까지 대기했다가 그 시점부터 시간을 센다.
                hasShownOnce = true;
                pending = true;
                pendingElapsed = 0f;
            }

            // 전투가 끝나면(라운드 종료 등) 대기 중이던 것/남아 있던 마커도 바로 정리한다.
            if (match.Phase != MatchPhase.Fighting)
            {
                pending = false;
                if (showing)
                    End();
            }

            lastPhase = match.Phase;
        }

        if (pending)
        {
            pendingElapsed += Time.unscaledDeltaTime;

            // 시작 연출("Fight!" 등)이 끝난 뒤에 띄운다: 최소 대기 시간이 지났고, 연결한 연출의
            // 파티클이 더 이상 재생 중이 아니며, 내 캐릭터가 존재할 때.
            bool introDone = pendingElapsed >= startDelay &&
                             (!IsStartVfxPlaying() || pendingElapsed >= maxVfxWait);
            if (introDone && CharacterBase.LocalPlayer != null)
            {
                pending = false;
                Begin();
            }
        }

        if (showing)
            Tick();
    }

    private void Begin()
    {
        showing = true;
        elapsed = 0f;
        SetAlpha(1f);
        SetVisible(true);
    }

    private void End()
    {
        showing = false;
        SetVisible(false);
    }

    private void Tick()
    {
        CharacterBase me = CharacterBase.LocalPlayer;
        if (me == null || me.IsDead || markerRoot == null)
        {
            End();
            return;
        }

        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= showDuration)
        {
            End();
            return;
        }

        Transform marker = markerRoot.transform;
        Vector3 position = me.transform.position;
        position.x += worldOffset.x;
        position.y += worldOffset.y + Mathf.Sin(elapsed * bobSpeed) * bobAmplitude;
        position.z = marker.position.z;
        marker.position = position;
        marker.rotation = Quaternion.identity; // 캐릭터가 뒤집혀도 마커는 항상 정면을 본다.

        float remaining = showDuration - elapsed;
        SetAlpha(fadeDuration > 0f ? Mathf.Clamp01(remaining / fadeDuration) : 1f);
    }

    private void SetVisible(bool visible)
    {
        if (markerRoot != null && markerRoot.activeSelf != visible)
            markerRoot.SetActive(visible);
    }

    private void SetAlpha(float alpha)
    {
        if (fadeGroup != null)
            fadeGroup.alpha = alpha;
    }
}
