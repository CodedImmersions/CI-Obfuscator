using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CodedImmersions.Obfuscator.Editor
{
    public class CIObfuscatorWindow
    {
        private static GUIStyle labelstyle;
        private static CIObfuscatorSettings settings;

        private static bool showscenesettingsfoldout;
        private static bool showscenesfoldout;
        private static bool showassetsfoldout;

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/CI Obfuscator", SettingsScope.Project)
            {
                label = "CI Obfuscator",
                keywords = new HashSet<string>(new[] { "Obfuscator", "Coded Immersions", "Obfuscation" }),

                guiHandler = (_) => DrawSettingsGUI(),
            };
        }

        private static void DrawSettingsGUI()
        {
            settings = CIObfuscatorSettings.instance;

            EditorGUI.BeginChangeCheck();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("GitHub", GUILayout.Width(75)))
                Application.OpenURL("https://github.com/CodedImmersions/CI-Obfuscator");

            if (GUILayout.Button("Website", GUILayout.Width(75)))
                Application.OpenURL("https://codedimmersions.com");

            GUILayout.EndHorizontal();
            GUILayout.Space(20);

            labelstyle = new GUIStyle(GUI.skin.label);

            labelstyle.alignment = TextAnchor.MiddleLeft;
            labelstyle.fontStyle = FontStyle.Bold;
            labelstyle.fontSize = 12;
            GUILayout.Label("Core", labelstyle);

            settings.enableObfuscator = EditorGUILayout.Toggle("Enable CI Obfuscator", settings.enableObfuscator);
            EditorGUI.BeginDisabledGroup(!settings.enableObfuscator);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Log Level");
            GUILayout.Space(4);
            settings.loggerLevel = (CIObfuscatorLoggerLevel)EditorGUILayout.EnumPopup(settings.loggerLevel);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);

            labelstyle.alignment = TextAnchor.MiddleLeft;
            labelstyle.fontStyle = FontStyle.Bold;
            labelstyle.fontSize = 12;
            GUILayout.Label("Modules", labelstyle);

            settings.enableSceneObfuscation = EditorGUILayout.Toggle("Scene Obfuscation", settings.enableSceneObfuscation);
            settings.enableAudioNameObfuscation = EditorGUILayout.Toggle("AudioClip Rename", settings.enableAudioNameObfuscation);
            settings.enableVideoNameObfuscation = EditorGUILayout.Toggle("VideoClip Rename", settings.enableVideoNameObfuscation);
            settings.enableTexture2DNameObfuscation = EditorGUILayout.Toggle("Texture2D Rename", settings.enableTexture2DNameObfuscation);
            settings.enableTexture3DNameObfuscation = EditorGUILayout.Toggle("Texture3D Rename", settings.enableTexture3DNameObfuscation);
            settings.enableRenderTextureNameObfuscation = EditorGUILayout.Toggle("Render Texture Rename", settings.enableRenderTextureNameObfuscation);
            settings.enableSpriteNameObfuscation = EditorGUILayout.Toggle("Sprite Rename", settings.enableSpriteNameObfuscation);
            settings.enableMaterialNameObfuscation = EditorGUILayout.Toggle("Material Rename", settings.enableMaterialNameObfuscation);
            settings.enableMeshNameObfuscation = EditorGUILayout.Toggle("Mesh Rename", settings.enableMeshNameObfuscation);
            settings.enablePrefabNameObfuscation = EditorGUILayout.Toggle("Prefab Rename", settings.enablePrefabNameObfuscation);
            settings.enableFontNameObfuscation = EditorGUILayout.Toggle("Font Rename", settings.enableFontNameObfuscation);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Enable All", GUILayout.Width(82))) SetAllModulesActive(true);
            if (GUILayout.Button("Disable All", GUILayout.Width(82))) SetAllModulesActive(false);

            GUILayout.EndHorizontal();

            if (!IsAnyModuleActive())
            {
                GUILayout.Space(4);
                EditorGUILayout.HelpBox("No modules are enabled. Please enable at least one to make CI Obfuscator functional.", MessageType.Warning);
            }

            GUILayout.Space(20);

            labelstyle.alignment = TextAnchor.MiddleLeft;
            labelstyle.fontStyle = FontStyle.Bold;
            labelstyle.fontSize = 12;
            GUILayout.Label("Rename Settings", labelstyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Rename Method");
            settings.renameMethod = (RenameMethod)EditorGUILayout.EnumPopup(settings.renameMethod);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(2);

            switch (settings.renameMethod)
            {
                case RenameMethod.Randomized:
                    EditorGUILayout.HelpBox("Randomized (ASCII) mode will rename to pre-set ASCII characters.", MessageType.Info);
                    GUILayout.Space(10);

                    float minFloat = settings.minCharacterCount;
                    float maxFloat = settings.maxCharacterCount;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Min/Max Character Count");
                    GUILayout.Space(4);
                    EditorGUILayout.MinMaxSlider(ref minFloat, ref maxFloat, 1, 500);
                    GUILayout.Label($"{minFloat} to {maxFloat}", GUILayout.Width(65));
                    EditorGUILayout.EndHorizontal();

                    settings.minCharacterCount = (int)minFloat;
                    settings.maxCharacterCount = (int)maxFloat;
                    break;

                case RenameMethod.RandomizedUnicode:
                    EditorGUILayout.HelpBox("Randomized (Unicode) mode will rename to any non-surrogate UTF-16 characters ranging from u0 to uffff.", MessageType.Info);

                    GUILayout.Space(10);

                    float minFloat2 = settings.minCharacterCount;
                    float maxFloat2 = settings.maxCharacterCount;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Min/Max Character Count");
                    GUILayout.Space(4);
                    EditorGUILayout.MinMaxSlider(ref minFloat2, ref maxFloat2, 1, 500);
                    GUILayout.Label($"{minFloat2} to {maxFloat2}", GUILayout.Width(65));
                    EditorGUILayout.EndHorizontal();

                    settings.minCharacterCount = (int)minFloat2;
                    settings.maxCharacterCount = (int)maxFloat2;

                    GUILayout.Space(5);

#if UNITY_2022_3_OR_NEWER
                    SerializedObject so = new SerializedObject(settings);
                    so.Update();

                    SerializedProperty prop = so.FindProperty("unicodeExclusionList");
                    EditorGUILayout.PropertyField(prop, true);

                    so.ApplyModifiedProperties();
#else
                    SerializedObject so = new SerializedObject(settings);
                    so.Update();

                    SerializedProperty listProp = so.FindProperty("unicodeExclusionList");

                    EditorGUILayout.LabelField("Unicode Exclusion List", EditorStyles.boldLabel);
                    EditorGUILayout.Space(2);

                    EditorGUI.indentLevel++;

                    for (int i = 0; i < listProp.arraySize; i++)
                    {
                        SerializedProperty element = listProp.GetArrayElementAtIndex(i);

                        EditorGUILayout.BeginHorizontal();

                        int value = element.intValue;
                        value = EditorGUILayout.IntField($"Element {i}", value);
                        element.intValue = Mathf.Clamp(value, 0, 0xFFFF);

                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            listProp.DeleteArrayElementAtIndex(i);
                            break;
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.Space(2);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("+", GUILayout.Width(24)))
                    {
                        listProp.InsertArrayElementAtIndex(listProp.arraySize);
                        listProp.GetArrayElementAtIndex(listProp.arraySize - 1).intValue = 0;
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel--;

                    so.ApplyModifiedProperties();
#endif
                    break;

                case RenameMethod.SameName:
                    EditorGUILayout.HelpBox("Same Name mode will rename every single GameObject and Asset to one singular name, set below.", MessageType.Info);
                    GUILayout.Space(10);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Rename String");
                    GUILayout.Space(4);
                    settings.sameNameString = GUILayout.TextField(settings.sameNameString);
                    EditorGUILayout.EndHorizontal();
                    break;

                case RenameMethod.NoName:
                    EditorGUILayout.HelpBox("No Name mode will completely nullify the all GameObject/Asset names. GameObject names will be blank, and Asset names will be different depending on the tool (Ex. Asset Ripper will show [AssetType]_[Number++], and UABEA will show \"Unnamed asset\").", MessageType.Info);
                    //no extra settings needed for NoName.
                    break;
            }



            GUILayout.Space(20);

            labelstyle.alignment = TextAnchor.MiddleLeft;
            labelstyle.fontStyle = FontStyle.Bold;
            labelstyle.fontSize = 12;
            GUILayout.Label("Module-Specific Settings", labelstyle);

            showscenesettingsfoldout = EditorGUILayout.Foldout(showscenesettingsfoldout, "Scene Obfuscation", true);

            if (showscenesettingsfoldout)
            {
                EditorGUI.BeginDisabledGroup(!settings.enableSceneObfuscation);

                EditorGUI.indentLevel++;
                GUILayout.Space(5);

                settings.enableRandomSceneObjects = EditorGUILayout.Toggle(new GUIContent("Allow Added Objects", "Creates randomly parented new GameObjects in the scene to confuse decompilers."), settings.enableRandomSceneObjects);

                float min = settings.minNewObjCount;
                float max = settings.maxNewObjCount;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Added Objects Range");
                GUILayout.Space(4);
                EditorGUILayout.MinMaxSlider(ref min, ref max, 1, 1000);
                GUILayout.Label($"{min} to {max}", GUILayout.Width(70));
                EditorGUILayout.EndHorizontal();

                settings.minNewObjCount = (int)min;
                settings.maxNewObjCount = (int)max;

                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();
            }

            GUILayout.Space(20);

            labelstyle.alignment = TextAnchor.MiddleLeft;
            labelstyle.fontStyle = FontStyle.Bold;
            labelstyle.fontSize = 12;
            GUILayout.Label("Exclusion List", labelstyle);

            showscenesfoldout = EditorGUILayout.Foldout(showscenesfoldout, "Scenes", true);
            if (showscenesfoldout)
            {
                EditorGUI.BeginDisabledGroup(!settings.enableSceneObfuscation);
                EditorGUI.indentLevel++;

                EditorGUILayout.HelpBox("If enabled, scene will be obfuscated. If disabled, the scene will NOT be obfuscated. This only applies if Scene Obfuscation is enabled.\nAlso, to ingore only some GameObjects, add the CI Obfuscator Ignore component to them.", MessageType.Info);
                GUILayout.Space(5);

                if (EditorBuildSettings.scenes.Length > 0)
                {
                    foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes.Where(scene => scene != null))
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(15 * EditorGUI.indentLevel);

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.BeginHorizontal();

                        bool enabled = settings.IsSceneEnabled(scene.guid.ToString());

                        EditorGUI.BeginChangeCheck();
                        bool newEnabled = EditorGUILayout.Toggle(GUIContent.none, enabled, GUILayout.Width(20));
                        if (EditorGUI.EndChangeCheck())
                        {
                            settings.SetSceneEnabled(scene.guid.ToString(), newEnabled);
                        }

                        GUILayout.Space(5);
                        EditorGUILayout.LabelField(Path.GetFileNameWithoutExtension(scene.path), GUILayout.Width(150));
                        EditorGUILayout.LabelField(scene.path, GUILayout.ExpandWidth(true));
                        EditorGUILayout.LabelField(scene.guid.ToString(), EditorStyles.miniLabel, GUILayout.Width(250));

                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(2);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("There are no scenes in Editor Build Settings. Please add at least one to exclude scenes.", MessageType.Warning, true);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Open Build Settings", GUILayout.Width(125)))
                    {
#if UNITY_6000_0_OR_NEWER
                        EditorApplication.ExecuteMenuItem("File/Build Profiles");
#else
                        EditorApplication.ExecuteMenuItem("File/Build Settings");
#endif
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();
            }

            GUILayout.Space(3);

            showassetsfoldout = EditorGUILayout.Foldout(showassetsfoldout, "Assets", true);

            if (showassetsfoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.HelpBox("Add an asset name to the list (without extension, case-sensitive) if you want CI Obfuscator to not rename it.", MessageType.Info);

                SerializedObject so = new SerializedObject(settings);
                so.Update();

                SerializedProperty prop = so.FindProperty("assetExclusionList");
                EditorGUILayout.PropertyField(prop, true);

                so.ApplyModifiedProperties();

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndDisabledGroup();
            GUILayout.Space(20); //add extra room to scroll at the end

            if (EditorGUI.EndChangeCheck())
            {
                settings.Save();
                CIObfuscatorLogger.OverrideLevel(settings.loggerLevel);
            }

            void SetAllModulesActive(bool enabled)
            {
                settings.enableSceneObfuscation = enabled;
                settings.enableAudioNameObfuscation = enabled;
                settings.enableVideoNameObfuscation = enabled;
                settings.enableTexture2DNameObfuscation = enabled;
                settings.enableTexture3DNameObfuscation = enabled;
                settings.enableRenderTextureNameObfuscation = enabled;
                settings.enableSpriteNameObfuscation = enabled;
                settings.enableMaterialNameObfuscation = enabled;
                settings.enableMeshNameObfuscation = enabled;
                settings.enablePrefabNameObfuscation = enabled;
                settings.enableFontNameObfuscation = enabled;
            }

            bool IsAnyModuleActive()
            {
                return settings.enableSceneObfuscation ||
                       settings.enableAudioNameObfuscation ||
                       settings.enableVideoNameObfuscation ||
                       settings.enableTexture2DNameObfuscation ||
                       settings.enableTexture3DNameObfuscation ||
                       settings.enableRenderTextureNameObfuscation ||
                       settings.enableSpriteNameObfuscation ||
                       settings.enableMaterialNameObfuscation ||
                       settings.enableMeshNameObfuscation ||
                       settings.enablePrefabNameObfuscation ||
                       settings.enableFontNameObfuscation;
            }
        }
    }
}