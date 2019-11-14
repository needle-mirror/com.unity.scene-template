
using System;
using NUnit.Framework;

namespace UnityEditor.SceneTemplate
{
    public class SceneTemplateAssetInspectorTests
    {
        [Test]
        public void GetTypeGameView()
        {
            var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            Assert.IsNotNull(gameViewType);
        }
    }
}
