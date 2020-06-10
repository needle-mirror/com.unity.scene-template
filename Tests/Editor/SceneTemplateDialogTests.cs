#if (SCENE_TEMPLATE_MODULE == false)
using System.Collections;
using System.Dynamic;
using System.Linq;
using System.Net;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.SceneTemplate;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;


public class SceneTemplateDialogTests
{
    [OneTimeSetUp]
    public void Init()
    {
        TestUtils.CreateFolder(TestUtils.k_TestGeneratedFolder);

        // Create a new scene and a scene template for it
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        EditorSceneManager.SaveScene(newScene, TestUtils.k_AssetsTempScene);
        var newSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(TestUtils.k_AssetsTempScene);
        var sceneTemplateAsset = SceneTemplate.CreateTemplateFromScene(newSceneAsset, TestUtils.k_AssetsTempSceneTemplate);
        sceneTemplateAsset.templateName = "Test title";
        sceneTemplateAsset.description = "Test description";
        sceneTemplateAsset.preview = Texture2D.whiteTexture;
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        TestUtils.DeleteFolder(TestUtils.k_TestGeneratedFolder);
    }

    [UnityTest]
    public IEnumerator Open()
    {
        var dialog = OpenDialog();
        yield return null;

        Assert.IsTrue(dialog.GetDefaultSceneTemplateInfo().name == SceneTemplateDialog.basicTemplateName);

        dialog.Close();
        yield return null;

        // The window should have been closed and destroy by then.
        Assert.IsTrue(!dialog);
    }

    [UnityTest]
    public IEnumerator CloseWithKeyboard()
    {
        var dialog = OpenDialog();
        yield return null;

        var rootElement = dialog.rootVisualElement;
        rootElement.Focus();

        var escapeKeyEvent = new Event
        {
            type = EventType.KeyUp,
            keyCode = KeyCode.Escape
        };
        using (var keyUpEvent = KeyUpEvent.GetPooled(escapeKeyEvent))
        {
            rootElement.SendEvent(keyUpEvent);
        }
        yield return null;

        // The window should have been closed and destroy by then.
        Assert.IsTrue(!dialog);
    }

    [UnityTest]
    public IEnumerator PopulateGridView()
    {
        var dialog = OpenDialog();
        yield return null;

        var gridViews = dialog.rootVisualElement.Query<GridView>().ToList();
        Assert.AreEqual(1, gridViews.Count, "There should be one ListView");
        var gridView = gridViews[0];

        var templatesFromItems = gridView.items.Select(item => item.userData as SceneTemplateInfo).ToList();
        var builtinTemplates = templatesFromItems.Where(t => t.IsInMemoryScene).ToList();
        var userDefineTemplates = templatesFromItems.Where(t => !t.IsInMemoryScene).ToList();
        Assert.IsTrue(builtinTemplates.Count == 2);

        // Validate that the Hidden Template is not shown in the dialog:
        var containsHiddenTemplate = userDefineTemplates.Any(template => template.assetPath.Contains("HiddenInDialog-template"));
        Assert.IsFalse(containsHiddenTemplate, "HiddenInDialog-template should not be shown in dialog.");

        dialog.Close();
    }

    [UnityTest]
    public IEnumerator PopulateDescriptionFromSelection()
    {
        yield return OpenDialogAndValidateDescription(true);
    }

    [UnityTest]
    public IEnumerator PopulateDescriptionLastTemplateSelection()
    {
        yield return OpenDialogAndValidateDescription(true);
        yield return OpenDialogAndValidateDescription(false);
    }

    public IEnumerator OpenDialogAndValidateDescription(bool selectTempSceneTemplate)
    {
        var dialog = OpenDialog();
        yield return null;

        var gridView = dialog.rootVisualElement.Query<GridView>().ToList()[0];

        var projectItems = gridView.items.Select(item => item.userData as SceneTemplateInfo).ToList();
        Assert.IsNotNull(projectItems);
        Assert.IsTrue(projectItems.Count > 0);

        var sceneTemplateInfo = projectItems.Find(info => info.Equals(TestUtils.k_AssetsTempSceneTemplate));
        Assert.IsNotNull(sceneTemplateInfo, $"The project listview should contain asset {TestUtils.k_AssetsTempSceneTemplate}");

        var index = projectItems.IndexOf(sceneTemplateInfo);
        Assert.Greater(index, -1);

        if (selectTempSceneTemplate)
        {
            gridView.SetSelection(sceneTemplateInfo.GetHashCode());
            Assert.IsTrue(gridView.selectedItems.First().userData == sceneTemplateInfo);
            yield return null;
        }
        
        var descriptionContainer = dialog.rootVisualElement.Q(null, StyleSheetLoader.Styles.classDescriptionContainer);
        Assert.IsNotNull(descriptionContainer, "There should be a description section in the dialog");

        var sceneTitleLabel = descriptionContainer.Q<Label>(SceneTemplateDialog.k_SceneTemplateTitleLabelName);
        Assert.IsNotNull(sceneTitleLabel);
        Assert.AreEqual(sceneTemplateInfo.name, sceneTitleLabel.text);

        var scenePathLabel = descriptionContainer.Q<Label>(SceneTemplateDialog.k_SceneTemplatePathName);
        Assert.IsNotNull(scenePathLabel);
        Assert.AreEqual(sceneTemplateInfo.assetPath, scenePathLabel.text);

        var sceneDescriptionLabel = descriptionContainer.Q<Label>(SceneTemplateDialog.k_SceneTemplateDescriptionName);
        Assert.IsNotNull(sceneDescriptionLabel);
        Assert.AreEqual(sceneTemplateInfo.description, sceneDescriptionLabel.text);

        var scenePreviewElement = descriptionContainer.Q(SceneTemplateDialog.k_SceneTemplateThumbnailName);
        Assert.IsNotNull(scenePreviewElement);
        Assert.IsTrue(!sceneTemplateInfo.thumbnail || sceneTemplateInfo.thumbnail == scenePreviewElement.style.backgroundImage.value.texture);

        dialog.Close();
    }

