using System.Collections;
using System.Collections.Generic;
using ProjectMS.CharacterSystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 처치(KO) 순간의 화려한 연출. RoundFinishController가 KO를 감지하면 Play()를 부른다.
///   1) 하얀 섬광 + 충격파 + 불꽃 이펙트가 터지고
///   2) 진 캐릭터가 연기 꼬리를 남기며 빙글빙글 돌면서 처치한 쪽 반대로 멀리 날아가고(카메라가 따라감)
///   3) 화면 밖으로 사라질 때쯤 반짝! 별이 된다.
/// 진짜 캐릭터는 네트워크 상태 그대로 두고(로컬에서 그림만 숨김), 날아가는 건 그림만 복사한 가짜다.
/// 그래서 각 클라가 자기 화면에서 독립적으로 재생해도 게임 진행에는 영향이 없다.
///
/// [씬 설정]
///   - RoundFinishController와 같은 오브젝트(Main Camera)에 붙이고 이펙트 프리팹/소리를 넣는다.
/// </summary>
public class KoFinishEffect : MonoBehaviour
{
    [Header("이펙트")]
    [Tooltip("KO 순간 진 캐릭터 위치에서 터지는 이펙트들(충격파, 섬광 등).")]
    [SerializeField] private GameObject[] impactVfx;
    [Tooltip("날아가다 사라질 때 터지는 반짝임(별) 이펙트.")]
    [SerializeField] private GameObject starVfx;
    [SerializeField] private Texture2D smokeTexture;
    [SerializeField] private Texture2D sparkTexture;
    [Tooltip("이펙트를 그릴 소팅 레이어/순서(캐릭터보다 앞).")]
    [SerializeField] private string vfxSortingLayer = "Default";
    [SerializeField] private int vfxSortingOrder = 150;

    [Header("날아가기")]
    [SerializeField] private Vector2 launchVelocity = new Vector2(24f, 14f);
    [SerializeField] private float gravity = 9f;
    [SerializeField] private float spinSpeed = 1080f;
    [SerializeField] private float flyDuration = 1.9f;
    [Tooltip("날아가는 동안 카메라 줌(orthographicSize).")]
    [SerializeField] private float followZoomSize = 5f;
    [Tooltip("날아가기 시작할 때 가짜 캐릭터 크기 배율(멀리 날아가도 잘 보이게 크게 시작해 줄어든다).")]
    [SerializeField] private float ghostLaunchScale = 1.6f;
    [Tooltip("맞는 순간 이펙트 크기 배율. 가짜 캐릭터가 이펙트보다 앞에 그려지므로 크게 터뜨려도 가려지지 않는다.")]
    [SerializeField] private float hitVfxScale = 1f;
    [Tooltip("날아가는 순간(파악!) 이펙트 크기 — 맞는 순간 이펙트 대비 배율.")]
    [SerializeField] private float launchVfxScale = 1.4f;

    [Header("섬광")]
    [Range(0f, 1f)] [SerializeField] private float flashAlpha = 0.85f;
    [SerializeField] private float flashDuration = 0.22f;

    [Header("사운드")]
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip flyClip;
    [Tooltip("멈췄던 화면이 확 풀리며 날아가는 순간의 \"파악!\" 타격음.")]
    [SerializeField] private AudioClip launchClip;

    private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
    private Material smokeMat;
    private Material sparkMat;
    private Image flashImage;
    private Transform ghost;
    private float launchDir = 1f;

    /// <summary>날아가는 데 걸리는 시간(실시간 초).</summary>
    public float FlyDuration => flyDuration;

    /// <summary>날아가는 가짜 캐릭터(카메라가 따라갈 대상). 연출 중이 아니면 null.</summary>
    public Transform Ghost => ghost;

    /// <summary>KO 연출 1단계: 맞는 순간 섬광·충격 이펙트와 함께 진 캐릭터를 가짜로 바꿔 그 자리에 멈춰 둔다.
    /// 반환값은 카메라가 비출 대상(가짜 캐릭터). 날려 보내는 건 Launch()에서.</summary>
    public Transform Play(CharacterBase loser)
    {
        Cleanup();
        launched = false;
        if (loser == null)
            return null;

        SoundManager.Instance?.PlaySfx(hitClip);
        StartCoroutine(Flash());

        Vector3 at = BodyCenter(loser);
        foreach (GameObject prefab in impactVfx)
            SpawnVfx(prefab, at, 3f, hitVfxScale);

        ghost = BuildGhost(loser);
        HideOriginal(loser);
        launchDir = LaunchDirection(loser);
        StartCoroutine(Shiver(ghost));
        return ghost;
    }

