using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace CodedImmersions.Obfuscator.Editor
{
    public abstract class Module
    {
        protected CIObfuscatorSettings settings => CIObfuscatorSettings.instance;
        protected AssetsManager assetsManager = new AssetsManager();

        protected abstract string name { get; }
        public abstract bool IsAssetModule { get; }


        protected bool SetUp(string buildPath, BuildTarget target)
        {
            EditorUtility.DisplayProgressBar($"CI Obfuscator - {name}", "Setting up module...", 0f);

            if (!IsSupportedAssetsBuildPlatform(target))
            {
                CIObfuscatorLogger.LogError($"{name} does not support '{target}' target platform.");
                return false;
            }

            string folder = Path.Combine(Directory.GetCurrentDirectory(), "Library", "Coded Immersions", "CI Obfuscator");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, "classdata.tpk");

            if (!File.Exists(path))
            {
                //from https://github.com/AssetRipper/Tpk
                UnityWebRequest uwr = UnityWebRequest.Get("https://nightly.link/AssetRipper/Tpk/workflows/type_tree_tpk/master/uncompressed_file.zip");
                string zippath = Path.Combine(folder, "tpk_uncompressed.zip");
                uwr.downloadHandler = new DownloadHandlerFile(zippath);
                uwr.timeout = 5;

                UnityWebRequestAsyncOperation op = uwr.SendWebRequest();

                while (!op.isDone)
                {
                    EditorUtility.DisplayProgressBar($"CI Obfuscator - {name}", $"Downloading classdata.tpk... {(op.progress * 100):F0}%", op.progress);
                    System.Threading.Thread.Sleep(50);
                }

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    EditorUtility.DisplayProgressBar($"CI Obfuscator - {name}", "Extracting classdata.tpk...", 0f);

#if UNITY_2021_3_OR_NEWER
                    ZipFile.ExtractToDirectory(zippath, folder, true);
                    File.Move(Path.Combine(folder, "uncompressed.tpk"), path);
#else
                    using (FileStream fs = File.OpenRead(zippath))
                    using (System.IO.Compression.DeflateStream deflate = new System.IO.Compression.DeflateStream(fs, System.IO.Compression.CompressionMode.Decompress))
                    {
                        byte[] buffer = new byte[30];
                        fs.Read(buffer, 0, 30);

                        int fileNameLength = BitConverter.ToUInt16(buffer, 26);
                        int extraFieldLength = BitConverter.ToUInt16(buffer, 28);

                        fs.Seek(fileNameLength + extraFieldLength, SeekOrigin.Current);

                        using (FileStream output = File.Create(path)) deflate.CopyTo(output);
                    }
#endif
                    File.Delete(zippath);
                }
                else
                {
                    CIObfuscatorLogger.LogError($"Failed to download classdata.tpk with error code '{uwr.error}', skipping {name}.");
                    uwr.Dispose();
                    EditorUtility.ClearProgressBar();
                    return false;
                }

                uwr.Dispose();
            }

            ClassPackageFile pkg = assetsManager.LoadClassPackage(path);
            UnityVersion version = new UnityVersion(Application.unityVersion);
            if (pkg.TpkTypeTree.Versions.Count == 0 || !pkg.TpkTypeTree.Versions.Any(vers => vers.major.Equals(version.major) && vers.minor.Equals(version.minor)))
            {
                CIObfuscatorLogger.LogError($"Cannot run {name}: Downloaded TPK at '{path.Replace('\\', '/')}' does not support Unity version {Application.unityVersion}.");
                EditorUtility.ClearProgressBar();
                return false;
            }

            GetAssetsFiles(buildPath, target).ForEach(a =>
            {
                //if (target is BuildTarget.Android or BuildTarget.WebGL)
                if (target == BuildTarget.Android)
                {
                    BundleFileInstance bundle = assetsManager.LoadBundleFile(a);
                    if (bundle == null)
                    {
                        CIObfuscatorLogger.LogError($"{name}: Failed to load bundle file: {a}");
                        return;
                    }

                    for (int i = 0; i < bundle.file.BlockAndDirInfo.DirectoryInfos.Count; i++)
                    {
                        AssetBundleDirectoryInfo info = bundle.file.BlockAndDirInfo.DirectoryInfos[i];
                        if (info.Name != "resources.assets" && !info.Name.EndsWith(".resS") && !info.Name.EndsWith(".resource")) assetsManager.LoadAssetsFileFromBundle(bundle, i);
                    }
                }
                else
                {
                    assetsManager.LoadAssetsFile(a);
                }
            });

            assetsManager.LoadClassDatabaseFromPackage(Application.unityVersion);
            return true;
        }

        protected void ModifyAssets(AssetClassID assetClassId, string buildPath, BuildTarget target)
        {
            CIObfuscatorLogger.Log($"{name}: Modifying assets.");

            string dataPath = GetDataPath(buildPath, target);

            if (assetsManager.Files == null || assetsManager.Files.Count == 0)
            {
                CIObfuscatorLogger.LogWarning($"{name}: Skipping module because no game data was loaded into AssetsTools.NET.");
                return;
            }

            //string1 is modified, string2 is original
            Dictionary<string, string> generatedFiles = new Dictionary<string, string>();

            for (int i = 0; i < assetsManager.Files.Count; i++)
            {
                AssetsFileInstance asset = assetsManager.Files[i];
                EditorUtility.DisplayProgressBar($"CI Obfuscator - {name}", $"Processing {asset.name} ({(float)(i + 1)}/{(float)assetsManager.Files.Count})", i + 1 / assetsManager.Files.Count);

                //resources.assets is stuff in Assets/Resources that we DO NOT want to touch.
                //.resS files are binary files that cannot be edited.
                if (asset.name == "resources.assets" || asset.name.EndsWith(".resS")) continue;

                foreach (AssetFileInfo info in asset.file.GetAssetsOfType(assetClassId))
                {
                    AssetTypeValueField basefield;
                    try { basefield = assetsManager.GetBaseField(asset, info); }
                    catch (NullReferenceException)
                    {
                        CIObfuscatorLogger.LogError($"{name}: Caught NullReferenceException. This probably means the TPK downloaded does not support Unity version {Application.unityVersion}. Please make sure it does. If it doesn't, delete the 'CI Obfuscator' folder in '<Project Root>/Library/Coded Immersions', then build again to redownload the latest TPK.");
                        return;
                    }

                    if (basefield == null) continue;

                    string originalName = basefield["m_Name"].AsString;

                    //exclude names on the asset exclusion list
                    if (settings.assetExclusionList.Contains(originalName)) continue;

                    basefield["m_Name"].AsString = settings.ObfuscatedString();
                    info.SetNewData(basefield);
                }

                string tempFile = Path.Combine(dataPath, asset.name + ".ciobf");

                using (AssetsFileWriter afw = new AssetsFileWriter(tempFile)) { asset.file.Write(afw); }

                generatedFiles.Add(tempFile, Path.Combine(dataPath, asset.name));
                CIObfuscatorLogger.Log($"{name}: Finished modifying {asset.name}.");
            }

            Cleanup();

#if UNITY_2021_3_OR_NEWER
            if (target is BuildTarget.Android or BuildTarget.WebGL)
#else
            if (target == BuildTarget.Android || target == BuildTarget.WebGL)
#endif
            {
                RebuildBundle(buildPath, target, generatedFiles);
            }
            else
            {
                foreach (KeyValuePair<string, string> file in generatedFiles)
                {
                    File.Delete(file.Value);
                    File.Move(file.Key, file.Value);
                }
            }

            CIObfuscatorLogger.Log($"{name}: Finished obfuscation.");
            EditorUtility.ClearProgressBar();
        }

        private void RebuildBundle(string buildPath, BuildTarget target, Dictionary<string, string> generatedFiles)
        {
            List<string> bundlePaths = GetAssetsFiles(buildPath, target);
            if (bundlePaths.Count == 0)
            {
                CIObfuscatorLogger.LogError($"{name}: No bundle files found for build target {target}.");
                return;
            }

            string bundlePath = bundlePaths[0];

            try
            {
                FileStream fs = File.OpenRead(bundlePath);
                AssetsFileReader reader = new AssetsFileReader(fs);
                BundleFileInstance bundle = new BundleFileInstance(reader.BaseStream, bundlePath);

                foreach (KeyValuePair<string, string> file in generatedFiles)
                {
                    string assetName = Path.GetFileName(file.Value);

                    for (int i = 0; i < bundle.file.BlockAndDirInfo.DirectoryInfos.Count; i++)
                    {
                        AssetBundleDirectoryInfo info = bundle.file.BlockAndDirInfo.DirectoryInfos[i];

                        if (info.Name == assetName)
                        {
                            info.SetNewData(File.ReadAllBytes(file.Key));
                            break;
                        }
                    }

                    File.Delete(file.Key);
                }

                AssetBundleCompressionType compressionType = bundle.originalCompression;

                string tmpPath = bundlePath + ".ciobf";
                using (AssetsFileWriter writer = new AssetsFileWriter(tmpPath)) { bundle.file.Pack(writer, compressionType); }

                reader.Dispose();
                fs.Dispose();

                File.Delete(bundlePath);
                File.Move(tmpPath, bundlePath);

                CIObfuscatorLogger.Log($"{name}: Rebuilt {target} bundle.");
            }
            catch (Exception ex)
            {
                CIObfuscatorLogger.LogError($"{name}: Error while rebuilding bundle: '{ex.ToString()}'");
            }
        }

        private string GetDataPath(string buildPath, BuildTarget target)
        {
            return target switch
            {
                BuildTarget.StandaloneWindows => Path.ChangeExtension(buildPath, null) + "_Data",
                BuildTarget.StandaloneWindows64 => Path.ChangeExtension(buildPath, null) + "_Data",
                BuildTarget.StandaloneOSX => Path.Combine(buildPath, "Contents", "Resources", "Data"),
                BuildTarget.StandaloneLinux64 => Path.ChangeExtension(buildPath, null) + "_Data",

                BuildTarget.Android => Path.Combine(buildPath, "src", "main", "assets", "bin", "Data"),
                BuildTarget.iOS => Path.Combine(buildPath, "Data"),

                //webgl unsupported because AssetsTools.NET doesn't support the UnityWebData1.0 signature.
                //BuildTarget.WebGL => Path.Combine(buildPath, "Build"),
                _ => Path.ChangeExtension(buildPath, null) + "_Data"
            };
        }

        private List<string> GetAssetsFiles(string buildPath, BuildTarget target)
        {
            List<string> sharedAssets = new List<string>();
            List<string> sharedAssetsPaths = new List<string>();

            int i = 0;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;

                int id = i++;
                sharedAssets.Add($"sharedassets{id}.assets");
                sharedAssets.Add($"level{id}");
            }

            string dataPath = GetDataPath(buildPath, target);

            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64:
                    foreach (string asset in sharedAssets)
                        sharedAssetsPaths.Add(Path.Combine(dataPath, asset));
                    break;

                case BuildTarget.Android:
                    sharedAssetsPaths.Add(Path.Combine(dataPath, "data.unity3d"));
                    break;

                case BuildTarget.iOS:
                    foreach (string asset in sharedAssets)
                        sharedAssetsPaths.Add(Path.Combine(dataPath, asset));
                    break;

                //webgl unsupported because AssetsTools.NET doesn't support the UnityWebData1.0 signature.
                /*case BuildTarget.WebGL:
                    string buildFolder = Path.GetFileNameWithoutExtension(buildPath);

                    string[] possibleExtensions = { ".data.unityweb", ".data.gz", ".data.br", ".data" };
                    foreach (string ext in possibleExtensions)
                    {
                        string webglDataPath = Path.Combine(buildPath, "Build", $"{buildFolder}{ext}");
                        Debug.Log(webglDataPath);
                        if (File.Exists(webglDataPath))
                        {
                            sharedAssetsPaths.Add(webglDataPath);
                            break;
                        }
                    }
                    break;*/

                default:
                    CIObfuscatorLogger.LogWarning($"{name}: Asset file structure for {target} may not be fully supported.");
                    foreach (string asset in sharedAssets)
                        sharedAssetsPaths.Add(Path.Combine(dataPath, asset));
                    break;
            }

            return sharedAssetsPaths;
        }

        protected bool IsSupportedAssetsBuildPlatform(BuildTarget target)
        {
#if UNITY_2021_3_OR_NEWER
            return target is BuildTarget.StandaloneWindows or
                BuildTarget.StandaloneWindows64 or
                BuildTarget.StandaloneOSX or
                BuildTarget.StandaloneLinux64 or
                BuildTarget.Android or
                BuildTarget.iOS;
#else
            return target == BuildTarget.StandaloneWindows
                || target == BuildTarget.StandaloneWindows64
                || target == BuildTarget.StandaloneOSX
                || target == BuildTarget.StandaloneLinux64
                || target == BuildTarget.Android
                || target == BuildTarget.iOS;
#endif

            //webgl unsupported because AssetsTools.NET doesn't support the UnityWebData1.0 signature.
            //or BuildTarget.WebGL;
        }

        protected void Cleanup()
        {
            assetsManager.UnloadAll(true);
            assetsManager = null;
        }

        internal abstract void Start(string buildPath, BuildTarget target);
    }
}