    [UnityTest]
    public IEnumerator SessionPrefs()
    {
        var dialog = OpenDialog();
        yield return null;

        var gridView = dialog.rootVisualElement.Query<GridView>().First();
        Assert.IsNotNull(gridView, "The dialog should have a gridView.");

        Assert.IsTrue(gridView.selectedItems.Any(), "Selection should not be empty");
        var selectedItem = gridView.selectedItems.First();

        var anotherItem = gridView.items.FirstOrDefault(i => i != selectedItem && !(i.userData as SceneTemplateInfo).IsInMemoryScene);
        var anotherTemplateInfo = anotherItem.userData as SceneTemplateInfo;
        Assert.NotNull(anotherTemplateInfo);
        Assert.NotNull(anotherTemplateInfo.assetPath);
        var anotherItemPath = anotherTemplateInfo.assetPath;
        gridView.SetSelection(anotherItem);

        dialog.Close();
        yield return null;

        dialog = OpenDialog();
        // Yield another time to make sure the order of the calls to delayCall does not matter
        yield return null;

        gridView = dialog.rootVisualElement.Query<GridView>().First();

        Assert.IsTrue(gridView.selectedItems.Any());
        var selectedTemplate = gridView.selectedItems.First().userData as SceneTemplateInfo;
        Assert.AreEqual(selectedTemplate.assetPath, anotherItemPath, "Selected item should have been restored from the preferences.");

        dialog.Close();
    }

    [UnityTest]
    public IEnumerator OpenBasicSceneAdditive()
    {
        if (SceneTemplateDialog.CanLoadAdditively())
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        yield return null;

        Assert.IsFalse(SceneTemplateDialog.CanLoadAdditively());

        var success = SceneTemplateDialog.CreateEmptyScene(true);
        Assert.IsFalse(success, "Load additively should fail for Empty Scene when there is already an im-memory scene loaded.");

        success = SceneTemplateDialog.CreateDefaultScene(true);
        Assert.IsFalse(success, "Load additively should fail for Default Scene when there is already an im-memory scene loaded.");
    }

    [UnityTest]
    public IEnumerator EditTemplate()
    {
        var dialog = OpenDialog();
        yield return null;

        var gridView = dialog.rootVisualElement.Query<GridView>().ToList()[0];
        var firstUserItem = gridView.items.FirstOrDefault(item => !(item.userData as SceneTemplateInfo).IsInMemoryScene);

        Assert.IsNotNull(firstUserItem, "There should be an item in the project template list.");

        var firstUserDefineTemplate = firstUserItem.userData as SceneTemplateInfo;

        dialog.OnEditTemplate(firstUserDefineTemplate);
        yield return null;

        // Selection should be our asset
        var selectedObject = Selection.activeObject;
        var selectedObjectAssetPath = AssetDatabase.GetAssetPath(selectedObject);
        Assert.AreEqual(firstUserDefineTemplate.assetPath, selectedObjectAssetPath);

        // The window should have been closed and destroy by then.
        Assert.IsTrue(!dialog);
    }

    [Test]
    public void TestSceneTemplateProjectSettings()
    {
        SceneTemplateProjectSettings.Reset();
        Assert.IsFalse(System.IO.File.Exists(SceneTemplateProjectSettings.k_Path));

        var settings = SceneTemplateProjectSettings.Get();
        Assert.IsTrue(System.IO.File.Exists(SceneTemplateProjectSettings.k_Path));

        Assert.IsFalse(settings.GetPinState(SceneTemplateDialog.emptyTemplateName));

        settings.SetPinState(SceneTemplateDialog.emptyTemplateName, true);

        Assert.IsTrue(settings.GetPinState(SceneTemplateDialog.emptyTemplateName));

        var settingsReloaded = SceneTemplateProjectSettings.Load(SceneTemplateProjectSettings.k_Path);
        Assert.IsTrue(settingsReloaded.GetPinState(SceneTemplateDialog.emptyTemplateName));
    }

    private static SceneTemplateDialog OpenDialog()
    {
        var dialog = SceneTemplateDialog.ShowWindow();
        Assert.IsNotNull(dialog);
        return dialog;
    }
}
#endif