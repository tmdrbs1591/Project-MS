using System.Collections;
using ProjectMS.CharacterSystem;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 매칭 UI 래퍼. 실제 네트워크 접속/매칭은 NetworkLauncher 가 담당한다. (Fusion 2)
///
/// [역할]
///   - 매칭 버튼/포탈에서 StartMatching() 을 호출하면 NetworkLauncher 로 위임한다.
///   - NetworkLauncher 의 상태 메시지를 받아 UI 텍스트로 표시한다.
///
/// [씬 설정]
///   - 로비 씬에 NetworkLauncher(빈 오브젝트)를 두고, 이 스크립트는 UI 와 함께 둔다.
///   - 정원/게임 씬 이름 설정은 NetworkLauncher 쪽에 있다.
/// </summary>
public class MatchmakingManager : MonoBehaviour
{
    [Header("UI (선택)")]
    [Tooltip("매칭 중에 켜질 패널. 평소엔 꺼둠")]
    [SerializeField] private GameObject matchPanel;
    [SerializeField] private Button matchButton;
    [Tooltip("패널 안의 매칭 취소 버튼 (선택)")]
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text statusText;
    [Tooltip("\"매칭 중...\" 점이 바뀌는 간격(초)")]
    [SerializeField] private float dotInterval = 0.4f;
    [Tooltip("매칭 경과 시간(00:00) 표시 텍스트 (선택)")]
    [SerializeField] private TMP_Text timerText;

    [Header("연출")]
    [Tooltip("매칭 패널 페이드용. 비워두면 matchPanel 에서 찾고, 없으면 자동으로 붙인다.")]
    [SerializeField] private CanvasGroup matchCanvasGroup;
    [SerializeField] private float panelFadeDuration = 0.9f;
    [Tooltip("매칭 시작 후 패널이 나타나기 시작할 때까지 기다리는 시간(초)")]
    [SerializeField] private float panelFadeDelay = 0.5f;
    [Tooltip("매칭 중 커질 포탈 비주얼(Portal 프리팹). 비워두면 Portal 오브젝트 밑의 \"Portal\" 자식을 찾는다.")]
    [SerializeField] private Transform portalVisual;
    [SerializeField] private float portalScaleMultiplier = 1.4f;
    [SerializeField] private float portalScaleDuration = 0.8f;
    [Tooltip("매칭 취소 시 포탈이 원래 크기로 돌아가는 시간(초)")]
    [SerializeField] private float portalShrinkDuration = 0.3f;
    [Tooltip("다 커졌을 때 포탈이 위로 올라가 있을 높이(월드 단위). 바닥이 제자리에 있도록 맞춘다.")]
    [SerializeField] private float portalRiseHeight = 0.4f;

    private Coroutine dotRoutine;
    private Coroutine fadeRoutine;
    private Coroutine portalRoutine;
    private Coroutine timerRoutine;
    private Vector3 portalBaseScale;
    private Vector3 portalBasePosition;
    private float portalMultiplier = 1f;
    private bool canCancel; // 매칭 패널이 떠 있고 아직 매칭 완료 전일 때만 ESC 로 취소 가능

    private void Start()
    {
        if (matchCanvasGroup == null && matchPanel != null)
        {
            matchCanvasGroup = matchPanel.GetComponent<CanvasGroup>();
            if (matchCanvasGroup == null)
                matchCanvasGroup = matchPanel.AddComponent<CanvasGroup>();
        }

        if (portalVisual == null)
            portalVisual = FindPortalVisual();
        if (portalVisual != null)
        {
            portalBaseScale = portalVisual.localScale;
            portalBasePosition = portalVisual.position;
        }

        if (matchButton != null)
        {
            matchButton.onClick.AddListener(StartMatching);
            // 이전 매칭 도중(interactable = false 인 상태)에 씬이 저장/리로드됐을 가능성에 대비해
            // 로비 진입 시 항상 눌러지는 상태로 명시적으로 되돌린다.
            matchButton.interactable = true;
        }

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelMatching);

        if (matchPanel != null)
            matchPanel.SetActive(false);

        // 로비 진입 시 조작 잠금을 확실히 해제한다(이전 세션의 잠금 잔존 방지).
        CharacterBase.SetLobbyControlLocked(false);

        if (NetworkLauncher.Instance != null)
            NetworkLauncher.Instance.StatusChanged += SetStatus;

