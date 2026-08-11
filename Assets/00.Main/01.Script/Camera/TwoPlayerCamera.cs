using System.Collections.Generic;
using ProjectMS.CharacterSystem;
using UnityEngine;

/// <summary>
/// 두 플레이어를 항상 화면 안에 담아주는 2D 카메라.
///
/// [동작]
///   - 매 프레임 살아있는 캐릭터들(CharacterBase.All)의 중간점으로 카메라를 옮긴다.
///   - 둘이 멀어지면 둘 다 화면에 들어오도록 orthographicSize 를 키워 줌 아웃한다.
///   - 너무 가까워도 minSize 아래로는 당기지 않는다(최소 줌). maxSize 로 상한도 둔다.
///   - boundsCollider(카메라 영역 콜라이더)를 지정하면, 화면이 그 영역 밖으로
///     절대 나가지 않게 위치와 줌을 제한한다. 콜라이더 아래 경계가 곧 카메라가
///     내려갈 수 있는 한계가 되므로 땅 밑을 비추지 않는다.
///   - 위치/줌 모두 SmoothDamp 로 부드럽게 따라간다.
///
/// [씬 설정]
///   - Main Camera 에 이 스크립트를 붙인다. 카메라는 반드시 Orthographic 이어야 한다.
///   - 카메라 영역: 맵을 덮는 빈 GameObject 에 BoxCollider2D 를 붙여 원하는 영역
///     크기로 맞추고(Is Trigger 권장 — 물리 충돌 방지), 그 콜라이더를
///     Bounds Collider 칸에 넣는다. 아래 경계를 바닥보다 살짝 위로 두면 땅 밑이 안 보인다.
///
/// [포커스 오버라이드]
///   - SetFocusOverride(target, zoomSize) 를 호출하면 두 플레이어 자동 프레이밍을 잠깐
///     멈추고 target 하나로 SmoothDamp 이동/줌한다(피니시 연출 등에서 사용, RoundFinishController 참고).
///   - ClearFocusOverride() 로 원래의 두 플레이어 프레이밍으로 복귀한다.
///   - 오버라이드 중엔 boundsCollider 제한을 끈다 — 죽는 위치가 맵 가장자리에 가까우면 경계
///     클램프가 실제 타깃 위치와 화면 중심을 몇 칸씩 어긋나게 만들어서(KO 줌인이 시체가 아닌
///     다른 곳을 비추는 것처럼 보임), 포커스 중엔 정확한 위치/줌을 우선한다. maxSize 상한만
///     그대로 유지된다.
///
/// [카메라 셰이크]
///   - Shake(magnitude, duration) 를 아무 스크립트에서나 TwoPlayerCamera.Instance.Shake(...)
///     로 호출하면 짧게 흔들린다(예: CharacterVisualController의 피격 이벤트).
///   - 흔들림은 SmoothDamp로 계산되는 "순수 추적 위치"(smoothedPosition)에는 더해지지 않고,
///     매 프레임 화면에 실제로 그리는 위치에만 얹힌다 — transform.position에 직접 더하면
///     다음 프레임 SmoothDamp가 그 흔들린 값을 "현재 위치"로 다시 읽어들여서 흔들림이
///     스무딩 상태에 먹혀 카메라가 계속 떨리게 된다. 그래서 별도 필드로 분리해둔다.
///   - 이미 흔들리는 중에 더 약한 흔들림 요청이 들어오면 무시한다(약한 타격이 강한
///     피니시 흔들림을 끊어버리지 않게).
/// </summary>
[RequireComponent(typeof(Camera))]
public class TwoPlayerCamera : MonoBehaviour
{
    public static TwoPlayerCamera Instance { get; private set; }
    [Header("여백 (월드 단위)")]
    [Tooltip("플레이어와 화면 가장자리 사이에 둘 최소 여백. 클수록 더 멀리서 잡는다.")]
    [SerializeField] private float paddingX = 3f;
    [SerializeField] private float paddingY = 3f;

