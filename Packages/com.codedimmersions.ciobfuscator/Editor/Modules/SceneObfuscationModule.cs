using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CodedImmersions.Obfuscator.Editor
{
    public sealed class SceneObfuscationModule : Module
    {
        protected override string name => "Scene Obfuscation Module";
        public override bool IsAssetModule => false;

        public SceneObfuscationModule() : base() { }

        private List<Transform> sceneObjects = new List<Transform>();

        internal void Start(Scene scene)
        {
            CIObfuscatorLogger.Log($"{name}: Started GameObject processing for scene {scene.name}.");
            EditorUtility.DisplayProgressBar($"CI Obfuscator - {name}", "Obfuscating scene GameObjects...", 0f);

            foreach (GameObject obj in scene.GetRootGameObjects()) ProcessObject(obj.transform);

            EditorUtility.DisplayProgressBar($"CI Obfuscator - {name}", "Adding random GameObjects...", 0f);
            if (settings.enableRandomSceneObjects)
            {
                int max = Random.Range(settings.minNewObjCount, settings.maxNewObjCount);
                for (int i = 1; i <= max; i++)
                {
                    GameObject obj = new GameObject();
                    obj.name = settings.ObfuscatedString();

                    obj.transform.position = new Vector3(Random.Range(-1000, 1000), Random.Range(-1000, 1000), Random.Range(-1000, 1000));
                    obj.transform.rotation = Random.rotation;

                    //65% chance
                    if (Random.value >= 0.35f && sceneObjects.Count > 0)
                        obj.transform.SetParent(sceneObjects[Random.Range(0, sceneObjects.Count - 1)]);

                    //30% chance
                    if (Random.value >= 0.7f)
                    {
                        MeshFilter mf = (MeshFilter)obj.AddComponent(typeof(MeshFilter));
                        MeshRenderer mr = (MeshRenderer)obj.AddComponent(typeof(MeshRenderer));
                        mr.enabled = false;

                        //https://github.com/Unity-Technologies/UnityCsReference/blob/c4a2a4d90d91496bf3d4602778223a0e660c2a56/Editor/Mono/ObjectFactory.bindings.cs
                        switch (Random.Range(1, 6))
                        {
                            case 1:
                                mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
                                break;

                            case 2:
                                mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("New-Capsule.fbx");
                                break;

                            case 3:
                                mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("New-Cylinder.fbx");
                                break;

                            case 4:
                                mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                                break;

                            case 5:
                                mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("New-Plane.fbx");
                                break;

                            case 6:
                                mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                                break;
                        }
                    }

                    //10% chance
                    if (Random.value >= 0.9f) obj.SetActive(false);

                    sceneObjects.Add(obj.transform);
                }
            }

            EditorUtility.ClearProgressBar();
            CIObfuscatorLogger.Log($"{name}: GameObject processing for scene {scene.name} finished.");
        }

        private void ProcessObject(Transform obj)
        {
            if (obj.TryGetComponent(out Animator _))
            {
                //animators rely on GameObject names, so we skip.
                return;
            }

            if (obj.TryGetComponent(out CIObfuscatorIgnore oi))
            {
                if (!oi.applyToChildren) for (int i = 0; i < obj.childCount; i++) ProcessObject(obj.GetChild(i));
                Object.DestroyImmediate(oi);
                return;
            }

            obj.gameObject.name = settings.ObfuscatedString();
            if (settings.enableRandomSceneObjects) sceneObjects.Add(obj);
            for (int i = 0; i < obj.childCount; i++) ProcessObject(obj.GetChild(i));
        }

        internal override void Start(string buildPath, BuildTarget target)
        {
            throw new System.NotImplementedException();
        }
    }
}
