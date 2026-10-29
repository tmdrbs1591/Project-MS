using UnityEngine;

/// <summary>
/// 로비 건물/포탈 앞으로 가면 살짝 커지면서, 밝은 외곽선이 아래에서부터 양옆으로 쫙 올라가 위에서 만난다.
/// 떠나면 외곽선은 빠르게 사라지고 크기도 돌아온다.
///
/// [동작]
///   - 원본 SpriteRenderer보다 조금 큰 사각형 메시를 자식으로 만들어 원본 바로 뒤에 깔고
///     Custom/Sprite/OutlineReveal 셰이더로 그린다(원본이 안쪽을 덮어서 테두리만 보인다).
///   - 스프라이트가 애니메이션으로 바뀌어도(포탈) 매 프레임 따라간다.
///   - 커질 때는 바닥을 기준으로 커진다(땅에서 뜨지 않게).
///
/// [씬 설정]
///   - 건물/포탈 오브젝트(SpriteRenderer, Collider2D가 있는 곳)에 붙이고 outlineShader를 연결한다.
/// </summary>
public class LobbyHighlight : MonoBehaviour
{
    [SerializeField] private Shader outlineShader;
    [SerializeField] private SpriteRenderer target;

    [Header("외곽선")]
    [Tooltip("끄면 외곽선 없이 커지기만 한다(포탈).")]
    [SerializeField] private bool showOutline = true;
    [ColorUsage(true, true)] [SerializeField] private Color outlineColor = new Color(1f, 0.95f, 0.75f, 1f);
    [ColorUsage(true, true)] [SerializeField] private Color glowColor = new Color(0.65f, 0.6f, 1f, 0.55f);
    [Tooltip("외곽선 두께(월드 단위).")]
    [SerializeField] private float thickness = 0.045f;
    [SerializeField] private float revealDuration = 0.55f;

    [Header("크기")]
    [SerializeField] private float scaleUp = 0.045f;
    [SerializeField] private float scaleDuration = 0.4f;

    [Header("사운드")]
    [Tooltip("앞으로 다가갔을 때 한 번 나는 소리.")]
    [SerializeField] private AudioClip nearSound;
    [Range(0f, 1f)] [SerializeField] private float nearVolume = 0.35f;

    [Header("감지")]
    [Tooltip("캐릭터가 콜라이더 가로 범위 안(+여유)에 들어오면 켠다.")]
    [SerializeField] private float extraRange = 0.1f;

    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int UVRectId = Shader.PropertyToID("_UVRect");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    private Collider2D area;
    private MeshRenderer outlineRenderer;
    private Mesh mesh;
    private Material material;
    private Sprite builtFor;
    private Vector3 baseScale;
    private Vector3 basePosition;
    private bool near;
    private float nearSince;
    private float scaleT;
    private float alpha;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<SpriteRenderer>();
        area = GetComponent<Collider2D>();
        baseScale = transform.localScale;
        basePosition = transform.localPosition;
        if (target == null)
        {
            enabled = false;
            return;
        }
        if (!showOutline || outlineShader == null)
            return;

