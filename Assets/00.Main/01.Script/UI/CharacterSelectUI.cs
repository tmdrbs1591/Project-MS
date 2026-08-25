using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 로비의 캐릭터 변경 건물(CharacterChangeBuilding)과 상호작용하면 뜨는 캐릭터 선택 패널.
/// 열리면 우선 패널을 완전히 숨긴 채로 LobbyCameraFocusController가 건물 쪽으로 줌인하고,
/// 카메라가 도착한 뒤에야 패널이 살짝 작은 크기에서 튀어 나오듯 페이드인한다(줌 연출과 패널이
/// 겹치지 않고 순서대로 나온다). 닫힐 때(닫기 버튼 또는 Esc)는 패널이 먼저 페이드아웃하며
/// 동시에 카메라도 원래 로비 프레이밍으로 돌아간다.
///
/// 패널 안의 캐릭터 버튼들은 씬에서 직접 배치하고, 각 버튼의 OnClick에
/// SelectCharacter(인덱스)를 연결해서 쓴다(인덱스는 PlayerSpawner.characterPrefabs 순서와 같아야 함).
///
/// [씬 설정]
///   - 로비 씬 Canvas 하위에 패널을 만들고 이 스크립트를 붙인다.
///   - panel: 평소엔 안 보이다가 Open() 호출 시 나타나는 패널. CanvasGroup이 없으면 자동으로 붙는다.
///     에디터에서 이 오브젝트를 처음부터 비활성(체크 해제)으로 둬도 되고, 활성 상태로 둬도 된다 —
///     숨김 처리는 GameObject 활성/비활성이 아니라 CanvasGroup(alpha/interactable)으로만 하기 때문에
///     둘 다 안전하다(활성 상태로 시작한 오브젝트를 Awake에서 곧장 SetActive(false) 했다가, 그
///     오브젝트가 처음부터 비활성이었던 경우 Open()의 SetActive(true) 도중 재귀적으로 다시 꺼져버려
///     코루틴이 "object is inactive" 에러를 내는 문제가 있었다 — 지금은 그 방식을 쓰지 않는다).
///   - closeButton: 누르면 패널을 닫고 카메라를 복귀시키는 X 버튼(선택 사항). Esc 키로도 닫힌다.
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    private const string SelectedCharacterIndexKey = "SelectedCharacterIndex";

    [SerializeField] private GameObject panel;
    [Tooltip("페이드/스케일 연출에 쓸 CanvasGroup. 비워두면 panel에서 자동으로 찾거나 붙인다.")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button closeButton;

    [Header("카메라 연출")]
    [Tooltip("UI가 열려있는 동안 줌인할 orthographicSize. 0 이하면 줌은 그대로 두고 이동만 한다.")]
    [SerializeField] private float focusZoomSize = 3f;

    [Header("패널 연출")]
    [Tooltip("열릴 때 페이드+스케일 애니메이션 시간(초).")]
    [Min(0f)] [SerializeField] private float openDuration = 0.25f;
    [Tooltip("닫힐 때 페이드+스케일 애니메이션 시간(초).")]
    [Min(0f)] [SerializeField] private float closeDuration = 0.15f;
    [Tooltip("열리기 시작할 때의 크기(1보다 작으면 살짝 작은 상태에서 튀어나오듯 커진다).")]
    [SerializeField] private float openStartScale = 0.85f;
    [Tooltip("카메라 도착을 최대 이 시간(초)까지만 기다린다. SmoothDamp는 목표에 '거의' 다가간\n뒤로도 완전히 수렴하기까지 꼬리가 길게 남아서, 정확히 도착할 때까지 기다리면 체감상\n너무 늦게 열린다 — 그래서 이 시간이 지나면 아직 덜 도착했어도 그냥 보여준다.")]
    [Min(0f)] [SerializeField] private float maxFocusWait = 0.25f;

    private Coroutine animRoutine;
    private bool isOpen;

    private void Awake()
    {
        if (panel != null && canvasGroup == null)
            canvasGroup = panel.GetComponent<CanvasGroup>();
        if (panel != null && canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        // 여기서 GameObject 자체를 SetActive(false) 하지 않는다. panel이 이 스크립트가 붙은
        // 오브젝트 자신인 경우, 그 오브젝트가 에디터에서 비활성 상태로 시작하면 Awake는
        // "누군가 처음 SetActive(true)를 부르는 순간" 그 호출 도중 동기적으로 실행되는데,
        // 거기서 다시 SetActive(false)를 부르면 그 활성화 자체가 취소되어 버려 바로 다음 줄인
        // Open()의 StartCoroutine이 "object is inactive" 에러를 낸다. 안 보이게/조작 안 되게
        // 하는 건 CanvasGroup만으로 처리하고, GameObject 활성 상태는 건드리지 않는다.
        SetVisualState(0f, openStartScale);
        SetInteractable(false);
    }

    private void Update()
    {
        if (!isOpen)
            return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            Close();
    }

    /// <summary>focusPoint(건물 위치 등)로 카메라를 줌인시키고, 다 도착한 뒤에 패널을 보여준다.</summary>
    public void Open(Transform focusPoint)
    {
        if (isOpen)
            return;
        isOpen = true;

        // 에디터에서 비활성 상태로 시작했더라도 여기서 켜준다. 이 시점엔 Awake가 더 이상
        // 자기 자신을 다시 끄지 않으므로(위 Awake 주석 참고) 안전하게 활성화된 채로 남는다.
        SetPanelActive(true);
        // 카메라가 도착하기 전까지는 완전히 숨겨둔다("줌이 끝나면 화면이 확 나타나는" 연출).
        SetVisualState(0f, openStartScale);
        SetInteractable(false);

        if (LobbyCameraFocusController.Instance != null)
            LobbyCameraFocusController.Instance.FocusOn(focusPoint, focusZoomSize);

        RestartRoutine(OpenSequence());
    }

    /// <summary>패널을 닫고 카메라를 기본 프레이밍으로 되돌린다.</summary>
    public void Close()
    {
        if (!isOpen)
            return;
        isOpen = false;

        if (LobbyCameraFocusController.Instance != null)
            LobbyCameraFocusController.Instance.ReturnToDefault();

        RestartRoutine(CloseSequence());
    }

    private void RestartRoutine(IEnumerator routine)
    {
        if (animRoutine != null)
            StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(routine);
    }

    /// <summary>패널 안 캐릭터 버튼의 OnClick에 직접 연결해서 쓴다.</summary>
    public void SelectCharacter(int index)
    {
        PlayerPrefs.SetInt(SelectedCharacterIndexKey, index);
        PlayerPrefs.Save();
    }

    /// <summary>카메라가 목표 지점에 도착할 때까지(최대 maxFocusWait 초, 패널은 숨겨진 채)
    /// 기다렸다가 튕기듯 페이드인한다. 카메라 컨트롤러가 없으면 대기 없이 바로 페이드인한다.</summary>
    private IEnumerator OpenSequence()
    {
        LobbyCameraFocusController cam = LobbyCameraFocusController.Instance;
        if (cam != null)
        {
            float waited = 0f;
            while (!cam.HasArrivedAtFocus && waited < maxFocusWait)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        yield return FadeTo(1f, 1f, openDuration, useOvershoot: true);
        SetInteractable(true);
        animRoutine = null;
    }

    /// <summary>바로 페이드아웃하며 줄어들었다가, 다 끝나면 패널을 비활성화한다.</summary>
    private IEnumerator CloseSequence()
    {
        SetInteractable(false);
        yield return FadeTo(0f, openStartScale, closeDuration, useOvershoot: false);
        SetPanelActive(false);
        animRoutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha, float targetScale, float duration, bool useOvershoot)
    {
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        float startScale = panel != null ? panel.transform.localScale.x : 1f;

        if (duration <= 0f)
        {
            SetVisualState(targetAlpha, targetScale);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scaleT = useOvershoot ? EaseOutBack(t) : t;
            SetVisualState(Mathf.Lerp(startAlpha, targetAlpha, t), Mathf.LerpUnclamped(startScale, targetScale, scaleT));
            yield return null;
        }

        SetVisualState(targetAlpha, targetScale);
    }

    /// <summary>0(시작 직전) → 1로 살짝 넘어갔다 돌아오는 튕김이 섞인 이징(ease-out-back).</summary>
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    private void SetVisualState(float alpha, float scale)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = alpha;

        if (panel != null)
            panel.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void SetInteractable(bool interactable)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }

    private void SetPanelActive(bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}
