using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CodedImmersions.Obfuscator.Editor
{
    public sealed class CIObfuscatorBuildManager : IPreprocessBuildWithReport, IPostprocessBuildWithReport, IPostGenerateGradleAndroidProject, IProcessSceneWithReport, IFilterBuildAssemblies
    {
        public int callbackOrder => int.MinValue;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!ShouldRunObfuscator(report, true)) return;
            ClearBuildCache();
        }

        //scene modules only
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (!BuildPipeline.isBuildingPlayer) return;
            if (!ShouldRunObfuscator(report)) return;

            CIObfuscatorSettings settings = CIObfuscatorSettings.instance;
            if (!settings.enableSceneObfuscation) return;

            string guid = AssetDatabase.AssetPathToGUID(scene.path);
            if (settings.sceneExclusionList.Contains(guid))
            {
                CIObfuscatorLogger.Log($"Skipped obfuscating scene '{scene.name}' (GUID: {guid}) because it has been excluded in CI Obfuscator Settings.");
                return;
            }

            new SceneObfuscationModule().Start(scene);
        }

        //asset modules only
        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android) return;
            if (!ShouldRunObfuscator(report)) return;

            ObfuscateAssets(report.summary.outputPath, report.summary.platform);
        }

        //asset modules on Android only
        public void OnPostGenerateGradleAndroidProject(string path)
        {
#if UNITY_2023_1_OR_NEWER
            if (!ShouldRunObfuscator(BuildReport.GetLatestReport())) return;
#else
            if (!ShouldRunObfuscator(null)) return;
#endif
            ObfuscateAssets(path, BuildTarget.Android);
        }

        //used to filter out runtime assembly that hints to bad actors that the current game is using it.
        public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
        {
            return assemblies.Where(name => !name.Contains("CodedImmersions.Obfuscator")).ToArray();
        }

        private void ObfuscateAssets(string path, BuildTarget platform)
        {
            CIObfuscatorSettings settings = CIObfuscatorSettings.instance;

            StartModule<AudioClipObfuscationModule>(settings.enableAudioNameObfuscation, path, platform);
            StartModule<VideoClipObfuscationModule>(settings.enableVideoNameObfuscation, path, platform);
            StartModule<Texture2DObfuscationModule>(settings.enableTexture2DNameObfuscation, path, platform);
            StartModule<Texture3DObfuscationModule>(settings.enableTexture3DNameObfuscation, path, platform);
            StartModule<RenderTextureObfuscationModule>(settings.enableRenderTextureNameObfuscation, path, platform);
            StartModule<SpriteObfuscationModule>(settings.enableSpriteNameObfuscation, path, platform);
            StartModule<MaterialObfuscationModule>(settings.enableMaterialNameObfuscation, path, platform);
            StartModule<MeshObfuscationModule>(settings.enableMeshNameObfuscation, path, platform);
            StartModule<PrefabObfuscationModule>(settings.enablePrefabNameObfuscation, path, platform);
            StartModule<FontObfuscationModule>(settings.enableFontNameObfuscation, path, platform);
        }

        private void StartModule<T>(bool enable, string path, BuildTarget platform) where T : Module, new()
        {
            if (!enable) return;
            Module module = new T();

            module.Start(path, platform);
        }

        private bool ShouldRunObfuscator(BuildReport report, bool log = false)
        {
#if UNITY_2023_1_OR_NEWER
            if (report == null)
            {
                CIObfuscatorLogger.LogWarning("No report was found, not running CI Obfuscator.");
                return false;
            }

            if (report.summary.result != BuildResult.Unknown && report.summary.result != BuildResult.Succeeded)
            {
                CIObfuscatorLogger.Log("Disabled due to failed or cancelled build.");
                return false;
            }
#else
            if (report != null && report.summary.result != BuildResult.Unknown && report.summary.result != BuildResult.Succeeded)
            {
                CIObfuscatorLogger.Log("Disabled due to failed or cancelled build.");
                return false;
            }
#endif

            if (!CIObfuscatorSettings.instance.enableObfuscator)
            {
                if (log) CIObfuscatorLogger.Log("Obfuscation has been disabled by user. Skipping obfuscation...");
                return false;
            }

            return true;
        }

        //clears Unity's build cache so the build can be properly completed.
        private void ClearBuildCache()
        {
            try
            {
                CIObfuscatorLogger.Log("PreProcess: Clearing player data cache.");

                string cachePath = Path.Combine(Directory.GetCurrentDirectory(), "Library", "PlayerDataCache");
                if (Directory.Exists(cachePath)) Directory.Delete(cachePath, true);
            }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}