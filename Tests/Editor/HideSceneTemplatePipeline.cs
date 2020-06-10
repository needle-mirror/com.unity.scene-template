#if (SCENE_TEMPLATE_MODULE == false)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneTemplate
{
    public class HideSceneTemplatePipeline : SceneTemplatePipelineAdapter
    {
        public override bool IsValidTemplateForInstantiation(SceneTemplateAsset sceneTemplateAsset)
        {
            return false;
        }
    }
}
#endif