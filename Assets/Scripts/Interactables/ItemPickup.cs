using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private string uniqueID;
    [SerializeField] private int quantity = 1;
    [SerializeField] private string interactionText = "撿起";

    [Header("音效（留空 = 用 AudioManager 的共用預設；只有這個拾取物想用不一樣的音效才需要設）")]
    [SerializeField] private AudioClip pickupSound;

    public string InteractionPrompt => interactionText;
    public Vector3 PromptWorldPosition => transform.position;
    public bool CanInteract => true;

    [ContextMenu("Generate Unique ID")]
    private void GenerateUniqueID()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }

    private void Awake()
    {
        if (WorldStateManager.Instance != null &&
            WorldStateManager.Instance.IsItemCollected(uniqueID))
        {
            Destroy(gameObject);
            return;
        }
    }

    public void Interact()
    {
        // 傳自己的座標，飄字提示才會從「物品原地」冒出來（而不是退回玩家頭上）
        bool success = Inventory.Instance.AddItem(itemData, quantity, transform.position);

        if (!success)
        {
            Debug.Log("背包已滿，無法撿取");
            return;
        }

        AudioClip pickup = pickupSound != null ? pickupSound : AudioManager.Instance.ItemPickupSound;
        if (pickup != null)
            AudioManager.Instance.PlaySFX(pickup);

        global::InteractionPrompt.Instance?.Hide();
        WorldStateManager.Instance.MarkItemCollected(uniqueID);
        Destroy(gameObject);
    }
}
