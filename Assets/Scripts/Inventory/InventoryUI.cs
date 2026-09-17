using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// 書本的「背包」那一頁。掛在 Page_Inventory 上，由 SideTabs 的 TabGroup 切換顯示——
// 開關書、鎖玩家移動、播動畫都是書（BookWindow 上的 BookMenuPanel）的事，這裡只管內容。
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("Detail Panel")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailIcon;
    [SerializeField] private TextMeshProUGUI detailNameText;
    [SerializeField] private TextMeshProUGUI detailDescriptionText;
    [Tooltip("屬性欄的標籤（左欄）與數值（右欄）分成兩個文字：這樣不必依賴等寬字型也能對齊")]
    [SerializeField] private TextMeshProUGUI detailAttrLabels;
    [SerializeField] private TextMeshProUGUI detailAttrValues;

    private int selectedIndex = -1;
    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

    // 訂閱跟著「這一頁有沒有顯示」走，不是跟著物件存在與否走：
    // 頁面藏起來的時候背包改了也不用重建，下次翻回來 OnEnable 會重新抓一次。
    // 開機當下 Inventory 可能還沒 Awake，那次就會跳過訂閱——但那時頁面馬上會被書關掉，
    // 玩家真的翻到這一頁時 OnEnable 會再跑一次，那時一定抓得到
    void OnEnable()
    {
        if (Inventory.Instance == null) return;
        Inventory.Instance.OnInventoryChanged += RefreshUI;
        RefreshUI();
    }

    void OnDisable()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= RefreshUI;

        selectedIndex = -1;
        if (detailPanel != null) detailPanel.SetActive(false);
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

        // 翻開背包卻兩頁都空著會讓玩家以為右頁本來就沒東西可看，
        // 所以還沒選過東西時自動選第一個有物品的格子，右頁跟著顯示
        if (selectedIndex < 0)
        {
            for (int i = 0; i < Inventory.Instance.Slots.Count; i++)
            {
                if (!Inventory.Instance.Slots[i].IsEmpty)
                {
                    selectedIndex = i;
                    break;
                }
            }
        }

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
        var item = slot.item;
        detailPanel.SetActive(true);

        // 跟格子那邊同樣的處理：沒指定 icon 時 Image 會畫成一塊白方塊
        detailIcon.enabled = item.icon != null;
        detailIcon.sprite = item.icon;

        detailNameText.text = item.itemName;
        detailDescriptionText.text = item.description;

        // 屬性欄。目前 ItemData 只有堆疊相關的資料可以顯示——
        // 之後要加攻擊力／售價／產地，就在這兩串各補一行，版面不用動
        if (detailAttrLabels != null && detailAttrValues != null)
        {
            detailAttrLabels.text = "持有\n可堆疊\n堆疊上限";
            detailAttrValues.text = $"×{slot.quantity}\n"
                                  + (item.isStackable ? "是\n" : "否\n")
                                  + (item.isStackable ? item.maxStack.ToString() : "—");
        }
    }
}
