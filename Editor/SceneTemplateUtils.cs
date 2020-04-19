// #define SCENE_TEMPLATE_DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.SceneTemplate
{
    internal static class SceneTemplateUtils
    {
        private const string k_LastSceneOperationFolder = "SceneTemplateLastOperationFolder";

#if UNITY_EDITOR
        [InitializeOnLoad]
#endif
        internal static class UnityVersion
        {
            enum Candidate
            {
                Dev = 0,
                Alpha = 1 << 8,
                Beta = 1 << 16,
                Final = 1 << 24
            }

            static UnityVersion()
            {
                var version = Application.unityVersion.Split('.');

                if (version.Length < 2)
                {
                    Console.WriteLine("Could not parse current Unity version '" + Application.unityVersion + "'; not enough version elements.");
                    return;
                }

                if (Int32.TryParse(version[0], out Major) == false)
                {
                    Console.WriteLine("Could not parse major part '" + version[0] + "' of Unity version '" + Application.unityVersion + "'.");
                }

                if (Int32.TryParse(version[1], out Minor) == false)
                {
                    Console.WriteLine("Could not parse minor part '" + version[1] + "' of Unity version '" + Application.unityVersion + "'.");
                }

                if (version.Length >= 3)
                {
                    try
                    {
                        Build = ParseBuild(version[2]);
                    }
                    catch
                    {
                        Console.WriteLine("Could not parse minor part '" + version[1] + "' of Unity version '" + Application.unityVersion + "'.");
                    }
                }

#if SCENE_TEMPLATE_DEBUG
            Debug.Log($"Unity {Major}.{Minor}.{Build}");
#endif
            }

            public static int ParseBuild(string build)
            {
                var rev = 0;
                if (build.Contains("a"))
                    rev = (int)Candidate.Alpha;
                else if (build.Contains("b"))
                    rev = (int)Candidate.Beta;
                if (build.Contains("f"))
                    rev = (int)Candidate.Final;
                var tags = build.Split('a', 'b', 'f', 'p', 'x');
                if (tags.Length == 2)
                {
                    rev += Convert.ToInt32(tags[0], 10) << 4;
                    rev += Convert.ToInt32(tags[1], 10) << 0;
                }

                return rev;
            }

            [UsedImplicitly, RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
            private static void EnsureLoaded()
            {
                // This method ensures that this type has been initialized before any loading of objects occurs.
                // If this isn't done, the static constructor may be invoked at an illegal time that is not
                // allowed by Unity. During scene deserialization, off the main thread, is an example.
            }

            public static bool IsVersionGreaterOrEqual(int major, int minor)
            {
                if (Major > major)
                    return true;
                if (Major == major)
                {
                    if (Minor >= minor)
                        return true;
                }

                return false;
            }

            public static bool IsVersionGreaterOrEqual(int major, int minor, int build)
            {
                if (Major > major)
                    return true;
                if (Major == major)
                {
                    if (Minor > minor)
                        return true;

                    if (Minor == minor)
                    {
                        if (Build >= build)
                            return true;
                    }
                }

                return false;
            }

            public static readonly int Major;
            public static readonly int Minor;
            public static readonly int Build;
        }

        internal static IEnumerable<string> GetSceneTemplatePaths()
        {
            return AssetDatabase.FindAssets("t:SceneTemplateAsset").Select(AssetDatabase.GUIDToAssetPath).Where(assetPath =>
            {
                if (IsDeveloperMode())
                    return true;
                return !assetPath.StartsWith(SceneTemplate.packageFolderName);
            });
        }

        internal static bool IsDeveloperMode()
        {
#if SCENE_TEMPLATE_DEBUG
            return true;
#else
            return Directory.Exists($"{SceneTemplate.packageFolderName}/.git");
#endif
        }

        internal static string JsonSerialize(object obj)
        {
            var assembly = typeof(Selection).Assembly;
            var managerType = assembly.GetTypes().First(t => t.Name == "Json");
            var method = managerType.GetMethod("Serialize", BindingFlags.Public | BindingFlags.Static);
            var jsonString = "";
            if (UnityVersion.IsVersionGreaterOrEqual(2019, 1, UnityVersion.ParseBuild("0a10")))
            {
                var arguments = new object[] { obj, false, "  " };
                jsonString = method.Invoke(null, arguments) as string;
            }
            else
            {
                var arguments = new object[] { obj };
                jsonString = method.Invoke(null, arguments) as string;
            }

            return jsonString;
        }

        internal static object JsonDeserialize(object obj)
        {
            Assembly assembly = typeof(Selection).Assembly;
            var managerType = assembly.GetTypes().First(t => t.Name == "Json");
            var method = managerType.GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static);
            var arguments = new object[] { obj };
            return method.Invoke(null, arguments);
        }

        internal static string GetPackageVersion()
        {
            string version = null;
            try
            {
                var filePath = File.ReadAllText($"{SceneTemplate.packageFolderName}/package.json");
                if (JsonDeserialize(filePath) is Dictionary<string, object> manifest && manifest.ContainsKey("version"))
                {
                    version = manifest["version"] as string;
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return version ?? "unknown";
        }

        internal static Type[] GetAllDerivedTypes(this AppDomain aAppDomain, Type aType)
        {
            #if UNITY_2019_2_OR_NEWER
            return TypeCache.GetTypesDerivedFrom(aType).ToArray();
            #else
            var result = new List<Type>();
            var assemblies = aAppDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetLoadableTypes();
                foreach (var type in types)
                {
                    if (type.IsSubclassOf(aType))
                        result.Add(type);
                }
            }
            return result.ToArray();
            #endif
        }

        static UnityEngine.Object s_MainWindow = null;
        internal static Rect GetEditorMainWindowPos()
        {
            if (s_MainWindow == null)
            {
                var containerWinType = AppDomain.CurrentDomain.GetAllDerivedTypes(typeof(ScriptableObject)).FirstOrDefault(t => t.Name == "ContainerWindow");
                if (containerWinType == null)
                    throw new MissingMemberException("Can't find internal type ContainerWindow. Maybe something has changed inside Unity");
                var showModeField = containerWinType.GetField("m_ShowMode", BindingFlags.NonPublic | BindingFlags.Instance);
                if (showModeField == null)
                    throw new MissingFieldException("Can't find internal fields 'm_ShowMode'. Maybe something has changed inside Unity");
                var windows = Resources.FindObjectsOfTypeAll(containerWinType);
                foreach (var win in windows)
                {
                    var showMode = (int)showModeField.GetValue(win);
                    if (showMode == 4) // main window
                    {
                        s_MainWindow = win;
                        break;
                    }
                }
            }

            if (s_MainWindow == null)
                return new Rect(0, 0, 800, 600);

            var positionProperty = s_MainWindow.GetType().GetProperty("position", BindingFlags.Public | BindingFlags.Instance);
            if (positionProperty == null)
                throw new MissingFieldException("Can't find internal fields 'position'. Maybe something has changed inside Unity.");
            return (Rect)positionProperty.GetValue(s_MainWindow, null);
        }

        internal static Rect GetCenteredWindowPosition(Rect parentWindowPosition, Vector2 size)
        {
            var pos = new Rect
            {
                x = 0,
                y = 0,
                width = Mathf.Min(size.x, parentWindowPosition.width * 0.90f),
                height = Mathf.Min(size.y, parentWindowPosition.height * 0.90f)
            };
            var w = (parentWindowPosition.width - pos.width) * 0.5f;
            var h = (parentWindowPosition.height - pos.height) * 0.5f;
            pos.x = parentWindowPosition.x + w;
            pos.y = parentWindowPosition.y + h;
            return pos;
        }

        internal static Rect GetMainWindowCenteredPosition(Vector2 size)
        {
            var mainWindowRect = GetEditorMainWindowPos();
            return GetCenteredWindowPosition(mainWindowRect, size);
        }
		
        internal static void SetLastFolder(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var lastFolder = path;
                if (File.Exists(path))
                    lastFolder = Path.GetDirectoryName(path);
                if (Path.IsPathRooted(lastFolder))
                {
                    lastFolder = FileUtil.GetProjectRelativePath(lastFolder);
                }
                if (Directory.Exists(lastFolder))
                {
                    EditorPrefs.SetString($"{k_LastSceneOperationFolder}{Path.GetExtension(path)}", lastFolder);
                }
            }
        }

        internal static string GetLastFolder(string fileExtension)
        {
            var lastFolder = EditorPrefs.GetString($"{k_LastSceneOperationFolder}.{fileExtension}", null);
            if (lastFolder != null)
            {
                if (Path.IsPathRooted(lastFolder))
                {
                    lastFolder = FileUtil.GetProjectRelativePath(lastFolder);
                }
                if (!Directory.Exists(lastFolder))
                {
                    lastFolder = null;
                }
            }

            return lastFolder ?? "Assets";
        }

        internal static string SaveFilePanelUniqueName(string title, string directory, string filename, string extension)
        {
            var initialPath = Path.Combine(directory, filename + "." + extension).Replace("\\", "/");
            if (Path.IsPathRooted(initialPath))
            {
                initialPath = FileUtil.GetProjectRelativePath(initialPath);
            }
            var uniqueAssetPath = AssetDatabase.GenerateUniqueAssetPath(initialPath);
            directory = Path.GetDirectoryName(uniqueAssetPath);
            filename = Path.GetFileName(uniqueAssetPath);
            var result = EditorUtility.SaveFilePanel(title, directory, filename, extension);
            if (!string.IsNullOrEmpty(result))
            {
                SetLastFolder(result);
                if (Path.IsPathRooted(result))
                {
                    result = FileUtil.GetProjectRelativePath(result);
                }
            }

            return result;
        }

        private static Type[] GetAllEditorWindowTypes()
        {
            return GetAllDerivedTypes(AppDomain.CurrentDomain, typeof(EditorWindow));
        }

        internal static Type GetProjectBrowserWindowType()
        {
            return GetAllEditorWindowTypes().FirstOrDefault(t => t.Name == "ProjectBrowser");
        }

        internal static void OpenDocumentationUrl()
        {
            const string documentationUrl = "https://docs.unity3d.com/Packages/com.unity.scene-template@latest/";
            var uri = new Uri(documentationUrl);
            Process.Start(uri.AbsoluteUri);
        }
    }
}