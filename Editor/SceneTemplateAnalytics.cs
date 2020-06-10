#if (SCENE_TEMPLATE_MODULE == false)
// #define SCENE_TEMPLATE_ANALYTICS_LOGGING
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Analytics;
#if SCENE_TEMPLATE_ANALYTICS_LOGGING
using UnityEngine;
using System.Threading;
#endif

namespace UnityEditor.SceneTemplate
{
    internal static class SceneTemplateAnalytics
    {
        [Serializable]
        internal class AnalyticDepInfo
        {
            public AnalyticDepInfo(DependencyInfo info)
            {

                dependencyType = info.dependency.GetType().ToString();
                _instantiationMode = info.instantiationMode;
                instantiationMode = Enum.GetName(typeof(TemplateInstantiationMode), info.instantiationMode);
                count = 1;
            }

            internal TemplateInstantiationMode _instantiationMode;
            public string dependencyType;
            public int count;
            public string instantiationMode;
        }

        internal static bool FillAnalyticDepInfos(SceneTemplateAsset template, List<AnalyticDepInfo> infos)
        {
            var hasCloneableDependencies = false;
            var tempDepInfos = new Dictionary<string, List<AnalyticDepInfo>>();

            foreach (var dep in template.dependencies)
            {
                if (dep.instantiationMode == TemplateInstantiationMode.Clone)
                    hasCloneableDependencies = true;

                if (!dep.dependency || dep.dependency == null)
                    continue;

                if (tempDepInfos.TryGetValue(dep.dependency.GetType().ToString(), out var infosPerType))
                {
                    var foundInfo = infosPerType.Find(info => info._instantiationMode == dep.instantiationMode);
                    if (foundInfo != null)
                    {
                        foundInfo.count++;
                    }
                    else
                    {
                        infosPerType.Add(new AnalyticDepInfo(dep));
                    }
                }
                else
                {
                    infosPerType = new List<AnalyticDepInfo>();
                    infosPerType.Add(new AnalyticDepInfo(dep));
                    tempDepInfos.Add(dep.dependency.GetType().ToString(), infosPerType);
                }
            }

            foreach (var kvp in tempDepInfos)
            {
                foreach (var depInfo in kvp.Value)
                {
                    infos.Add(depInfo);
                }
            }

            return hasCloneableDependencies;
        }

        internal enum SceneInstantiationType
        {
            NewSceneMenu,
            TemplateDoubleClick,
            Scripting,
            EmptyScene,
            DefaultScene
        }

        [Serializable]
        internal class SceneInstantiationEvent
        {
            private DateTime m_StartTime;

            public long elapsedTimeMs => (long)(DateTime.Now - m_StartTime).TotalMilliseconds;
            public string sceneName;
            public List<AnalyticDepInfo> dependencyInfos = new List<AnalyticDepInfo>();
            public string instantiationType;
            public long duration;
            public bool isCancelled;
            public bool additive;
            public bool hasCloneableDependencies;
            public SceneInstantiationEvent(SceneTemplateAsset template, SceneInstantiationType instantiationType)
            {
                this.instantiationType = Enum.GetName(typeof(SceneInstantiationType), instantiationType);
                sceneName = AssetDatabase.GetAssetPath(template.templateScene);
                hasCloneableDependencies = FillAnalyticDepInfos(template, dependencyInfos);
                m_StartTime = DateTime.Now;
            }

            public SceneInstantiationEvent(SceneInstantiationType instantiationType)
            {
                this.instantiationType = Enum.GetName(typeof(SceneInstantiationType), instantiationType);
                m_StartTime = DateTime.Now;
            }

            public void Done()
            {
                if (duration == 0)
                    duration = elapsedTimeMs;
            }

        }

        internal enum TemplateCreationType
        {
            CreateFromTargetSceneMenu,
            SaveCurrentSceneAsTemplateMenu,
            Scripting
        }

        [Serializable]
        internal class SceneTemplateCreationEvent
        {
            public string sceneName;
            public List<AnalyticDepInfo> dependencyInfos = new List<AnalyticDepInfo>();
            public string templateCreationType;
            public int numberOfTemplatesInProject;
            public bool hasCloneableDependencies;

            public SceneTemplateCreationEvent(SceneTemplateAsset template, TemplateCreationType templateCreationType)
            {
                this.templateCreationType = Enum.GetName(typeof(TemplateCreationType), templateCreationType);
                sceneName = AssetDatabase.GetAssetPath(template.templateScene);
                hasCloneableDependencies = FillAnalyticDepInfos(template, dependencyInfos);
                numberOfTemplatesInProject = SceneTemplateUtils.GetSceneTemplatePaths().Count();
            }
        }

        enum EventName
        {
            SceneInstantiationEvent,
            SceneTemplateCreationEvent
        }

        internal static string Version;
        private static bool s_Registered;

