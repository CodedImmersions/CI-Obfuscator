using UnityEditor;
using UnityEngine;

namespace CodedImmersions.Obfuscator.Editor
{
    public sealed class CIObfuscatorLogger
    {
        public static CIObfuscatorLoggerLevel LoggerLevel { get; private set; }

#if UNITY_EDITOR
        private const string Prefix = "<color=#5eb4ff>[CI Obfuscator]</color> ";
#else
        private const string Prefix = "[CI Obfuscator] ";
#endif

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            CIObfuscatorSettings settings = CIObfuscatorSettings.instance;
            if (settings == null) return;

            LoggerLevel = settings.loggerLevel;
        }

        internal static void OverrideLevel(CIObfuscatorLoggerLevel level)
        {
            LoggerLevel = level;
        }

        internal static void Log(string message)
        {
#if UNITY_2021_3_OR_NEWER
            if (LoggerLevel is CIObfuscatorLoggerLevel.None or CIObfuscatorLoggerLevel.Error or CIObfuscatorLoggerLevel.Warning) return;
#else
            if (LoggerLevel == CIObfuscatorLoggerLevel.None || LoggerLevel == CIObfuscatorLoggerLevel.Error || LoggerLevel == CIObfuscatorLoggerLevel.Warning) return;
#endif
            Debug.Log(Prefix + message);
        }

        internal static void LogWarning(string message)
        {
#if UNITY_2021_3_OR_NEWER
            if (LoggerLevel is CIObfuscatorLoggerLevel.None or CIObfuscatorLoggerLevel.Error) return;
#else
            if (LoggerLevel == CIObfuscatorLoggerLevel.None || LoggerLevel == CIObfuscatorLoggerLevel.Error) return;
#endif
            Debug.LogWarning(Prefix + message);
        }

        internal static void LogError(string message)
        {
#if UNITY_2021_3_OR_NEWER
            if (LoggerLevel is CIObfuscatorLoggerLevel.None) return;
#else
            if (LoggerLevel == CIObfuscatorLoggerLevel.None) return;
#endif
            Debug.LogError(Prefix + message);
        }
    }
}