    /// <summary>멈춰 있는 동안: 맞은 캐릭터가 하얗게 번쩍였다가 돌아오며 부르르 떨린다(누가 맞았는지 바로 보이게).</summary>
    private IEnumerator Shiver(Transform g)
    {
        var srs = g.GetComponentsInChildren<SpriteRenderer>();
        var cols = new Color[srs.Length];
        for (int i = 0; i < srs.Length; i++) cols[i] = srs[i].color;
        Vector3 basePos = g.position;
        float t = 0f;
        while (g != null && !launched)
        {
            t += Time.unscaledDeltaTime;
            float white = Mathf.Clamp01(1f - t / 0.25f);
            for (int i = 0; i < srs.Length; i++)
                if (srs[i] != null) srs[i].color = Color.Lerp(cols[i], new Color(3f, 3f, 3f, cols[i].a), white);
            float k = Mathf.Max(0f, 0.12f * (1f - t * 0.8f));
            g.position = basePos + new Vector3(Random.Range(-k, k), Random.Range(-k, k), 0f);
            g.localScale = Vector3.one * (1f + 0.25f * Mathf.Clamp01(t / 0.15f));
            yield return null;
        }
        for (int i = 0; i < srs.Length; i++)
            if (srs[i] != null) srs[i].color = cols[i];
    }

    private bool launched;

    /// <summary>KO 연출 2단계: 멈췄던 화면이 확 풀리며 "파악!" 하고 멀리 날려 보낸다.</summary>
    public void Launch()
    {
        if (ghost == null)
            return;
        launched = true;
        SoundManager.Instance?.PlaySfx(launchClip);
        StartCoroutine(Flash());
        foreach (GameObject prefab in impactVfx)
            SpawnVfx(prefab, ghost.position, 3f, hitVfxScale * launchVfxScale);
        StartCoroutine(Fly(ghost, launchDir));
    }

    /// <summary>숨겼던 진짜 캐릭터 그림을 되돌리고 남은 가짜를 치운다(다음 라운드 시작 전에 부른다).</summary>
    public void Cleanup()
    {
        StopAllCoroutines();
        foreach (Renderer r in hiddenRenderers)
            if (r != null)
                r.enabled = true;
        hiddenRenderers.Clear();
        if (ghost != null)
            Destroy(ghost.gameObject);
        ghost = null;
        if (flashImage != null)
            flashImage.color = new Color(1f, 1f, 1f, 0f);
    }

    private void OnDisable() => Cleanup();

    // ---------------- 흐름 ----------------

