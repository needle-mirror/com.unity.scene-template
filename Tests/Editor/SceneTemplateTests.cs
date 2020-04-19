using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.SceneTemplate;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;


public class SceneTemplateTests
{
    [OneTimeSetUp]
    public void Init()
    {
        TestUtils.CreateFolder(TestUtils.k_TestGeneratedFolder);
        TestUtils.CreateFolder(TestUtils.k_TestGeneratedTemplateFolder);
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        TestUtils.DeleteFolder(TestUtils.k_TestGeneratedFolder);
    }

    [Test]
    public void CreateTemplateFromSceneTest()
    {
        var sourceScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(TestUtils.k_TestSceneDependenciesScene);
        Assert.NotNull(sourceScene, "sourceScene");

        var (templatePath, sceneTemplate) = CreateTemplateFromScene(sourceScene);
        var validateTemplatePath = AssetDatabase.GetAssetPath(sceneTemplate);
        Assert.AreEqual(templatePath, validateTemplatePath, "templatePath");
        Assert.AreEqual(sourceScene, sceneTemplate.templateScene, "sourceScene");
        Assert.AreEqual(TestUtils.k_TestSceneDependenciesClonable.Length + TestUtils.k_TestSceneDependenciesReference.Length, sceneTemplate.dependencies.Length, "Nb of dependencies");
        foreach (var dep in sceneTemplate.dependencies)
        {
            var depPath = AssetDatabase.GetAssetPath(dep.dependency);
            if (dep.instantiationMode == TemplateInstantiationMode.Clone)
            {
                Assert.IsTrue(Array.FindIndex(TestUtils.k_TestSceneDependenciesClonable, p => p == depPath) != -1, "Cannot find cloned dep: " + depPath);
            }
            else
            {
                Assert.IsTrue(Array.FindIndex(TestUtils.k_TestSceneDependenciesReference, p => p == depPath) != -1, "Cannot find referenced dep: " + depPath);
            }
        }

        // Test template is selected
        Assert.AreSame(sceneTemplate, Selection.activeObject);
    }

    [Test]
    public void CreateEmptyTemplateNoPath()
    {
        Assert.Throws<Exception>(() =>
        {
            var sceneTemplate = SceneTemplate.CreateSceneTemplate("");
        });
    }

    [Test]
    public void CreateEmptyTemplateWithPath()
    {
        var sceneTemplatePath = AssetDatabase.GenerateUniqueAssetPath(TestUtils.k_TestGeneratedTemplateFolder + "/EmptySceneTemplate.asset");
        var sceneTemplate = SceneTemplate.CreateSceneTemplate(sceneTemplatePath);
        Assert.NotNull(sceneTemplate);
        var validateTemplatePath = AssetDatabase.GetAssetPath(sceneTemplate);
        Assert.AreEqual(validateTemplatePath, sceneTemplatePath, "sceneTemplatePath");
        Assert.IsNull(sceneTemplate.templateScene);

        // Test template is selected
        Assert.AreSame(sceneTemplate, Selection.activeObject);
    }

    [UnityTest]
    public IEnumerator InstantiateTemplate()
    {
        DummySceneTemplatePipeline.CleanTestData();
        var (newScene, newSceneAsset, expectedPath) = InstantiateSceneFromTemplate(TestUtils.k_TestSceneDependenciesTemplate, "InstantiateTemplate_scene.unity");

        yield return null;

        Assert.IsTrue(DummySceneTemplatePipeline.beforeHit, "DummySceneTemplatePipeline.beforeHit");
        Assert.IsTrue(DummySceneTemplatePipeline.afterHit, "DummySceneTemplatePipeline.afterHit");

        ValidateInstantiation(newScene.path, newSceneAsset, TestUtils.k_TestSceneDependenciesClonable, TestUtils.k_TestSceneDependenciesReference);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

        yield return null;
    }

    [UnityTest]
    public IEnumerator InstantiateTemplateAdditive()
    {
        var (newScene, newSceneAsset, expectedPath) = InstantiateSceneFromTemplate(TestUtils.k_TestSceneDependenciesTemplate, "InstantiateTemplateAdditive_scene.unity", true);
        yield return null;

        ValidateInstantiation(newScene.path, newSceneAsset, TestUtils.k_TestSceneDependenciesClonable, TestUtils.k_TestSceneDependenciesReference);

        ValidateAdditiveLoading(newScene);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

        yield return null;
    }

