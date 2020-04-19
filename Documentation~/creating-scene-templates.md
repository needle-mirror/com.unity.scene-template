# Creating Scene templates

You can create a new Scene template in one of the following ways:

- Start an [empty template](#creating-an-empty-scene-template).
- Create a template [from an existing Scene Asset](#creating-a-template-from-an-existing-scene-asset).
- Create a template [from the current Scene](#creating-a-template-from-the-current-scene).

After you create a template, you can [edit its properties](editing-scene-templates.md) or [create new Scenes from it](creating-scenes-from-templates.md).

> [!TIP]
> Before you create a template from a Scene, create a folder with the name as the Scene, and put any Assets you want to clone in it. When you create the template, Unity automatically enables the **Clone** property for those Assets. For details, see [Editing Scene templates](editing-scene-templates.md).

## Creating an empty Scene template

You can create empty Scene templates and configure them later. An empty template does not appear in the New Scene dialog until you [edit its properties](editing-scene-templates.md) to associate a Scene Asset to it.    

To create an empty Scene template in the current Project folder:

- From the menu, select **Assets > Create > Scene Template**.

To create an empty Scene template in a specific Project folder:

1. Do one of the following:

  - In the Project view, right-click the folder to open the context menu.
  - Open the folder in the Project view, and right-click the preview pane to open the context menu.
1. Select **Create > Scene Template**.

## Creating a template from an existing Scene Asset

You can turn any existing Scene into a Scene template. After you create a template from an existing Scene, you might want to [edit its properties](editing-scene-templates.md) to specify which of its dependencies Unity clones when you create a new Scene from it.

To create a template from an existing Scene Asset:

In the Project view, do one of the following:

- Right click a Scene Asset to open the context menu. Then select **Create > Scene Template From Scene**.
- Select the Scene Asset, and from the main menu, select **Assets > Create > Scene Template From Scene**.

## Creating a template from the current Scene

To create a Scene template from the current Scene, from the menu, select **File > Save As Scene Template**.

If you have unsaved changes, Unity prompts you to save the Scene before it saves the template.

After you create a template from the current Scene, you might want to [edit its properties](editing-scene-templates.md) to specify which of its dependencies Unity clones when you create a new Scene from it.

## Creating templates from C&#35; scripts

You can create Scene templates from your C&#35; scripts.

To create an empty Scene template, use the [**CreateSceneTemplate** method](../api/UnityEditor.SceneTemplate.SceneTemplate.html#UnityEditor_SceneTemplate_SceneTemplate_CreateSceneTemplate_System_String_).

```CSharp
SceneTemplate.CreateSceneTemplate(string sceneTemplatePath)
```

To create a template from an existing Scene, use the [**CreateTemplateFromScene** method](../api/UnityEditor.SceneTemplate.SceneTemplate.html#UnityEditor_SceneTemplate_SceneTemplate_CreateTemplateFromScene_SceneAsset_System_String_). Unity automatically associates the Scene with the template, and extracts the Scene's dependencies.

```CSharp
SceneTemplate.CreateTemplateFromScene(SceneAsset sourceSceneAsset, string sceneTemplatePath);
```





