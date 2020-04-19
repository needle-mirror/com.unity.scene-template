using System;
using NUnit.Framework;

public class SceneTemplateAssetInspectorTests
{
    [Test]
    public void GetTypeGameView()
    {
        var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        Assert.IsNotNull(gameViewType);
    }
}

