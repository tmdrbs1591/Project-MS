using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 두 플레이어를 항상 화면 안에 담아주는 2D 카메라.
///
/// [동작]
///   - 매 프레임 살아있는 캐릭터들(CharacterBase.All)의 중간점으로 카메라를 옮긴다.
///   - 둘이 멀어지면 둘 다 화면에 들어오도록 orthographicSize 를 키워 줌 아웃한다.
///   - 너무 가까워도 minSize 아래로는 당기지 않는다(최소 줌). maxSize 로 상한도 둔다.
///   - 위치/줌 모두 SmoothDamp 로 부드럽게 따라간다.
///
/// [씬 설정]
///   - Main Camera 에 이 스크립트를 붙인다. 카메라는 반드시 Orthographic 이어야 한다.
///   - 플레이어가 1명만 있으면 그 한 명을, 0명이면 마지막 위치를 유지한다.
///   - 별도 타깃 지정은 필요 없다. PlayerSpawner 로 스폰된 캐릭터를 자동으로 잡는다.
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

    [Header("부드러움")]
    [Tooltip("위치가 목표를 따라잡는 데 걸리는 대략적인 시간(초). 작을수록 빠릿.")]
    [SerializeField] private float positionSmoothTime = 0.2f;
    [Tooltip("줌이 목표를 따라잡는 데 걸리는 대략적인 시간(초).")]
    [SerializeField] private float zoomSmoothTime = 0.3f;

    private Camera cam;
    private float zoomVelocity;
    private Vector3 moveVelocity;

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

        // 1) 중간점으로 이동 (카메라 z 는 그대로 유지)
        Vector3 center = (min + max) * 0.5f;
        Vector3 targetPos = new Vector3(center.x, center.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref moveVelocity, positionSmoothTime);

        // 2) 둘 다 담을 orthographicSize 계산
        //    세로: 절반 높이 = (간격/2) + 여백
        //    가로: aspect 로 나눠 세로 size 단위로 환산
        float halfHeightNeeded = (max.y - min.y) * 0.5f + paddingY;
        float halfWidthNeeded = (max.x - min.x) * 0.5f + paddingX;
        float sizeFromWidth = cam.aspect > 0f ? halfWidthNeeded / cam.aspect : halfWidthNeeded;

        float targetSize = Mathf.Max(halfHeightNeeded, sizeFromWidth, minSize);
        if (maxSize > 0f)
            targetSize = Mathf.Min(targetSize, maxSize);

        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref zoomVelocity, zoomSmoothTime);
    }
}
