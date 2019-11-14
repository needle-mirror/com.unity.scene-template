using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[assembly: InternalsVisibleTo("Unity.SceneTemplate.Editor.Tests")]

namespace UnityEditor.SceneTemplate
{
    internal class SceneTemplateInfo : IComparable<SceneTemplateInfo>, IEquatable<SceneTemplateInfo>
    {
        public string name;
        public string assetPath;
        public string description;
        public Texture2D thumbnail;
        public Func<bool, bool> onCreateCallback;
        public bool isDefault;

        public string ValidPath => string.IsNullOrEmpty(assetPath) ? name : assetPath;

        public bool IsInMemoryScene => string.IsNullOrEmpty(assetPath);

        public int CompareTo(SceneTemplateInfo other)
        {
            return name.CompareTo(other.name);
        }

        public bool Equals(SceneTemplateInfo other)
        {
            if (other == null)
                return false;

            return ValidPath == other.ValidPath;
        }

        public bool Equals(string assetPathToCheck)
        {
            return ValidPath == assetPathToCheck;
        }

        public bool CanOpenAdditively()
        {
            return !IsInMemoryScene || SceneTemplateDialog.CanLoadAdditively();
        }
    }

    internal class SceneTemplateDialog : EditorWindow
    {
        public const string emptyTemplateName = "Empty";
        public const string basicTemplateName = "Basic";

        private List<SceneTemplateInfo> m_SceneTemplateInfos;
        private static readonly GUIContent k_WindowTitle = new GUIContent("New Scene");

        private const string k_LastSelectedTemplateSessionKey = "SceneTemplateDialogLastSelectedTemplate";

        private const string k_SceneTemplateDefaultListLabelName = "scene-template-default-label";
        private const string k_SceneTemplateProjectListLabelName = "scene-template-project-label";
        internal const string k_SceneTemplateTitleLabelName = "scene-template-title-label";
        internal const string k_SceneTemplatePathName = "scene-template-path-label";
        internal const string k_SceneTemplateDescriptionName = "scene-template-description-label";
        internal const string k_SceneTemplateThumbnailName = "scene-template-thumbnail-element";
        internal const string k_SceneTemplateListViewThumbnailName = "scene-template-list-view-thumbnail-element";
        private const string k_SceneTemplateEditTemplateButtonName = "scene-template-edit-template-button";
        private const string k_SceneTemplateCreateAdditiveButtonName = "scene-template-create-additive-button";

        private const string k_LoadAdditivelyError = "Cannot load an in-memory scene additively while another in-memory scene is loaded. Save the current scene or load a project scene.";

        private const string k_EmptyTemplateDescription = "Empty scene.";
        private const string k_DefaultTemplateDescription = "Default scene. Contains a camera and a directional light.";

        private static readonly string k_EmptyTemplateThumbnailPath = $"{SceneTemplate.packageFolderName}/Editor/Resources/scene-template-empty-scene.png";
        private static readonly string k_DefaultTemplateThumbnailPath = $"{SceneTemplate.packageFolderName}/Editor/Resources/scene-template-default-scene.png";

        private static SceneTemplateInfo s_EmptySceneTemplateInfo = new SceneTemplateInfo { name = emptyTemplateName, isDefault = true, description = k_EmptyTemplateDescription, onCreateCallback = CreateEmptyScene };
        private static SceneTemplateInfo s_BasicSceneTemplateInfo = new SceneTemplateInfo { name = basicTemplateName, isDefault = true, description = k_DefaultTemplateDescription, onCreateCallback = CreateDefaultScene };
        private SceneTemplateInfo m_LastSelectedTemplate;

        private SceneTemplatePreviewArea m_PreviewArea;
        private ZebraList m_DefaultList;
        private ZebraList m_ProjectList;

        private const int k_ListViewRowHeight = 32;
        internal Texture2D m_DefaultListViewThumbnail;

        private static readonly Vector2 k_DefaultWindowSize = new Vector2(800, 600);
        private static readonly Vector2 k_MinWindowSize = new Vector2(775, 240);

        private delegate void OnButtonCallback(SceneTemplateInfo info);
        private class ButtonInfo
        {
            public Button button;
            public OnButtonCallback callback;
        }
        private List<ButtonInfo> m_Buttons;
        private int m_SelectedButtonIndex = -1;