    [UnityTest]
    public IEnumerator InstantiateTemplateNoCloneAdditive()
    {
        var validSceneSetup = GenerateNoCloneSceneSetup();
        Assert.IsTrue(validSceneSetup, "In project scene template setup failed");

        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);

        var (newScene, newSceneAsset, expectedPath) = InstantiateSceneFromTemplate(TestUtils.k_NoCloneGeneratedTemplate, "InstantiateTemplateNoCloneAdditive_scene.unity", true);
        yield return null;

        ValidateAdditiveLoading(newScene);
        ValidateInstantiationNoClone(newScene, expectedPath);

        yield return null;
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
    }

    [UnityTest]
    public IEnumerator InstantiateTemplateNoClone()
    {
        var validSceneSetup = GenerateNoCloneSceneSetup();
        Assert.IsTrue(validSceneSetup, "In project scene template setup failed");

        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);

        var (newScene, newSceneAsset, expectedPath) = InstantiateSceneFromTemplate(TestUtils.k_NoCloneGeneratedTemplate, "InstantiateTemplateNoClone_scene.unity", false);

        yield return null;

        Assert.IsNull(newSceneAsset, "newSceneAsset exists?!?");
        Assert.IsTrue(string.IsNullOrEmpty(newScene.path));

        ValidateInstantiationNoClone(newScene, expectedPath);

        yield return null;
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
    }

    [UnityTest]
    public IEnumerator AddThumbnailToAsset()
    {
        var sourceScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(TestUtils.k_TestSceneDependenciesScene);
        Assert.NotNull(sourceScene, "sourceScene");
        var (templatePath, sceneTemplate) = CreateTemplateFromScene(sourceScene);
        yield return null;

        var textureCopy = new Texture2D(Texture2D.whiteTexture.width, Texture2D.whiteTexture.height);
        EditorUtility.CopySerialized(Texture2D.whiteTexture, textureCopy);
        sceneTemplate.AddThumbnailToAsset(textureCopy);
        yield return null;

        var allAssets = AssetDatabase.LoadAllAssetsAtPath(templatePath);
        var textureAsset = allAssets.FirstOrDefault(obj => obj is Texture2D) as Texture2D;
        Assert.IsNotNull(textureAsset, $"There should be a texture asset embedded in {templatePath}");
        Assert.AreEqual(textureCopy, textureAsset);
    }

    private static bool GenerateNoCloneSceneSetup()
    {
        var sceneTemplateAsset = AssetDatabase.LoadAssetAtPath<SceneTemplateAsset>(TestUtils.k_NoCloneGeneratedTemplate);
        if (sceneTemplateAsset != null)
            return true;

        var inProjectTestScenePath = Path.Combine(TestUtils.k_TestGeneratedTemplateFolder, TestUtils.k_NoCloneSceneName);
        var result = AssetDatabase.CopyAsset(TestUtils.k_NoCloneScene, inProjectTestScenePath);
        if (!result)
            return false;
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(inProjectTestScenePath);
        if (sceneAsset == null)
            return false;

        SceneTemplate.CreateTemplateFromScene(sceneAsset, TestUtils.k_NoCloneGeneratedTemplate);
        sceneTemplateAsset = AssetDatabase.LoadAssetAtPath<SceneTemplateAsset>(TestUtils.k_NoCloneGeneratedTemplate);
        if (sceneTemplateAsset == null)
            return false;

        foreach(var dep in sceneTemplateAsset.dependencies)
        {
            dep.instantiationMode = TemplateInstantiationMode.Reference;
        }

        EditorUtility.SetDirty(sceneTemplateAsset);
        AssetDatabase.SaveAssets();

        return true;
    }

    private static Tuple<Scene, SceneAsset, string> InstantiateSceneFromTemplate(string sceneTemplateAssetPath, string newSceneName, bool additive = false)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

        var sceneTemplate = AssetDatabase.LoadAssetAtPath<SceneTemplateAsset>(sceneTemplateAssetPath);
        Assert.NotNull(sceneTemplate, $"sceneTemplate {sceneTemplateAssetPath} doesn't exist");
        var sceneTemplatePath = AssetDatabase.GetAssetPath(sceneTemplate);
        Assert.AreEqual(sceneTemplateAssetPath, sceneTemplatePath);

        var newScenePath = Path.Combine(TestUtils.k_TestGeneratedFolder, newSceneName).Replace("\\", "/");
        var instantiationResult = SceneTemplate.Instantiate(sceneTemplate, additive, newScenePath);

        return new Tuple<Scene, SceneAsset, string>(instantiationResult.Item1, instantiationResult.Item2, newScenePath);
    }

    private static void ValidateInstantiation(string newScenePath, Object newSceneAsset, IEnumerable<string> clonedAssetPaths, IEnumerable<string> referencedAssetPaths)
    {
        Assert.NotNull(newSceneAsset, "newSceneAsset");

        var validateScenePath = AssetDatabase.GetAssetPath(newSceneAsset);
        Assert.AreEqual(validateScenePath, newScenePath, "newSceneAsset");

        // Check for Cloned asset directory existence:
        var cloneAssetDirectoryName = Path.GetFileNameWithoutExtension(newScenePath);
        var newSceneDirectory = Path.GetDirectoryName(newScenePath).Replace("\\", "/");
        var clonedAssetDirectory = Path.Combine(newSceneDirectory, cloneAssetDirectoryName).Replace("\\", "/");
        Assert.IsTrue(Directory.Exists(clonedAssetDirectory), $"Cloned asset directory {clonedAssetDirectory} doesn't exists");

        var expectedDependencyPaths = new HashSet<string>();
        foreach (var cloneableDep in clonedAssetPaths)
        {
            var depName = Path.GetFileName(cloneableDep);
            expectedDependencyPaths.Add(Path.Combine(clonedAssetDirectory, depName).Replace("\\", "/"));
        }

        foreach (var path in expectedDependencyPaths)
        {
            Assert.IsTrue(File.Exists(path), $"Expected ClonedAsset asset {path} doesn't exists");
        }

        var clonedDependencies = new List<Object>();
        ReferenceUtils.GetSceneDependencies(newScenePath, clonedDependencies);
        Assert.AreEqual(clonedAssetPaths.Count() + referencedAssetPaths.Count(), clonedDependencies.Count, "Nb of dependencies");

        var clonedDependencyPaths = GetDependencyPaths(clonedDependencies);
        foreach (var expectedDep in expectedDependencyPaths)
        {
            Assert.IsTrue(clonedDependencyPaths.Contains(expectedDep), $"Expected ClonedAsset asset {expectedDep} doesn't exists");
        }
    }

    private static void ValidateInstantiationNoClone(Scene newScene, string expectedPath)
    {
        EditorSceneManager.SaveScene(newScene, expectedPath);
        AssetDatabase.Refresh();

        var dependencies = new List<Object>();
        ReferenceUtils.GetSceneDependencies(expectedPath, dependencies);

        var expectedDependencies = new List<string>();
        foreach (var dep in TestUtils.k_NoCloneReferences)
        {
            expectedDependencies.Add(dep);
        }

        var dependencyPaths = GetDependencyPaths(dependencies);
        foreach (var expectedDep in expectedDependencies)
        {
            Assert.IsTrue(dependencyPaths.Contains(expectedDep), $"{expectedDep} is not found in dependencies");
        }

        Assert.AreEqual(expectedDependencies.Count, dependencies.Count);
    }

    private static HashSet<string> GetDependencyPaths(List<Object> dependencies)
    {
        var dependencyPaths = new HashSet<string>();
        foreach (var dep in dependencies)
        {
            dependencyPaths.Add(AssetDatabase.GetAssetPath(dep));
        }

        return dependencyPaths;
    }

    private static void ValidateAdditiveLoading(Scene newScene)
    {
        Assert.AreEqual(2, EditorSceneManager.loadedSceneCount);

        var foundScene = new Scene();
        for (var i = 0; i < SceneManager.sceneCount; ++i)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.path == newScene.path)
                foundScene = scene;
        }
        Assert.True(foundScene.IsValid(), "Template scene not found");
        Assert.True(foundScene.isLoaded, "Template scene should be loaded");
    }

    private static Tuple<string, SceneTemplateAsset> CreateTemplateFromScene(SceneAsset sourceScene)
    {
        var templatePath = Path.Combine(TestUtils.k_TestGeneratedFolder, "CreateTemplateFromSceneTest_template.asset").Replace("\\", "/");
        var sceneTemplate = SceneTemplate.CreateTemplateFromScene(sourceScene, templatePath);
        return Tuple.Create(templatePath, sceneTemplate);
    }
}
