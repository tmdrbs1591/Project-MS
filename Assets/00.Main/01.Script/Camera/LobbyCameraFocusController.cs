using UnityEngine;

/// <summary>
/// 로비 메인 카메라를 특정 대상(건물 등)으로 부드럽게 줌인시켰다가, 다시 씬 시작 시점의
/// 기본 위치/줌으로 복귀시키는 컨트롤러. CharacterSelectUI가 열리고 닫힐 때 사용한다.
///
/// [동작]
///   - Awake 시점의 카메라 위치/orthographicSize를 "기본 상태"로 기억해둔다.
///   - FocusOn(target, zoomSize) 를 호출하면 target 위치로 이동하며 zoomSize로 줌인한다.
///   - ReturnToDefault() 를 호출하면 기억해둔 기본 상태로 되돌아간다.
///   - 위치/줌 모두 SmoothDamp 로 부드럽게 보간한다(TwoPlayerCamera와 동일한 방식).
///
/// [씬 설정]
///   - 로비 씬의 Main Camera 에 이 스크립트를 붙인다. 카메라는 Orthographic 이어야 한다.
/// </summary>
[RequireComponent(typeof(Camera))]
public class LobbyCameraFocusController : MonoBehaviour
{
    public static LobbyCameraFocusController Instance { get; private set; }

    [Header("부드러움")]
    [Tooltip("위치가 목표를 따라잡는 데 걸리는 대략적인 시간(초).")]
    [SerializeField] private float positionSmoothTime = 0.25f;
    [Tooltip("줌이 목표를 따라잡는 데 걸리는 대략적인 시간(초).")]
    [SerializeField] private float zoomSmoothTime = 0.25f;

    [Header("도착 판정")]
    [Tooltip("포커스 대상까지 이 거리(월드 단위) 이내로 들어오면 '도착'으로 본다.")]
    [SerializeField] private float arrivalPositionTolerance = 0.05f;
    [Tooltip("줌 크기가 목표값과 이 값 이내로 차이나면 '도착'으로 본다.")]
    [SerializeField] private float arrivalSizeTolerance = 0.05f;

    private Camera cam;

    private Vector3 defaultPosition;
    private float defaultSize;

    // 흔들림 없이 순수 추적하는 위치. TwoPlayerCamera와 같은 이유로 transform.position과 분리.
    private Vector3 smoothedPosition;
    private float zoomVelocity;
    private float moveVelocityX;
    private float moveVelocityY;

    private Transform focusTarget;
    private float focusSize;
    private bool hasFocus;

    private void Awake()
    {
        Instance = this;

        cam = GetComponent<Camera>();
        if (!cam.orthographic)
            Debug.LogWarning("[LobbyCameraFocusController] 카메라가 Orthographic 이 아닙니다. 2D 줌이 정상 동작하려면 Orthographic 으로 바꿔주세요.");

        defaultPosition = transform.position;
        defaultSize = cam.orthographicSize;
        smoothedPosition = defaultPosition;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>target 위치로 부드럽게 이동하며 orthographicSize를 zoomSize로 줄인다(0 이하면 줌 변경 없음).</summary>
    public void FocusOn(Transform target, float zoomSize)
    {
        if (target == null)
            return;

        focusTarget = target;
        focusSize = zoomSize > 0f ? zoomSize : defaultSize;
        hasFocus = true;
    }

    /// <summary>씬 시작 시점의 기본 위치/줌으로 되돌아간다.</summary>
    public void ReturnToDefault()
    {
        hasFocus = false;
        focusTarget = null;
    }

    /// <summary>포커스 중이 아니면 true(할 일이 없으니 이미 "도착"한 것으로 본다).
    /// 포커스 중이면 카메라가 목표 위치/줌에 충분히 가까워졌을 때만 true — 줌 연출이 끝난
    /// 뒤에 UI를 보여주고 싶을 때(CharacterSelectUI 참고) 매 프레임 폴링해서 쓴다.</summary>
    public bool HasArrivedAtFocus
    {
        get
        {
            if (!hasFocus || focusTarget == null)
                return true;

            Vector3 targetPos = new Vector3(focusTarget.position.x, focusTarget.position.y, defaultPosition.z);
            float sqrDist = ((Vector2)(smoothedPosition - targetPos)).sqrMagnitude;
            bool posClose = sqrDist <= arrivalPositionTolerance * arrivalPositionTolerance;
            bool sizeClose = Mathf.Abs(cam.orthographicSize - focusSize) <= arrivalSizeTolerance;
            return posClose && sizeClose;
        }
    }

    private void LateUpdate()
    {
        Vector3 targetPos;
        float targetSize;

        if (hasFocus && focusTarget != null)
        {
            targetPos = new Vector3(focusTarget.position.x, focusTarget.position.y, defaultPosition.z);
            targetSize = focusSize;
        }
        else
        {
            // 포커스 타깃이 사라졌으면(오브젝트 파괴 등) 자동으로 풀고 기본 상태로 복귀.
            hasFocus = false;
            targetPos = defaultPosition;
            targetSize = defaultSize;
        }

        float dt = Time.unscaledDeltaTime;

        float size = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref zoomVelocity, zoomSmoothTime, Mathf.Infinity, dt);
        cam.orthographicSize = size;

        float x = Mathf.SmoothDamp(smoothedPosition.x, targetPos.x, ref moveVelocityX, positionSmoothTime, Mathf.Infinity, dt);
        float y = Mathf.SmoothDamp(smoothedPosition.y, targetPos.y, ref moveVelocityY, positionSmoothTime, Mathf.Infinity, dt);

        smoothedPosition = new Vector3(x, y, defaultPosition.z);
        transform.position = smoothedPosition;
    }
}
