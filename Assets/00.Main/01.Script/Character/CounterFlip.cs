using UnityEngine;

/// <summary>
/// 부모(플레이어)가 방향전환으로 scale.x 를 뒤집어도, 이 오브젝트는 항상 똑바로 보이게
/// 부모의 뒤집힘을 상쇄한다. (UI 캔버스, 이름표 등 좌우 반전되면 안 되는 것에 붙인다)
///
/// 플레이어 방향전환이 루트 scale.x = ±1 로 처리되므로, 그 자식인 Key UI 캔버스의
/// 글자가 거울처럼 뒤집히는 문제를 막아준다.
/// </summary>
[DisallowMultipleComponent]
public class CounterFlip : MonoBehaviour
{
    private Vector3 baseLocalScale;

    private void Awake()
    {
        baseLocalScale = transform.localScale;
    }

    // 부모의 방향전환은 Update 에서 일어나므로, 그 뒤인 LateUpdate 에서 상쇄한다.
    private void LateUpdate()
    {
        Transform parent = transform.parent;
        if (parent == null) return;

        // 부모의 월드 스케일 부호를 보고, 내 로컬 x 부호를 반대로 맞춰
        // 최종(월드) x 가 항상 양수가 되게 한다 → 절대 뒤집히지 않음.
        float parentSignX = Mathf.Sign(parent.lossyScale.x);

        Vector3 s = baseLocalScale;
        s.x = Mathf.Abs(baseLocalScale.x) * parentSignX;
        transform.localScale = s;
    }
}