        static SceneTemplateAnalytics()
        {
            Version = SceneTemplateUtils.GetPackageVersion();
        }

        internal static void SendSceneInstantiationEvent(SceneInstantiationEvent evt)
        {
            evt.Done();
            Send(EventName.SceneInstantiationEvent, evt);
        }

        internal static void SendSceneTemplateCreationEvent(SceneTemplateCreationEvent evt)
        {
            Send(EventName.SceneTemplateCreationEvent, evt);
        }

        private static bool RegisterEvents()
        {
            if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
            {
                return false;
            }

            if (!EditorAnalytics.enabled)
            {
                Console.WriteLine("[ST] Editor analytics are disabled");
                return false;
            }

            if (s_Registered)
            {
                return true;
            }

            var allNames = Enum.GetNames(typeof(EventName));
            if (allNames.Any(eventName => !RegisterEvent(eventName)))
            {
                return false;
            }

            s_Registered = true;
            return true;
        }

        private static bool RegisterEvent(string eventName)
        {
            const string vendorKey = "unity.scene-template";
            var result = EditorAnalytics.RegisterEventWithLimit(eventName, 100, 1000, vendorKey);
            switch (result)
            {
                case AnalyticsResult.Ok:
                    {
#if SCENE_TEMPLATE_ANALYTICS_LOGGING
                    Debug.Log($"SceneTemplate: Registered event: {eventName}");
#endif
                        return true;
                    }
                case AnalyticsResult.TooManyRequests:
                    // this is fine - event registration survives domain reload (native)
                    return true;
                default:
                    {
                        Console.WriteLine($"[ST] Failed to register analytics event '{eventName}'. Result: '{result}'");
                        return false;
                    }
            }
        }

        private static void Send(EventName eventName, object eventData)
        {
#if SCENE_TEMPLATE_ANALYTICS_LOGGING
#else
            if (SceneTemplateUtils.IsDeveloperMode())
                return;
#endif

            if (!RegisterEvents())
            {
#if SCENE_TEMPLATE_ANALYTICS_LOGGING
                Console.WriteLine($"[ST] Analytics disabled: event='{eventName}', time='{DateTime.Now:HH:mm:ss}', payload={EditorJsonUtility.ToJson(eventData, true)}");
#endif
                return;
            }
            try
            {
                var result = EditorAnalytics.SendEventWithLimit(eventName.ToString(), eventData);
                if (result == AnalyticsResult.Ok)
                {
#if SCENE_TEMPLATE_ANALYTICS_LOGGING
                    Console.WriteLine($"[ST] Event='{eventName}', time='{DateTime.Now:HH:mm:ss}', payload={EditorJsonUtility.ToJson(eventData, true)}");
#endif
                }
                else
                {
                    Console.WriteLine($"[ST] Failed to send event {eventName}. Result: {result}");
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

#if SCENE_TEMPLATE_ANALYTICS_LOGGING
        static T RandomValue<T>(T[] values)
        {
            var index = UnityEngine.Random.Range(0, values.Length - 1);
            return values[index];
        }

        static string[] s_SampleScenes = new[] { "Scene.unity", "MyNewStuff.unity", "SomethingElse.unity", "Level1.unity", "Level4.unity", "Level6.unity" };
        static bool[] s_Bools = new[] { true, false };

        [MenuItem("Assets/Spam SceneTemplateCreation Event", false, 180005)]
        static void SpamCreationEvents()
        {
            var template = Selection.activeObject as SceneTemplateAsset;
            if (template == null)
                return;

            for (int i = 0; i < 25; ++i)
            {
                var creationEvent = new SceneTemplateCreationEvent(template, (TemplateCreationType)UnityEngine.Random.Range(0, 2));
                creationEvent.sceneName = "Assets/" + RandomValue(s_SampleScenes);
                creationEvent.numberOfTemplatesInProject = UnityEngine.Random.Range(0, 100);
                
                SendSceneTemplateCreationEvent(creationEvent);
                Thread.Sleep(500);
            }
        }

        [MenuItem("Assets/Spam SceneTemplateInstantiation Event", false, 180005)]
        static void SpamInstantiationEvents()
        {
            var template = Selection.activeObject as SceneTemplateAsset;
            if (template == null)
                return;

            for (int i = 0; i < 25; ++i)
            {
                var creationEvent = new SceneInstantiationEvent(template, (SceneInstantiationType)UnityEngine.Random.Range(0, 2));
                creationEvent.sceneName = "Assets/" + RandomValue(s_SampleScenes);
                creationEvent.isCancelled = RandomValue(s_Bools);
                creationEvent.additive = RandomValue(s_Bools);
                creationEvent.duration = UnityEngine.Random.Range(100, 2000);
                SendSceneInstantiationEvent(creationEvent);
                Thread.Sleep(500);
            }
        }
#endif
    }
}
#endif