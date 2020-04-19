using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.SceneTemplate
{
    public enum TemplateInstantiationMode
    {
        Clone,
        Reference
    }

    /// <summary>
    /// Asset storing everything needed to instantiate a scene from a templated scene
    /// </summary>
    [Serializable]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.scene-template@latest/")]
    [CreateAssetMenu(menuName = "Scene Template", order = 201)]
    public class SceneTemplateAsset : ScriptableObject
    {
        /// <summary>
        /// Scene that will be copied when instantiating the template
        /// </summary>
        public SceneAsset templateScene;
        /// <summary>
        /// Pretty name for the template asset. By default: the file name of the template
        /// </summary>
        public string templateName;
        /// <summary>
        /// Description of the template (can be long, displayed in multiline UI).
        /// </summary>
        public string description;
        /// <summary>
        /// Preview/Icon of the template.
        /// </summary>
        public Texture2D preview;
        /// <summary>
        /// List of dependencies of the scene and what to do (Clone or Reference) with each dependency.
        /// </summary>
        public DependencyInfo[] dependencies;
        /// <summary>
        /// Script dering from ISceneTemplatePipeline allowing custom code to be executed on template instantiation.
        /// </summary>
        public MonoScript templatePipeline;

        /// <summary>
        /// Is the template valid? If not it wont be displayed in the New Scene Dialog.
        /// </summary>
        public bool IsValid => templateScene;

        /// <summary>
        /// If this template is tagged as add to defaults, it will be put in the defaults list of the new scene dialog.
        /// </summary>
        public bool addToDefaults;

        /// <summary>
        /// Assign a new scene to the template and recompute all dependencies
        /// </summary>
        public void BindScene(SceneAsset scene)
        {
            templateScene = scene;
            dependencies = new DependencyInfo[0];
            UpdateDependencies();
        }

        /// <summary>
        /// Refresh all the dependencies of the template.
        /// </summary>
        public void UpdateDependencies()
        {
            if (!IsValid)
            {
                dependencies = new DependencyInfo[0];
                return;
            }

            var scenePath = AssetDatabase.GetAssetPath(templateScene.GetInstanceID());
            if (string.IsNullOrEmpty(scenePath))
            {
                dependencies = new DependencyInfo[0];
                return;
            }

            var sceneName = Path.GetFileNameWithoutExtension(scenePath);
            var sceneFolder = Path.GetDirectoryName(scenePath).Replace("\\", "/");
            var sceneCloneableDependenciesFolder = Path.Combine(sceneFolder, sceneName).Replace("\\", "/");

            var depList = new List<Object>();
            ReferenceUtils.GetSceneDependencies(scenePath, depList);

            dependencies = depList.Select(d =>
            {
                var oldDependencyInfo = dependencies.FirstOrDefault(di => di.dependency.GetInstanceID() == d.GetInstanceID());
                if (oldDependencyInfo != null)
                    return oldDependencyInfo;

                var depTypeInfo = SceneTemplateProjectSettings.Get().GetDependencyInfo(d);
                var dependencyPath = AssetDatabase.GetAssetPath(d);
                var instantiationMode = depTypeInfo.defaultInstantiationMode;
                if (depTypeInfo.supportsModification && !string.IsNullOrEmpty(dependencyPath))
                {
                    var assetFolder = Path.GetDirectoryName(dependencyPath).Replace("\\", "/");
                    if (assetFolder == sceneCloneableDependenciesFolder)
                    {
                        instantiationMode = TemplateInstantiationMode.Clone;
                    }
                }

                return new DependencyInfo()
                {
                    dependency = d,
                    instantiationMode = instantiationMode
                };
            }).ToArray();
        }

        /// <summary>
        /// Set the preview of the template and store it in the template asset itself.
        /// </summary>
        /// <param name="thumbnail">Thumbnail or preview of the template</param>
        public void AddThumbnailToAsset(Texture2D thumbnail)
        {
            if (!IsValid)
                return;

            var assetPath = AssetDatabase.GetAssetPath(this);
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var oldTexture = allAssets.FirstOrDefault(obj => obj is Texture2D);
            if (oldTexture != null)
                AssetDatabase.RemoveObjectFromAsset(oldTexture);

            AssetDatabase.AddObjectToAsset(thumbnail, assetPath);

            // You need to dirty and save the asset if you want the thumbnail to appear
            // in the project browser and the object selector.
            EditorUtility.SetDirty(thumbnail);
            AssetDatabase.SaveAssets();
        }
        /// <summary>
        /// Validate if a given script contains a class deriving from ISceneTemplatePipeline
        /// </summary>
        /// <param name="script">Script to check if ISceneTemplatePipeline instance is available.</param>
        /// <returns>True if the given script implements ISceneTemplatePipeline</returns>
        public static bool IsValidPipeline(MonoScript script)
        {
            if (script == null)
                return false;

            var scriptType = script.GetClass();
            if (!typeof(ISceneTemplatePipeline).IsAssignableFrom(scriptType))
                return false;

            return true;
        }

        /// <summary>
        /// Instantiate a SceneTemplatePipeline. This is done on each template instantiation.
        /// </summary>
        /// <returns>New instance of a ISceneTemplatePipeline that will be notified throughout the whole template instantiation process</returns>
        public ISceneTemplatePipeline CreatePipeline()
        {
            if (!IsValidPipeline(templatePipeline))
                return null;

            var pipelineInstance = Activator.CreateInstance(templatePipeline.GetClass()) as ISceneTemplatePipeline;
            return pipelineInstance;
        }
    }

    /// <summary>
    /// Descriptor storing a dependency asset of a scene and what to do with that dependency (clone or reference) on template instantiation.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{dependency} - {instantiationMode}")]
    public class DependencyInfo
    {
        /// <summary>
        /// Dependency asset of the template scene
        /// </summary>
        public Object dependency;
        /// <summary>
        /// What to do with that dependency on instantiation.
        /// </summary>
        public TemplateInstantiationMode instantiationMode;
    }
}

