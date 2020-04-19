
using System;
using JetBrains.Annotations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.SceneTemplate
{
    internal class EditorPrefBool : IDisposable
    {
        private string m_SettingKey;
        private bool m_DefaultValue;

        private bool m_LocalValue;
        private bool m_UpToDate;

        public event Action valueChanged;

        public bool Value
        {
            get
            {
                if (!m_UpToDate)
                    m_LocalValue = EditorPrefs.GetBool(m_SettingKey, m_DefaultValue);
                return m_LocalValue;
            }
            set
            {
                if (value == m_LocalValue)
                    return;
                m_LocalValue = value;
                m_UpToDate = true;
                EditorPrefs.SetBool(m_SettingKey, value);
            }
        }

        public EditorPrefBool(string settingKey, bool defaultValue)
        {
            m_SettingKey = settingKey;
            m_DefaultValue = defaultValue;
            m_LocalValue = m_DefaultValue;
            m_UpToDate = false;
            EditorPrefs.onValueWasUpdated += OnValueUpdated;
        }

        public void Dispose()
        {
            EditorPrefs.onValueWasUpdated -= OnValueUpdated;
        }

        private void OnValueUpdated(string key)
        {
            if (key != m_SettingKey)
                return;

            m_UpToDate = false;
            valueChanged?.Invoke();
        }
    }

    internal static class SceneTemplatePreferences
    {
        public const string showOnProjectLoadLabel = "Show on Project Load";
        public const string showOnProjectLoadTooltip = "When enabled, the New Scene dialog will open when a project is loaded without a scene.";
        private const string k_SettingsKey = "Preferences/SceneTemplates";
        private const string k_ShowOnProjectLoadPrefKey = "SceneTemplateDialogShowOnProjectLoad";

        private static EditorPrefBool s_ShowOnProjectLoad;
        public static EditorPrefBool ShowOnProjectLoad => s_ShowOnProjectLoad ?? (s_ShowOnProjectLoad = new EditorPrefBool(k_ShowOnProjectLoadPrefKey, false));

        private static class Styles
        {
            public static GUIContent showOnProjectLoadContent = new GUIContent(
                showOnProjectLoadLabel,
                showOnProjectLoadTooltip);
        }

        [UsedImplicitly, SettingsProvider]
        private static SettingsProvider CreateSettings()
        {
            return new SettingsProvider(k_SettingsKey, SettingsScope.User)
            {
                keywords = new[] { "unity", "editor", "scene", "creation", "template" },
                label = "Scene Template",
                guiHandler = OnGUIHandler
            };
        }

        private static void OnGUIHandler(string obj)
        {
            using (new SettingsWindow.GUIScope())
            {
                ShowOnProjectLoad.Value = EditorGUILayout.Toggle(Styles.showOnProjectLoadContent, ShowOnProjectLoad.Value);
            }
        }
    }
}
