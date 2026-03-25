using System.Collections.Generic;
using System.Text;
using UnityEditor;
using Random = System.Random;

namespace CodedImmersions.Obfuscator.Editor
{
    [FilePath("ProjectSettings/CIObfuscatorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class CIObfuscatorSettings : ScriptableSingleton<CIObfuscatorSettings>
    {
        public bool enableObfuscator = true;
        public CIObfuscatorLoggerLevel loggerLevel = CIObfuscatorLoggerLevel.Debug;

        public bool enableSceneObfuscation = true;
        public bool enableAudioNameObfuscation = true;
        public bool enableVideoNameObfuscation = true;
        public bool enableTexture2DNameObfuscation = true;
        public bool enableTexture3DNameObfuscation = true;
        public bool enableRenderTextureNameObfuscation = true;
        public bool enableSpriteNameObfuscation = true;
        public bool enableMaterialNameObfuscation = true;
        public bool enableMeshNameObfuscation = true;
        public bool enablePrefabNameObfuscation = true;
        public bool enableFontNameObfuscation = true;

        public bool enableRandomSceneObjects = true;

        //string is the guid of the scene
        public List<string> sceneExclusionList = new List<string>();

        //string is the name filter
        public List<string> assetExclusionList = new List<string>();


        public List<ushort> unicodeExclusionList = new List<ushort>();

        public RenameMethod renameMethod = RenameMethod.Randomized;
        public string sameNameString = "Object";

        public int minCharacterCount = 50;
        public int maxCharacterCount = 100;

        public int minNewObjCount = 250;
        public int maxNewObjCount = 500;

        internal const string ASCIICharacterSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 -=!@#$%^&*()_+[]{}\\|;:'\"<,>.?/`~";
        private static readonly Random random = new Random();

        public bool IsSceneEnabled(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return true;
            return !sceneExclusionList.Contains(guid);
        }

        public void SetSceneEnabled(string guid, bool enabled)
        {
            if (string.IsNullOrEmpty(guid)) return;
            sceneExclusionList.RemoveAll(string.IsNullOrEmpty);

            if (enabled && sceneExclusionList.Contains(guid)) sceneExclusionList.Remove(guid);
            else if (!enabled && !sceneExclusionList.Contains(guid)) sceneExclusionList.Add(guid);

            Save();
        }

        public string ObfuscatedString()
        {
            return renameMethod switch
            {
                RenameMethod.Randomized => RandomUtf8String(),
                RenameMethod.RandomizedUnicode => RandomUnicodeString(),
                RenameMethod.SameName => !string.IsNullOrWhiteSpace(sameNameString) ? sameNameString : "Object",
                RenameMethod.NoName => string.Empty,
                _ => RandomUtf8String()
            };
        }

        private string RandomUtf8String()
        {
            if (minCharacterCount <= 0)
            {
                minCharacterCount = 1;
                Save();
            }

            if (maxCharacterCount < minCharacterCount)
            {
                maxCharacterCount = minCharacterCount + 1;
                Save();
            }

            int length = random.Next(minCharacterCount, maxCharacterCount);
            StringBuilder sb = new StringBuilder(length);

            while (sb.Length < length)
            {
                char c = ASCIICharacterSet[random.Next(0, ASCIICharacterSet.Length)];
                sb.Append(c);
            }

            return sb.ToString();
        }

        private string RandomUnicodeString()
        {
            if (minCharacterCount <= 0)
            {
                minCharacterCount = 1;
                Save();
            }

            if (maxCharacterCount < minCharacterCount)
            {
                maxCharacterCount = minCharacterCount + 1;
                Save();
            }

            int length = random.Next(minCharacterCount, maxCharacterCount);
            StringBuilder sb = new StringBuilder(length);

            while (sb.Length < length)
            {
                char c = (char)random.Next(char.MinValue, char.MaxValue);

                if (char.IsSurrogate(c)) continue;
                //if (!char.IsLetterOrDigit(c)) continue;

                sb.Append(c);
            }

            return sb.ToString();
        }

        public void Save() => base.Save(true);
    }
}