        var go = new GameObject("Outline");
        go.transform.SetParent(target.transform, false);
        mesh = new Mesh { name = "Lobby Outline" };
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        outlineRenderer = go.AddComponent<MeshRenderer>();
        material = new Material(outlineShader);
        material.SetColor("_OutlineColor", outlineColor);
        material.SetColor("_GlowColor", glowColor);
        outlineRenderer.sharedMaterial = material;
        outlineRenderer.sortingLayerID = target.sortingLayerID;
        outlineRenderer.sortingOrder = target.sortingOrder - 1;
        outlineRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
        if (mesh != null)
            Destroy(mesh);
    }

    private void LateUpdate()
    {
        bool nowNear = IsPlayerNear();
        if (nowNear && !near)
        {
            nearSince = Time.time;
            SoundManager.Instance?.PlaySfx(nearSound, nearVolume, 0.03f);
        }
        near = nowNear;

        float dt = Time.deltaTime;
        scaleT = Mathf.MoveTowards(scaleT, near ? 1f : 0f, dt / scaleDuration);
        alpha = Mathf.MoveTowards(alpha, near ? 1f : 0f, dt / (near ? 0.12f : 0.22f));

        ApplyScale();
        UpdateOutline();
    }

    private bool IsPlayerNear()
    {
        LobbyCharacterChange player = LobbyCharacterChange.Instance;
        if (player == null || area == null)
            return false;
        Bounds b = area.bounds;
        float x = player.transform.position.x;
        return x >= b.min.x - extraRange && x <= b.max.x + extraRange;
    }

    private void ApplyScale()
    {
        // 살짝 넘쳤다 돌아오는 튕김(나갈 땐 그냥 부드럽게).
        float e;
        if (near)
        {
            const float c1 = 1.7f;
            float x = scaleT - 1f;
            e = 1f + (c1 + 1f) * x * x * x + c1 * x * x;
        }
        else
            e = scaleT * scaleT * (3f - 2f * scaleT);

        float s = 1f + scaleUp * e;
        transform.localScale = baseScale * s;

        // 바닥 기준으로 커지게: 피벗 아래쪽 거리만큼 위로 올려 준다.
        if (target.sprite != null)
        {
            float bottom = -target.sprite.pivot.y / target.sprite.pixelsPerUnit * baseScale.y;
            transform.localPosition = basePosition - new Vector3(0f, (s - 1f) * bottom, 0f);
        }
    }

    private void UpdateOutline()
    {
        if (outlineRenderer == null)
            return;
        bool show = alpha > 0.001f && target.sprite != null;
        outlineRenderer.enabled = show;
        if (!show)
            return;

        if (target.sprite != builtFor)
            BuildMesh(target.sprite);

        outlineRenderer.transform.localScale = new Vector3(target.flipX ? -1f : 1f, target.flipY ? -1f : 1f, 1f);

        float t = Mathf.Clamp01((Time.time - nearSince) / revealDuration);
        float progress = near ? 1f - Mathf.Pow(1f - t, 2.2f) : 1.1f;
        // 다 올라간 뒤엔 앞쪽 반짝임이 위로 빠져나가게 조금 더 밀어 준다.
        if (near && t >= 1f)
            progress = 1f + Mathf.Min(0.2f, (Time.time - nearSince - revealDuration) * 0.6f);
        material.SetFloat(ProgressId, progress * 1.02f);
        material.SetFloat(AlphaId, alpha);
    }

    private void BuildMesh(Sprite sprite)
    {
        builtFor = sprite;
        Texture2D tex = sprite.texture;
        // textureRect는 투명 여백이 잘린 영역일 수 있어 피벗과 안 맞는다 → 스프라이트 전체 사각형을 쓴다
        // (로비 건물들은 아틀라스에 묶이지 않은 스프라이트라 rect가 곧 텍스처 안 위치다).
        Rect r = sprite.rect;
        // 메시 경계(Tight)는 잘려 있을 수 있어서 스프라이트 사각형/피벗으로 계산한다.
        var b = new Bounds();
        b.SetMinMax(-sprite.pivot / sprite.pixelsPerUnit, (sprite.rect.size - sprite.pivot) / sprite.pixelsPerUnit);

        // 스프라이트 픽셀 ↔ 월드 비율로 두께를 uv로 바꾼다.
        float worldPerUnit = Mathf.Abs(transform.lossyScale.x);
        float pixelsPerLocal = r.width / b.size.x;
        float radiusPx = thickness / Mathf.Max(0.0001f, worldPerUnit) * pixelsPerLocal;
        material.SetTexture(MainTexId, tex);
        material.SetVector(RadiusId, new Vector4(radiusPx / tex.width, radiusPx / tex.height, 0f, 0f));
        material.SetVector(UVRectId, new Vector4(r.xMin / tex.width, r.yMin / tex.height, r.xMax / tex.width, r.yMax / tex.height));

        // 바깥 빛까지 들어갈 만큼 키운 사각형.
        float pad = radiusPx * 3.2f / pixelsPerLocal;
        Vector2 min = (Vector2)b.min - Vector2.one * pad;
        Vector2 max = (Vector2)b.max + Vector2.one * pad;
        var verts = new[] { new Vector3(min.x, min.y), new Vector3(min.x, max.y), new Vector3(max.x, max.y), new Vector3(max.x, min.y) };
        var uvs = new Vector2[4];
        var heights = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            Vector2 local = verts[i];
            uvs[i] = new Vector2(
                (r.xMin + (local.x - b.min.x) * pixelsPerLocal) / tex.width,
                (r.yMin + (local.y - b.min.y) * pixelsPerLocal) / tex.height);
            heights[i] = new Vector2((local.y - b.min.y) / b.size.y, 0f);
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.uv2 = heights;
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
    }
}
