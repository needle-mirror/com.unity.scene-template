﻿using System;
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
        public SceneTemplateAsset sceneTemplate;

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
        public const string emptyTemplateName = "Empty Scene";
        public const string basicTemplateName = "Default Scene";

        private List<SceneTemplateInfo> m_SceneTemplateInfos;
        private static readonly GUIContent k_WindowTitle = new GUIContent("New Scene");

        private const string k_KeyPrefix = "SceneTemplateDialog";

        internal const string k_SceneTemplateTitleLabelName = "scene-template-title-label";
        internal const string k_SceneTemplatePathSection = "scene-template-path-section";
        internal const string k_SceneTemplatePathName = "scene-template-path-label";
        internal const string k_SceneTemplateDescriptionSection = "scene-template-description-section";
        internal const string k_SceneTemplateDescriptionName = "scene-template-description-label";
        internal const string k_SceneTemplateThumbnailName = "scene-template-thumbnail-element";
        internal const string k_SceneTemplateListViewThumbnailName = "scene-template-list-view-thumbnail-element";
        private const string k_SceneTemplateEditTemplateButtonName = "scene-template-edit-template-button";
        private const string k_SceneTemplateCreateAdditiveButtonName = "scene-template-create-additive-button";
        
        private const string k_LoadAdditivelyError = "Cannot load an in-memory scene additively while another in-memory scene is loaded. Save the current scene or load a project scene.";

        private const string k_EmptyTemplateDescription = "Just an empty scene - no Game Objects, uses the built-in renderer.";
        private const string k_DefaultTemplateDescription = "Contains a camera and directional light, works with built-in renderer. Pin any scene that uses Volume Profiles compatible with URP or HDRP in order to hide this default scene.";

        private static readonly string k_EmptyTemplateThumbnailPath = $"{SceneTemplate.resourcesFolder}/scene-template-empty-scene.png";
        private static readonly string k_DefaultTemplateThumbnailPath = $"{SceneTemplate.resourcesFolder}/scene-template-default-scene.png";

        private static SceneTemplateInfo s_EmptySceneTemplateInfo = new SceneTemplateInfo { name = emptyTemplateName, isDefault = true, description = k_EmptyTemplateDescription, onCreateCallback = CreateEmptyScene };
        private static SceneTemplateInfo s_BasicSceneTemplateInfo = new SceneTemplateInfo { name = basicTemplateName, isDefault = true, description = k_DefaultTemplateDescription, onCreateCallback = CreateDefaultScene };
        private SceneTemplateInfo m_LastSelectedTemplate;

        private SceneTemplatePreviewArea m_PreviewArea;
        private GridView m_GridView;
        private HelpBox m_NoUserTemplateHelpBox;

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
                }
            });

            // Load stylesheets
            var styleSheetLoader = new StyleSheetLoader();
            styleSheetLoader.LoadStyleSheets();
            rootVisualElement.AddToClassList(StyleSheetLoader.Styles.unityThemeVariables);
            rootVisualElement.AddToClassList(StyleSheetLoader.Styles.sceneTemplateThemeVariables);
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
            sceneTemplatesContainer.AddToClassList(StyleSheetLoader.Styles.sceneTemplateDialogBorder);
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
            buttonRow.AddToClassList(StyleSheetLoader.Styles.sceneTemplateDialogFooter);
            offsetContainer.Add(buttonRow);
            buttonRow.style.flexDirection = FlexDirection.Row;

            var loadAdditiveButton = new Toggle() { name = k_SceneTemplateCreateAdditiveButtonName, text = "Load additively" };
            buttonRow.Add(loadAdditiveButton);

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
            createSceneButton.AddToClassList(StyleSheetLoader.Styles.classButton);
            var cancelButton = new Button(Close) { text = "Cancel" };
            cancelButton.AddToClassList(StyleSheetLoader.Styles.classButton);
            buttonSection.Add(cancelButton);
            buttonSection.Add(createSceneButton);

            m_Buttons = new List<ButtonInfo>
            {
                new ButtonInfo {button = createSceneButton, callback = OnCreateNewScene},
                new ButtonInfo {button = cancelButton, callback = info => Close()}
            };
            m_SelectedButtonIndex = m_Buttons.FindIndex(bi => bi.button == createSceneButton);
            UpdateSelectedButton();

            SetAllElementSequentiallyFocusable(rootVisualElement, false);
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
            EditorPrefs.SetFloat(GetKeyName(nameof(m_GridView.sizeLevel)), m_GridView.sizeLevel);
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
            else
            {
                SetLastSelectedTemplate(m_SceneTemplateInfos[lastSelectedTemplateIndex]);
            }

            RefreshTemplateGridView();

            UpdateTemplateDescriptionUI(m_LastSelectedTemplate);
        }

        private void RefreshTemplateGridView()
        {
            m_GridView.onPinnedChanged -= OnPinnedChanged;
            m_GridView.onSelectionChanged -= OnTemplateListViewSelectionChanged;

            var items = CreateGridViewItems();
            m_GridView.SetItems(items);
            m_GridView.SetPinned(m_SceneTemplateInfos.Where(template => template.isDefault).Select(template => template.GetHashCode()));
            m_GridView.SetSelection(m_LastSelectedTemplate.GetHashCode());


            m_GridView.onPinnedChanged += OnPinnedChanged;
            m_GridView.onSelectionChanged += OnTemplateListViewSelectionChanged;
        }

        private void CreateAllSceneTemplateListsUI(VisualElement rootContainer)
        {
            if (m_SceneTemplateInfos == null)
                return;

            var templateItems = CreateGridViewItems();
            m_GridView = new GridView(templateItems, "Scene Templates in Project", k_ListViewRowHeight, 64, 256, 4/3);
            m_GridView.wrapAroundKeyboardNavigation = true;
            m_GridView.sizeLevel = EditorPrefs.GetFloat(GetKeyName(nameof(m_GridView.sizeLevel)), 128);
            rootContainer.Add(m_GridView);

            m_GridView.SetPinned(m_SceneTemplateInfos.Where(template => template.isDefault).Select(template => template.GetHashCode()));

            m_GridView.onSelectionChanged += OnTemplateListViewSelectionChanged;
            m_GridView.onPinnedChanged += OnPinnedChanged;
            m_GridView.onItemsActivated += objects =>
            {
                var sceneTemplateInfo = objects.First().userData as SceneTemplateInfo;
                if (sceneTemplateInfo == null)
                    return;
                if (m_SelectedButtonIndex != -1)
                    m_Buttons[m_SelectedButtonIndex].callback(sceneTemplateInfo);
            };

            var toSelect = templateItems.FirstOrDefault(item => item.userData.Equals(m_LastSelectedTemplate));
            if (toSelect != null)
            {
                m_GridView.SetSelection(toSelect);
            }
            else
            {
                m_GridView.SetSelection(templateItems.First());
            }

            m_NoUserTemplateHelpBox = new HelpBox("To begin using a template, create a template from an existing scene in your project. Click to see Scene template documentation.", HelpBoxMessageType.Info);
            m_NoUserTemplateHelpBox.AddToClassList(StyleSheetLoader.Styles.sceneTemplateNoTemplateHelpBox);
            m_NoUserTemplateHelpBox.style.display = m_SceneTemplateInfos.All(t => t.IsInMemoryScene) ? DisplayStyle.Flex : DisplayStyle.None;
            m_GridView.Insert(2, m_NoUserTemplateHelpBox);

            EditorApplication.delayCall += () =>
            {
                m_GridView.SetFocus();
            };
        }

        private static string GetKeyName(string name)
        {
            return $"{k_KeyPrefix}.{name}";
        }

        private IEnumerable<GridView.Item> CreateGridViewItems()
        {
            // What to do with defaults item? auto pin them?
            var defaultThumbnail = EditorGUIUtility.IconContent("d_SceneAsset Icon").image as Texture2D;
            var defaultSceneTemplateInfos = m_SceneTemplateInfos.Where(info => info.isDefault).ToList();
            var projectSceneTemplateInfos = m_SceneTemplateInfos.Where(info => !info.isDefault).ToList();
            var templateItems = defaultSceneTemplateInfos.Concat(projectSceneTemplateInfos).Select(info =>
            {
                var item = new GridView.Item(info.GetHashCode(), info.name, info.thumbnail ? info.thumbnail : defaultThumbnail, info);
                return item;
            });
            return templateItems;
        }

        private void CreateTemplateDescriptionUI(VisualElement rootContainer)
        {
            rootContainer.style.flexDirection = FlexDirection.Column;

            // Thumbnail container
            m_PreviewArea = new SceneTemplatePreviewArea(k_SceneTemplateThumbnailName, m_LastSelectedTemplate?.thumbnail, "No preview thumbnail available");
            var thumbnailElement = m_PreviewArea.Element;
            rootContainer.Add(thumbnailElement);

            rootContainer.RegisterCallback<GeometryChangedEvent>(evt => UpdatePreviewAreaSize());

            // Text container
            var sceneTitleLabel = new Label();
            sceneTitleLabel.name = k_SceneTemplateTitleLabelName;
            sceneTitleLabel.AddToClassList(StyleSheetLoader.Styles.classWrappingText);
            rootContainer.Add(sceneTitleLabel);

            var assetPathSection = new VisualElement();
            assetPathSection.name = k_SceneTemplatePathSection;
            {

                var scenePathLabel = new Label();
                scenePathLabel.name = k_SceneTemplatePathName;
                scenePathLabel.AddToClassList(StyleSheetLoader.Styles.classWrappingText);
                assetPathSection.Add(scenePathLabel);

                var editLocateRow = new VisualElement();
                editLocateRow.style.flexDirection = FlexDirection.Row;
                {
                    var scenePathLocate = new Label();
                    scenePathLocate.text = "Locate";
                    scenePathLocate.AddToClassList(StyleSheetLoader.Styles.classTextLink);
                    scenePathLocate.RegisterCallback<MouseDownEvent>(e =>
                    {
                        if (string.IsNullOrEmpty(scenePathLabel.text))
                            return;

                        var asset = AssetDatabase.LoadAssetAtPath<SceneTemplateAsset>(scenePathLabel.text);
                        if (!asset)
                            return;

                        EditorApplication.delayCall += () =>
                        {
                            EditorWindow.FocusWindowIfItsOpen(SceneTemplateUtils.GetProjectBrowserWindowType());
                            EditorApplication.delayCall += () => EditorGUIUtility.PingObject(asset);
                        };
                    });
                    editLocateRow.Add(scenePathLocate);

                    editLocateRow.Add(new Label() { text = " | " });

                    var scenePathEdit = new Label();
                    scenePathEdit.name = k_SceneTemplateEditTemplateButtonName;
                    scenePathEdit.text = "Edit";
                    scenePathEdit.AddToClassList(StyleSheetLoader.Styles.classTextLink);
                    scenePathEdit.RegisterCallback<MouseDownEvent>(e =>
                    {
                        OnEditTemplate(m_LastSelectedTemplate);
                    });
                    editLocateRow.Add(scenePathEdit);
                }
                assetPathSection.Add(editLocateRow);
            }            
            rootContainer.Add(assetPathSection);

            var descriptionSection = new VisualElement();
            descriptionSection.name = k_SceneTemplateDescriptionSection;
            {
                var descriptionLabel = new Label();
                descriptionLabel.AddToClassList(StyleSheetLoader.Styles.classHeaderLabel);
                descriptionLabel.text = "Description";
                descriptionSection.Add(descriptionLabel);

                var sceneDescriptionLabel = new Label();
                sceneDescriptionLabel.AddToClassList(StyleSheetLoader.Styles.classWrappingText);
                sceneDescriptionLabel.name = k_SceneTemplateDescriptionName;
                descriptionSection.Add(sceneDescriptionLabel);
            }
            rootContainer.Add(descriptionSection);
            
            UpdateTemplateDescriptionUI(m_LastSelectedTemplate);
        }

        private void UpdatePreviewAreaSize(SceneTemplateInfo info = null)
        {
            info = info ?? m_LastSelectedTemplate;
            if (m_PreviewArea == null)
                return;

            if (info == null || info.thumbnail == null || info.thumbnail.height > info.thumbnail.width)
            {
                m_PreviewArea.Element.style.height = Length.Percent(50);
                return;
            }

            var thumbnail = info.thumbnail;
            var aspectRatio = (float)thumbnail.height / (float)thumbnail.width;
            var width = m_PreviewArea.Element.worldBound.width;
            var newHeight = m_PreviewArea.Element.worldBound.width * aspectRatio;
            // Debug.Log($"Preview size: {info.name} width: {info.thumbnail.width} height:{info.thumbnail.height} ratio: {aspectRatio} AreaW: {width} NewHeigth: {newHeight}");
            m_PreviewArea.Element.style.height = newHeight;
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
            var sceneTemplatePathSection = rootVisualElement.Q(k_SceneTemplatePathSection);
            if (sceneTemplatePathLabel != null && newSceneTemplateInfo != null && sceneTemplatePathSection != null)
            {
                sceneTemplatePathLabel.text = newSceneTemplateInfo.assetPath;
                sceneTemplatePathSection.visible = !string.IsNullOrEmpty(newSceneTemplateInfo.assetPath);
            }

            var sceneTemplateDescriptionLabel = rootVisualElement.Q<Label>(k_SceneTemplateDescriptionName);
            var sceneTemplateDescriptionSection = rootVisualElement.Q(k_SceneTemplateDescriptionSection);
            if (sceneTemplateDescriptionLabel != null && newSceneTemplateInfo != null && sceneTemplateDescriptionSection != null)
            {
                sceneTemplateDescriptionLabel.text = newSceneTemplateInfo.description;
                sceneTemplateDescriptionSection.visible = !string.IsNullOrEmpty(newSceneTemplateInfo.description);
            }

            // Thumbnail
            m_PreviewArea?.UpdatePreview(newSceneTemplateInfo?.thumbnail);
            UpdatePreviewAreaSize(newSceneTemplateInfo);
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
            var lastTemplateAssetPath = EditorPrefs.GetString(GetKeyName(nameof(m_LastSelectedTemplate)), null);
            if (!string.IsNullOrEmpty(lastTemplateAssetPath) && m_SceneTemplateInfos != null)
            {
                m_LastSelectedTemplate = m_SceneTemplateInfos.Find(info => info.Equals(lastTemplateAssetPath));
            }

            if (m_LastSelectedTemplate == null)
            {
                m_LastSelectedTemplate = GetDefaultSceneTemplateInfo();
            }
        }

        private void OnPinnedChanged(GridView.Item item, bool isPinned)
        {
            var info = item.userData as SceneTemplateInfo;
            if (info.sceneTemplate != null)
            {
                var infoObj = new SerializedObject(info.sceneTemplate);
                var prop = infoObj.FindProperty("addToDefaults");
                prop.boolValue = isPinned;
                infoObj.ApplyModifiedProperties();
                OnSceneTemplateAssetModified(info.sceneTemplate);
            }
        }

        private void OnTemplateListViewSelectionChanged(IEnumerable<GridView.Item> oldSelection, IEnumerable<GridView.Item> newSelection)
        {
            var objList = newSelection.ToList();
            if (objList.Count == 0)
                return;

            var info = objList[0].userData as SceneTemplateInfo;
            if (info == null)
                return;

            UpdateTemplateDescriptionUI(info);

            SetLastSelectedTemplate(info);

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

            var loadAdditive = rootVisualElement.Q(k_SceneTemplateCreateAdditiveButtonName) as Toggle;
            var success = sceneTemplateInfo.onCreateCallback(sceneTemplateInfo.CanOpenAdditively() && loadAdditive.value);
            if (success)
                Close();
        }

        private static List<SceneTemplateInfo> GetSceneTemplateInfos()
        {
            var sceneTemplateList = new List<SceneTemplateInfo>();

            // Add the special Empty and Default template
            sceneTemplateList.Add(s_EmptySceneTemplateInfo);

            // Check for real templateAssets:
            var hasAnyDefaults = false;
            var sceneTemplateAssetInfos = SceneTemplateUtils.GetSceneTemplatePaths().Select(templateAssetPath =>
            {
                var sceneTemplateAsset = AssetDatabase.LoadAssetAtPath<SceneTemplateAsset>(templateAssetPath);
                return Tuple.Create(templateAssetPath, sceneTemplateAsset);
            })
            .Where(templateData => {
                if (templateData.Item2 == null)
                    return false;
                if (!templateData.Item2.IsValid)
                    return false;
                var pipeline = templateData.Item2.CreatePipeline();
                if (pipeline == null)
                    return true;
                return pipeline.IsValidTemplateForInstantiation(templateData.Item2);
            }).
            Select(templateData =>
            {
                var assetName = Path.GetFileNameWithoutExtension(templateData.Item1);
                hasAnyDefaults = hasAnyDefaults || templateData.Item2.addToDefaults;
                return new SceneTemplateInfo {
                    name = string.IsNullOrEmpty(templateData.Item2.templateName) ? assetName : templateData.Item2.templateName,
                    isDefault = templateData.Item2.addToDefaults,
                    assetPath = templateData.Item1,
                    description = templateData.Item2.description,
                    thumbnail = templateData.Item2.preview,
                    sceneTemplate = templateData.Item2,
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
            EditorPrefs.SetString(GetKeyName(nameof(m_LastSelectedTemplate)), info.ValidPath);
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

        private static void SetAllElementSequentiallyFocusable(VisualElement parent, bool focusable)
        {
            parent.tabIndex = focusable ? 0 : -1;
            foreach (var child in parent.Children())
            {
                SetAllElementSequentiallyFocusable(child, focusable);
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
