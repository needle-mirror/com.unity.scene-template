using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.SceneTemplate
{
    public class ReferenceUtilsTests
    {
        [Test]
        public void TestGetSceneDependencies()
        {
            var assetPaths = TestUtils.k_TestSceneDependenciesClonable.Concat(TestUtils.k_TestSceneDependenciesReference);
            var assets = new List<Object>();

            // Load all assets
            foreach (var assetPath in assetPaths)
            {
                var type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                var obj = AssetDatabase.LoadAssetAtPath(assetPath, type);
                Assert.NotNull(obj, "Asset should not be null");
                assets.Add(obj);
            }

            // Get scene dependencies
            var dependencies = new List<Object>();
            ReferenceUtils.GetSceneDependencies(TestUtils.k_TestSceneDependenciesScene, dependencies);

            var missingAssets = new List<Object>();
            var extraDependencies = new List<Object>();
            foreach (var asset in assets)
            {
                var foundAsset = dependencies.Find(obj => obj.GetInstanceID() == asset.GetInstanceID());
                if (foundAsset == null)
                    missingAssets.Add(asset);
            }
            Assert.IsEmpty(missingAssets, $"Some assets were not found through the dependencies: {missingAssets.Select(obj => obj.name)}");

            foreach (var dependency in dependencies)
            {
                var foundAsset = assets.Find(obj => obj.GetInstanceID() == dependency.GetInstanceID());
                if (foundAsset == null)
                    extraDependencies.Add(dependency);
            }
            var extraDeps = string.Join(",", extraDependencies.Select(dep => AssetDatabase.GetAssetPath(dep)));
            Assert.IsEmpty(extraDependencies, $"Found extra dependencies: {extraDeps}");
        }
    }
}
