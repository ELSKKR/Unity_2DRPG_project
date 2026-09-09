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
        icon.enabled = true;
        icon.sprite = item.icon;
        quantityText.text = quantity > 1 ? quantity.ToString() : "";
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