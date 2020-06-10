# About Scene Templates

Scene Templates are a way to customize the "New Scene" workflow of Unity. By creating scenes that you wish to act as templates, and assigning them to the new 'Scene Template' asset type, you can use the new File > New menu to spawn more effective starting points to your projects.

# Installing the Scene Templates Package

Although Scene Templates will eventually be rolled into Unity itself as a core feature, for now during the feedback phase, we provide it as a Preview package that you can install by going to Window > Package Manager.

To install this package, follow the instructions in the [Package Manager documentation](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@latest/index.html).

Note that Scene Templates are a *Preview Package* and that you need to use the [Advanced button](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@2.1/manual/index.html#advanced) in order to have access to those kind of packages.

# Using Scene Templates

A quick introduction video to the feature:

<a href="https://www.youtube.com/watch?feature=player_embedded&v=xAY32lbIeNo" target="_blank"><img src="https://img.youtube.com/vi/xAY32lbIeNo/0.jpg"
alt="IMAGE ALT TEXT HERE" width="240" height="180" border="10" /></a>


To create a new template:
- Create a scene that you wish to be your template and save it in your Project
- Right click on an existing scene and select: 'Create Scene Template From Scene' OR use the top menu item 'File > Save As Scene Template'. You will be prompted to save the asset somewhere in your project. We suggest creating a Templates folder in which to keep your scenes and their dependent assets, along with the Scene Template assets.

Once you have created a scene template, simply select the asset in the Project and then fill in the details in the Inspector. Here you can provide a title and description, as well as take a snapshot for the File > New menu. You can also control how the dependencies behave when the template is used to create a new scene - do you wish the asset used in the scene to be 'Cloned' (creates new copies in a folder named the same as your new scene) or Referenced? (uncheck the 'Clone' box for this option - this simply refers back to the original asset, but beware of overwriting settings, and use 'Clone' if unsure).

To Create a new scene from a template asset:

- From the top menu choose File -> New 

From here simply select a template from the left column, and then choose whether to Create the scene or Create Additively. Creating additively means that they will be instantiated alongside any scenes you have open currently. 

The manual can be found here:

* [Scene Template](Documentation~/index.md)
* [Usage](Documentation~/usage.md)
* [Known Limitations](Documentation~/limitations.md)
* [API](Documentation~/api.md)

## Requirements

This version of Scene Template is compatible with the following versions of the Unity Editor:

* 2020.1 and later
