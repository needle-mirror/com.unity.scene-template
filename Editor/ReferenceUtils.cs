using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneTemplate
{
    internal static class ReferenceUtils
    {
        public class DependencyTypeInfo
        {
            public DependencyTypeInfo(System.Type type)
            {
                this.type = type.ToString();
                supportsModification = true;
                defaultInstantiationMode = TemplateInstantiationMode.Reference;
            }

            public DependencyTypeInfo(string type)
            {
                this.type = type;
                supportsModification = true;
                defaultInstantiationMode = TemplateInstantiationMode.Reference;
            }

            public string type;
            public bool ignore;
            public TemplateInstantiationMode defaultInstantiationMode;
            public bool supportsModification;
        }

        public static Dictionary<string, DependencyTypeInfo> kDependencyTypeInfos = new Dictionary<string, DependencyTypeInfo>();
        public static DependencyTypeInfo kDefaultDependencyTypeInfo;

        static ReferenceUtils()
        {
            kDefaultDependencyTypeInfo = new DependencyTypeInfo("<default_scene_template_dependencies>")
            {
                ignore = false,
                defaultInstantiationMode = TemplateInstantiationMode.Reference,
                supportsModification = true
            };
            AddSceneDependency(new DependencyTypeInfo(typeof(MonoScript))
            {
                ignore = true
            });
            AddSceneDependency(new DependencyTypeInfo(typeof(Shader))
            {
                ignore = true
            });
            AddSceneDependency(new DependencyTypeInfo(typeof(ComputeShader))
            {
                ignore = true
            });
            AddSceneDependency(new DependencyTypeInfo(typeof(ShaderVariantCollection))
            {
                ignore = true
            });
            AddSceneDependency(new DependencyTypeInfo(typeof(LightingDataAsset))
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
                supportsModification = false
            });
            AddSceneDependency(new DependencyTypeInfo(typeof(GameObject))
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            AddSceneDependency(new DependencyTypeInfo(typeof(Material))
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            AddSceneDependency(new DependencyTypeInfo(typeof(Cubemap))
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            AddSceneDependency(new DependencyTypeInfo(typeof(AnimationClip))
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            AddSceneDependency(new DependencyTypeInfo(typeof(UnityEditor.Animations.AnimatorController))
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            AddSceneDependency(new DependencyTypeInfo(typeof(AnimatorOverrideController))
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            AddSceneDependency(new DependencyTypeInfo(typeof(PhysicMaterial))
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            AddSceneDependency(new DependencyTypeInfo(typeof(UnityEngine.PhysicsMaterial2D))
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            AddSceneDependency(new DependencyTypeInfo("UnityEngine.Timeline.TimelineAsset")
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            AddSceneDependency(new DependencyTypeInfo("UnityEditor.Audio.AudioMixerController")
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            AddSceneDependency(new DependencyTypeInfo("UnityEngine.Rendering.PostProcessing.PostProcessResources")
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            AddSceneDependency(new DependencyTypeInfo("UnityEngine.Rendering.PostProcessing.PostProcessProfile")
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            AddSceneDependency(new DependencyTypeInfo("UnityEngine.Rendering.VolumeProfile")
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
        }

        public static void AddSceneDependency(DependencyTypeInfo info)
        {
            kDependencyTypeInfos.Add(info.type, info);
        }

        public static void GetSceneDependencies(SceneAsset scene, List<Object> dependencies)
        {
            var path = AssetDatabase.GetAssetPath(scene);
            GetSceneDependencies(path, dependencies);
        }

        public static void GetSceneDependencies(Scene scene, List<Object> dependencies)
        {
            GetSceneDependencies(scene.path, dependencies);
        }

        public static void GetSceneDependencies(string scenePath, List<Object> dependencies)
        {
            var dependencyPaths = AssetDatabase.GetDependencies(scenePath);

            // Convert the dependency paths to assets
            // Remove scene from dependencies
            foreach (var dependencyPath in dependencyPaths)
            {
                if (dependencyPath.Equals(scenePath))
                    continue;

                var dependencyType = AssetDatabase.GetMainAssetTypeAtPath(dependencyPath);
                if (dependencyType == null)
                    continue;
                var typeInfo = GetDependencyInfo(dependencyType);
                if (typeInfo.ignore)
                    continue;
                var obj = AssetDatabase.LoadAssetAtPath(dependencyPath, dependencyType);
                dependencies.Add(obj);
            }
        }

        public static DependencyTypeInfo GetDependencyInfo(System.Type type)
        {
            if (kDependencyTypeInfos.TryGetValue(type.ToString(), out var info))
            {
                return info;
            }

            return kDefaultDependencyTypeInfo;
        }

        public static DependencyTypeInfo GetDependencyInfo(UnityEngine.Object obj)
        {
            if (obj == null)
                return kDefaultDependencyTypeInfo;

            return GetDependencyInfo(obj.GetType());
        }
    }
}