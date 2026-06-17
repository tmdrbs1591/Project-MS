using UnityEngine;

/// <summary>
/// 팔(작은 동그라미)이 몸통을 스프링처럼 살짝 늦게 따라오게 만드는 2차 모션.
///
/// 원리(Follow-through / Secondary motion)
///   - 팔이 "있어야 할 자리(anchor)"는 부모(몸통/루트)를 따라 매 프레임 움직인다.
///   - 실제로 보이는 팔은 그 자리로 스프링처럼 천천히 따라간다.
///   - 그래서 몸이 통통 튀거나 좌우로 움직이면 팔이 관성으로 살짝 늦게 출렁이며
///     자연스럽게 스며들듯 따라온다. (가만히 있으면 제자리에 딱 붙어 어색하지 않음)
///
/// [사용법]
///   - 양쪽 팔 오브젝트(동그라미)에 각각 이 스크립트를 붙이기만 하면 끝.
///   - 팔은 몸통이 아니라 루트(Player) 자식으로 두는 걸 권장
///     (몸통의 늘어남/눌림 스케일이 동그란 팔까지 찌그러뜨리지 않게).
/// </summary>
[DisallowMultipleComponent]
public class SlimeArmJiggle : MonoBehaviour
{
    [Header("따라오는 느낌")]
    [Tooltip("클수록 빠릿하게 제자리로 붙음 (작으면 더 늘어지게 따라옴)")]
    [SerializeField] private float stiffness = 180f;

    [Tooltip("작을수록 더 오래 출렁출렁 (탱탱함). 너무 작으면 부들부들 떨림")]
    [SerializeField] private float damping = 13f;

    [Tooltip("팔이 제자리에서 벗어날 수 있는 최대 거리 (너무 멀어지지 않게)")]
    [SerializeField] private float maxOffset = 0.25f;

    [Header("몸통 따라가기 (숨쉬기/바운스 반응)")]
    [Tooltip("몸통(Body) Transform 을 넣으면, 몸통이 숨쉬며 까딱이거나 통통 튈 때 " +
             "팔도 그 움직임을 따라 출렁인다. 비우면 루트 이동에만 반응(가만히 있을 때 안 움직임).")]
    [SerializeField] private Transform body;

    [Tooltip("몸통 움직임을 얼마나 따라갈지 배수 (1 = 그대로, 2 = 더 과장)")]
    [SerializeField] private float bodyFollow = 1.5f;

    [Header("흔들림 회전 (선택)")]
    [Tooltip("따라오며 벗어난 방향만큼 팔을 살짝 회전시켜 더 생동감 있게")]
    [SerializeField] private float swingRotation = 25f;

    private Transform parent;          // 따라갈 대상 (부모)
    private Vector3 baseLocalPos;      // 팔의 기준 로컬 위치
    private Quaternion baseLocalRot;   // 팔의 기준 로컬 회전
    private Vector3 bodyBaseLocalPos;  // 몸통의 기준 로컬 위치

    private Vector3 currentWorldPos;   // 스프링으로 움직이는 실제 위치
    private Vector3 velocity;          // 스프링 속도

    private void Start()
    {
        parent = transform.parent;
        baseLocalPos = transform.localPosition;
        baseLocalRot = transform.localRotation;
        currentWorldPos = transform.position;

        if (body != null)
            bodyBaseLocalPos = body.localPosition;
    }

    private void LateUpdate()
    {
        if (parent == null) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // 1) 팔이 "있어야 할 자리" (부모를 따라 움직이는 기준점)
        //    몸통(Body)이 지정돼 있으면, 몸통이 기준 위치에서 벗어난 만큼(숨쉬기/바운스)을
        //    더해줘서 가만히 있을 때도 팔이 몸통을 따라 까딱이게 한다.
        Vector3 targetLocal = baseLocalPos;
        if (body != null)
            targetLocal += (body.localPosition - bodyBaseLocalPos) * bodyFollow;

        Vector3 anchor = parent.TransformPoint(targetLocal);

        // 2) 그 자리로 스프링처럼 따라간다 (감쇠 조화 진동)
        Vector3 force = (anchor - currentWorldPos) * stiffness - velocity * damping;
        velocity += force * dt;
        currentWorldPos += velocity * dt;

        // 3) 너무 멀리 벗어나지 않게 제한
        Vector3 offset = currentWorldPos - anchor;
        if (offset.magnitude > maxOffset)
        {
            offset = offset.normalized * maxOffset;
            currentWorldPos = anchor + offset;
        }

        transform.position = currentWorldPos;

        // 4) 벗어난 방향만큼 살짝 회전시켜 흔들리는 맛 추가
        if (swingRotation != 0f)
        {
            // 부모 기준 로컬로 환산한 가로 벗어남으로 회전량 결정
            Vector3 localOffset = parent.InverseTransformVector(offset);
            float angle = -localOffset.x * swingRotation;
            transform.localRotation = baseLocalRot * Quaternion.Euler(0f, 0f, angle);
        }
    }
}
