// #define SCENE_TEMPLATE_DEBUG
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityEditor.SceneTemplate
{
    public static class TestUtils
    {
        public const string k_PackageName = "com.unity.scene-template";
        public static readonly string k_TestFolder = $"Packages/{k_PackageName}/Tests/";
        public static readonly string k_TestDataFolder = $"{k_TestFolder}TestData/";
        public static readonly string k_TestGeneratedFolder = $"Assets/TempTestGeneratedData/";
        public static readonly string k_TestGeneratedTemplateFolder = $"{k_TestGeneratedFolder}Templates/";

        public static string k_TestSceneDependenciesSceneName = "testSceneDependencies.unity";
        public static string k_TestSceneDependenciesScene = $"{k_TestDataFolder}{k_TestSceneDependenciesSceneName}";
        public static string k_NoCloneSceneName = $"NoClone.unity";
        public static string k_NoCloneScene = $"{k_TestDataFolder}NoClone.unity";
        public static string k_NoCloneTemplate = $"{k_TestDataFolder}NoClone.asset";
        public static string k_NoCloneGeneratedTemplate = $"{k_TestGeneratedTemplateFolder}NoClone.asset";
        public static string k_TestSceneDependenciesTemplate = $"{k_TestDataFolder}testSceneDependencies.asset";
        public static readonly string k_TestSceneDependenciesAssetPath = $"{k_TestDataFolder}testSceneDependencies/";
        public static readonly string k_AssetsTempScene = $"{k_TestGeneratedFolder}tempScene.unity";
        public static readonly string k_AssetsTempSceneTemplate = $"{k_TestGeneratedFolder}tempSceneTemplate.asset";


        public static string[] k_TestSceneDependenciesClonable =
            {
                TestUtils.k_TestSceneDependenciesAssetPath + "cubeMaterial.mat",
                TestUtils.k_TestSceneDependenciesAssetPath + "spherePrefab.prefab",
                TestUtils.k_TestSceneDependenciesAssetPath + "spherePrefabMaterial.mat",
                TestUtils.k_TestSceneDependenciesAssetPath + "LensFlare.flare",
                TestUtils.k_TestSceneDependenciesAssetPath + "ReflectionProbe-0.exr",
                TestUtils.k_TestDataFolder + "Square.png",
                TestUtils.k_TestSceneDependenciesAssetPath + "LightingData.asset",
                TestUtils.k_TestSceneDependenciesAssetPath + "Animation.anim",
                TestUtils.k_TestSceneDependenciesAssetPath + "PhysicsMaterial.physicMaterial",
                TestUtils.k_TestSceneDependenciesAssetPath + "PhysicsMaterial2D.physicsMaterial2D",
                TestUtils.k_TestSceneDependenciesAssetPath + "Timeline.playable",
            };

        public static string[] k_TestSceneDependenciesReference =
            {
                TestUtils.k_TestDataFolder + "LightmapParameters.giparams",
            };

        public static string[] k_NoCloneReferences =
        {
            TestUtils.k_TestSceneDependenciesAssetPath + "cubeMaterial.mat",
            TestUtils.k_TestSceneDependenciesAssetPath + "LensFlare.flare",
            TestUtils.k_TestSceneDependenciesAssetPath + "spherePrefab.prefab",
            TestUtils.k_TestSceneDependenciesAssetPath + "spherePrefabMaterial.mat",
            TestUtils.k_TestDataFolder + "LightmapParameters.giparams"
        };

        public static void DeleteFolder(string path)
        {
            Directory.Delete(path, true);
            if (path.EndsWith("/"))
            {
                path = path.Remove(path.Length - 1);
            }
            File.Delete(path + ".meta");
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        public static void CreateFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
        }

#if SCENE_TEMPLATE_DEBUG
        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            Debug.Log("Register events listening");

            EditorSceneManager.newSceneCreated += (scene, setup, mode) => Debug.Log($"Scene created {scene.path} {setup} {mode}");
            EditorSceneManager.sceneClosing += (scene, removingScene) => Debug.Log($"Scene closing {scene.path} {removingScene}");
            EditorSceneManager.sceneClosed += (scene) => Debug.Log($"Scene closed {scene.path}");
            EditorSceneManager.sceneOpening += (scene, mode) => Debug.Log($"Scene opening {scene} {mode}");
            EditorSceneManager.sceneOpened += (scene, mode) => Debug.Log($"Scene opened {scene.path} {mode}");
            EditorSceneManager.sceneSaving += (scene, path) => Debug.Log($"Scene saving {scene.path} {path}");
            EditorSceneManager.sceneSaved += (scene) => Debug.Log($"Scene saved {scene.path}");

            SceneTemplate.newSceneTemplateInstantiating += (sceneTemplate, newSceneOuputPath, loadAdditive) => Debug.Log($"Template instantiating {newSceneOuputPath} {loadAdditive}");
            SceneTemplate.newSceneTemplateInstantiated += (sceneTemplate, scene, sceneAsset, loadAdditive) => Debug.Log($"Template instantiated {scene.path} {sceneAsset} {loadAdditive}");
        }

        [MenuItem("Tools/Log Dependencies")]
        private static void LoadDependencies()
        {
            if (!Selection.activeObject || Selection.activeObject == null)
                return;

            var currentSelectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(currentSelectionPath))
                return;

            var dependencies = AssetDatabase.GetDependencies(currentSelectionPath);
            var stringBuilder = new StringBuilder();
            foreach (var depPath in dependencies)
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(depPath);
                if (!obj || obj == null)
                    continue;

                if (currentSelectionPath == depPath)
                    continue;

                var typeInfo = SceneTemplateProjectSettings.Get().GetDependencyInfo(obj);
                if (typeInfo.ignore)
                    continue;

                stringBuilder.AppendLine($"name: {obj.name} - path {depPath}");
            }
            Debug.Log(stringBuilder.ToString());
        }

        [MenuItem("Tools/Log SelectionInfo")]
        private static void LoadSelectionInfo()
        {
            if (!Selection.activeObject || Selection.activeObject == null)
                return;

            var currentSelectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(currentSelectionPath))
                return;

            Debug.Log($"obj: {Selection.activeObject} - name: {Selection.activeObject.name} - type: {Selection.activeObject.GetType()} - typeStr: {Selection.activeObject.GetType().ToString()}");
        }

        [MenuItem("Tools/Log all scriptable objects")]
        private static void LogAllScriptableObjectTypes()
        {
            var str = new StringBuilder();
            var allScriptableObjects = TypeCache.GetTypesDerivedFrom<ScriptableObject>();
            foreach (var so in allScriptableObjects)
            {
                str.AppendLine(so.FullName);
            }

            Debug.Log($"all scriptable objects {str.ToString()}");
        }

        [MenuItem("Tools/Log all object types (not scriptableObjects)")]
        private static void LogAllObjectTypes()
        {
            var str = new StringBuilder();

            var allScriptableObjects = TypeCache.GetTypesDerivedFrom<ScriptableObject>().ToDictionary(t => t.FullName, t => t);

            var allObjectTypes = TypeCache.GetTypesDerivedFrom<Object>().ToList();
            var allObjectTypeList = new System.Collections.Generic.List<System.Type>();
            foreach (var t in allObjectTypes)
            {
                if (allScriptableObjects.ContainsKey(t.FullName))
                    continue;
                allObjectTypeList.Add(t);
            }

            foreach (var t in allObjectTypeList)
            {
                str.AppendLine(t.FullName);
            }

            Debug.Log($"all object types: {str.ToString()}");
        }
#endif
    }
}