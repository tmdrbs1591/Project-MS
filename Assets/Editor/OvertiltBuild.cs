using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 배치모드 빌드용 스크립트: OVERTILT 이름/아이콘/씬 순서(Lobby가 0번)를 적용해서 Windows 64bit로 빌드한다.
public static class OvertiltBuild
{
    private const string ProductName = "OVERTILT";
    private const string IconPath = "Assets/00.Main/02.Art/Sprite/OVERTILT_Icon.png";

    // 빌드 인덱스 0번이 Lobby(첫 씬).
    private static readonly string[] Scenes =
    {
        "Assets/00.Main/08.Scene/Lobby.unity",
        "Assets/00.Main/08.Scene/SampleScene.unity",
        "Assets/00.Main/08.Scene/LoadingScene.unity",
    };

    public static void Build()
    {
        int exitCode = 1;
        try
        {
            string outDir = GetArg("-overtiltOut") ?? "Builds/OVERTILT";
            Directory.CreateDirectory(outDir);
            string exePath = Path.Combine(outDir, ProductName + ".exe");

            foreach (string scene in Scenes)
            {
                if (!File.Exists(scene))
                    throw new FileNotFoundException("씬 파일을 찾을 수 없습니다: " + scene);
            }

            PlayerSettings.productName = ProductName;
            ApplyIcon();

            EditorBuildSettings.scenes = Scenes
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log($"[OvertiltBuild] result={summary.result} errors={summary.totalErrors} warnings={summary.totalWarnings} " +
                      $"size={summary.totalSize / (1024 * 1024)}MB time={summary.totalTime} output={summary.outputPath}");

            exitCode = summary.result == BuildResult.Succeeded ? 0 : 1;
        }
        catch (Exception ex)
        {
            Debug.LogError("[OvertiltBuild] 빌드 중 예외: " + ex);
            exitCode = 1;
        }

        EditorApplication.Exit(exitCode);
    }

    private static void ApplyIcon()
    {
        var importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
        if (importer == null)
            throw new FileNotFoundException("아이콘 에셋을 찾을 수 없습니다: " + IconPath);

        importer.textureType = TextureImporterType.Default;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon == null)
            throw new InvalidOperationException("아이콘 텍스처 로드 실패: " + IconPath);

        // 기본 아이콘(모든 플랫폼 공통)
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

        // Windows(Standalone) 앱 아이콘도 모든 사이즈 슬롯에 같은 아이콘을 명시적으로 지정
        int slotCount = PlayerSettings.GetIcons(NamedBuildTarget.Standalone, IconKind.Application).Length;
        if (slotCount > 0)
        {
            Texture2D[] slots = Enumerable.Repeat(icon, slotCount).ToArray();
            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, slots, IconKind.Application);
        }
    }

    private static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }
        return null;
    }
}