        public static SceneTemplateDialog ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            SceneTemplateDialog window = GetWindow<SceneTemplateDialog>(true);
            window.titleContent = k_WindowTitle;
            window.minSize = k_MinWindowSize;
            window.Show();
            return window;
        }

        private void ValidatePosition()
        {
            const float tolerance = 0.0001f;
            if (Math.Abs(position.xMin) < tolerance && position.yMin < tolerance)
            {
                position = SceneTemplateUtils.GetMainWindowCenteredPosition(k_DefaultWindowSize);
            }
        }

        [UsedImplicitly]
        private void OnEnable()
        {
            SceneTemplateAssetInspectorWindow.sceneTemplateAssetModified += OnSceneTemplateAssetModified;
            ValidatePosition();
            SetupData();

            // Keyboard events need a focusable element to trigger
            rootVisualElement.focusable = true;
            rootVisualElement.RegisterCallback<KeyUpEvent>(e =>
            {
                switch (e.keyCode) {
                    case KeyCode.Escape when !docked:
                        Close();
                        break;
                    case KeyCode.Tab:
                        SelectNextEnabledButton();
                        UpdateSelectedButton();
                        break;
                }
            });

            // Load stylesheets
            var styleSheetLoader = new StyleSheetLoader();
            styleSheetLoader.LoadStyleSheets();
            rootVisualElement.styleSheets.Add(styleSheetLoader.CommonStyleSheet);
            rootVisualElement.styleSheets.Add(styleSheetLoader.VariableStyleSheet);

            // Create a container to offset everything nicely inside the window
            var offsetContainer = new VisualElement();
            offsetContainer.AddToClassList(StyleSheetLoader.Styles.classOffsetContainer);
            rootVisualElement.Add(offsetContainer);

            // Create a container for the scene templates and description
            var mainContainer = new VisualElement();
            mainContainer.style.flexDirection = FlexDirection.Row;
            mainContainer.AddToClassList(StyleSheetLoader.Styles.classMainContainer);
            offsetContainer.Add(mainContainer);

            // Create a container for the scene templates lists(left side)
            var sceneTemplatesContainer = new VisualElement();
            sceneTemplatesContainer.AddToClassList(StyleSheetLoader.Styles.classTemplatesContainer);
            mainContainer.Add(sceneTemplatesContainer);
            CreateAllSceneTemplateListsUI(sceneTemplatesContainer);

            // Create a container for the template description (right side)
            var descriptionContainer = new VisualElement();
            descriptionContainer.AddToClassList(StyleSheetLoader.Styles.classDescriptionContainer);
            descriptionContainer.AddToClassList(StyleSheetLoader.Styles.classBorder);
            mainContainer.Add(descriptionContainer);
            CreateTemplateDescriptionUI(descriptionContainer);

            // Create the button row
            var buttonRow = new VisualElement();
            offsetContainer.Add(buttonRow);
            buttonRow.style.flexDirection = FlexDirection.Row;

            var editTemplateButton = new Button(() =>
            {
                OnEditTemplate(m_LastSelectedTemplate);
            }) { name = k_SceneTemplateEditTemplateButtonName, text = "Edit Template" };
            editTemplateButton.SetEnabled(!m_LastSelectedTemplate.IsInMemoryScene);
            buttonRow.Add(editTemplateButton);

            // The buttons need to be right-aligned
            var buttonSection = new VisualElement();
            buttonSection.style.flexDirection = FlexDirection.RowReverse;
            buttonSection.AddToClassList(StyleSheetLoader.Styles.classButtons);
            buttonRow.Add(buttonSection);
            var createSceneButton = new Button(() =>
            {
                if (m_LastSelectedTemplate == null)
                    return;
                OnCreateNewScene(m_LastSelectedTemplate);
            }) { text = "Create" };
            var createAdditivelyButton = new Button(() =>
            {
                if (m_LastSelectedTemplate == null)
                    return;
                OnCreateNewSceneAdditive(m_LastSelectedTemplate);
            }) { name = k_SceneTemplateCreateAdditiveButtonName, text = "Create Additive"};
            createAdditivelyButton.SetEnabled(m_LastSelectedTemplate.CanOpenAdditively());
            var cancelButton = new Button(Close) { text = "Cancel" };
            buttonSection.Add(cancelButton);
            buttonSection.Add(createAdditivelyButton);
            buttonSection.Add(createSceneButton);

            m_Buttons = new List<ButtonInfo>
            {
                new ButtonInfo {button = editTemplateButton, callback = OnEditTemplate},
                new ButtonInfo {button = createSceneButton, callback = OnCreateNewScene},
                new ButtonInfo {button = createAdditivelyButton, callback = OnCreateNewSceneAdditive},
                new ButtonInfo {button = cancelButton, callback = info => Close()}
            };
            m_SelectedButtonIndex = m_Buttons.FindIndex(bi => bi.button == createSceneButton);
            UpdateSelectedButton();

            SetAllElementSequentiallyFocusable(rootVisualElement, false);
            UpdateAllListsSelection(m_LastSelectedTemplate.isDefault ? 0 : 1);
        }

