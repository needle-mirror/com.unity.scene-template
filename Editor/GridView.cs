﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEditor.SceneTemplate;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class GridView : VisualElement
{
    List<Item> m_PinnedItems;
    List<Item> m_UnpinnedItems;
    List<Item> m_SelectedItems;
    List<Item> m_Items;
    Dictionary<int, VisualElement> m_IdToElements;
    VisualElement m_Header;
    VisualElement m_Footer;
    VisualElement m_ItemsContainer;
    ToolbarSearchField m_SearchField;
    Slider m_TileSizeSlider;
    ScrollView m_ItemsScrollView;

    bool m_ShowHeader;
    bool m_ShowFooter;

    public class Item
    {
        private int m_Id;
        private string m_Label;
        private Texture2D m_Icon;
        private object m_UserData;

        public Item(int id, string label, Texture2D icon = null, object userData = null, int priority = -1)
        {
            m_Id = id;
            m_Label = label;
            m_Icon = icon;
            m_UserData = userData;
            this.priority = priority;
        }

        public int id => m_Id;
        public Texture2D icon => m_Icon;
        public string label => m_Label;
        public object userData => m_UserData;
        public int priority
        {
            get;
            internal set;
        }
    }

    float m_SizeLevel;
    public float sizeLevel
    {
        get => m_SizeLevel;
        set
        {
            var listViewChange = isListView;
            m_SizeLevel = value;
            if (listViewChange != isListView)
            {
                if (isListView)
                {
                    m_ItemsContainer.RemoveFromClassList(StyleSheetLoader.Styles.gridViewItemsContainerGrid);
                    m_ItemsContainer.AddToClassList(StyleSheetLoader.Styles.gridViewItemsContainerList);
                }
                else
                {
                    m_ItemsContainer.RemoveFromClassList(StyleSheetLoader.Styles.gridViewItemsContainerList);
                    m_ItemsContainer.AddToClassList(StyleSheetLoader.Styles.gridViewItemsContainerGrid);
                }
            }
            LayoutItems();
            m_TileSizeSlider.SetValueWithoutNotify(value);
            sizeLevelChanged?.Invoke(m_SizeLevel, isListView);
        }
    }

    public bool isListView => sizeLevel < minTileSize;

    string m_FilterString;
    public string filterString
    {
        get => m_FilterString;
        set
        {
            m_FilterString = value.Trim();
            foreach (var element in m_IdToElements.Values)
            {
                var isFiltered = IsFiltered(element.userData as Item, m_FilterString);
                if (isFiltered && element.style.display == DisplayStyle.None)
                {
                    element.style.display = DisplayStyle.Flex;
                }
                else if (!isFiltered && element.style.display != DisplayStyle.None)
                {
                    element.style.display = DisplayStyle.None;
                }
            }
        }
    }

    public bool multiSelection
    {
        get;
        set;
    }

    public bool wrapAroundKeyboardNavigation
    {
        get;
        set;
    }

    public bool showHeader
    {
        get => m_ShowHeader;
        set
        {
            m_ShowHeader = value;
            m_Header.visible = value;
        }
    }

    public bool showFooter
    {
        get => m_ShowFooter;
        set
        {
            m_ShowFooter = value;
            m_Footer.visible = value;
        }
    }

    public float minTileSize 
    {
        get;
        private set;
    }

    public float maxTileSize
    {
        get;
        private set;
    }

    public float listItemHeight
    {
        get;
        private set;
    }

    public float aspectRatio
    {
        get;
        private set;
    }

    public IEnumerable<Item> items => m_Items;
    public IEnumerable<Item> pinnedItems => m_PinnedItems;
    public IEnumerable<Item> unpinnedItems => m_UnpinnedItems;
    public IEnumerable<Item> selectedItems => m_SelectedItems;

    public event Action<float, bool> sizeLevelChanged;
    public event Action<IEnumerable<Item>, IEnumerable<Item>> onSelectionChanged;
    public event Action<Item, bool> onPinnedChanged;
    public event Action<IEnumerable<Item>> onItemsActivated;

    public GridView(IEnumerable<Item> items, string title, float listItemHeight, float minTileSize, float maxTileSize, float aspectRatio = 1f)
    {
        AddToClassList(StyleSheetLoader.Styles.gridView);
        this.aspectRatio = aspectRatio;
        this.listItemHeight = listItemHeight;
        this.minTileSize = minTileSize;
        this.maxTileSize = maxTileSize;
        m_Header = new VisualElement();
        m_Header.AddToClassList(StyleSheetLoader.Styles.gridViewHeader);
        {
            var headerLabel = new Label(title);
            headerLabel.AddToClassList(StyleSheetLoader.Styles.gridViewHeaderLabel);
            m_Header.Add(headerLabel);
            m_SearchField = new ToolbarSearchField();
            m_SearchField.AddToClassList(StyleSheetLoader.Styles.gridViewHeaderSearchField);
            m_SearchField.RegisterValueChangedCallback(evt => filterString = evt.newValue);
            m_Header.Add(m_SearchField);
        }
        Add(m_Header);

        m_ItemsScrollView = new ScrollView();
        m_ItemsScrollView.AddToClassList(StyleSheetLoader.Styles.gridViewItemsScrollView);
        m_ItemsScrollView.RegisterCallback<MouseDownEvent>(evt => HandleSelect(evt, null));
        m_ItemsScrollView.focusable = true;
        m_ItemsScrollView.RegisterCallback<KeyDownEvent>(OnKeyDown);
        Add(m_ItemsScrollView);

        m_ItemsContainer = new VisualElement();
        m_ItemsContainer.AddToClassList(StyleSheetLoader.Styles.gridViewItems);
        m_ItemsScrollView.Add(m_ItemsContainer);

        m_Footer = new VisualElement();
        m_Footer.AddToClassList(StyleSheetLoader.Styles.gridViewFooter);
        {
            m_TileSizeSlider = new Slider();
            m_TileSizeSlider.lowValue = minTileSize - 1;
            m_TileSizeSlider.highValue = maxTileSize;
            m_TileSizeSlider.RegisterValueChangedCallback(evt =>
            {
                sizeLevel = evt.newValue;
            });
            m_TileSizeSlider.AddToClassList(StyleSheetLoader.Styles.gridViewFooterTileSize);
            m_Footer.Add(m_TileSizeSlider);
        }
        Add(m_Footer);

        showHeader = true;
        showFooter = true;

        SetItems(items);
        sizeLevel = maxTileSize;
    }


    public void SetItems(IEnumerable<Item> items)
    {
        m_UnpinnedItems = items.ToList();
        m_Items = items.ToList();
        for (var i = 0; i < m_Items.Count; i++)
        {
            var item = m_Items[i];
            if (item.priority == -1)
            {
                item.priority = i;
            }
        }
        m_Items.Sort((a, b) => a.priority - b.priority);
        m_IdToElements = new Dictionary<int, VisualElement>();
        m_PinnedItems = new List<Item>();
        m_SelectedItems = new List<Item>();

        RefreshItemElements();
    }

    public void SetFocus()
    {
        m_ItemsScrollView.tabIndex = 0;
        m_ItemsScrollView.Focus();
    }

    public bool IsPinned(Item item)
    {
        return m_PinnedItems.Contains(item);
    }

    public bool IsSelected(Item item)
    {
        return m_SelectedItems.Contains(item);
    }

    int GetSelectedVisibleIndex()
    {
        var lastSelectedItem = m_SelectedItems.LastOrDefault();
        var lastSelectedElement = lastSelectedItem == null ? null : m_IdToElements[lastSelectedItem.id];
        var selectedIndex = lastSelectedElement == null || lastSelectedElement .style.display == DisplayStyle.None ? -1 : m_ItemsContainer.IndexOf(lastSelectedElement);
        return selectedIndex;
    }

    VisualElement SetSelectedVisibleIndex(int index, bool forward)
    {
        if (index >= m_ItemsContainer.childCount)
        {
            index = m_ItemsContainer.childCount;
        }

        if (index == -1)
        {
            ClearSelection();
            return null;
        }

        var elementToSelect = m_ItemsContainer.ElementAt(index);
        if (forward)
        {
            while (m_ItemsContainer.ElementAt(index).style.display == DisplayStyle.None && ++index < m_ItemsContainer.childCount)
            {
                elementToSelect = m_ItemsContainer.ElementAt(index);
            }
        }
        else
        {
            while (elementToSelect.style.display == DisplayStyle.None && --index >= 0)
            {
                elementToSelect = m_ItemsContainer.ElementAt(index);
            }
        }

        if (elementToSelect.style.display == DisplayStyle.None)
        {
            return null;
        }

        var itemToSelect = elementToSelect.userData as Item;
        if (IsSelected(itemToSelect) && m_SelectedItems.Count == 1)
            return elementToSelect;

        SetSelection(itemToSelect);
        return elementToSelect;
    }

    public void SetSelection(Item itemToSelect)
    {
        SetSelection(new[] { itemToSelect });
    }

    public void SetSelection(IEnumerable<Item> itemToSelect)
    {
        if (m_SelectedItems.Count > 0 )
        {
            // Unselect currently selected item:
            foreach (var toUnselectElement in m_SelectedItems.Select(item => m_IdToElements[item.id]))
            {
                toUnselectElement.RemoveFromClassList(StyleSheetLoader.Styles.selected);
            }
        }

        var oldSelection = m_SelectedItems.ToList();
        m_SelectedItems = itemToSelect.ToList();

        if (itemToSelect.Any())
        {
            // Select new item
            var toSelectElements = itemToSelect.Select(item => m_IdToElements[item.id]);
            foreach (var toSelectElement in toSelectElements)
            {
                toSelectElement.AddToClassList(StyleSheetLoader.Styles.selected);
            }
        }
        
        onSelectionChanged?.Invoke(oldSelection, itemToSelect);
    }

    private void AddToSelection(Item item)
    {
        var oldSelection = m_SelectedItems.ToList();
        m_SelectedItems.Add(item);
        m_IdToElements[item.id].AddToClassList(StyleSheetLoader.Styles.selected);
        onSelectionChanged?.Invoke(oldSelection, m_SelectedItems);
    }

    private void RemoveFromSelection(Item item)
    {
        var oldSelection = m_SelectedItems.ToList();
        m_SelectedItems.Remove(item);
        m_IdToElements[item.id].RemoveFromClassList(StyleSheetLoader.Styles.selected);
        onSelectionChanged?.Invoke(oldSelection, m_SelectedItems);
    }

    public void ClearSelection()
    {
        SetSelection(new Item[0]);
    }

    public void SelectAll()
    {
        SetSelection(m_Items);
    }

    public void SetSelection(int idToSelect)
    {
        SetSelection(IdToItem(idToSelect));
    }

    public void SetSelection(IEnumerable<int> idToSelect)
    {
        SetSelection(idToSelect.Select(IdToItem));
    }

    public void SetPinned(IEnumerable<int> idToPinned)
    {
        SetPinned(idToPinned.Select(IdToItem));
    }

    public void SetPinned(IEnumerable<Item> idToPinned)
    {
        m_PinnedItems.Clear();
        foreach (var item in idToPinned)
        {
            TogglePinned(item);
        }
    }

    public void TogglePinned(Item item)
    {
        bool toggleToPin = !IsPinned(item);
        var element = m_IdToElements[item.id];
        if (toggleToPin)
        {
            element.AddToClassList(StyleSheetLoader.Styles.pinned);
            m_PinnedItems.Add(item);
            element.RemoveFromHierarchy();

            var insertionPoint = 0;
            for (; insertionPoint < m_ItemsContainer.childCount; ++insertionPoint)
            {
                var e = m_ItemsContainer.ElementAt(insertionPoint);
                if (!e.ClassListContains(StyleSheetLoader.Styles.pinned))
                {
                    break;
                }
                if ((e.userData as Item).priority >= item.priority)
                {
                    break;
                }
            }
            
            m_ItemsContainer.Insert(insertionPoint, element);
        }
        else
        {
            element.RemoveFromClassList(StyleSheetLoader.Styles.pinned);
            m_PinnedItems.Remove(item);
            element.RemoveFromHierarchy();
            var insertionPoint = m_PinnedItems.Count;
            for (; insertionPoint < m_ItemsContainer.childCount; ++insertionPoint)
            {
                var e = m_ItemsContainer.ElementAt(insertionPoint);
                if ((e.userData as Item).priority >= item.priority)
                {
                    break;
                }
            }
            m_ItemsContainer.Insert(insertionPoint, element);
        }

        onPinnedChanged?.Invoke(item, toggleToPin);
    }

    public void OnKeyDown(KeyDownEvent evt)
    {
        if (evt == null)
            return;

        var shouldStopPropagation = true;
        VisualElement elementSelected = null;
        switch (evt.keyCode)
        {
            case KeyCode.UpArrow:
                elementSelected = NavigateToPreviousItem();
                break;
            case KeyCode.DownArrow:
                elementSelected = NavigateToNextItem();
                break;
            case KeyCode.RightArrow:
                if (!isListView)
                    elementSelected = NavigateToNextItem();
                break;
            case KeyCode.LeftArrow:
                if (!isListView)
                    elementSelected = NavigateToPreviousItem();
                break;
            case KeyCode.Tab:
                elementSelected = NavigateToNextItem();
                evt.PreventDefault();
                break;
            case KeyCode.Home:
                elementSelected = SetSelectedVisibleIndex(0, true);
                break;
            case KeyCode.End:
                elementSelected = SetSelectedVisibleIndex(m_Items.Count - 1, false);
                break;
            case KeyCode.Return:
                onItemsActivated?.Invoke(m_SelectedItems);
                break;
            case KeyCode.A:
                if (evt.actionKey && multiSelection)
                {
                    SelectAll();
                }
                break;
            case KeyCode.Escape:
                ClearSelection();
                break;
            default:
                shouldStopPropagation = false;
                break;
        }

        if (shouldStopPropagation)
            evt.StopPropagation();

        if (elementSelected != null)
        {
            m_ItemsScrollView.ScrollTo(elementSelected);
        }
    }

    private VisualElement NavigateToNextItem()
    {
        var selectedIndex = GetSelectedVisibleIndex();
        if (selectedIndex == -1)
            selectedIndex = 0;
        else if (selectedIndex + 1 < m_Items.Count)
            selectedIndex = selectedIndex + 1;
        else if (wrapAroundKeyboardNavigation)
            selectedIndex = 0;

        return SetSelectedVisibleIndex(selectedIndex, true);
    }

    private VisualElement NavigateToPreviousItem()
    {
        var selectedIndex = GetSelectedVisibleIndex();
        if (selectedIndex == -1)
            selectedIndex = 0;
        else if (selectedIndex > 0)
            selectedIndex = selectedIndex - 1;
        else if (wrapAroundKeyboardNavigation)
            selectedIndex = m_Items.Count - 1;

        return SetSelectedVisibleIndex(selectedIndex, false);
    }

    public Item IdToItem(int id)
    {
        return m_IdToElements[id].userData as Item;
    }

    private static bool IsFiltered(Item item, string filter)
    {
        if (string.IsNullOrEmpty(filter))
            return true;
        return item.label.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) != -1;
    }

    public void RefreshItemElements()
    {
        m_ItemsContainer.Clear();
        foreach (var item in m_Items)
        {
            var element = CreateItemElement(item);
            m_IdToElements[item.id] = element;
            m_ItemsContainer.Add(element);
        }

        LayoutItems();
    }

    private VisualElement CreateItemElement(Item item)
    {
        var element = new VisualElement();
        element.AddToClassList(StyleSheetLoader.Styles.gridViewItemElement);
        element.userData = item;

        var icon = new VisualElement();
        icon.AddToClassList(StyleSheetLoader.Styles.gridViewItemIcon);
        if (item.icon != null)
        {
            icon.style.backgroundImage = item.icon;
        }

        element.Add(icon);

        var pinBtn = new Button(() =>
        {
            TogglePinned(item);
        });
        pinBtn.AddToClassList(StyleSheetLoader.Styles.gridViewItemPin);
        icon.Add(pinBtn);

        var label = new Label(item.label);
        label.AddToClassList(StyleSheetLoader.Styles.gridViewItemLabel);
        element.Add(label);

        element.RegisterCallback<MouseDownEvent>(evt => HandleSelect(evt, element));

        return element;
    }

    private void HandleSelect(MouseDownEvent evt, VisualElement clicked)
    {
        if (clicked == null)
        {
            ClearSelection();
            return;
        }

        if (evt.button != 0)
            return;

        var item = clicked.userData as Item;

        if (evt.clickCount == 1)
        {
            if (multiSelection)
            {
                if (evt.ctrlKey)
                {
                    if (IsSelected(item))
                    {
                        RemoveFromSelection(item);
                    }
                    else
                    {
                        AddToSelection(item);
                    }
                }
                else if (!IsSelected(item))
                {
                    SetSelection(new[] { item });
                }
            }
            else
            {
                if (IsSelected(item))
                {
                    if (evt.ctrlKey)
                    {
                        ClearSelection();
                    }
                }
                else
                {
                    SetSelection(new[] { item });
                }
            }
        }
        else if (evt.clickCount == 2 && m_SelectedItems.Count > 0)
        {
            onItemsActivated?.Invoke(m_SelectedItems);
        }
        
        evt.StopPropagation();
    }

    private void LayoutItems()
    {
        var allItemElements = m_ItemsContainer.Children();
        foreach (var element in allItemElements)
        {
            var pin = element.Q(null, StyleSheetLoader.Styles.gridViewItemPin);
            var icon = element.Q(null, StyleSheetLoader.Styles.gridViewItemIcon);

            if (isListView)
            {
                element.style.height = listItemHeight;
                element.style.width = Length.Percent(100);
                icon.style.width = listItemHeight;
                pin.RemoveFromHierarchy();
                element.Insert(0, pin);
            }
            else
            {
                element.style.width = sizeLevel * aspectRatio;
                element.style.height = sizeLevel;
                icon.style.width = StyleKeyword.Auto;
                pin.RemoveFromHierarchy();
                icon.Add(pin);
            }
        }
    }
}
