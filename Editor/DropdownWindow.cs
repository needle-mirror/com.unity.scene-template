#if (SCENE_TEMPLATE_MODULE == false)
using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.SceneTemplate
{
    public abstract class DropdownWindow<T> : EditorWindow
    {
        internal static bool requestShowWindow;
        internal static double s_CloseTime;

        internal static bool canShow
        {
            get
            {
                if (EditorApplication.timeSinceStartup - s_CloseTime < 0.250)
                    return false;
                return true;
            }
        }

        public static void RequestShowWindow()
        {
            requestShowWindow = true;
        }

        public static void DropDownButton(Rect rect, Vector2 windowSize, GUIContent content, GUIStyle style, Func<DropdownWindow<T>> createWindow)
        {
            if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, style) || requestShowWindow)
            {
                if (canShow)
                {
                    requestShowWindow = false;
                    var screenRect = new Rect(GUIUtility.GUIToScreenPoint(rect.position), rect.size);
                    var window = createWindow();
                    window.ShowAsDropDown(screenRect, windowSize);
                    GUIUtility.ExitGUI();
                }
            }
        }

        [UsedImplicitly]
        protected virtual void OnDestroy()
        {
            s_CloseTime = EditorApplication.timeSinceStartup;
        }
    }

}
#endif