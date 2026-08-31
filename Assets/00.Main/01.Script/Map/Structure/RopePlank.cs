using Fusion;
using UnityEngine;

/// <summary>
/// 줄 두 가닥(좌/우)에 매달려 중력을 받으며 흔들리는 판자다. (Fusion 2 / Shared 모드)
///
/// [동작]
///   - DistanceJoint2D 두 개를 코드로 생성해서 leftAnchor/rightAnchor(씬의 고정점)에 매단다.
///     HingeJoint2D(두 점을 아예 붙여버리는 경첩)가 아니라 DistanceJoint2D(거리만 유지,
///     회전은 자유)를 쓰는 이유: 줄은 "정해진 길이만큼 떨어진 채로 자유롭게 흔들리는" 게
///     맞지, 두 점이 딱 붙어야 하는 게 아니다. 줄 길이는 Awake 시점에 고정점~판자 연결
///     지점 사이의 실제 거리를 그대로 재서 쓴다 — 즉 에디터에서 배치한 모양(예: 위쪽
///     고정점은 좁고 아래 판자는 넓은 뒤집힌 사다리꼴)이 그대로 정지 상태의 매달린 모양이
///     된다. 그 이후 중력/흔들림/플레이어가 올라탔을 때의 반응은 전부 Unity 물리 엔진이
///     처리한다 — 별도의 Update 로직이 필요 없다.
///   - 폭발에 흔들리는 건 Grenade.cs의 폭발 판정이 이 판자의 Rigidbody2D에 직접 힘을
///     가하는 방식으로 처리한다(Grenade.ApplyExplosionForceToStructures 참고).
///   - 이 박스의 StateAuthority(기본: Shared 모드 마스터)가 물리를 시뮬레이션하고,
///     위치/회전은 NetworkRigidbody2D(Fusion 물리 애드온)가 동기화한다 —
///     PushableStructure와 동일한 권한 패턴.
///
/// [필요 컴포넌트 (프리팹)]
///   - Collider2D + Rigidbody2D (Dynamic, Gravity Scale > 0)
///   - NetworkObject + Fusion의 NetworkRigidbody2D (Physics 애드온, 에디터에서 추가)
///
/// [씬 설정]
///   - 판자보다 위에 빈 오브젝트 2개(좌/우 고정점)를 만들어 leftAnchor/rightAnchor에 연결한다.
///   - leftLocalAnchor/rightLocalAnchor는 판자 로컬 좌표계 기준 줄이 매달리는 지점(기본값은
///     판자 좌우 끝) — 판자 크기에 맞게 조절한다.
///
/// [가정]
///   - 판자 3개가 서로 독립적으로 각자 줄 2가닥에 매달린 것으로 설계했다(하나로 이어진
///     다리가 아니라 개별 흔들다리 판). 서로 연결된 하나의 다리를 원한다면, 가운데
///     판자들의 anchor를 옆 판자의 모서리 Transform으로 바꾸면 된다.
///
/// [줄 시각화]
///   - leftRopeLine/rightRopeLine에 LineRenderer를 연결하면 고정점~판자 사이에 실제로
///     보이는 줄을 그려준다(SPARK의 UpdateLineVisual과 같은 방식). 비워두면 그냥 안 그림 —
///     물리 동작에는 영향 없다.
///   - 고정점의 월드 위치는 캡처해서 쓴다. connectedBody가 없는 조인트의 connectedAnchor는
///     그 순간의 고정 좌표를 그대로 박아넣는 값이라(이후로 anchor Transform이 움직여도
///     조인트는 반응하지 않음), 줄도 조인트가 실제로 보고 있는 그 값을 그대로 그려야
///     판자 물리와 어긋나지 않는다.
///
/// [왜 Awake가 아니라 Spawned에서 조인트를 구성하는가]
///   - Runner.Spawn(prefab, position, ...)으로 넘긴 위치는 Unity의 Awake() 시점엔 아직
///     반영되기 전이고(스폰/동기화 절차가 끝난 뒤인 Spawned()에서야 확정됨), Awake에서
///     좌표를 읽으면 프리팹 원본 상태의(아직 스폰 위치가 안 적용된) 좌표를 읽게 된다.
///     여러 인스턴스를 스폰하면 전부 이 시점엔 같은 좌표라서, 조인트가 전부 똑같이
///     구성되는 버그가 생긴다. Grenade/ItemBase 등 기존 코드도 스폰 이후 값이 필요한
///     초기화는 전부 Awake가 아니라 Spawned에서 한다 — 그 패턴을 따른다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class RopePlank : NetworkBehaviour
{
    [Header("고정점 (씬의 빈 오브젝트, 판자보다 위)")]
    [SerializeField] private Transform leftAnchor;
    [SerializeField] private Transform rightAnchor;

    [Header("판자 쪽 연결 지점 (로컬 좌표, 판자 중심 기준)")]
    [SerializeField] private Vector2 leftLocalAnchor = new Vector2(-0.9f, 0f);
    [SerializeField] private Vector2 rightLocalAnchor = new Vector2(0.9f, 0f);

    [Header("줄 시각화 (선택)")]
    [SerializeField] private LineRenderer leftRopeLine;
    [SerializeField] private LineRenderer rightRopeLine;

    private Rigidbody2D rb;
    private Vector3 leftAnchorWorldPosition;
    private Vector3 rightAnchorWorldPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void Spawned()
    {
        // 이 시점엔 Runner.Spawn에 넘긴 실제 스폰 위치가 이미 반영되어 있다.
        leftAnchorWorldPosition = ConfigureJoint(leftAnchor, leftLocalAnchor);
        rightAnchorWorldPosition = ConfigureJoint(rightAnchor, rightLocalAnchor);
    }

    public override void Render()
    {
        UpdateRopeLine(leftRopeLine, leftAnchorWorldPosition, leftLocalAnchor);
        UpdateRopeLine(rightRopeLine, rightAnchorWorldPosition, rightLocalAnchor);
    }

    private Vector3 ConfigureJoint(Transform anchor, Vector2 localAnchor)
    {
        if (anchor == null)
        {
            Debug.LogWarning($"[{nameof(RopePlank)}] 고정점이 비어있습니다.", this);
            return transform.position;
        }

        Vector3 anchorWorldPosition = anchor.position;
        Vector3 attachWorldPosition = transform.TransformPoint(localAnchor);

        DistanceJoint2D joint = gameObject.AddComponent<DistanceJoint2D>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedBody = null; // null이면 connectedAnchor를 월드 좌표로 해석한다.
        joint.anchor = localAnchor;
        joint.connectedAnchor = anchorWorldPosition;
        joint.autoConfigureDistance = false;
        // 줄 길이 = 에디터에서 배치해둔 그대로의 거리. 이게 고정 길이가 되어, 판자가
        // 흔들려도 이 거리보다 늘어나거나 줄어들지 않는다(뻣뻣한 줄처럼 동작).
        joint.distance = Vector3.Distance(anchorWorldPosition, attachWorldPosition);
        joint.maxDistanceOnly = false;

        return anchorWorldPosition;
    }

    private void UpdateRopeLine(LineRenderer line, Vector3 anchorWorldPosition, Vector2 localAnchor)
    {
        if (line == null)
            return;

        line.positionCount = 2;
        line.SetPosition(0, anchorWorldPosition);
        line.SetPosition(1, transform.TransformPoint(localAnchor));
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (leftAnchor != null)
            Gizmos.DrawLine(leftAnchor.position, transform.TransformPoint(leftLocalAnchor));
        if (rightAnchor != null)
            Gizmos.DrawLine(rightAnchor.position, transform.TransformPoint(rightLocalAnchor));
    }
#endif
}
