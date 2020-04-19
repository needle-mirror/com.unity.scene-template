using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using NUnit.Framework.Constraints;
using UnityEngine;

namespace UnityEditor.SceneTemplate
{
    public class ListSelectionWindow : DropdownWindow<ListSelectionWindow>
    {
        private string[] m_Models;
        private Action<int> m_ElementSelectedHandler;
        private StringListView m_ListView;
        private string m_SearchValue;
        private bool m_SearchFieldGiveFocus;
        const string k_SearchField = "ListSearchField";

        [UsedImplicitly]
        void OnEnable()
        {
            m_SearchFieldGiveFocus = true;
            
        }

        [UsedImplicitly]
        protected void OnDisable()
        {
        }

        public static void SelectionButton(Rect rect, Vector2 windowSize, GUIContent content, GUIStyle style, string[] models, Action<int> elementSelectedHandler)
        {
            DropDownButton(rect, windowSize, content, style, () =>
            {
                var window = CreateInstance<ListSelectionWindow>();
                window.InitWindow(models, elementSelectedHandler);
                return window;
            });
        }

        [UsedImplicitly]
        internal void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    Close();
                    m_ElementSelectedHandler(-1);
                }
                else  if ( Event.current.keyCode == KeyCode.DownArrow &&
                    GUI.GetNameOfFocusedControl() == k_SearchField)
                {
                    m_ListView.SetFocusAndEnsureSelectedItem();
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.UpArrow &&
                    m_ListView.HasFocus() &&
                    m_ListView.IsFirstItemSelected())
                {
                    EditorGUI.FocusTextInControl(k_SearchField);
                    Event.current.Use();
                }
            }

            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName(k_SearchField);
            m_SearchValue = SearchField(m_SearchValue);
            if (EditorGUI.EndChangeCheck())
            {
                m_ListView.searchString = m_SearchValue;
            }
            if (m_SearchFieldGiveFocus)
            {
                m_SearchFieldGiveFocus = false;
                GUI.FocusControl(k_SearchField);
            }

            var rect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandHeight(true));
            m_ListView.OnGUI(rect);
        }

        private void InitWindow(string[] models, Action<int> elementSelectedHandler)
        {
            m_Models = models;
            m_ElementSelectedHandler = elementSelectedHandler;
            m_ListView = new StringListView(models);
            m_ListView.elementActivated += OnElementActivated;
        }

        private void OnElementActivated(int indexSelected)
        {
            Close();
            m_ElementSelectedHandler.Invoke(indexSelected);
        }

        static MethodInfo ToolbarSearchField;
        private static string SearchField(string value, params GUILayoutOption[] options)
        {
            if (ToolbarSearchField == null)
            {
                ToolbarSearchField = typeof(EditorGUILayout).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).First(mi => mi.Name == "ToolbarSearchField" && mi.GetParameters().Length == 2);
            }

            return ToolbarSearchField.Invoke(null, new[] { value, (object)options }) as string;
        }
    }
}