    [Header("줌 한계 (orthographicSize)")]
    [Tooltip("가장 가까울 때도 이보다 더 당기지 않는다(최소 줌).")]
    [SerializeField] private float minSize = 5f;
    [Tooltip("가장 멀어졌을 때의 줌 상한. 0 이하면 무제한.")]
    [SerializeField] private float maxSize = 20f;

    [Header("카메라 영역 (콜라이더)")]
    [Tooltip("지정하면 카메라 화면이 이 콜라이더 영역 밖으로 나가지 않는다. 아래 경계가 카메라 하한이 된다. 비워두면 제한 없음.")]
    [SerializeField] private Collider2D boundsCollider;

    [Header("부드러움")]
    [Tooltip("위치가 목표를 따라잡는 데 걸리는 대략적인 시간(초). 작을수록 빠릿.")]
    [SerializeField] private float positionSmoothTime = 0.2f;
    [Tooltip("줌이 목표를 따라잡는 데 걸리는 대략적인 시간(초).")]
    [SerializeField] private float zoomSmoothTime = 0.3f;

    private Camera cam;
    private float zoomVelocity;
    private float moveVelocityX;
    private float moveVelocityY;

    // 흔들림이 안 섞인 순수 추적 위치. SmoothDamp의 "현재 위치" 입력은 항상 이 값을 쓴다
    // (transform.position을 직접 쓰면 흔들림이 다음 프레임 스무딩에 먹혀 들어간다).
    private Vector3 smoothedPosition;

    private Transform focusOverrideTarget;
    private float focusOverrideSize;
    private bool hasFocusOverride;

    private float shakeElapsed;
    private float shakeDuration;
    private float shakeMagnitude;

    private void Awake()
    {
        Instance = this;

        cam = GetComponent<Camera>();
        if (!cam.orthographic)
            Debug.LogWarning("[TwoPlayerCamera] 카메라가 Orthographic 이 아닙니다. 2D 줌이 정상 동작하려면 Orthographic 으로 바꿔주세요.");

        smoothedPosition = transform.position;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>두 플레이어 자동 프레이밍을 멈추고 target 하나로 포커스+줌한다.</summary>
    public void SetFocusOverride(Transform target, float zoomSize)
    {
        focusOverrideTarget = target;
        focusOverrideSize = zoomSize;
        hasFocusOverride = true;
    }

    /// <summary>포커스 오버라이드를 풀고 두 플레이어 자동 프레이밍으로 복귀한다.</summary>
    public void ClearFocusOverride()
    {
        hasFocusOverride = false;
        focusOverrideTarget = null;
    }

    /// <summary>화면을 magnitude(월드 단위) 세기로 duration(실시간 초) 동안 흔든다.
    /// 이미 더 강한 흔들림이 재생 중이면 무시한다.</summary>
    public void Shake(float magnitude, float duration)
    {
        bool alreadyShaking = shakeElapsed < shakeDuration;
        if (alreadyShaking && shakeMagnitude >= magnitude)
            return;

        shakeMagnitude = magnitude;
        shakeDuration = duration;
        shakeElapsed = 0f;
    }

    // 캐릭터 이동(FixedUpdate)과 보간이 끝난 뒤 따라가도록 LateUpdate 에서 처리한다.
    private void LateUpdate()
    {
        if (!TryGetFramingTarget(out Vector3 targetPos, out float targetSize))
            return;

        // 포커스 오버라이드(예: KO 줌인) 중엔 영역 제한을 끈다 — 평소 게임플레이 프레이밍은
        // 둘 다 화면 안에 담아야 해서 경계를 지켜야 하지만, 죽는 위치가 맵 가장자리에 가까우면
        // 경계 클램프가 실제 시체 위치와 화면 중심을 몇 칸씩 어긋나게 만든다. 포커스 중엔 정확한
        // 위치/줌이 더 중요하므로 이 제한을 건너뛴다(화면 일부가 맵 밖을 살짝 비쳐도 괜찮다).
        bool hasBounds = boundsCollider != null && !hasFocusOverride;
        Bounds area = hasBounds ? boundsCollider.bounds : default;
        float aspect = cam.aspect > 0f ? cam.aspect : 1f;

        if (maxSize > 0f)
            targetSize = Mathf.Min(targetSize, maxSize);

        // 영역이 있으면 화면이 영역보다 커지지 않게 줌 상한 제한
        if (hasBounds)
        {
            float maxByHeight = area.extents.y;
            float maxByWidth = area.extents.x / aspect;
            targetSize = Mathf.Min(targetSize, maxByHeight, maxByWidth);
        }

        // Time.deltaTime(스케일 적용)이 아니라 unscaledDeltaTime을 쓴다 — RoundFinishController가
        // KO 연출 중 Time.timeScale을 낮추는데, 줌/이동까지 그 영향을 받으면 슬로우모션 동안
        // 카메라가 굼떠 보인다(흔들림(Shake)은 원래도 unscaled라 이질감이 났음).
        float unscaledDeltaTime = Time.unscaledDeltaTime;

        float size = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref zoomVelocity, zoomSmoothTime, Mathf.Infinity, unscaledDeltaTime);
        cam.orthographicSize = size;

        float x = Mathf.SmoothDamp(smoothedPosition.x, targetPos.x, ref moveVelocityX, positionSmoothTime, Mathf.Infinity, unscaledDeltaTime);
        float y = Mathf.SmoothDamp(smoothedPosition.y, targetPos.y, ref moveVelocityY, positionSmoothTime, Mathf.Infinity, unscaledDeltaTime);

        // 영역 콜라이더 안으로 화면을 가둔다
        if (hasBounds)
        {
            float halfH = size;
            float halfW = size * aspect;

            x = area.size.x >= halfW * 2f ? Mathf.Clamp(x, area.min.x + halfW, area.max.x - halfW) : area.center.x;
            y = area.size.y >= halfH * 2f ? Mathf.Clamp(y, area.min.y + halfH, area.max.y - halfH) : area.center.y;
        }

        smoothedPosition = new Vector3(x, y, transform.position.z);
        transform.position = smoothedPosition + (Vector3)GetShakeOffset();
    }

