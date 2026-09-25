using UnityEngine;

// 試玩版專用：玩家把目前做好的內容都跑完時，跳一則「內容到這裡結束」的通知。
// 不然玩家做完最後一條線之後不會知道還有沒有東西，會一直在村子裡繞。
//
// 森林留客之後，這則通知就是「森林的路，開了」，shownFlagID 也是 ForestLoopZone 判斷路開了沒的依據。
//
// 判斷條件分三種，全部成立才算跑完：
//   requiredQuests    —— 這些任務全部完成
//   requiredAnyOfFlags —— 這些世界旗標「至少一個」成立
//   requiredAllFlags  —— 這些世界旗標「全部」成立（例如魯克線的 Luke_Helped）
// 第二種是為了處理「有結果但不見得是完成任務」的支線：例如亞爾的佩劍，玩家可以選擇還給他
// （任務完成）或自己留著（任務不會完成，但標記 Yaer_SwordKept）——兩種都算玩家已經給出答案。
//
// 只會跳一次：用世界事件旗標記住，而且旗標本身有進存檔，重開遊戲也不會再跳。
public class DemoCompletionNotice : MonoBehaviour
{
    [Header("這些任務全部完成")]
    [SerializeField] private QuestData[] requiredQuests;

    [Header("這些世界旗標至少成立一個（留空 = 不檢查）")]
    [SerializeField] private string[] requiredAnyOfFlags;

    [Header("這些世界旗標全部成立（留空 = 不檢查）")]
    [SerializeField] private string[] requiredAllFlags;

    [Header("通知內容")]
    [TextArea(2, 3)]
    [SerializeField] private string message = "目前的試玩內容到這裡結束了，感謝遊玩！";

    [Tooltip("記住已經跳過的世界事件 ID，避免重複跳")]
    [SerializeField] private string shownFlagID = "DemoCompletionNoticeShown";

    void Start()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestsChanged += Check;
        if (WorldStateManager.Instance != null)
            WorldStateManager.Instance.OnWorldStateChanged += Check;
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestsChanged -= Check;
        if (WorldStateManager.Instance != null)
            WorldStateManager.Instance.OnWorldStateChanged -= Check;
    }

    void Check()
    {
        if (WorldStateManager.Instance == null || QuestManager.Instance == null) return;
        if (WorldStateManager.Instance.HasEvent(shownFlagID)) return;   // 已經跳過了

        if (requiredQuests != null)
            foreach (var q in requiredQuests)
                if (q != null && !QuestManager.Instance.IsQuestCompleted(q)) return;

        if (requiredAnyOfFlags != null && requiredAnyOfFlags.Length > 0)
        {
            bool any = false;
            foreach (var f in requiredAnyOfFlags)
                if (!string.IsNullOrEmpty(f) && WorldStateManager.Instance.HasEvent(f)) { any = true; break; }
            if (!any) return;
        }

        if (requiredAllFlags != null)
            foreach (var f in requiredAllFlags)
                if (!string.IsNullOrEmpty(f) && !WorldStateManager.Instance.HasEvent(f)) return;

        // MarkEvent 會再觸發一次 OnWorldStateChanged，但上面的 HasEvent 已經擋掉重入
        WorldStateManager.Instance.MarkEvent(shownFlagID);
        NotificationToast.Instance?.ShowMessage(message);
    }
}