        SetStatus("매칭 대기 중");
    }

    private void OnDestroy()
    {
        if (NetworkLauncher.Instance != null)
            NetworkLauncher.Instance.StatusChanged -= SetStatus;
    }

    /// <summary>포탈/버튼에서 호출.</summary>
    public void StartMatching()
    {
        if (NetworkLauncher.Instance == null)
        {
            SetStatus("NetworkLauncher 가 씬에 없습니다.");
            Debug.LogError("[Matchmaking] NetworkLauncher.Instance 가 null 입니다. 로비 씬에 NetworkLauncher 를 배치하세요.");
            return;
        }

        if (matchButton != null)
            matchButton.interactable = false;

        // 매칭 패널을 켜고 로비 캐릭터 조작을 잠근다.
        if (matchPanel != null)
            matchPanel.SetActive(true);
        CharacterBase.SetLobbyControlLocked(true);
        LobbyCharacterController.SetLocked(true);

        if (matchCanvasGroup != null)
        {
            matchCanvasGroup.alpha = 0f;
            Restart(ref fadeRoutine, FadeCanvasGroup(matchCanvasGroup, 1f, panelFadeDuration, panelFadeDelay));
        }
        if (portalVisual != null)
            Restart(ref portalRoutine, ScalePortal(portalScaleMultiplier, portalScaleDuration));
        Restart(ref timerRoutine, RunTimer());

        canCancel = true;
        NetworkLauncher.Instance.StartMatchmaking();
    }

    private void Update()
    {
        if (!canCancel)
            return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            CancelMatching();
    }

    public void CancelMatching()
    {
        canCancel = false;

        if (NetworkLauncher.Instance != null)
            NetworkLauncher.Instance.CancelMatchmaking();

        if (matchButton != null)
            matchButton.interactable = true;

        // 매칭 패널을 끄고 로비 캐릭터 조작을 푼다.
        Stop(ref fadeRoutine);
        Stop(ref timerRoutine);
        if (matchPanel != null)
            matchPanel.SetActive(false);
        CharacterBase.SetLobbyControlLocked(false);
        LobbyCharacterController.SetLocked(false);

        // 포탈은 원래 크기로 서서히 되돌린다.
        if (portalVisual != null)
            Restart(ref portalRoutine, ScalePortal(1f, portalShrinkDuration));
    }

    private Transform FindPortalVisual()
    {
        Portal portal = FindObjectOfType<Portal>();
        if (portal == null)
            return null;

        // 트리거 콜라이더가 있는 부모까지 키우면 상호작용 범위도 같이 커지므로, 비주얼 자식만 키운다.
        foreach (Transform child in portal.transform)
        {
            if (child.name.StartsWith("Portal"))
                return child;
        }
        return null;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float target, float duration, float delay = 0f)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        float start = group.alpha;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        group.alpha = target;
        fadeRoutine = null;
    }

    // 피벗이 가운데라 그냥 키우면 아래로도 늘어난다. 커지는 만큼 위치를 같이 위로 올려서
    // 바닥 피벗으로 키우는 것처럼 보이게 한다.
    private IEnumerator ScalePortal(float targetMultiplier, float duration)
    {
        float start = portalMultiplier;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            ApplyPortalMultiplier(Mathf.Lerp(start, targetMultiplier, Mathf.SmoothStep(0f, 1f, t / duration)));
            yield return null;
        }
        ApplyPortalMultiplier(targetMultiplier);
        portalRoutine = null;
    }

    private void ApplyPortalMultiplier(float multiplier)
    {
        portalMultiplier = multiplier;
        portalVisual.localScale = portalBaseScale * multiplier;
        // 원래 크기(1) → 목표 배율 사이 진행도만큼 portalRiseHeight 를 올린다.
        float progress = Mathf.Approximately(portalScaleMultiplier, 1f)
            ? 0f
            : (multiplier - 1f) / (portalScaleMultiplier - 1f);
        portalVisual.position = portalBasePosition + Vector3.up * (progress * portalRiseHeight);
    }

    // 매칭 경과 시간을 00:00 형식으로 1초마다 갱신한다. 매칭 완료 시점에 멈춘다.
    private IEnumerator RunTimer()
    {
        float startTime = Time.unscaledTime;
        int lastSeconds = -1;
        while (true)
        {
            int seconds = Mathf.FloorToInt(Time.unscaledTime - startTime);
            if (seconds != lastSeconds && timerText != null)
            {
                timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
                lastSeconds = seconds;
            }
            yield return null;
        }
    }

    private void Restart(ref Coroutine routine, IEnumerator next)
    {
        Stop(ref routine);
        routine = StartCoroutine(next);
    }

    private void Stop(ref Coroutine routine)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    private void SetStatus(string message)
    {
        if (dotRoutine != null)
        {
            StopCoroutine(dotRoutine);
            dotRoutine = null;
        }

        // 매칭이 잡혀 게임 씬으로 넘어가는 중에는 ESC 취소를 막는다.
        if (message == NetworkLauncher.MatchedStatus)
        {
            canCancel = false;
            Stop(ref timerRoutine);
        }

        // "매칭 중"이면 뒤에 점을 . → .. → ... 으로 반복해서 붙인다.
        if (message == NetworkLauncher.MatchingStatus)
        {
            dotRoutine = StartCoroutine(AnimateMatchingDots(message));
            return;
        }

        if (statusText != null)
            statusText.text = message;
    }

    private IEnumerator AnimateMatchingDots(string baseText)
    {
        var wait = new WaitForSecondsRealtime(dotInterval);
        int dots = 1;
        while (true)
        {
            if (statusText != null)
                statusText.text = baseText + new string('.', dots);
            dots = dots % 3 + 1;
            yield return wait;
        }
    }
}
