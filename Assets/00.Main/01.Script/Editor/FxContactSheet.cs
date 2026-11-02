#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 이펙트 프리팹 여러 개를 빈 씬에 격자로 늘어놓고, 지정한 시간대마다 파티클을 시뮬레이션해 PNG 한 장(콘택트 시트)으로 찍는다.
/// 이펙트를 고를 때 이름만 보지 않고 실제 모습을 나란히 비교하려는 에디터 전용 도구.
/// </summary>
public static class FxContactSheet
{
    public static string Render(string name, IList<string> prefabPaths, float[] times, int columns = 6, float cell = 5f, int cellPx = 260)
    {
        string prevScene = EditorSceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Cam");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.16f, 0.12f, 0.24f, 1f);   // 게임 배경 비슷한 보라 톤
        int rows = Mathf.CeilToInt(prefabPaths.Count / (float)columns);
        cam.orthographicSize = rows * cell * 0.5f;
        cam.transform.position = new Vector3((columns - 1) * cell * 0.5f, -(rows - 1) * cell * 0.5f, -20f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;

        var spawned = new List<GameObject>();
        for (int i = 0; i < prefabPaths.Count; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
            if (prefab == null) { spawned.Add(null); continue; }
            var go = (GameObject)Object.Instantiate(prefab);
            go.transform.position = new Vector3((i % columns) * cell, -(i / columns) * cell, 0f);
            UrpParticleMaterial.ConvertAll(go);
            spawned.Add(go);
        }

        var rt = new RenderTexture(columns * cellPx, rows * cellPx, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        var outPaths = new List<string>();
        foreach (float t in times)
        {
            foreach (var go in spawned)
            {
                if (go == null) continue;
                foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
                {
                    if (ps.transform.parent != null && ps.transform.parent.GetComponentInParent<ParticleSystem>() != null)
                        continue;   // 자식 시스템은 부모 Simulate(withChildren)로 같이 돈다
                    ps.Simulate(t, true, true, true);
                }
            }
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            string path = $"Temp/fx_{name}_{Mathf.RoundToInt(t * 100):D3}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            outPaths.Add(path);
        }
        cam.targetTexture = null;
        rt.Release();

        if (!string.IsNullOrEmpty(prevScene))
            EditorSceneManager.OpenScene(prevScene);
        return string.Join(",", outPaths);
    }
}
#endif
