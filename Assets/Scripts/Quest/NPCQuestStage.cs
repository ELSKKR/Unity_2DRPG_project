using UnityEngine;

// 交任務要繳交多種道具時使用（例如同時交蘿蔔、高麗菜、洋蔥才算達成）
[System.Serializable]
public class ItemRequirement
{
    public ItemData item;
    [Min(1)] public int amount = 1;
}

// NPC 任務鏈裡的一個階段：一個任務 + 四種狀態的對話 + 解鎖條件
// 同一個任務可以被兩個 NPC 各自掛一個 stage：
//   發任務的人 → canAcceptQuest = true、completesQuestOnHandIn = false
//   收物件的人 → canAcceptQuest = false、completesQuestOnHandIn = true
[System.Serializable]
public class NPCQuestStage
{
    [Header("這一階段對應的任務（必填）")]
    public QuestData quest;

    [Header("這個 NPC 在這條任務裡的角色")]
    [Tooltip("勾選 = 這個 NPC 可以發這條任務。取消 = 只是關係人（例如收物件的對象），任務沒接時只會播 reminder")]
    public bool canAcceptQuest = true;
    [Tooltip("勾選 = 玩家帶著道具來找他就完成任務。取消 = 只播台詞不完成（例如發任務的人看到你拿著東西，只叫你去找別人）")]
    public bool completesQuestOnHandIn = true;

    [Header("解鎖條件（全部留空 = 前一階段一完成就直接解鎖）")]
    public QuestData requiredCompletedQuest;   // 需要先完成別的任務
    [Tooltip("需要玩家已經『接下』另一條任務（不用完成）。例如：委託人不會主動提起，要等玩家先跟另一個 NPC 談過才會開口")]
    public QuestData requiredAcceptedQuest;
    public ItemData requiredItem;               // 需要持有特定道具
    [Min(1)] public int requiredItemCount = 1;
    public string requiredEventID;               // 需要某個世界事件已觸發（例如 RiftZone 標記的事件）

    [Header("交任務要繳交的道具（單一道具用這個；留空 = 這階段不用交道具，達成條件即可完成）")]
    public ItemData turnInItem;
    [Tooltip("勾選 = 交任務後把道具扣掉。劇情物件想留在玩家身上就取消")]
    public bool consumeTurnInItem = true;

    [Header("要同時交多種道具才算達成時用這個（設了就取代上面單一道具的設定，全部道具都要有才會進入可交任務狀態）")]
    public ItemRequirement[] turnInItems;

    [Header("交任務時要發給玩家的道具（留空 = 不發放。例如：中繼 NPC 收下素材、回贈成品）")]
    public ItemData rewardItem;
    [Min(1)] public int rewardItemCount = 1;

    [Header("完成這階段時要標記的世界事件（留空 = 不標記）")]
    public string unlockEventID;

    [Header("任務階段索引（對應 QuestData.stageObjectives）")]
    public int stageIndexInProgress = 0;
    public int stageIndexReadyToHandIn = 1;

    [Header("四種狀態的對話內容")]
    public DialogSegment intro;       // 尚未接任務時：委託任務（通常帶選項）
    public DialogSegment reminder;    // 已接任務、條件未達成
    public DialogSegment handIn;      // 條件達成，可以交任務
    public DialogSegment completed;   // 任務已完成後的閒聊

    // 這一階段的解鎖條件是否都滿足了
    public bool ConditionsMet()
    {
        if (requiredCompletedQuest != null)
        {
            if (QuestManager.Instance == null || !QuestManager.Instance.IsQuestCompleted(requiredCompletedQuest))
                return false;
        }

        if (requiredAcceptedQuest != null)
        {
            if (QuestManager.Instance == null || !QuestManager.Instance.IsQuestAccepted(requiredAcceptedQuest))
                return false;
        }

        if (requiredItem != null)
        {
            if (Inventory.Instance == null || Inventory.Instance.GetItemCount(requiredItem) < requiredItemCount)
                return false;
        }

        if (!string.IsNullOrEmpty(requiredEventID))
        {
            if (WorldStateManager.Instance == null || !WorldStateManager.Instance.HasEvent(requiredEventID))
                return false;
        }

        return true;
    }
}
