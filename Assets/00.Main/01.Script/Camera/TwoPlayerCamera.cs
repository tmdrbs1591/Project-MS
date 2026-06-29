using System.Collections.Generic;
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
/// </summary>
[RequireComponent(typeof(Camera))]
public class TwoPlayerCamera : MonoBehaviour
{
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

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (!cam.orthographic)
            Debug.LogWarning("[TwoPlayerCamera] 카메라가 Orthographic 이 아닙니다. 2D 줌이 정상 동작하려면 Orthographic 으로 바꿔주세요.");
    }

    // 캐릭터 이동(FixedUpdate)과 보간이 끝난 뒤 따라가도록 LateUpdate 에서 처리한다.
    private void LateUpdate()
    {
        IReadOnlyList<CharacterBase> players = CharacterBase.All;
        if (players.Count == 0)
            return;

        Vector3 min = players[0].transform.position;
        Vector3 max = min;
        for (int i = 1; i < players.Count; i++)
        {
            Vector3 p = players[i].transform.position;
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        bool hasBounds = boundsCollider != null;
        Bounds area = hasBounds ? boundsCollider.bounds : default;
        float aspect = cam.aspect > 0f ? cam.aspect : 1f;

        // 1) 둘 다 담을 orthographicSize 계산
        float halfWidthNeeded = (max.x - min.x) * 0.5f + paddingX;
        float halfHeightNeeded = (max.y - min.y) * 0.5f + paddingY;
        float sizeFromWidth = halfWidthNeeded / aspect;

        float targetSize = Mathf.Max(sizeFromWidth, halfHeightNeeded, minSize);
        if (maxSize > 0f)
            targetSize = Mathf.Min(targetSize, maxSize);

        // 영역이 있으면 화면이 영역보다 커지지 않게 줌 상한 제한
        if (hasBounds)
        {
            float maxByHeight = area.extents.y;
            float maxByWidth = area.extents.x / aspect;
            targetSize = Mathf.Min(targetSize, maxByHeight, maxByWidth);
        }

        float size = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref zoomVelocity, zoomSmoothTime);
        cam.orthographicSize = size;

        // 2) 중간점으로 이동
        float targetX = (min.x + max.x) * 0.5f;
        float targetY = (min.y + max.y) * 0.5f;
        float x = Mathf.SmoothDamp(transform.position.x, targetX, ref moveVelocityX, positionSmoothTime);
        float y = Mathf.SmoothDamp(transform.position.y, targetY, ref moveVelocityY, positionSmoothTime);

        // 3) 영역 콜라이더 안으로 화면을 가둔다
        if (hasBounds)
        {
            float halfH = size;
            float halfW = size * aspect;

            x = area.size.x >= halfW * 2f ? Mathf.Clamp(x, area.min.x + halfW, area.max.x - halfW) : area.center.x;
            y = area.size.y >= halfH * 2f ? Mathf.Clamp(y, area.min.y + halfH, area.max.y - halfH) : area.center.y;
        }

        transform.position = new Vector3(x, y, transform.position.z);
    }
}
