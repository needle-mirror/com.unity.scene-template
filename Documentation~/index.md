# Scene Templates

Use Scene templates to customize the types of new Scene you can create in Unity. You can create a template from any Unity Scene, and then create new Scenes that are copies of the template Scene. For more information about creating Scene templates, see [Creating Scene templates](creating-scene-templates.md)

For example, you might create templates for each level in a game so everyone working on the Project can start their Scenes with the correct Assets and configuration.

## The New Scene dialog

This package modifies the **New Scene** command (menu: **File > New Scene**) to open a New Scene dialog, where you can create a new Scene from any available template. For more information, see [Creating new Scenes from templates](creating-scenes-from-templates.md).

## Built-in templates and user-defined templates

Scene templates are user-defined, meaning you create them from your own Scenes. Some Unity packages may also include Scene templates that they install when you install the package. Unity stores user-defined templates in the Project.

The Scene templates package also ships with two built-in templates: an empty Scene, and a basic Scene that contains only a camera and a directional light. Built-in templates are different from other templates because they are not Assets stored in the Project, and you cannot modify them.

## Templates and Scene dependencies

When you create a Scene template, you can specify whether Unity clones or references its dependencies (the Assets it includes) when you create a new Scene from it.

To specify which Assets are cloned for a specific template, [edit the template's properties](editing-scene-templates.md).

A typical template might contain a mix of cloned and referenced Assets. Unity sets several Asset types to be cloned by default.

To change whether Unity clones or references a given Asset type by default, edit the [Scene template Project settings](scene-template-settings.md#setting-scene-template-project-settings).

### Cloning template Assets

Cloned Assets are copies of the original Assets that the template Scene uses. When Unity creates the new Scene from the template, it automatically modifies the new Scene to use any cloned Assets. If you modify the cloned Assets, it does not affect the template Scene. If you modify the original Assets in the template Scene, it does not affect the new Scene.

Cloning template Assets is useful when you want new Scenes to contain a starting set of Assets that users might modify.

### Referencing template Assets

Referenced Assets are the original Assets that the template Scene uses. When Unity creates the new Scene from the template, the new Scene points to the same Assets as the template Scene. If you modify those Assets, it affects both the new Scene and the template Scene.

Referencing template Assets is useful when you want new Scenes to contain a default set of Assets that users build on top of, but do not modify.