        internal void OnEditTemplate(SceneTemplateInfo sceneTemplateInfo)
        {
            if (sceneTemplateInfo == null)
                return;
            if (sceneTemplateInfo.IsInMemoryScene)
                return;

            // Select the asset
            var templateAsset = AssetDatabase.LoadMainAssetAtPath(sceneTemplateInfo.assetPath);
            Selection.SetActiveObjectWithContext(templateAsset, null);

            // Close the dialog
            Close();
        }

        [UsedImplicitly]
        private void OnDisable()
        {
            SceneTemplateAssetInspectorWindow.sceneTemplateAssetModified -= OnSceneTemplateAssetModified;
        }

        private void OnSceneTemplateAssetModified(SceneTemplateAsset asset)
        {
            m_SceneTemplateInfos = GetSceneTemplateInfos();
            var lastSelectedTemplateIndex = m_SceneTemplateInfos.IndexOf(m_LastSelectedTemplate);
            if (lastSelectedTemplateIndex == -1)
            {
                SetLastSelectedTemplate(GetDefaultSceneTemplateInfo());
            }
            RebuildLists();
            UpdateTemplateDescriptionUI(m_LastSelectedTemplate);
        }

        private void CreateAllSceneTemplateListsUI(VisualElement rootContainer)
        {
            if (m_SceneTemplateInfos == null)
                return;

            // Default templates
            var defaultSceneTemplateInfos = m_SceneTemplateInfos.Where(info => info.isDefault).ToList();
            m_DefaultList = CreateSceneTemplateListUI(rootContainer, defaultSceneTemplateInfos, "Defaults", true, k_SceneTemplateDefaultListLabelName, new []{StyleSheetLoader.Styles.classDefaultListView});
            m_DefaultList.style.height = Math.Min(6, defaultSceneTemplateInfos.Count + 1) * k_ListViewRowHeight + 2;

            // Project templates
            var projectSceneTemplateInfos = m_SceneTemplateInfos.Where(info => !info.isDefault).ToList();
            m_ProjectList = CreateSceneTemplateListUI(rootContainer, projectSceneTemplateInfos, "Template Scene(s) in Project", false, k_SceneTemplateProjectListLabelName, new[] { StyleSheetLoader.Styles.classTemplateListView } );
        }

