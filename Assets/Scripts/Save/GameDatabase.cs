using UnityEngine;

// 存檔系統用：把「字串 ID」查回真正的 ItemData／QuestData 資產參照。
// 遊戲執行期不能用 AssetDatabase（那個是 Editor-only），所以要有一份
// 手動維護的清單。新增道具/任務時記得把它拖進這份清單的陣列裡，
// 不然存檔讀回來會找不到對應資產（ImportSave 裡有 Debug.LogWarning 提醒）。
[CreateAssetMenu(fileName = "GameDatabase", menuName = "JRPG/Game Database")]
public class GameDatabase : ScriptableObject
{
    public ItemData[] allItems;
    public QuestData[] allQuests;
    public IntelData[] allIntel;

    public ItemData FindItem(string itemID)
    {
        if (string.IsNullOrEmpty(itemID) || allItems == null) return null;
        foreach (var item in allItems)
            if (item != null && item.itemID == itemID) return item;
        return null;
    }

    public QuestData FindQuest(string questID)
    {
        if (string.IsNullOrEmpty(questID) || allQuests == null) return null;
        foreach (var quest in allQuests)
            if (quest != null && quest.questID == questID) return quest;
        return null;
    }

    public IntelData FindIntel(string intelID)
    {
        if (string.IsNullOrEmpty(intelID) || allIntel == null) return null;
        foreach (var intel in allIntel)
            if (intel != null && intel.intelID == intelID) return intel;
        return null;
    }
}
