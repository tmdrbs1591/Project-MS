using UnityEngine;

/// <summary>
/// 패럴랙스 스크롤링. 카메라가 움직이면 이 오브젝트를 카메라 이동량의 일정 비율만큼 따라 움직여서
/// 멀리 있는 배경일수록 느리게 지나가는 것처럼 보이게 한다.
///
/// [factor 의미]
///   - 0   : 따라가지 않음 = 일반 오브젝트처럼 월드에 고정 (가장 가까움)
///   - 0.5 : 카메라의 절반 속도로 흘러감
///   - 1   : 카메라와 똑같이 움직임 = 화면에 붙어있음 (하늘처럼 무한히 먼 것)
///   - 음수 : 카메라 반대로 더 빨리 움직임 (캐릭터보다 앞에 있는 전경용)
///
/// [씬 설정]
///   - 배경 레이어(하늘, 먼 산, 성, 가까운 바위 …)마다 부모 오브젝트에 붙이고 factor 를 다르게 준다.
///     예) 하늘 0.95 / 먼 산 0.8 / 공중섬 0.6 / 건물 0.3 / 바닥 0
///   - 카메라는 자동으로 찾는다(렌더텍스처로 그리는 보조 카메라는 제외).
///   - 카메라를 처음 찾은 시점의 위치를 기준으로 하므로, 에디터에서 보이는 배치가 그대로 시작 화면이 된다.
/// </summary>
// 카메라 이동 스크립트(LateUpdate)가 끝난 뒤에 따라가야 떨림이 없다.
[DefaultExecutionOrder(1000)]
public class ParallaxLayer : MonoBehaviour
{
    [Tooltip("카메라 이동을 따라가는 비율. 0 = 월드 고정(가까움), 1 = 화면 고정(무한히 멂)")]
    [SerializeField] private Vector2 factor = new Vector2(0.5f, 0.5f);

    private Transform cameraTransform;
    private Vector3 startPosition;
    private Vector3 cameraStartPosition;

    private void Start()
    {
        startPosition = transform.position;
        TryBindCamera();
    }

    private void LateUpdate()
    {
        // 카메라가 늦게 생성되는 씬(게임 씬 등)에 대비해 못 찾았으면 계속 다시 찾는다.
        if (cameraTransform == null && !TryBindCamera())
            return;

        Vector3 cameraDelta = cameraTransform.position - cameraStartPosition;
        transform.position = new Vector3(
            startPosition.x + cameraDelta.x * factor.x,
            startPosition.y + cameraDelta.y * factor.y,
            startPosition.z);
    }

    private bool TryBindCamera()
    {
        Camera cam = FindScreenCamera();
        if (cam == null)
            return false;

        cameraTransform = cam.transform;
        cameraStartPosition = cameraTransform.position;
        return true;
    }

    // 로비엔 WaterCamera 처럼 MainCamera 태그가 붙은 보조 카메라도 있어서 Camera.main 을 그대로 믿지 않는다.
    // 렌더텍스처로 그리는 카메라는 빼고, MainCamera 태그 → depth 높은 순으로 실제 화면 카메라를 고른다.
    private static Camera FindScreenCamera()
    {
        Camera best = null;
        foreach (Camera cam in Camera.allCameras)
        {
            if (cam.targetTexture != null)
                continue;

            if (best == null)
            {
                best = cam;
                continue;
            }

            bool camIsMain = cam.CompareTag("MainCamera");
            bool bestIsMain = best.CompareTag("MainCamera");
            if (camIsMain != bestIsMain ? camIsMain : cam.depth > best.depth)
                best = cam;
        }
        return best;
    }
}
