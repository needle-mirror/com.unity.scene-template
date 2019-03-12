using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using UnityEditor;
using UnityEditor.SceneTemplate;
using UnityEngine;
using UnityEngine.UIElements;

public class GridViewTests : EditorWindow
{
    List<GridView.Item> m_Items = new List<GridView.Item>();

    [MenuItem("Tools/Grid View Tests")]
    static void ShowGridViewTest()
    {
        GetWindow<GridViewTests>();
    }

    static string[] kElements = { "this is some", "serious of doom", "excellent grid", "with some", "nice serious", "layout", "and tiles in gridd",
        "or in list", "with lots of ", "excellent features", "a really long", "list of item", "that doesn make", "a lot of sense", "list wise", "let's keep writing",
        "a little bit", "more non sense", "thi list is getting", "a bit long"
    };

    private void OnEnable()
    {
        var img = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Images/Blocks/metal.png");
        // var img = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Tutorial_Info/Icons/3DiCON.png");
        m_Items = new List<GridView.Item>();
        for (var i = 0; i < kElements.Length; ++i)
        {
            m_Items.Add(new GridView.Item(i, $"{i} {kElements[i]}", i% 3 == 0 ? null : img));
        }

        var loader = new StyleSheetLoader();
        loader.LoadStyleSheets();
        rootVisualElement.styleSheets.Add(loader.CommonStyleSheet);
        rootVisualElement.styleSheets.Add(loader.VariableStyleSheet);

        CreateGridView();
        // CreateListView();
    }

    private void CreateListView()
    {
        rootVisualElement.Add(new TextField());

        var listView = new ListView(m_Items, 24, () => new Label(), (element, index) =>
        {
            (element as Label).text = m_Items[index].label;
        });
        listView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
        listView.style.flexGrow = 1;
        rootVisualElement.Add(listView);

        var slider = new Slider();
        slider.style.borderTopWidth = 1;
        slider.style.borderTopColor = new StyleColor(new Color(255, 0, 0));
        rootVisualElement.Add(slider);
    }

    private void CreateGridView()
    {
        var gridView = new GridView(m_Items, "My grid View", 24, 64, 256);
        gridView.style.width = Length.Percent(100);
        gridView.style.height = Length.Percent(100);
        gridView.onItemsActivated += (activatedItems) =>
        {
            if (activatedItems.Any())
            {
                Debug.Log($"{activatedItems.First().label} Activated!!");
            }
        };


        gridView.sizeLevel = 128;

        var testBtn = new Button(() =>
        {
            gridView.SetSelection(new int[] { 2, 5, 6 });
        });
        testBtn.text = "test";

        rootVisualElement.Add(testBtn);
        rootVisualElement.Add(gridView);

        gridView.Focus();
    }

    private void OnDisable()
    {

    }

}