        private ZebraList CreateSceneTemplateListUI(VisualElement rootContainer, List<SceneTemplateInfo> infos, string listName, bool isDefault, string labelElementName, string[] ussClasses = null)
        {
            var allListViews = rootContainer.Query<ListView>().ToList();
            var currentIndex = allListViews.Count;

            // Listview
            var templateSceneZebra = new ZebraList(infos, k_ListViewRowHeight, () =>
            {
                var header = new Label(listName);
                header.AddToClassList(StyleSheetLoader.Styles.classHeaderLabel);
                header.AddToClassList(StyleSheetLoader.Styles.classDialogListViewHeader);
                header.name = labelElementName;
                return header;
            }, MakeListViewItem, (e, i) => BindListViewItem(e, i, infos));
            templateSceneZebra.AddToClassList(StyleSheetLoader.Styles.classListView);

            templateSceneZebra.listView.selectionType = SelectionType.Single;
            templateSceneZebra.listView.onItemsChosen += objects =>
            {
                var obj = objects.FirstOrDefault();
                if (!(obj is SceneTemplateInfo sceneTemplateInfo))
                    return;
                if (m_SelectedButtonIndex != -1)
                    m_Buttons[m_SelectedButtonIndex].callback(sceneTemplateInfo);
            };
            templateSceneZebra.listView.onSelectionChange += objects => OnTemplateListViewSelectionChanged(objects, rootContainer, currentIndex);
            if (ussClasses != null)
            {
                foreach (var ussClass in ussClasses)
                {
                    templateSceneZebra.AddToClassList(ussClass);
                }
            }
            rootContainer.Add(templateSceneZebra);

            // Select the last selected template in the listview and focus.
            if (m_LastSelectedTemplate != null && m_LastSelectedTemplate.isDefault == isDefault)
            {
                // Tests don't like setting the initial selection in the delay call, so do it here
                templateSceneZebra.listView.selectedIndex = infos.FindIndex(info => info.Equals(m_LastSelectedTemplate));
                EditorApplication.delayCall += () =>
                {
                    templateSceneZebra.listView.Focus();
                };
            }

            var scrollView = templateSceneZebra.listView.Q<ScrollView>();
            scrollView?.RegisterCallback<KeyDownEvent>(e =>
            {
                var listViews = rootContainer.Query<ListView>().ToList();
                var clearCurrentSelection = false;

                switch (e.keyCode) {
                    case KeyCode.UpArrow:
                        if (templateSceneZebra.listView.selectedIndex == 0)
                        {
                            var previousIndex = FindPreviousListIndex(currentIndex, listViews);
                            var previousList = listViews[previousIndex];
                            SelectListViewItem(previousList, previousList.itemsSource.Count - 1);
                            clearCurrentSelection = previousIndex != currentIndex;
                            e.StopPropagation();
                        }

                        break;
                    case KeyCode.DownArrow:
                        if (templateSceneZebra.listView.selectedIndex == templateSceneZebra.listView.itemsSource.Count - 1)
                        {
                            var nextIndex = FindNextListIndex(currentIndex, listViews);
                            var nextList = listViews[nextIndex];
                            SelectListViewItem(nextList, 0);
                            clearCurrentSelection = nextIndex != currentIndex;
                            e.StopPropagation();
                        }
                        break;
                }
                if (clearCurrentSelection)
                    ClearListSelection(templateSceneZebra.listView);
            }, TrickleDown.TrickleDown);

            return templateSceneZebra;
        }

        private static int FindPreviousListIndex(int currentIndex, IReadOnlyList<ListView> listViews)
        {
            while (true)
            {
                var previousIndex = currentIndex == 0 ? listViews.Count - 1 : currentIndex - 1;
                if (listViews[previousIndex].itemsSource.Count > 0) return previousIndex;
                currentIndex = previousIndex;
            }
        }

        private static int FindNextListIndex(int currentIndex, IReadOnlyList<ListView> listViews)
        {
            while (true)
            {
                var nextIndex = (currentIndex + 1) % listViews.Count;
                if (listViews[nextIndex].itemsSource.Count > 0) return nextIndex;
                currentIndex = nextIndex;
            }
        }

        private void CreateTemplateDescriptionUI(VisualElement rootContainer)
        {
            rootContainer.style.flexDirection = FlexDirection.Row;

            // Text container
            var textContainer = new VisualElement();
            textContainer.AddToClassList(StyleSheetLoader.Styles.classDescriptionTextContainer);
            rootContainer.Add(textContainer);

            var sceneTitleLabel = new Label();
            sceneTitleLabel.name = k_SceneTemplateTitleLabelName;
            sceneTitleLabel.AddToClassList(StyleSheetLoader.Styles.classHeaderLabel);
            sceneTitleLabel.AddToClassList(StyleSheetLoader.Styles.classWrappingText);
            textContainer.Add(sceneTitleLabel);

            var scenePathLabel = new Label();
            scenePathLabel.name = k_SceneTemplatePathName;
            scenePathLabel.AddToClassList(StyleSheetLoader.Styles.classWrappingText);
            scenePathLabel.AddToClassList(StyleSheetLoader.Styles.classTextLink);
            scenePathLabel.RegisterCallback<MouseDownEvent>(e =>
            {
                if (string.IsNullOrEmpty(scenePathLabel.text))
                    return;

                var asset = AssetDatabase.LoadAssetAtPath<SceneTemplateAsset>(scenePathLabel.text);
                if (!asset)
                    return;
                EditorGUIUtility.PingObject(asset.GetInstanceID());
            });
            textContainer.Add(scenePathLabel);

            var sceneDescriptionLabel = new Label();
            sceneDescriptionLabel.name = k_SceneTemplateDescriptionName;
            sceneDescriptionLabel.AddToClassList(StyleSheetLoader.Styles.classWrappingText);
            textContainer.Add(sceneDescriptionLabel);

            // Thumbnail container
            var thumbnailContainer = new VisualElement();
            thumbnailContainer.AddToClassList(StyleSheetLoader.Styles.classDescriptionThumbnailContainer);
            rootContainer.Add(thumbnailContainer);

            m_PreviewArea = new SceneTemplatePreviewArea(k_SceneTemplateThumbnailName, m_LastSelectedTemplate?.thumbnail, "No preview thumbnail added");
            var thumbnailElement = m_PreviewArea.Element;
            thumbnailContainer.Add(thumbnailElement);

            UpdateTemplateDescriptionUI(m_LastSelectedTemplate);
        }

