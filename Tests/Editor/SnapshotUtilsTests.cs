using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.SceneTemplate
{
    public class SnapshotUtilsTests
    {
        [Test]
        public void GetSceneViewCameraRect()
        {
            var sceneView = EditorWindow.GetWindow<SceneView>();
            Assert.IsNotNull(sceneView);
            Assert.DoesNotThrow(() =>
            {
                SnapshotUtils.GetSceneViewCameraRect(sceneView);
            });
        }

        [Test]
        public void RawViewportRect()
        {
            Assert.DoesNotThrow(() =>
            {
                var rect = SnapshotUtils.GetRawViewportRect();
                SnapshotUtils.SetRawViewportRect(rect);
            });
        }

        [Test]
        public void SetRenderTextureNoViewport()
        {
            var currentRT = RenderTexture.active;
            Assert.DoesNotThrow(() =>
            {
                SnapshotUtils.SetRenderTextureNoViewport(currentRT);
            });
        }

        [Test]
        public void GetMaterialForSpecialTexture()
        {
            var texture = new Texture2D(1, 1);
            Material mat = null;
            Assert.DoesNotThrow(() =>
            {
                mat = SnapshotUtils.GetMaterialForSpecialTexture(texture, null, QualitySettings.activeColorSpace == ColorSpace.Linear);
            });
        }
    }
}
