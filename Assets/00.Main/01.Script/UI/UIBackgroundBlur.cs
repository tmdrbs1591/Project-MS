using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 팝업이 열려 있는 동안 뒤의 게임 화면(로비)을 흐리게 보여주는 배경 블러.
///
/// 2D 렌더러는 깊이 기반 후처리(피사계 심도)를 못 써서, 메인 카메라를 그대로 따라다니는 보조 카메라로
/// 화면을 낮은 해상도로 한 번 더 그린 뒤 Kawase 블러를 몇 번 걸어 전체 화면 RawImage에 띄운다.
/// 화면은 계속 움직이는 그대로(실시간) 흐려진다. 블러가 꺼져 있을 땐 보조 카메라도 꺼서 비용이 없다.
///
/// [씬 설정]
///   - Screen Space - Overlay 캔버스에 전체 화면 RawImage를 만들고 이 스크립트를 붙인다.
///     흐려진 배경 위에 HUD/팝업이 그려지도록 캔버스에서 앞쪽(먼저 그려지는) 순서에 둔다.
///   - blurMaterial: Hidden/UI/KawaseBlur 셰이더 머티리얼.
///   - 팝업(CharacterSelectUI 등)이 SetTarget(0~1)으로 켜고 끈다. 값은 부드럽게 따라가서
///     "샤라락" 하고 흐려졌다가 풀린다.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class UIBackgroundBlur : MonoBehaviour
{
    [SerializeField] private Material blurMaterial;
    [Tooltip("화면 해상도를 이 값으로 나눈 크기로 그린다. 클수록 싸고 더 뭉개진다.")]
    [Range(1, 8)] [SerializeField] private int downsample = 2;
    [Range(1, 8)] [SerializeField] private int iterations = 3;
    [Tooltip("블러가 최대로 켜졌을 때 한 번에 퍼지는 거리(텍셀).")]
    [SerializeField] private float maxSpread = 0.8f;
    [Tooltip("흐려진 배경에 곱할 색(살짝 어둡게 하면 위의 UI가 더 잘 보인다).")]
    [SerializeField] private Color tint = new Color(0.92f, 0.94f, 1f, 1f);
    [Tooltip("목표 세기로 따라가는 속도. 작을수록 천천히 샤라락 흐려진다.")]
    [Min(0.1f)] [SerializeField] private float fadeSpeed = 5f;

    private RawImage rawImage;
    private Camera blurCamera;
    private RenderTexture sourceRT;
    private RenderTexture pingRT;
    private RenderTexture pongRT;
    private float target;
    private float amount;

    private static readonly int OffsetId = Shader.PropertyToID("_Offset");

    /// <summary>0이면 꺼짐, 1이면 최대로 흐림. 바로 바뀌지 않고 부드럽게 따라간다.</summary>
    public void SetTarget(float value)
    {
        target = Mathf.Clamp01(value);
    }

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
        rawImage.raycastTarget = false;
        rawImage.enabled = false;
    }

    private void OnDestroy()
    {
        ReleaseTextures();
        if (blurCamera != null)
            Destroy(blurCamera.gameObject);
    }

    private void LateUpdate()
    {
        float k = 1f - Mathf.Exp(-fadeSpeed * Time.unscaledDeltaTime);
        amount = Mathf.Lerp(amount, target, k);
        if (target <= 0f && amount < 0.005f)
            amount = 0f;

        bool active = amount > 0f && blurMaterial != null && Camera.main != null;
        rawImage.enabled = active;
        if (blurCamera != null)
            blurCamera.enabled = active;
        if (!active)
            return;

        EnsureResources();
        SyncCamera(Camera.main);
        Blur();

        Color c = tint;
        c.a = amount;
        rawImage.color = c;
    }

    private void EnsureResources()
    {
        int w = Mathf.Max(1, Screen.width / downsample);
        int h = Mathf.Max(1, Screen.height / downsample);
        if (sourceRT == null || sourceRT.width != w || sourceRT.height != h)
        {
            ReleaseTextures();
            sourceRT = new RenderTexture(w, h, 16, RenderTextureFormat.Default) { name = "BackgroundBlur Source" };
            pingRT = new RenderTexture(w, h, 0, RenderTextureFormat.Default) { name = "BackgroundBlur Ping" };
            pongRT = new RenderTexture(w, h, 0, RenderTextureFormat.Default) { name = "BackgroundBlur Pong" };
            if (blurCamera != null)
                blurCamera.targetTexture = sourceRT;
        }

        if (blurCamera == null)
        {
            GameObject go = new GameObject("Background Blur Camera");
            go.hideFlags = HideFlags.DontSave;
            blurCamera = go.AddComponent<Camera>();
            blurCamera.CopyFrom(Camera.main);
            blurCamera.targetTexture = sourceRT;
            // 메인 카메라보다 먼저 그려서 같은 프레임 안에 결과가 나오게 한다.
            blurCamera.depth = Camera.main.depth - 1f;
        }
    }

    private void SyncCamera(Camera main)
    {
        Transform t = blurCamera.transform;
        t.SetPositionAndRotation(main.transform.position, main.transform.rotation);
        blurCamera.orthographic = main.orthographic;
        blurCamera.orthographicSize = main.orthographicSize;
        blurCamera.fieldOfView = main.fieldOfView;
        blurCamera.cullingMask = main.cullingMask;
        blurCamera.backgroundColor = main.backgroundColor;
        blurCamera.clearFlags = main.clearFlags;
    }

    /// <summary>offset을 점점 키워가며 Kawase 블러를 반복한다. amount가 작을 땐 덜 퍼져서
    /// 선명 → 흐림으로 자연스럽게 번지는 느낌이 난다.</summary>
    private void Blur()
    {
        RenderTexture src = sourceRT;
        RenderTexture dst = pingRT;
        for (int i = 0; i < iterations; i++)
        {
            blurMaterial.SetFloat(OffsetId, (i + 0.5f) * maxSpread * amount);
            Graphics.Blit(src, dst, blurMaterial);
            src = dst;
            dst = dst == pingRT ? pongRT : pingRT;
        }

        rawImage.texture = src;
    }

    private void ReleaseTextures()
    {
        foreach (RenderTexture rt in new[] { sourceRT, pingRT, pongRT })
        {
            if (rt != null)
            {
                rt.Release();
                Destroy(rt);
            }
        }
        sourceRT = pingRT = pongRT = null;
    }
}
