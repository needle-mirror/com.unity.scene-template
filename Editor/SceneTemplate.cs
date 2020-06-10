#if (SCENE_TEMPLATE_MODULE == false)
// #define SCENE_TEMPLATE_DEBUG
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityEditor.SceneTemplate
{
    public static class SceneTemplate
    {
        internal static string packageName = "com.unity.scene-template";
        internal static string packageFolderName = $"Packages/{packageName}";
        internal static string resourcesFolder = $"{packageFolderName}/Editor/Resources";

        /// <summary>
        /// Event called before a template is instantiated.
        /// </summary>
        /// <param name="sceneTemplateAsset">Template to be instantiated</param>
        /// <param name="newSceneOutputPath">New Scene output path. Can be empty if the scene is created in memory.</param>
        /// <param name="additiveLoad">Is the template to be instantiated in additive mode.</param>
        public delegate void NewTemplateInstantiating(SceneTemplateAsset sceneTemplateAsset, string newSceneOutputPath, bool additiveLoad);

        /// <summary>
        /// Event called after a template is instantiated.
        /// </summary>
        /// <param name="sceneTemplateAsset">Template that was instantiated</param>
        /// <param name="scene">Newly created scene</param>
        /// <param name="sceneAsset">Scene Asset</param>
        /// <param name="additiveLoad">Was the template instantiated in additive mode.</param>
        public delegate void NewTemplateInstantiated(SceneTemplateAsset sceneTemplateAsset, Scene scene, SceneAsset sceneAsset, bool additiveLoad);

        /// <summary>
        /// Events fired before a template is instantiated
        /// </summary>
        public static event NewTemplateInstantiating newSceneTemplateInstantiating;

        /// <summary>
        /// Events fired after a template is instantiated.
        /// </summary>
        public static event NewTemplateInstantiated newSceneTemplateInstantiated;

        /// <summary>
        /// Instantiate a new scene from a template.
        /// </summary>
        /// <param name="sceneTemplate">Scene template asset containing all information to properly instantiate the scene.</param>
        /// <param name="loadAdditively">Is the new scene created additively in the currently loaded scene.</param>
        /// <param name="newSceneOutputPath">If the new scene needs to be saved on disk, it will be its path.</param>
        /// <returns>It returns a tuple of the newly created scene and its matching scene asset</returns>
        public static System.Tuple<Scene, SceneAsset> Instantiate(SceneTemplateAsset sceneTemplate, bool loadAdditively, string newSceneOutputPath = null)
        {
            return Instantiate(sceneTemplate, loadAdditively, newSceneOutputPath, SceneTemplateAnalytics.SceneInstantiationType.Scripting);
        }

        /// <summary>
        /// Create a new Scene template at a specific path. This scene template won't be bound to a scene.
        /// </summary>
        /// <param name="sceneTemplatePath">Path of the new scene template asset</param>
        /// <returns>A new scene template asset instance</returns>
        public static SceneTemplateAsset CreateSceneTemplate(string sceneTemplatePath)
        {
            return CreateTemplateFromScene(null, sceneTemplatePath, SceneTemplateAnalytics.TemplateCreationType.Scripting);
        }

        /// <summary>
        /// Create a new scene template bound to a specific scene. All its dependencies will automatically be extracted and set to reference.
        /// </summary>
        /// <param name="sourceSceneAsset">Scene asset that will serve as the template</param>
        /// <param name="sceneTemplatePath">new path os the Scene template asset</param>
        /// <returns>A new scene template asset instance</returns>
        public static SceneTemplateAsset CreateTemplateFromScene(SceneAsset sourceSceneAsset, string sceneTemplatePath)
        {
            return CreateTemplateFromScene(sourceSceneAsset, sceneTemplatePath, SceneTemplateAnalytics.TemplateCreationType.Scripting);
        }

        internal static System.Tuple<Scene, SceneAsset> Instantiate(SceneTemplateAsset sceneTemplate, bool loadAdditively, string newSceneOutputPath, SceneTemplateAnalytics.SceneInstantiationType instantiationType)
        {
            if (!sceneTemplate.IsValid)
            {
                throw new Exception("templateScene is empty");
            }

            var sourceScenePath = AssetDatabase.GetAssetPath(sceneTemplate.templateScene);
            if (String.IsNullOrEmpty(sourceScenePath))
            {
                throw new Exception("Cannot find path for sceneTemplate: " + sceneTemplate.ToString());
            }

            if (!Application.isBatchMode && !loadAdditively && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return null;

            var instantiateEvent = new SceneTemplateAnalytics.SceneInstantiationEvent(sceneTemplate, instantiationType)
            {
                additive = loadAdditively
            };

            sceneTemplate.UpdateDependencies();
            var hasAnyCloneableDependencies = sceneTemplate.dependencies.Any(dep => dep.instantiationMode == TemplateInstantiationMode.Clone);

            SceneAsset newSceneAsset = null;
            Scene newScene;

            var templatePipeline = sceneTemplate.CreatePipeline();

            if (hasAnyCloneableDependencies)
            {
                if (!InstantiateScene(sceneTemplate, sourceScenePath, ref newSceneOutputPath))
                {
                    instantiateEvent.isCancelled = true;
                    SceneTemplateAnalytics.SendSceneInstantiationEvent(instantiateEvent);
                    return null;
                }

                templatePipeline?.BeforeTemplateInstantiation(sceneTemplate, loadAdditively, newSceneOutputPath);
                newSceneTemplateInstantiating?.Invoke(sceneTemplate, newSceneOutputPath, loadAdditively);

                var refPathMap = new Dictionary<string, string>();
                var refMap = CopyCloneableDependencies(sceneTemplate, newSceneOutputPath, ref refPathMap);

                newScene = EditorSceneManager.OpenScene(newSceneOutputPath, loadAdditively ? OpenSceneMode.Additive : OpenSceneMode.Single);
                newSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(newSceneOutputPath);

                var idMap = new Dictionary<int, int>();
                idMap.Add(sceneTemplate.templateScene.GetInstanceID(), newSceneAsset.GetInstanceID());

                EditorSceneManager.RemapAssetReferencesInScene(newScene, refPathMap, idMap);

                EditorSceneManager.SaveScene(newScene, newSceneOutputPath);

                foreach (var clone in refMap.Values)
                {
                    if (clone)
                        EditorUtility.SetDirty(clone);
                }
                AssetDatabase.SaveAssets();
            }
            else
            {
                templatePipeline?.BeforeTemplateInstantiation(sceneTemplate, loadAdditively, newSceneOutputPath);
                newSceneTemplateInstantiating?.Invoke(sceneTemplate, newSceneOutputPath, loadAdditively);
                if (loadAdditively)
                {
                    newScene = EditorSceneManager.OpenScene(sourceScenePath, OpenSceneMode.Additive);
                }
                else
                {
                    newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    var sourceScene = EditorSceneManager.OpenScene(sourceScenePath, OpenSceneMode.Additive);
                    SceneManager.MergeScenes(sourceScene, newScene);
                }
            }

            SceneTemplateAnalytics.SendSceneInstantiationEvent(instantiateEvent);
            templatePipeline?.AfterTemplateInstantiation(sceneTemplate, newScene, loadAdditively, newSceneOutputPath);
            newSceneTemplateInstantiated?.Invoke(sceneTemplate, newScene, newSceneAsset, loadAdditively);

            SceneTemplateUtils.SetLastFolder(newSceneOutputPath);

            return new System.Tuple<Scene, SceneAsset>(newScene, newSceneAsset);
        }

        internal static SceneTemplateAsset CreateTemplateFromScene(SceneAsset sourceSceneAsset, string sceneTemplatePath, SceneTemplateAnalytics.TemplateCreationType creationType)
        {
            var sourceScenePath = sourceSceneAsset == null ? null : AssetDatabase.GetAssetPath(sourceSceneAsset);
            if (sourceSceneAsset != null && sourceScenePath != null && String.IsNullOrEmpty(sceneTemplatePath))
            {
                var newSceneAssetName = Path.GetFileNameWithoutExtension(sourceScenePath) + "-template.asset";
                sceneTemplatePath = Path.Combine(Path.GetDirectoryName(sourceScenePath), newSceneAssetName).Replace("\\", "/");
                sceneTemplatePath = AssetDatabase.GenerateUniqueAssetPath(sceneTemplatePath);
            }

            if (string.IsNullOrEmpty(sceneTemplatePath))
            {
                throw new Exception("No path specified for new Scene template");
            }

            var sceneTemplate = ScriptableObject.CreateInstance<SceneTemplateAsset>();
            AssetDatabase.CreateAsset(sceneTemplate, sceneTemplatePath);

            if (!String.IsNullOrEmpty(sourceScenePath))
            {
                if (SceneManager.GetActiveScene().path == sourceScenePath && SceneManager.GetActiveScene().isDirty)
                {
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                }

                sceneTemplate.BindScene(sourceSceneAsset);
            }

            EditorUtility.SetDirty(sceneTemplate);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(sceneTemplatePath);

            var sceneCreationEvent = new SceneTemplateAnalytics.SceneTemplateCreationEvent(sceneTemplate, creationType);
            SceneTemplateAnalytics.SendSceneTemplateCreationEvent(sceneCreationEvent);

            SceneTemplateUtils.SetLastFolder(sceneTemplatePath);

            Selection.SetActiveObjectWithContext(sceneTemplate, null);

            return sceneTemplate;
        }

        private static bool InstantiateScene(SceneTemplateAsset sceneTemplate, string sourceScenePath, ref string newSceneOutputPath)
        {
            if (String.IsNullOrEmpty(newSceneOutputPath))
            {
                newSceneOutputPath = SceneTemplateUtils.SaveFilePanelUniqueName(
                    $"Save scene instantiated from template ({sceneTemplate.name})", 
                    SceneTemplateUtils.GetLastFolder("unity"), 
                    Path.GetFileNameWithoutExtension(sourceScenePath), "unity");
                if (string.IsNullOrEmpty(newSceneOutputPath))
                    return false;
            }

            if (Path.IsPathRooted(newSceneOutputPath))
            {
                newSceneOutputPath = FileUtil.GetProjectRelativePath(newSceneOutputPath);
            }

            if (sourceScenePath == newSceneOutputPath)
            {
                Debug.LogError($"Cannot instantiate over template scene: {newSceneOutputPath}");
                return false;
            }

            var destinationDir = Path.GetDirectoryName(newSceneOutputPath);
            if (destinationDir != null && !Directory.Exists(destinationDir))
                Directory.CreateDirectory(destinationDir);

            AssetDatabase.CopyAsset(sourceScenePath, newSceneOutputPath);

            return true;
        }

        private static Dictionary<Object, Object> CopyCloneableDependencies(SceneTemplateAsset sceneTemplate, string newSceneOutputPath, ref Dictionary<string, string> refPathMap)
        {
            var refMap = new Dictionary<Object, Object>();
            var outputSceneFileName = Path.GetFileNameWithoutExtension(newSceneOutputPath);
            var outputSceneDirectory = Path.GetDirectoryName(newSceneOutputPath);
            var dependencyFolder = Path.Combine(outputSceneDirectory, outputSceneFileName);
            if (!Directory.Exists(dependencyFolder))
            {
                Directory.CreateDirectory(dependencyFolder);
            }

            foreach (var dependency in sceneTemplate.dependencies)
            {
                if (dependency.instantiationMode != TemplateInstantiationMode.Clone)
                    continue;

                var dependencyPath = AssetDatabase.GetAssetPath(dependency.dependency);
                if (String.IsNullOrEmpty(dependencyPath))
                {
                    Debug.LogError("Cannot find dependency path for: " + dependency.dependency, dependency.dependency);
                    continue;
                }

                var clonedDepName = Path.GetFileName(dependencyPath);
                var clonedDepPath = Path.Combine(dependencyFolder, clonedDepName).Replace("\\", "/");
                // FYI: CopyAsset already does Import and Refresh
                AssetDatabase.CopyAsset(dependencyPath, clonedDepPath);
                var clonedDependency = AssetDatabase.LoadMainAssetAtPath(clonedDepPath);
                if (clonedDependency == null || !clonedDependency)
                {
                    Debug.LogError("Cannot load cloned dependency path at: " + clonedDepPath);
                    continue;
                }

                refPathMap.Add(dependencyPath, clonedDepPath);
                refMap.Add(dependency.dependency, clonedDependency);
            }

            return refMap;
        }

        #region MenuActions
        [MenuItem("File/Save As Scene Template...", false, 172)]
        private static void SaveTemplateFromCurrentScene()
        {
            var currentScene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(currentScene.path))
            {
                var suggestedScenePath = SceneTemplateUtils.SaveFilePanelUniqueName("Save scene", "Assets", "newscene", "unity");
                if (string.IsNullOrEmpty(suggestedScenePath) || !EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), suggestedScenePath))
                    return;
            }

            var sceneTemplateFile = SceneTemplateUtils.SaveFilePanelUniqueName("Save scene", Path.GetDirectoryName(currentScene.path), Path.GetFileNameWithoutExtension(currentScene.path) + "-template", "asset");
            if (string.IsNullOrEmpty(sceneTemplateFile))
                return;

            var sourceSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(currentScene.path);
            CreateTemplateFromScene(sourceSceneAsset, FileUtil.GetProjectRelativePath(sceneTemplateFile), SceneTemplateAnalytics.TemplateCreationType.SaveCurrentSceneAsTemplateMenu);
        }

        [CommandHandler("File/Menu/NewSceneOverride")]
        private static void NewSceneFromTemplate(CommandExecuteContext context)
        {
            SceneTemplateDialog.ShowWindow();
        }

        [MenuItem("Assets/Create/Scene Template From Scene", false, 201)]
        private static void CreateTemplateFromScene()
        {
            var sourceSceneAsset = Selection.activeObject as SceneAsset;
            if (sourceSceneAsset == null)
                return;

            CreateTemplateFromScene(sourceSceneAsset, null, SceneTemplateAnalytics.TemplateCreationType.CreateFromTargetSceneMenu);
        }

        [MenuItem("Assets/Create/Scene Template From Scene", true, 201)]
        private static bool ValidateCreateTemplateFromScene()
        {
            return Selection.activeObject is SceneAsset;
        }

        // Disable for now until the feature is more establish.
        // [OnOpenAsset]
        private static bool OpenTemplate(int instanceID, int line)
        {
            var possibleTemplate = EditorUtility.InstanceIDToObject(instanceID) as SceneTemplateAsset;
            if (possibleTemplate != null)
            {
                if (!possibleTemplate.IsValid)
                {
                    Debug.LogError("Cannot instantiate scene template: scene is null or deleted.");
                    return false;
                }

                EditorApplication.delayCall += () =>
                {
                    Instantiate(possibleTemplate, false, null, SceneTemplateAnalytics.SceneInstantiationType.TemplateDoubleClick);
                };
                return true;
            }
            return false;
        }
        #endregion
    }
}
#endif