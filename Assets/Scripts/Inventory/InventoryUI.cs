using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MenuPanelBase
{
    public static InventoryUI Instance { get; private set; }

    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("Detail Panel")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailIcon;
    [SerializeField] private TextMeshProUGUI detailNameText;
    [SerializeField] private TextMeshProUGUI detailDescriptionText;

    private int selectedIndex = -1;
    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    protected override void Start()
    {
        base.Start();
        detailPanel.SetActive(false);
        Inventory.Instance.OnInventoryChanged += RefreshUI;
    }

    void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= RefreshUI;
    }

    // 開關的快捷鍵由資訊視窗的 TabGroup 統一處理（要判斷「切分頁」還是「關視窗」），這裡不再自己監聽

    protected override void OnOpened() => RefreshUI();

    protected override void OnClosed()
    {
        selectedIndex = -1;
        detailPanel.SetActive(false);
    }

    void RefreshUI()
    {
        // 倒著走並先脫離父物件：Destroy 要到這一幀結束才生效，
        // 同一幀內若再次 Refresh，舊的項目會被算進 childCount 而重複出現
        for (int i = slotContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = slotContainer.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        slotUIs.Clear();

        int totalSlots = Inventory.Instance.MaxSlots;

        for (int i = 0; i < totalSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
            slotUI.Setup(i, this);

            bool hasItem = i < Inventory.Instance.Slots.Count && !Inventory.Instance.Slots[i].IsEmpty;

            if (hasItem)
            {
                var slot = Inventory.Instance.Slots[i];
                slotUI.SetItem(slot.item, slot.quantity);
            }
            else
            {
                slotUI.SetEmpty();
            }

            slotUIs.Add(slotUI);
        }

        // 檢查目前選取的格子是否已經沒有物品（用完/丟棄），若是則取消選取
        bool selectionStillValid = selectedIndex >= 0
            && selectedIndex < Inventory.Instance.Slots.Count
            && !Inventory.Instance.Slots[selectedIndex].IsEmpty;

        if (!selectionStillValid)
            selectedIndex = -1;

        UpdateHighlights();
        UpdateDetailPanel();
    }

    public void SelectSlot(int index)
    {
        bool hasItem = index < Inventory.Instance.Slots.Count && !Inventory.Instance.Slots[index].IsEmpty;
        if (!hasItem) return;

        if (selectedIndex == index)
        {
            // 再次點擊同一格 -> 取消選取，關閉 Detail 面板
            selectedIndex = -1;
        }
        else
        {
            selectedIndex = index;
        }

        UpdateHighlights();
        UpdateDetailPanel();
    }

    void UpdateHighlights()
    {
        for (int i = 0; i < slotUIs.Count; i++)
            slotUIs[i].SetHighlighted(i == selectedIndex);
    }

    void UpdateDetailPanel()
    {
        if (selectedIndex < 0)
        {
            detailPanel.SetActive(false);
            return;
        }

        var slot = Inventory.Instance.Slots[selectedIndex];
        detailPanel.SetActive(true);
        detailIcon.sprite = slot.item.icon;
        detailNameText.text = slot.item.itemName;
        detailDescriptionText.text = slot.item.description;
    }
}
