using UnityEngine;

/// <summary>스프라이트를 우주선처럼 위아래로 둥둥 띄운다. 공중 캐릭터(체이서/팝업)의
/// 스프라이트 오브젝트에 붙이면 끝. 보이는 위치만 흔들 뿐 콜라이더/물리와는 무관하다.</summary>
public class HoverBob : MonoBehaviour
{
    [Tooltip("위아래로 움직이는 폭(월드 유닛). 부모 스케일이 0.05든 5든 실제로 보이는 폭은 같다.")]
    [SerializeField] private float amount = 0.15f;
    [SerializeField] private float speed = 3f;

    private Vector3 basePosition;

    private void Awake() => basePosition = transform.localPosition;

    private void LateUpdate()
    {
        // amount는 월드 유닛이라 부모 스케일로 나눠서 로컬 오프셋으로 바꾼다.
        // (이걸 안 하면 스케일 0.055짜리 부모 밑에서는 눈에 안 보일 만큼만 움직인다)
        float parentScale = transform.parent != null ? transform.parent.lossyScale.y : 1f;
        if (Mathf.Approximately(parentScale, 0f))
            parentScale = 1f;

        float offset = Mathf.Sin(Time.time * speed) * amount / parentScale;
        transform.localPosition = basePosition + Vector3.up * offset;
    }
}
