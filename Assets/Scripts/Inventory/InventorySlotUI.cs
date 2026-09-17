using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject highlight;

    private int slotIndex;
    private InventoryUI inventoryUI;

    public void Setup(int index, InventoryUI ui)
    {
        slotIndex = index;
        inventoryUI = ui;
    }

    public void SetEmpty()
    {
        icon.enabled = false;
        icon.sprite = null;
        quantityText.text = "";
    }

    public void SetItem(ItemData item, int quantity)
    {
        // 道具忘了指定 icon 時 sprite 會是 null，而 Image 元件在 sprite 為 null 時
        // 會畫出一塊純白方塊蓋住格子——看起來像 UI 壞掉，不像資料漏填，很難查。
        // 這裡直接不顯示圖示，並指名是哪個道具，免得又是一個沒有線索的安靜失敗
        icon.enabled = item.icon != null;
        icon.sprite = item.icon;

        if (item.icon == null)
            Debug.LogWarning($"道具「{item.name}」沒有指定 icon，背包格子不會顯示圖示。");

        quantityText.text = "x" + quantity;
    }

    public void SetHighlighted(bool state)
    {
        if (highlight != null)
            highlight.SetActive(state);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        inventoryUI.SelectSlot(slotIndex);
    }
}