        private void UpdateTemplateDescriptionUI(SceneTemplateInfo newSceneTemplateInfo)
        {
            // Text info
            var sceneTemplateTitleLabel = rootVisualElement.Q<Label>(k_SceneTemplateTitleLabelName);
            if (sceneTemplateTitleLabel != null && newSceneTemplateInfo != null)
            {
                sceneTemplateTitleLabel.text = newSceneTemplateInfo.name;
            }

            var sceneTemplatePathLabel = rootVisualElement.Q<Label>(k_SceneTemplatePathName);
            if (sceneTemplatePathLabel != null && newSceneTemplateInfo != null)
            {
                sceneTemplatePathLabel.text = newSceneTemplateInfo.assetPath;
            }

            var sceneTemplateDescriptionLabel = rootVisualElement.Q<Label>(k_SceneTemplateDescriptionName);
            if (sceneTemplateDescriptionLabel != null && newSceneTemplateInfo != null)
            {
                sceneTemplateDescriptionLabel.text = newSceneTemplateInfo.description;
            }

            // Thumbnail
            m_PreviewArea?.UpdatePreview(newSceneTemplateInfo?.thumbnail);
        }

        private VisualElement MakeListViewItem()
        {
            var rowElement = new VisualElement();
            rowElement.AddToClassList(StyleSheetLoader.Styles.classListViewItem);

            // Template thumbnail
            var thumbnail = new VisualElement() { name = k_SceneTemplateListViewThumbnailName };
            thumbnail.style.height = k_ListViewRowHeight;
            thumbnail.style.width = k_ListViewRowHeight;
            thumbnail.style.backgroundImage = new StyleBackground(m_DefaultListViewThumbnail);
            rowElement.Add(thumbnail);

            // Template title
            var label = new Label();
            rowElement.Add(label);

            return rowElement;
        }

        private static void BindListViewItem(VisualElement element, int index, List<SceneTemplateInfo> infoList)
        {
            if (infoList == null || index >= infoList.Count)
                return;

            var sceneTemplateInfo = infoList[index];

            // Bind thumbnail
            if (sceneTemplateInfo.thumbnail != null)
            {
                var thumbnail = element.Q(k_SceneTemplateListViewThumbnailName);
                thumbnail.style.backgroundImage = new StyleBackground(sceneTemplateInfo.thumbnail);
            }

            // Bind title
            var label = element.Q<Label>();
            label.text = sceneTemplateInfo.name;
        }

        private static void SelectListViewItem(ListView listView, int selectedIndex)
        {
            listView.selectedIndex = selectedIndex;
            listView.Focus();
        }

        private static void ClearListSelection(ListView listView)
        {
            listView.selectedIndex = -1;
        }

        private void SetupData()
        {
            m_SceneTemplateInfos = GetSceneTemplateInfos();
            LoadSessionPreferences();
            m_DefaultListViewThumbnail = UnityEditorInternal.InternalEditorUtility.FindIconForFile("foo.unity");

            if (s_EmptySceneTemplateInfo.thumbnail == null)
                s_EmptySceneTemplateInfo.thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(k_EmptyTemplateThumbnailPath);
            if (s_BasicSceneTemplateInfo.thumbnail == null)
                s_BasicSceneTemplateInfo.thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(k_DefaultTemplateThumbnailPath);
        }

