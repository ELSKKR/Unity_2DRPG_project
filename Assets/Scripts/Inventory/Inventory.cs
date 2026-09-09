using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [SerializeField] private int maxSlots = 20;
    public int MaxSlots => maxSlots;
    public List<InventorySlot> Slots { get; private set; } = new List<InventorySlot>();

    [Header("音效（留空 = 不播放）")]
    [SerializeField] private AudioClip fullSound;

    public delegate void InventoryChanged();
    public event InventoryChanged OnInventoryChanged;

    // 成功加入道具時觸發，給「獲得道具」的飄字提示用。
    // Vector3 是提示要冒出來的世界座標——由呼叫端提供（例如地上的拾取物傳自己的位置）；
    // 沒提供的來源（任務獎勵之類）會退回玩家頭上，見 FloatingTextManager。
    public delegate void ItemAdded(ItemData item, int amount, Vector3? worldPosition);
    public event ItemAdded OnItemAdded;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 舊的呼叫端維持原樣，不用逐一改
    public bool AddItem(ItemData item, int amount = 1) => AddItem(item, amount, null);

    public bool AddItem(ItemData item, int amount, Vector3? worldPosition)
    {
        if (item == null || amount <= 0) return false;

        // 先確認裝得下再動手：不然數量一多、背包快滿時，可能塞進去一部分卻回傳
        // false，呼叫端（例如 ItemPickup）以為完全沒加成功而不摧毀場上物件，
        // 結果玩家已經拿到一部分道具、又能回去把同一個物件再撿一次形成複製
        if (!CanFit(item, amount))
        {
            Debug.LogWarning("背包已滿");
            if (fullSound != null)
                AudioManager.Instance.PlaySFX(fullSound);
            return false;
        }

        int remaining = amount;

        if (item.isStackable)
        {
            foreach (var slot in Slots)
            {
                if (remaining <= 0) break;
                if (slot.item != item || slot.quantity >= item.maxStack) continue;

                int spaceLeft = item.maxStack - slot.quantity;
                int addAmount = Mathf.Min(spaceLeft, remaining);
                slot.quantity += addAmount;
                remaining -= addAmount;
            }
        }

        while (remaining > 0)
        {
            int stackAmount = item.isStackable ? Mathf.Min(remaining, item.maxStack) : 1;
            Slots.Add(new InventorySlot(item, stackAmount));
            remaining -= stackAmount;
        }

        OnInventoryChanged?.Invoke();
        OnItemAdded?.Invoke(item, amount, worldPosition);
        return true;
    }

    // 計算 amount 個 item 裝不裝得下，不實際修改任何資料
    bool CanFit(ItemData item, int amount)
    {
        int remaining = amount;

        if (item.isStackable)
        {
            foreach (var slot in Slots)
            {
                if (slot.item != item) continue;
                remaining -= Mathf.Max(0, item.maxStack - slot.quantity);
                if (remaining <= 0) return true;
            }
        }

        int slotsNeeded = item.isStackable
            ? Mathf.CeilToInt(remaining / (float)item.maxStack)
            : remaining;

        return Slots.Count + slotsNeeded <= maxSlots;
    }

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        // 同樣改成原子操作：數量不夠就完全不動手，避免「扣到一半」的殘局
        // （呼叫端如果沒檢查回傳值，道具會被扣光卻沒人知道任務其實沒達成條件）
        if (GetItemCount(item) < amount) return false;

        int remaining = amount;
        for (int i = Slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            if (Slots[i].item != item) continue;

            int removeAmount = Mathf.Min(Slots[i].quantity, remaining);
            Slots[i].quantity -= removeAmount;
            remaining -= removeAmount;

            if (Slots[i].quantity <= 0)
                Slots.RemoveAt(i);
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public int GetItemCount(ItemData item)
    {
        int total = 0;
        foreach (var slot in Slots)
            if (slot.item == item) total += slot.quantity;
        return total;
    }

    // ---- 存檔 ----

    public List<InventorySaveEntry> ExportSave()
    {
        var list = new List<InventorySaveEntry>();
        foreach (var slot in Slots)
        {
            if (slot.IsEmpty) continue;
            list.Add(new InventorySaveEntry { itemID = slot.item.itemID, quantity = slot.quantity });
        }
        return list;
    }

    public void ImportSave(List<InventorySaveEntry> entries, GameDatabase database)
    {
        Slots.Clear();
        if (entries != null)
        {
            foreach (var entry in entries)
            {
                ItemData item = database.FindItem(entry.itemID);
                if (item == null)
                {
                    Debug.LogWarning($"存檔裡的道具 ID「{entry.itemID}」在 GameDatabase 裡找不到對應資產，這格道具會遺失");
                    continue;
                }
                Slots.Add(new InventorySlot(item, entry.quantity));
            }
        }
        OnInventoryChanged?.Invoke();
    }

    // 新遊戲／回標題用：清空背包
    public void ResetAll()
    {
        Slots.Clear();
        OnInventoryChanged?.Invoke();
    }
}
