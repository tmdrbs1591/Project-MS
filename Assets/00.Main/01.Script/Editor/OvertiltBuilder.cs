using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// OVERTILT 윈도우 빌드. 제품명/아이콘을 맞추고, Lobby를 첫 씬으로 Builds/OVERTILT/OVERTILT.exe 에 빌드한다.
/// 에디터 메뉴(Tools/OVERTILT/Build Windows) 또는 배치모드(-executeMethod OvertiltBuilder.BuildWindows)로 실행.
/// </summary>
public static class OvertiltBuilder
{
    private const string ProductName = "OVERTILT";
    private const string IconPath = "Assets/00.Main/02.Art/Sprite/OVERTILT_Icon.png";
    private const string FirstScenePath = "Assets/00.Main/08.Scene/Lobby.unity";
    private const string OutputDir = "Builds/OVERTILT";

    [MenuItem("Tools/OVERTILT/Build Windows")]
    public static void BuildWindows()
    {
        PlayerSettings.productName = ProductName;

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon != null)
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        else
            Debug.LogWarning($"[OvertiltBuilder] 아이콘을 못 찾았습니다: {IconPath}");

        List<string> scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
        scenes.Remove(FirstScenePath);
        scenes.Insert(0, FirstScenePath);

        string exePath = Path.GetFullPath(Path.Combine(OutputDir, ProductName + ".exe"));
        if (Directory.Exists(Path.GetDirectoryName(exePath)))
            Directory.Delete(Path.GetDirectoryName(exePath), true);

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes.ToArray(),
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None,
        });

        bool ok = report.summary.result == BuildResult.Succeeded;
        Debug.Log($"[OvertiltBuilder] {(ok ? "성공" : "실패")}: {exePath} ({report.summary.totalSize / (1024 * 1024)} MB, 오류 {report.summary.totalErrors})");

        if (Application.isBatchMode)
            EditorApplication.Exit(ok ? 0 : 1);
    }
}
