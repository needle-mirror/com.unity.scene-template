using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneTemplate
{
    public class DummySceneTemplatePipeline : ISceneTemplatePipeline
    {
        public static bool beforeHit;
        public static bool afterHit;

        public static void CleanTestData()
        {
            beforeHit = false;
            afterHit = false;
        }

        public void BeforeTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, bool isAdditive, string sceneName)
        {
            if (sceneTemplateAsset)
            {
                Debug.Log($"BeforeTemplateInstantiation {sceneTemplateAsset.name} isAdditive: {isAdditive} sceneName: {sceneName}");
            }
            beforeHit = true;
        }

        public void AfterTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, Scene scene, bool isAdditive, string sceneName)
        {
            if (sceneTemplateAsset)
            {
                Debug.Log($"AfterTemplateInstantiation {sceneTemplateAsset.name} scene: {scene} isAdditive: {isAdditive} sceneName: {sceneName}");
            }
            afterHit = true;
        }
    }
}