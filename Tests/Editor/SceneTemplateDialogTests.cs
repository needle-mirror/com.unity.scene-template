using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.SceneTemplate
{
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
        public IEnumerator PopulateListView()
        {
            var dialog = OpenDialog();
            yield return null;

            var listViews = dialog.rootVisualElement.Query<ListView>().ToList();
            Assert.AreEqual(2, listViews.Count, "There should be two listviews.");

            var defaultsListView = listViews[0];
            Assert.IsNotNull(defaultsListView, "The dialog should have a default listview.");

            var projectListView = listViews[1];
            Assert.IsNotNull(projectListView, "The dialog should have a project listview.");

            var defaultItems = defaultsListView.itemsSource as List<SceneTemplateInfo>;
            Assert.IsNotNull(defaultItems);

            var projectItems = projectListView.itemsSource as List<SceneTemplateInfo>;
            Assert.IsNotNull(projectItems);

            void ValidateItem(string assetPath, List<SceneTemplateInfo> items, VisualElement listview)
            {
                // Validate source items contains sceneTemplateInfo
                var foundItem = items.Find(info => info.Equals(assetPath));
                Assert.IsNotNull(foundItem, $"Listview does not contain asset {assetPath}");

                // Validate label
                var rows = listview.Query(null, StyleSheetLoader.Styles.classListViewItem).ToList();
                var rowElement = rows.FirstOrDefault(el =>
                {
                    var label = el.Q<Label>();
                    return label.text == foundItem.name;
                });
                Assert.IsNotNull(rowElement, $"Listview should have a row with label {foundItem.name}");

                // Validate thumbnail
                var thumbnail = rowElement.Q(SceneTemplateDialog.k_SceneTemplateListViewThumbnailName);
                if (!foundItem.thumbnail)
                    Assert.AreSame(dialog.m_DefaultListViewThumbnail, thumbnail.style.backgroundImage.value.texture);
                else
                    Assert.AreSame(foundItem.thumbnail, thumbnail.style.backgroundImage.value.texture);
            }

            // Check if it contains default templates
            Assert.IsTrue(defaultItems.Count >= 2);
            Assert.IsTrue(defaultItems.All(item => item.isDefault));
            ValidateItem(SceneTemplateDialog.emptyTemplateName, defaultItems, defaultsListView);

            Assert.IsTrue(defaultItems[1].name == SceneTemplateDialog.basicTemplateName || defaultItems[1].isDefault);

            // Check if it contains a user template
            ValidateItem(TestUtils.k_AssetsTempSceneTemplate, projectItems, projectListView);

            // Validate that the Hidden Template is not shown in the dialog:
            var containsHiddenTemplate = projectItems.Any(item => item.assetPath.Contains("HiddenInDialog-template"));
            Assert.IsFalse(containsHiddenTemplate, "HiddenInDialog-template should not be shown in dialog.");

            dialog.Close();
        }

        [UnityTest]
        public IEnumerator PopulateDescription()
        {
            var dialog = OpenDialog();
            yield return null;

            var listViews = dialog.rootVisualElement.Query<ListView>().ToList();
            Assert.AreEqual(2, listViews.Count, "There should be two listviews.");

            var projectListView = listViews[1];
            Assert.IsNotNull(projectListView, "The dialog should have a project listview.");

            var projectItems = projectListView.itemsSource as List<SceneTemplateInfo>;
            Assert.IsNotNull(projectItems);

            var sceneTemplateInfo = projectItems.Find(info => info.Equals(TestUtils.k_AssetsTempSceneTemplate));
            Assert.IsNotNull(sceneTemplateInfo, $"The project listview should contain asset {TestUtils.k_AssetsTempSceneTemplate}");

            var index = projectItems.IndexOf(sceneTemplateInfo);
            Assert.Greater(index, -1);

            projectListView.selectedIndex = index;
            yield return null;

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
            Assert.AreSame(sceneTemplateInfo.thumbnail, scenePreviewElement.style.backgroundImage.value.texture);

            dialog.Close();
        }

        [UnityTest]
        public IEnumerator SessionPrefs()
        {
            var dialog = OpenDialog();
            yield return null;

            var listView = dialog.rootVisualElement.Query<ListView>().First();
            Assert.IsNotNull(listView, "The dialog should have a listview.");

            var oldLastSelectedTemplateIndex = listView.selectedIndex;

            var newLastSelectedTemplateIndex = (++oldLastSelectedTemplateIndex) % listView.itemsSource.Count;

            listView.selectedIndex = newLastSelectedTemplateIndex;

            dialog.Close();
            yield return null;

            dialog = OpenDialog();
            // Yield another time to make sure the order of the calls to delayCall does not matter
            yield return null;

            listView = dialog.rootVisualElement.Query<ListView>().First();

            Assert.AreEqual(newLastSelectedTemplateIndex, listView.selectedIndex, "Selected index should have been restored from the preferences.");

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

            var listViews = dialog.rootVisualElement.Query<ListView>().ToList();
            var projectListView = listViews[1];
            var projectItems = projectListView.itemsSource as List<SceneTemplateInfo>;

            var selectedItem = projectItems[0];
            Assert.IsNotNull(selectedItem, "There should be an item in the project template list.");

            projectListView.selectedIndex = 0;
            yield return null;

            dialog.OnEditTemplate(selectedItem);
            yield return null;

            // Selection should be our asset
            var selectedObject = Selection.activeObject;
            var selectedObjectAssetPath = AssetDatabase.GetAssetPath(selectedObject);
            Assert.AreEqual(selectedItem.assetPath, selectedObjectAssetPath);

            // The window should have been closed and destroy by then.
            Assert.IsTrue(!dialog);
        }

        private static SceneTemplateDialog OpenDialog()
        {
            var dialog = SceneTemplateDialog.ShowWindow();
            Assert.IsNotNull(dialog);
            return dialog;
        }
    }
}
