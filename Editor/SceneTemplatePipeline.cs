#if (SCENE_TEMPLATE_MODULE == false)
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneTemplate;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Interface a user should derive from when a user wants to package a custom code sequence when a template is instantiated. ISceneTemplatePipeline are instantiated once when a template is instantiated and are notified multiple times through the instantiation sequence.
/// </summary>
public interface ISceneTemplatePipeline
{
    /// <summary>
    /// Event called before we display the New Template Dialog to determine if this template should be available to user.
    /// </summary>
    /// <param name="sceneTemplateAsset">Scene Template asset that would be avauilable in New Scene Dialog</param>    
    bool IsValidTemplateForInstantiation(SceneTemplateAsset sceneTemplateAsset);

    /// <summary>
    /// Event called before the template is instantiated.
    /// </summary>
    /// <param name="sceneTemplateAsset">Scene Template asset that will be instantiated</param>
    /// <param name="isAdditive">Is the new scene created in additive mode.</param>
    /// <param name="sceneName">What is the path of the new scene (could be empty if untitled)</param>
    void BeforeTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, bool isAdditive, string sceneName);
    /// <summary>
    /// Event called after the template is instantiated and the new scene is still loaded.
    /// </summary>
    /// <param name="sceneTemplateAsset">Scene Template asset that will be instantiated</param>
    /// <param name="scene">The newly created scene</param>
    /// <param name="isAdditive">Is the new scene created in additive mode.</param>
    /// <param name="sceneName">What is the path of the new scene (could be empty if untitled)</param>
    void AfterTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, Scene scene, bool isAdditive, string sceneName);
}

/// <summary>
/// Adapter implementing all the function of ISceneTemplatePipeline for easier usage.
/// </summary>
public class SceneTemplatePipelineAdapter : ISceneTemplatePipeline
{
    /// <summary>
    /// Event called before we display the New Template Dialog to determine if this template should be available to user.
    /// </summary>
    /// <param name="sceneTemplateAsset">Scene Template asset that would be avauilable in New Scene Dialog</param>    
    public virtual bool IsValidTemplateForInstantiation(SceneTemplateAsset sceneTemplateAsset)
    {
        return true;
    }

    /// <summary>
    /// Event called before the template is instantiated.
    /// </summary>
    /// <param name="sceneTemplateAsset">Scene Template asset that will be instantiated</param>
    /// <param name="isAdditive">Is the new scene created in additive mode.</param>
    /// <param name="sceneName">What is the path of the new scene (could be empty if untitled)</param>
    public virtual void BeforeTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, bool isAdditive, string sceneName)
    {
    }

    /// <summary>
    /// Event called after the template is instantiated and the new scene is still loaded.
    /// </summary>
    /// <param name="sceneTemplateAsset">Scene Template asset that will be instantiated</param>
    /// <param name="scene">The newly created scene</param>
    /// <param name="isAdditive">Is the new scene created in additive mode.</param>
    /// <param name="sceneName">What is the path of the new scene (could be empty if untitled)</param>
    public virtual void AfterTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, Scene scene, bool isAdditive, string sceneName)
    {
    }
}
#endif