using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 옛(Built-in) 파티클 셰이더(Particles/Standard Unlit 등)를 쓰는 이펙트 프리팹을 URP 2D에서 띄울 때 쓰는 변환기.
/// 옛 셰이더는 URP 2D 렌더러에서 소팅 레이어/순서를 무시하고 맨 나중에(맨 앞에) 그려져서, "카드 뒤 후광"처럼
/// 순서가 중요한 곳에서는 URP 파티클 셰이더로 바꿔 끼워야 순서대로 그려진다.
/// 텍스처·색·더하기/반투명 방식은 원본을 그대로 따라간다(원본 머티리얼은 건드리지 않고 복사본을 캐시).
/// </summary>
public static class UrpParticleMaterial
{
    private static readonly Dictionary<Material, Material> cache = new Dictionary<Material, Material>();
    private static Shader urpShader;

    public static Material Convert(Material src)
    {
        if (src == null)
            return null;
        string name = src.shader.name;
        if (name.StartsWith("Universal Render Pipeline") || name.StartsWith("Shader Graphs") || name.StartsWith("LoadComplete") || name.StartsWith("Custom"))
            return src;
        if (cache.TryGetValue(src, out Material m) && m != null)
            return m;

        if (urpShader == null)
            urpShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (urpShader == null)
            return src;

        m = new Material(urpShader) { name = src.name + " (URP)" };
        if (src.HasProperty("_MainTex"))
        {
            m.SetTexture("_BaseMap", src.GetTexture("_MainTex"));
            m.SetTextureScale("_BaseMap", src.GetTextureScale("_MainTex"));
            m.SetTextureOffset("_BaseMap", src.GetTextureOffset("_MainTex"));
        }
        Color tint = src.HasProperty("_TintColor") ? src.GetColor("_TintColor") * 2f
                   : src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white;
        m.SetColor("_BaseColor", tint);

        bool additive = name.Contains("Additive") || (src.HasProperty("_DstBlend") && Mathf.Approximately(src.GetFloat("_DstBlend"), 1f));
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", additive ? 2f : 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        if (additive) m.EnableKeyword("_BLENDMODE_ADD");
        m.renderQueue = 3000;
        cache[src] = m;
        return m;
    }

    /// <summary>오브젝트 안의 모든 파티클 렌더러 머티리얼을 URP용으로 바꿔 끼운다.</summary>
    public static void ConvertAll(GameObject go)
    {
        foreach (ParticleSystemRenderer r in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            r.sharedMaterial = Convert(r.sharedMaterial);
            if (r.trailMaterial != null)
                r.trailMaterial = Convert(r.trailMaterial);
        }
    }
}
