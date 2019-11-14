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
            public const string classHeaderLabel = "scene-template-header-label";
            public const string classListView = "scene-template-dialog-list-view";
            public const string classDialogListViewHeader = "scene-template-dialog-list-header";
            public const string classDefaultListView = "scene-template-dialog-default-list-view";
            public const string classTemplateListView = "scene-template-dialog-template-list-view";
            public const string classBorder = "scene-template-dialog-border";
            public const string classListViewItem = "scene-template-dialog-list-view-item";
            public const string classDescriptionTextContainer = "scene-template-dialog-description-text-container";
            public const string classDescriptionThumbnailContainer = "scene-template-dialog-description-thumbnail-container";
            public const string classWrappingText = "scene-template-dialog-wrapping-text";
            public const string classPreviewArea = "scene-template-preview-area";
            public const string classUnityBaseField = "unity-base-field";
            public const string classUnityLabel = "unity-label";
            public const string classUnityBaseFieldLabel = "unity-base-field__label";
            public const string classUnityBaseFieldInput = "unity-base-field__input";
            public const string classTextLink = "scene-template-text-link";
            public const string classElementSelected = "scene-template-element-selected";
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
