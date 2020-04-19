# Customizing new Scene creation

To run custom code when Unity instantiates a new Scene from a template, create a Scene Template Pipeline script and connect it to the template. Each time you create a new Scene from the template, Unity creates a new instance of the pipeline script as well.

To connect the script to a template.

1. Inspect the template to [edit its properties](editing-scene-templates.md).
2. Set the **Scene Template Pipeline** property to point to your Scene Template Pipeline script.

You can also use the [`SceneTemplateAsset.templatePipeline`](../api/UnityEditor.SceneTemplate.SceneTemplateAsset.html#UnityEditor_SceneTemplate_SceneTemplateAsset_templatePipeline) method to connect the script to the template via C&#35;.

A Scene Template Pipeline script must derive from the [`ISceneTemplatePipeline`] interface or [`SceneTemplatePipelineAdapter`]. It should implement the events you want to react to; for example, `BeforeTemplateInstantiation` or `AfterTemplateInstantiation` in the code below.

**Example:**

```CSharp
using UnityEditor.SceneTemplate;
using UnityEngine;
using UnityEngine.SceneManagement;
public class DummySceneTemplatePipeline : ISceneTemplatePipeline
{
    public void BeforeTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, bool isAdditive, string sceneName)
    {
        if (sceneTemplateAsset)
        {
            Debug.Log($"Before Template Pipeline {sceneTemplateAsset.name} isAdditive: {isAdditive} sceneName: {sceneName}");
        }
    }

    public void AfterTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, Scene scene, bool isAdditive, string sceneName)
    {
        if (sceneTemplateAsset)
        {
            Debug.Log($"After Template Pipeline {sceneTemplateAsset.name} scene: {scene} isAdditive: {isAdditive} sceneName: {sceneName}");
        }
    }
}
```

## Scene Template instantiation sequence

When you create a new Scene from a template with cloneable dependencies, Unity performs several file operations. Most of these operations trigger Unity events that you can listen for, and react to, in scripts.

The instantiation sequence is as follows:

1. You click **Create** in the [New Scene dialog](creating-scenes-from-templates.md). Unity calls the:
    - Scene template Asset.
    - Template Scene. This is the Unity Scene associated with the template.
    - New Scene. This is a new instance of the template Scene.

1. Unity triggers the `ISceneTemplatePipeline.BeforeTemplateInstantiation` event for the template Asset, and binds the Asset to a `ISceneTemplatePipeline` script that it triggers.
1. Unity triggers the [`SceneTemplate.NewTemplateInstantiating`](../api/UnityEditor.SceneTemplate.SceneTemplate.html#UnityEditor_SceneTemplate_SceneTemplate_newSceneTemplateInstantiating) event.
1. Unity creates a new Scene that is a copy of the template Scene.
1. Unity creates a folder with the same name as the new Scene, and copies all cloneable dependencies into that folder.
1. Unity opens the new Scene in memory, and triggers the following events:
    - [`EditorSceneManager.sceneOpening`](https://docs.unity3d.com/ScriptReference/SceneManagement.EditorSceneManager-sceneOpening.html)
    - [`MonoBehavior.OnValidate`](https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnValidate.html) (on all GameObjects that implement it)
    - [`EditorSceneManager.sceneOpened`](https://docs.unity3d.com/ScriptReference/SceneManagement.EditorSceneManager-sceneOpened.html)

1. Unity remaps references to all cloneable Assets, so the new Scene points to the clones.
1. Unity saves the new Scene, and triggers the following events:
    - [`EditorSceneManager.sceneSaving`](https://docs.unity3d.com/ScriptReference/SceneManagement.EditorSceneManager-sceneSaving.html)
    - [`EditorSceneManager.sceneSaved`](https://docs.unity3d.com/ScriptReference/SceneManagement.EditorSceneManager-sceneSaved.html)
1. Unity triggers the `ISceneTemplatePipeline.AfterTemplateInstantiation` for the template Asset, and binds the Asset to a `ISceneTemplatePipeline` script that it triggers.
1. Unity triggers the [`SceneTemplate.NewTemplateInstantiated`](../api/UnityEditor.SceneTemplate.SceneTemplate.html#UnityEditor_SceneTemplate_SceneTemplate_newSceneTemplateInstantiated) event.