        private void LoadSessionPreferences()
        {
            var lastTemplateAssetPath = EditorPrefs.GetString(k_LastSelectedTemplateSessionKey, null);
            if (!string.IsNullOrEmpty(lastTemplateAssetPath) && m_SceneTemplateInfos != null)
            {
                m_LastSelectedTemplate = m_SceneTemplateInfos.Find(info => info.Equals(lastTemplateAssetPath));
            }

            if (m_LastSelectedTemplate == null)
            {
                m_LastSelectedTemplate = GetDefaultSceneTemplateInfo();
            }
        }

        private void OnTemplateListViewSelectionChanged(IEnumerable<object> objects, VisualElement root, int currentListIndex)
        {
            var objList = objects.ToList();
            if (objList.Count == 0)
                return;

            var info = objList[0] as SceneTemplateInfo;
            if (info == null)
                return;

            UpdateAllListsSelection(currentListIndex);

            UpdateTemplateDescriptionUI(info);

            SetLastSelectedTemplate(info);

            // Enable/Disable Edit Template button
            var editTemplateButton = rootVisualElement.Q<Button>(k_SceneTemplateEditTemplateButtonName);
            editTemplateButton?.SetEnabled(!info.IsInMemoryScene);

            // Enable/Disable Create Additive button
            var createAdditiveButton = rootVisualElement.Q<Button>(k_SceneTemplateCreateAdditiveButtonName);
            createAdditiveButton?.SetEnabled(info.CanOpenAdditively());

            if (m_SelectedButtonIndex != -1 && !m_Buttons[m_SelectedButtonIndex].button.enabledSelf)
            {
                SelectNextEnabledButton();
                UpdateSelectedButton();
            }
        }

        private void OnCreateNewScene(SceneTemplateInfo sceneTemplateInfo)
        {
            if (sceneTemplateInfo == null)
                return;
            var success = sceneTemplateInfo.onCreateCallback(false);
            if (success)
                Close();
        }

        private void OnCreateNewSceneAdditive(SceneTemplateInfo sceneTemplateInfo)
        {
            if (sceneTemplateInfo == null)
                return;
            var success = sceneTemplateInfo.onCreateCallback(true);
            if (success)
                Close();
        }

        private static List<SceneTemplateInfo> GetSceneTemplateInfos()
        {
            var sceneTemplateList = new List<SceneTemplateInfo>();

            // Add the special Empty and Default template
            sceneTemplateList.Add(s_EmptySceneTemplateInfo);

            // Check for real remplateAssets:
            var hasAnyDefaults = false;
            var sceneTemplateAssetInfos = SceneTemplateUtils.GetSceneTemplatePaths().Select(templateAssetPath =>
            {
                var sceneTemplateAsset = AssetDatabase.LoadAssetAtPath<SceneTemplateAsset>(templateAssetPath);
                return Tuple.Create(templateAssetPath, sceneTemplateAsset);
            }).Where(templateData => templateData.Item2 != null && templateData.Item2.IsValid).Select(templateData =>
            {
                var assetName = Path.GetFileNameWithoutExtension(templateData.Item1);
                hasAnyDefaults = hasAnyDefaults || templateData.Item2.addToDefaults;
                return new SceneTemplateInfo {
                    name = string.IsNullOrEmpty(templateData.Item2.templateName) ? assetName : templateData.Item2.templateName,
                    isDefault = templateData.Item2.addToDefaults,
                    assetPath = templateData.Item1,
                    description = templateData.Item2.description,
                    thumbnail = templateData.Item2.preview,
                    onCreateCallback = loadAdditively => CreateSceneFromTemplate(templateData.Item1, loadAdditively)
                };
            }).ToList();

            if (!hasAnyDefaults)
            {
                sceneTemplateList.Add(s_BasicSceneTemplateInfo);
            }

            sceneTemplateAssetInfos.Sort();
            sceneTemplateList.AddRange(sceneTemplateAssetInfos);
            return sceneTemplateList;
        }

        // Internal for testing
        internal static bool CreateEmptyScene(bool loadAdditively)
        {
            return CreateBasicScene(false, loadAdditively);
        }

        // Internal for testing
        internal static bool CreateDefaultScene(bool loadAdditively)
        {
            return CreateBasicScene(true, loadAdditively);
        }

        // Internal for testing
        internal static bool CanLoadAdditively()
        {
            for (var i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (string.IsNullOrEmpty(scene.path))
                    return false;
            }
            return true;
        }

