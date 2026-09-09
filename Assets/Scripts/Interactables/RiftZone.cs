using UnityEngine;
using UnityEngine.Events;

// 裂縫地點：玩家走進來時，若前置事件已解鎖就播一次演出
public class RiftZone : MonoBehaviour
{
    [Header("裂縫設定")]
    [SerializeField] private string riftID = "zone_01";

    [Header("前置條件（留空 = 不需要條件，走進來就播）")]
    [SerializeField] private string requiredEventID = "riftUnlocked";

    [Header("演出文字（逐行淡入淡出）")]
    [TextArea(1, 3)]
    [SerializeField] private string[] cutsceneLines;

    [Header("條件未達成時的提示（留空 = 靠近沒有任何反應）")]
    [TextArea(1, 2)]
    [SerializeField] private string lockedMessage = "……暫時沒有異常。";
    [SerializeField] private string lockedSpeakerName = "";

    [Header("事件")]
    public UnityEvent onRiftActivated;   // Inspector 可掛額外效果（音效、粒子…）

    // 演出是否播過，記在 WorldStateManager，用 riftID 當事件 ID
    private string PlayedEventID => $"rift_played_{riftID}";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var world = WorldStateManager.Instance;
        if (world == null) return;

        // 已經播過就不再觸發
        if (world.HasEvent(PlayedEventID)) return;

        // 前置條件還沒解鎖：給個提示就好，不算觸發
        if (!string.IsNullOrEmpty(requiredEventID) && !world.HasEvent(requiredEventID))
        {
            ShowLockedMessage();
            return;
        }

        ActivateRift();
    }

    void ShowLockedMessage()
    {
        if (string.IsNullOrEmpty(lockedMessage)) return;
        if (DialogManager.Instance == null || DialogManager.Instance.IsDialogActive) return;

        var segment = new DialogSegment { lines = new[] { lockedMessage } };
        DialogManager.Instance.StartDialog(lockedSpeakerName, segment);
    }

    void ActivateRift()
    {
        // 先標記再播，避免演出期間又被觸發一次
        WorldStateManager.Instance.MarkEvent(PlayedEventID);

        Debug.Log($"裂縫演出觸發: {riftID}");
        onRiftActivated?.Invoke();

        if (RiftCutscene.Instance != null && cutsceneLines != null && cutsceneLines.Length > 0)
            RiftCutscene.Instance.Play(cutsceneLines);
    }
}
