﻿#if (SCENE_TEMPLATE_MODULE == false)
using UnityEngine.UIElements;

namespace UnityEditor.SceneTemplate
{
    internal class StyleSheetLoader
    {
        public static class Styles
        {
            public const string classOffsetContainer = "scene-template-dialog-offset-container";
            public const string classMainContainer = "scene-template-dialog-main-container";
            public const string classTemplatesContainer = "scene-template-dialog-templates-container";
            public const string classDescriptionContainer = "scene-template-dialog-description-container";
            public const string classToggleLabel = "scene-template-dialog-toggle-label";
            public const string classButtons = "scene-template-dialog-buttons";
            public const string classButton = "scene-template-dialog-button";
            public const string classHeaderLabel = "scene-template-header-label";
            public const string classListView = "scene-template-dialog-list-view";
            public const string classDialogListViewHeader = "scene-template-dialog-list-header";
            public const string classDefaultListView = "scene-template-dialog-default-list-view";
            public const string classTemplateListView = "scene-template-dialog-template-list-view";
            public const string classBorder = "scene-template-dialog-border";
            public const string classListViewItem = "scene-template-dialog-list-view-item";
            public const string classDescriptionThumbnailContainer = "scene-template-dialog-description-thumbnail-container";
            public const string classWrappingText = "scene-template-dialog-wrapping-text";
            public const string classPreviewArea = "scene-template-preview-area";
            public const string classUnityBaseField = "unity-base-field";
            public const string classUnityLabel = "unity-label";
            public const string classUnityBaseFieldLabel = "unity-base-field__label";
            public const string classUnityBaseFieldInput = "unity-base-field__input";
            public const string classTextLink = "scene-template-text-link";
            public const string classElementSelected = "scene-template-element-selected";
            public const string classInspectorFoldoutHeader = "Inspector-Title";
            public const string classInspectorFoldoutHeaderText = "Inspector-TitleText";
            public const string unityThemeVariables = "unity-theme-env-variables";
            public const string sceneTemplateThemeVariables = "scene-template-variables";
            public const string sceneTemplateNoTemplateHelpBox = "scene-template-no-template-help-box";
            public const string sceneTemplateDialogFooter = "scene-template-dialog-footer";
            public const string sceneTemplateDialogBorder = "scene-template-dialog-border";

            public const string selected = "selected";
            public const string pinned = "pinned";
            public const string gridView = "grid-view";
            public const string gridViewHeader = "grid-view-header";
            public const string gridViewItemIcon = "grid-view-item-icon";
            public const string gridViewItemPin = "grid-view-item-pin";
            public const string gridViewItemLabel = "grid-view-item-label";
            public const string gridViewHeaderSearchField = "grid-view-header-search-field";
            public const string gridViewItemsScrollView = "grid-view-items-scrollview";

            public const string gridViewItemElement = "grid-view-item-element";

            public const string gridViewItems = "grid-view-items";
            public const string gridViewFooter = "grid-view-footer";
            public const string gridViewFooterTileSize = "grid-view-footer-tile-size";
            public const string gridViewHeaderLabel = "grid-view-header-label";
            public const string gridViewItemsContainerGrid = "grid-view-items-container-grid";
            public const string gridViewItemsContainerList = "grid-view-items-container-list";
        }

        private static readonly string k_StyleSheetsFolder = $"{SceneTemplate.packageFolderName}/Editor/StyleSheets/";
        private static readonly string k_CommonStyleSheetPath = $"{k_StyleSheetsFolder}Common.uss";
        private static readonly string k_DarkStyleSheetPath = $"{k_StyleSheetsFolder}Dark.uss";
        private static readonly string k_LightStyleSheetPath = $"{k_StyleSheetsFolder}Light.uss";

        public StyleSheet CommonStyleSheet { get; private set; }
        public StyleSheet VariableStyleSheet { get; private set; }

        public void LoadStyleSheets()
        {
            CommonStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_CommonStyleSheetPath);
            VariableStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorGUIUtility.isProSkin ? k_DarkStyleSheetPath : k_LightStyleSheetPath);
        }
    }
}
#endif