    private IEnumerator Fly(Transform g, float dir)
    {
        // 화면이 정상 속도로 풀린 뒤에 날아가므로 실제 시간으로 움직인다(슬로우모션이 남아 있어도 시원하게).
        Vector2 vel = new Vector2(launchVelocity.x * dir, launchVelocity.y);
        float spin = -spinSpeed * dir;
        bool flySoundPlayed = false;
        float t = 0f;
        while (t < flyDuration && g != null)
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f && !flySoundPlayed)
            {
                flySoundPlayed = true;
                SoundManager.Instance?.PlaySfx(flyClip);
            }
            t += dt;
            vel.y -= gravity * dt;
            g.position += (Vector3)(vel * dt);
            g.Rotate(0f, 0f, spin * dt);
            // 멀어지는 느낌: 살짝 작아진다
            g.localScale = Vector3.one * Mathf.Lerp(ghostLaunchScale, 0.8f, t / flyDuration);
            yield return null;
        }

        if (g == null)
            yield break;

        // 반짝! 별이 되어 사라진다.
        SpawnVfx(starVfx, g.position, 2.5f);
        foreach (ParticleSystem ps in g.GetComponentsInChildren<ParticleSystem>())
        {
            ps.transform.SetParent(null, true);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(ps.gameObject, 2f);
        }
        foreach (SpriteRenderer sr in g.GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = false;
    }

    private IEnumerator Flash()
    {
        EnsureFlash();
        for (float t = 0f; t < flashDuration; t += Time.unscaledDeltaTime)
        {
            float k = 1f - t / flashDuration;
            flashImage.color = new Color(1f, 1f, 1f, flashAlpha * k * k);
            yield return null;
        }
        flashImage.color = new Color(1f, 1f, 1f, 0f);
    }

    // ---------------- 만들기 ----------------

    private static float LaunchDirection(CharacterBase loser)
    {
        // 처치한 쪽(살아있는 상대)의 반대 방향으로 날린다.
        foreach (CharacterBase c in CharacterBase.All)
            if (c != null && c != loser && !c.IsDead)
                return Mathf.Sign(loser.transform.position.x - c.transform.position.x + 0.001f);
        return Random.value < 0.5f ? -1f : 1f;
    }

    private static Vector3 BodyCenter(CharacterBase c)
    {
        Bounds b = default;
        bool any = false;
        foreach (SpriteRenderer sr in c.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!sr.enabled || sr.sprite == null) continue;
            if (!any) { b = sr.bounds; any = true; }
            else b.Encapsulate(sr.bounds);
        }
        return any ? b.center : c.transform.position;
    }

    private Transform BuildGhost(CharacterBase loser)
    {
        var root = new GameObject("KO Ghost").transform;
        root.position = BodyCenter(loser);
        foreach (SpriteRenderer src in loser.GetComponentsInChildren<SpriteRenderer>())
        {
            // 적 테두리(EnemyOutline)는 복사하지 않는다 — 날아가는 그림은 본체만.
            if (!src.enabled || !src.gameObject.activeInHierarchy || src.sprite == null || src.name == "Outline")
                continue;
            var go = new GameObject(src.name);
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(src.transform.position, src.transform.rotation);
            go.transform.localScale = src.transform.lossyScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = src.sprite;
            sr.color = src.color;
            sr.flipX = src.flipX;
            sr.flipY = src.flipY;
            sr.sharedMaterial = src.sharedMaterial;
            sr.sortingLayerID = src.sortingLayerID;
            sr.sortingOrder = vfxSortingOrder + 60 + src.sortingOrder;   // 이펙트보다 항상 앞: 캐릭터가 가려지지 않게
        }
        AddTrail(root);
        return root;
    }

    private void HideOriginal(CharacterBase loser)
    {
        foreach (Renderer r in loser.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue;
            r.enabled = false;
            hiddenRenderers.Add(r);
        }
    }

    private void AddTrail(Transform root)
    {
        // 연기 꼬리: 지나간 자리에 뭉게뭉게 퍼지며 사라지는 연기
        smokeMat ??= MakeMaterial(smokeTexture, false);
        var smoke = MakeSystem("Smoke Trail", root, smokeMat);
        var m = smoke.main;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        m.startSize = new ParticleSystem.MinMaxCurve(1.1f, 1.9f);
        m.startSpeed = 0f;
        m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        m.startColor = new ParticleSystem.MinMaxGradient(new Color(0.92f, 0.9f, 0.95f, 0.8f), new Color(0.7f, 0.68f, 0.75f, 0.7f));
        var em = smoke.emission;
        em.rateOverTime = 0f;
        em.rateOverDistance = 9f;
        var size = smoke.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(1f, 1.8f)));
        var col = smoke.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        // 불꽃 꼬리: 노란 불티가 흩날린다
        sparkMat ??= MakeMaterial(sparkTexture, true);
        var spark = MakeSystem("Spark Trail", root, sparkMat);
        var sm = spark.main;
        sm.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        sm.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        sm.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        sm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.4f), new Color(1f, 0.5f, 0.15f));
        var se = spark.emission;
        se.rateOverTime = 0f;
        se.rateOverDistance = 5f;
        var sh = spark.shape;
        sh.enabled = true;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = 0.3f;
        var ss = spark.sizeOverLifetime;
        ss.enabled = true;
        ss.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));
    }

    private ParticleSystem MakeSystem(string name, Transform parent, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;
        main.playOnAwake = true;
        var shape = ps.shape;
        shape.enabled = false;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.sortingLayerName = vfxSortingLayer;
        r.sortingOrder = vfxSortingOrder - 10;
        ps.Play();
        return ps;
    }

    private static Material MakeMaterial(Texture2D tex, bool additive)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        m.SetTexture("_BaseMap", tex);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", additive ? 2f : 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        return m;
    }

    private void SpawnVfx(GameObject prefab, Vector3 at, float life, float scale = 1f)
    {
        if (prefab == null)
            return;
        GameObject go = Instantiate(prefab, new Vector3(at.x, at.y, 0f), Quaternion.identity);
        go.transform.localScale *= scale;
        foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }
        // 옛 파티클 셰이더는 그리는 순서를 무시해 가짜 캐릭터를 덮으므로 URP 셰이더로 바꿔 끼운다.
        UrpParticleMaterial.ConvertAll(go);
        foreach (ParticleSystemRenderer r in go.GetComponentsInChildren<ParticleSystemRenderer>())
        {
            r.sortingLayerName = vfxSortingLayer;
            r.sortingOrder = vfxSortingOrder;
        }
        Destroy(go, life);
    }

    private void EnsureFlash()
    {
        if (flashImage != null)
            return;
        var go = new GameObject("KO Flash Canvas");
        go.transform.SetParent(transform, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 820;
        var img = new GameObject("Flash", typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(go.transform, false);
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        img.raycastTarget = false;
        img.color = new Color(1f, 1f, 1f, 0f);
        flashImage = img;
    }
}
