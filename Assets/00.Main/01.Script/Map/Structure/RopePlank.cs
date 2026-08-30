using Fusion;
using UnityEngine;

/// <summary>
/// 줄 두 가닥(좌/우)에 매달려 중력을 받으며 흔들리는 판자다. (Fusion 2 / Shared 모드)
///
/// [동작]
///   - HingeJoint2D 두 개를 코드로 생성해서 leftAnchor/rightAnchor(씬의 고정점)에 매단다.
///     그 이후 중력/흔들림/플레이어가 올라탔을 때의 반응은 전부 Unity 물리 엔진이 처리한다 —
///     별도의 Update 로직이 필요 없다.
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

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ConfigureJoint(leftAnchor, leftLocalAnchor);
        ConfigureJoint(rightAnchor, rightLocalAnchor);
    }

    private void ConfigureJoint(Transform anchor, Vector2 localAnchor)
    {
        if (anchor == null)
        {
            Debug.LogWarning($"[{nameof(RopePlank)}] 고정점이 비어있습니다.", this);
            return;
        }

        HingeJoint2D joint = gameObject.AddComponent<HingeJoint2D>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedBody = null; // null이면 connectedAnchor를 월드 좌표로 해석한다.
        joint.anchor = localAnchor;
        joint.connectedAnchor = anchor.position;
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
