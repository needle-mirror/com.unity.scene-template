# Changelog

## [1.0.0-preview.9] - 2020-04-20
- [UX] Add a mechanism to persist the pin state of builtin and in readonly templates.
- [UX] Remove the workflow where we would hide Basic template if any user defined template was pinned.
- [UX] Fix most user facing labels and helpbox according to a doc review.
- [UX] Add Scene Template Project Settings page that allows a user to change the default instantiation mode of various types.
- [UX] Textures is to be cloned by default
- [UX] Builtin templates are sorted last
- [UX] Use the proper "save scene dialog" when instantiating a template are current scene is dirty.
- [UX] Add proper doc link for scene template asset and in the gridview if no template are available.
- [UX] Add a splitter in Scene template dialog between template list and description.
- [UX] SubScenes are always cloned.
- [Fix] Ensure we properly merge from default type infos after loading existing ones.
- [DOC] Final edited version of documentation.
- [Fix] Fix Description populate from last template.

## [1.0.0-preview.8] - 2020-03-13
- [UX] New Grid view widget
- [UX] New Grid view design for new scene dialog
- [UX] New Pin workflow + replace basic scene workflow
- [UX] New Layout and design for the Scene Dialog
- [UX] Meaningful description for both default builtin new scene workflow (emtpy + basic).
- [UX] Inspector uses a "Component with header" way of displaying its multiple sections.

## [1.0.0-preview.7] - 2019-11-28
- [UX-API] Allow SceneTemplatePipeline to filter out its bound template according to a set of condition.
- [UX] Add a toggle in dependency header to batch clone/reference all dependencies.

## [1.0.0-preview.6] - 2019-11-15
- [Package] Properly specify the unityVersion for this package.

## [1.0.0-preview.5] - 2019-10-31
- [UX] New Scene Templates are selected upon creation.
- [Fix] Fix multi-selection toggle when toggling items not contained in the selection.
- [UX] Added tab support to cycle through the buttons. Pressing "Enter" will execute the selected button.
- [Fix] Cropped thumbnails instead of stretched.

## [1.0.0-preview.4] - 2019-10-29
- [Fix] Fix exception using arrow keys to seek to empty template list.
- [Fix] Boosted the minimal size of the dialog.
- [UX] Renamed Default list to Defaults. The 'Is Default' toggle is now 'Add to Defaults"
- [UX] Replaced the 'Load additively' toggle with a button called 'Edit Template' to edit the selected template.
- [UX] Renamed 'Create Scene' button to 'Create'.
- [UX] Added 'Create Additive' button, which creates a new scene from a template and opens it additively.
- [UX] The dialog is now aware of modifications of SceneTemplate assets. Any change made in the inspector will be reflected in the dialog.

## [1.0.0-preview.3] - 2019-10-17
- [Fix] Updated the minimum size of the dialog.
- [Fix] Fix listview header padding in the dialog.
- [Fix] VolumeProfile is now set to clone by default.
- [UX] Users can now toggle Clone with spacebar.
- [UX] The New Scene dialog is now centered on first opening.
- [UX] New UI for snapshots.
- [UX] New scene window is undoackable
- [UX] Append "-template" to all new Scene template
- [UX] Use LastFolder for all file operations
- [Fix] Ensure defaults are set for scene template bound through the inspector to their scene.
- [Fix] Fix exception when selecting thumbnail.
- [Fix] Show a warning instead of an exception when loading additively an in-memory scene while another in-memory is loaded.
- [UX] Default tenmplate workflow (which will populate the Default List)


## [1.0.0-preview.2] - 2019-10-13
- [UX] Dependency list pixel polishing
- [UX] Prefab are mapped as GameObject in ReferenceUtils

## [1.0.0-preview.1] - 2019-09-05
### This is the first release of the *Unity Scene Template Package* (Create new scenes from various user defined templates.)
