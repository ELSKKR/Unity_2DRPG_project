using UnityEngine;

// 可閱讀的告示牌／路標。跟 NPCDialog 走同一套對話系統，但沒有任務、沒有選項，
// 純粹播一段文字（要選項的話 DialogSegment 本來就支援，直接在 content 裡加就好）。
public class SignPost : MonoBehaviour, IInteractable
{
    [Header("對話框上顯示的名稱")]
    [SerializeField] private string signName = "路標";

    [Header("牌子上的內容")]
    [SerializeField] private DialogSegment content;

    [SerializeField] private string interactionText = "閱讀";

    public string InteractionPrompt => interactionText;
    public bool CanInteract => true;

    private Collider2D cachedCollider;
    private Transform cachedPlayer;

    // 互動區可能鋪得很寬（例如整段柵欄），這時提示要浮在「玩家最靠近的那一點」，
    // 不然玩家站在邊緣時提示會飄到區域中心、離自己好幾個單位遠。
    // 小的告示牌則 ClosestPoint 幾乎就等於自己的位置，不用另外分兩種寫法。
    public Vector3 PromptWorldPosition
    {
        get
        {
            if (cachedCollider == null) cachedCollider = GetComponent<Collider2D>();
            if (cachedPlayer == null)
            {
                var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
                if (pc != null) cachedPlayer = pc.transform;
            }

            if (cachedCollider == null || cachedPlayer == null) return transform.position;
            return cachedCollider.ClosestPoint(cachedPlayer.position);
        }
    }

    public void Interact()
    {
        if (content == null || content.lines == null || content.lines.Length == 0) return;
        DialogManager.Instance.StartDialog(signName, content);
    }
}