        private static bool CreateBasicScene(bool isDefault, bool loadAdditively)
        {
            if (loadAdditively && !CanLoadAdditively())
            {
                Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, k_LoadAdditivelyError);
                return false;
            }
            var eventType = isDefault ? SceneTemplateAnalytics.SceneInstantiationType.DefaultScene : SceneTemplateAnalytics.SceneInstantiationType.EmptyScene;
            var instantiateEvent = new SceneTemplateAnalytics.SceneInstantiationEvent(eventType)
            {
                additive = loadAdditively
            };
            var sceneSetup = isDefault ? NewSceneSetup.DefaultGameObjects : NewSceneSetup.EmptyScene;
            EditorSceneManager.NewScene(sceneSetup, loadAdditively ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneTemplateAnalytics.SendSceneInstantiationEvent(instantiateEvent);
            return true;
        }

        private static bool CreateSceneFromTemplate(string templateAssetPath, bool loadAdditively)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneTemplateAsset>(templateAssetPath);
            if (sceneAsset == null)
                return false;
            if (!sceneAsset.IsValid)
            {
                Debug.LogError("Cannot instantiate scene template: scene is null or deleted.");
                return false;
            }

            return SceneTemplate.Instantiate(sceneAsset, loadAdditively, null, SceneTemplateAnalytics.SceneInstantiationType.NewSceneMenu) != null;
        }

        private void SetLastSelectedTemplate(SceneTemplateInfo info)
        {
            m_LastSelectedTemplate = info;
            EditorPrefs.SetString(k_LastSelectedTemplateSessionKey, info.ValidPath);
        }

        private SceneTemplateInfo GetDefaultSceneTemplateInfo()
        {
            var nbDefaults = 0;
            return m_SceneTemplateInfos.Find(info =>
            {
                if (info.isDefault)
                    nbDefaults++;
                if (nbDefaults == 2)
                    return true;
                return false;
            });
        }

        private void UpdateListView(ZebraList list, List<SceneTemplateInfo> infos)
        {
            // Update item source
            list.listView.itemsSource = infos;
            list.listView.bindItem = (element, i) => BindListViewItem(element, i, infos);

            // Select the last selected template in the listview
            if (m_LastSelectedTemplate != null)
            {
                list.listView.selectedIndex = infos.FindIndex(info => info.Equals(m_LastSelectedTemplate));
            }
            list.listView.Refresh();
        }

        private void RebuildLists()
        {
            if (m_DefaultList != null)
            {
                var defaultSceneTemplateInfos = m_SceneTemplateInfos.Where(info => info.isDefault).ToList();
                UpdateListView(m_DefaultList, defaultSceneTemplateInfos);
            }

            if (m_ProjectList != null)
            {
                var projectSceneTemplateInfos = m_SceneTemplateInfos.Where(info => !info.isDefault).ToList();
                UpdateListView(m_ProjectList, projectSceneTemplateInfos);
            }
        }

        private static void SetAllElementSequentiallyFocusable(VisualElement parent, bool focusable)
        {
            parent.tabIndex = focusable ? 0 : -1;
            foreach (var child in parent.Children())
            {
                SetAllElementSequentiallyFocusable(child, focusable);
            }
        }

        private void UpdateAllListsSelection(int selectedListIndex)
        {
            // Clear selection of every other lists, and set their tab index to enable/prevent focusing
            var allListViews = rootVisualElement.Query<ListView>().ToList();
            for (var i = 0; i < allListViews.Count; ++i)
            {
                if (i != selectedListIndex)
                {
                    ClearListSelection(allListViews[i]);
                    allListViews[i].tabIndex = -1;
                }
                else
                {
                    allListViews[i].tabIndex = 0;
                }
            }
        }

        private void UpdateSelectedButton()
        {
            for (var i = 0; i < m_Buttons.Count; i++)
            {
                m_Buttons[i].button.EnableInClassList(StyleSheetLoader.Styles.classElementSelected, i == m_SelectedButtonIndex);
            }
        }

        private void SelectNextEnabledButton()
        {
            var nextIndex = (m_SelectedButtonIndex + 1) % m_Buttons.Count;
            while (!m_Buttons[nextIndex].button.enabledSelf)
            {
                nextIndex = (nextIndex + 1) % m_Buttons.Count;
            }
            m_SelectedButtonIndex = nextIndex;
        }
    }
}
