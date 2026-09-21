using System.Collections.Generic;

// 存檔的完整內容。純資料類別（不是 MonoBehaviour/ScriptableObject），
// 用 JsonUtility 直接序列化成一個 .json 檔。
[System.Serializable]
public class SaveData
{
    public string sceneName;
    public float playerX;
    public float playerY;

    public List<QuestSaveEntry> quests = new List<QuestSaveEntry>();
    public string trackedQuestID;   // 角落 HUD 目前追蹤中的任務，對應 QuestManager.TrackedQuestID
    public List<InventorySaveEntry> inventory = new List<InventorySaveEntry>();

    // 對應 WorldStateManager 的三個 HashSet<string>，本來就是字串，直接存
    public List<string> collectedItemIDs = new List<string>();
    public List<string> eventFlags = new List<string>();
    public List<string> harvestedCropCells = new List<string>();

    // 已解鎖的情報（IntelData.intelID），讀檔用 GameDatabase 查回真正的資產參照
    public List<string> unlockedIntelIDs = new List<string>();

    // 純粹給存檔槽選單顯示用（ISO 8601），不影響讀檔邏輯
    public string savedAtUtc;

    // 這個存檔累積的總遊玩秒數（跨很多次讀檔/存檔累加，不是這次 session 的時間）
    public float totalPlaytimeSeconds;
}

[System.Serializable]
public class QuestSaveEntry
{
    public string questID;   // 對應 QuestData.questID，讀檔時用 GameDatabase 查回真正的 QuestData
    public int status;       // (int)QuestStatus
    public int currentStage;
}

[System.Serializable]
public class InventorySaveEntry
{
    public string itemID;    // 對應 ItemData.itemID
    public int quantity;
}
