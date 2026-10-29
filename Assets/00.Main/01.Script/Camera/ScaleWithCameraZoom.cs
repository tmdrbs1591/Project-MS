using UnityEngine;

/// <summary>
/// 카메라 자식으로 붙은 월드 공간 연출(VS 배경 파티클 등)을 카메라 줌(orthographicSize)에 맞춰 키우고 줄인다.
/// 게임 카메라는 두 캐릭터를 다 담으려고 줌아웃하는데, 기준 크기(referenceSize)에 맞춰 만든 연출은 그만큼
/// 작아 보여서 뒤의 맵이 비친다. 줌 비율만큼 크기와 카메라 기준 위치를 같이 늘려서 늘 같은 크기로 보이게 한다.
///
/// [씬 설정]
///   - 카메라의 자식인 연출 루트에 붙인다. referenceSize는 연출을 만들 때의 카메라 크기(보통 5).
///   - 자식 파티클이 Local 스케일 모드면 부모 크기를 무시하므로 실행 시 Hierarchy 모드로 바꾼다
///     (기준 크기에서는 모양이 그대로다).
///   - Screen Space - Camera 캔버스는 원래 화면에 맞춰지므로 영향받지 않는다.
///   - fitTo(선택): 화면을 꽉 채울 배경 파티클. 넣으면 줌 비율 대신 "그 배경이 화면에 딱 맞는" 크기로 맞추고
///     배경 중심을 화면 중심에 맞춘다(fitMargin으로 살짝 더 크게 해서 가장자리 틈을 막는다).
/// </summary>
[DefaultExecutionOrder(1000)] // 카메라 줌(TwoPlayerCamera.LateUpdate)이 끝난 뒤에 맞춘다.
public class ScaleWithCameraZoom : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [Min(0.01f)] [SerializeField] private float referenceSize = 5f;
    [Tooltip("화면에 딱 맞출 배경 파티클(비우면 줌 비율만큼만 키운다).")]
    [SerializeField] private ParticleSystem fitTo;
    [Tooltip("딱 맞춘 크기에 곱하는 여유. 1이면 정확히 화면 크기, 조금 크게 하면 가장자리 틈이 안 보인다.")]
    [Min(1f)] [SerializeField] private float fitMargin = 1.01f;

    private Vector2 fitSize;     // 크기 1일 때 배경의 가로/세로(월드 단위)
    private Vector2 fitOffset;   // 크기 1일 때 카메라 중심에서 배경 중심까지

    private Vector3 baseLocalScale;
    private Vector3 baseLocalPosition;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponentInParent<Camera>();
        baseLocalScale = transform.localScale;
        baseLocalPosition = transform.localPosition;

        if (fitTo != null && targetCamera != null)
        {
            ParticleSystem.MainModule fitMain = fitTo.main;
            Vector3 lossy = fitTo.transform.lossyScale;
            float w = fitMain.startSize3D ? fitMain.startSizeX.constantMax : fitMain.startSize.constantMax;
            float h = fitMain.startSize3D ? fitMain.startSizeY.constantMax : fitMain.startSize.constantMax;
            fitSize = new Vector2(w * Mathf.Abs(lossy.x), h * Mathf.Abs(lossy.y));
            Vector3 offset = targetCamera.transform.InverseTransformPoint(fitTo.transform.position);
            fitOffset = new Vector2(offset.x, offset.y);
        }

        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = ps.main;
            if (main.scalingMode == ParticleSystemScalingMode.Local)
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null || !targetCamera.orthographic)
            return;

        float ratio = targetCamera.orthographicSize / referenceSize;
        Vector2 shift = Vector2.zero;
        if (fitTo != null && fitSize.x > 0f && fitSize.y > 0f)
        {
            // 화면을 빈틈없이 덮는 가장 작은 크기(가로/세로 중 더 모자란 쪽 기준).
            float viewH = targetCamera.orthographicSize * 2f;
            float viewW = viewH * targetCamera.aspect;
            ratio = Mathf.Max(viewW / fitSize.x, viewH / fitSize.y) * fitMargin;
            shift = -fitOffset; // 배경 중심을 화면 중심으로
        }

        transform.localScale = baseLocalScale * ratio;
        // 카메라 기준 위치도 같은 비율로(z는 카메라와의 거리라 그대로 둔다).
        transform.localPosition = new Vector3(
            (baseLocalPosition.x + shift.x) * ratio,
            (baseLocalPosition.y + shift.y) * ratio,
            baseLocalPosition.z);
    }
}