    /// <summary>이번 프레임의 흔들림 오프셋. 시간이 지날수록 세기가 줄어든다(감쇠).</summary>
    private Vector2 GetShakeOffset()
    {
        if (shakeElapsed >= shakeDuration)
            return Vector2.zero;

        shakeElapsed += Time.unscaledDeltaTime;
        float damper = shakeDuration > 0f ? 1f - Mathf.Clamp01(shakeElapsed / shakeDuration) : 0f;
        return Random.insideUnitCircle * shakeMagnitude * damper;
    }

    /// <summary>포커스 오버라이드가 걸려있으면 그 타깃을, 아니면 두 플레이어를 담는 목표
    /// 위치/줌을 계산한다. 담을 대상이 없으면(플레이어 없음) false.</summary>
    private bool TryGetFramingTarget(out Vector3 targetPos, out float targetSize)
    {
        if (hasFocusOverride)
        {
            if (focusOverrideTarget != null)
            {
                targetPos = focusOverrideTarget.position;
                targetSize = focusOverrideSize;
                return true;
            }

            // 오버라이드 타깃이 사라졌으면(캐릭터 디스폰 등) 자동으로 풀고 평소 프레이밍으로 복귀.
            hasFocusOverride = false;
        }

        IReadOnlyList<CharacterBase> players = CharacterBase.All;
        if (players.Count == 0)
        {
            targetPos = default;
            targetSize = minSize;
            return false;
        }

        Vector3 min = players[0].transform.position;
        Vector3 max = min;
        for (int i = 1; i < players.Count; i++)
        {
            Vector3 p = players[i].transform.position;
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        float aspect = cam.aspect > 0f ? cam.aspect : 1f;
        float halfWidthNeeded = (max.x - min.x) * 0.5f + paddingX;
        float halfHeightNeeded = (max.y - min.y) * 0.5f + paddingY;
        float sizeFromWidth = halfWidthNeeded / aspect;

        targetPos = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f);
        targetSize = Mathf.Max(sizeFromWidth, halfHeightNeeded, minSize);
        return true;
    }
}
