using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

namespace JudgementCalendar.EditorTools
{
    public static class SceneBuilder
    {
        public static void BuildAndSave()
        {
            PlayerSettings.companyName = "Mkxxuan";
            PlayerSettings.productName = "审判日历";

            ConfigureArtImports();
            ApplyAppIcon();

            var sceneDir = "Assets/Scenes";
            if (!Directory.Exists(sceneDir)) Directory.CreateDirectory(sceneDir);

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var go = new GameObject("GameBootstrap");
            go.AddComponent<GameBootstrap>();

            EditorSceneManager.SaveScene(scene, sceneDir + "/Main.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBuilder] 场景构建完成: " + sceneDir + "/Main.unity");
        }

        public static void BuildWindowsPlayer()
        {
            ConfigureArtImports();
            ApplyAppIcon();

            var report = BuildPipeline.BuildPlayer(
                new[] { "Assets/Scenes/Main.unity" },
                "Build/Windows/审判日历.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);
            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log("[BuildPlayer] 打包成功: " + report.summary.outputPath + " (" + report.summary.totalSize / (1024 * 1024) + " MB)");
            else
                Debug.LogError("[BuildPlayer] 打包失败: " + report.summary.result);
        }

        // 概念图素材：点采样、关 mipmap、可读（运行时改 filterMode / 建 Sprite 需要）
        static void ConfigureArtImports()
        {
            string artDir = "Assets/Resources/Art";
            if (!Directory.Exists(artDir)) return;
            foreach (var file in Directory.GetFiles(artDir, "*.png"))
                ConfigureImport(file.Replace('\\', '/'));
            foreach (var file in Directory.GetFiles(artDir, "*.jpg"))
                ConfigureImport(file.Replace('\\', '/'));
            AssetDatabase.Refresh();
        }

        static void ConfigureImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            bool wantReadable = path.Contains("emblem"); // 图标纹理需可读供 Unity 缩放生成各尺寸 icon
            bool dirty = false;
            if (importer.isReadable != wantReadable) { importer.isReadable = wantReadable; dirty = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.filterMode != FilterMode.Point)
            {
                settings.filterMode = FilterMode.Point;
                importer.SetTextureSettings(settings);
            }
            if (dirty) importer.SaveAndReimport();
        }

        // 天平徽章 → Windows 应用图标（含任务栏/exe 图标）
        static void ApplyAppIcon()
        {
            const string iconPath = "Assets/Resources/Art/emblem.jpg";
            if (!File.Exists(iconPath))
            {
                Debug.LogWarning("[SceneBuilder] 未找到图标素材 emblem.jpg，跳过图标设置");
                return;
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (tex == null)
            {
                AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceUpdate);
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            }
            if (tex == null)
            {
                Debug.LogWarning("[SceneBuilder] 图标纹理加载失败，跳过图标设置");
                return;
            }
            try
            {
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, new[] { tex });
                Debug.Log("[SceneBuilder] 应用图标已设置: " + iconPath);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[SceneBuilder] 图标设置失败（跳过）: " + e.Message);
            }
        }
    }
}
