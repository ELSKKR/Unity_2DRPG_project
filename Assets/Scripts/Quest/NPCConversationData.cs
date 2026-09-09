using UnityEngine;

// 一個 NPC 完整的對話/任務資料：所有階段都放在同一個檔案裡，
// 不用像以前一樣一段對話就要拆成好幾個獨立的 .asset
[CreateAssetMenu(fileName = "NewNPCConversation", menuName = "JRPG/NPC Conversation")]
public class NPCConversationData : ScriptableObject
{
    [Header("角色顯示名稱")]
    public string speakerName;

    [Header("任務階段（依序推進：完成上一階段、解鎖條件也滿足了，才會換下一階段）")]
    public NPCQuestStage[] stages;

    [Header("純聊天（沒有任務、或所有階段都還沒解鎖時用；條件符合的最後一筆優先）")]
    public ConditionalChat[] chats;

    // 挑出目前該用哪一段閒聊：由後往前找第一個條件符合的，
    // 這樣把預設台詞放第一筆、特殊變體放後面就會自然覆蓋
    public DialogSegment GetActiveChat()
    {
        if (chats == null) return null;

        for (int i = chats.Length - 1; i >= 0; i--)
        {
            if (chats[i] != null && chats[i].ConditionsMet())
                return chats[i].segment;
        }
        return null;
    }
}
