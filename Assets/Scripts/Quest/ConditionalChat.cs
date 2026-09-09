using UnityEngine;

// 純聊天用的對話，可依外部條件切換（例如別人的任務完成後才說的話）
// NPCDialog 會挑「條件符合的最後一筆」，所以請把預設台詞放前面、特殊變體放後面
[System.Serializable]
public class ConditionalChat
{
    [Header("條件（全部留空 = 無條件，當作預設台詞）")]
    public QuestData requiredCompletedQuest;   // 需要某個任務已完成
    public string requiredEventID;             // 需要某個世界事件已觸發

    public DialogSegment segment;

    public bool ConditionsMet()
    {
        if (requiredCompletedQuest != null)
        {
            if (QuestManager.Instance == null || !QuestManager.Instance.IsQuestCompleted(requiredCompletedQuest))
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
