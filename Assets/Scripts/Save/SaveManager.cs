using System;
using System.IO;
using UnityEngine;

// 存檔系統的總協調者：跟 QuestManager／Inventory／WorldStateManager 要資料、
// 包成 SaveData、寫成 JSON 檔；讀檔時反過來把資料灌回這三個 Manager，
// 並透過 SceneTransitionManager 切到存檔記錄的場景＋座標。
//
// 3 個固定存檔槽，檔案放在 Application.persistentDataPath（跨平台的使用者資料夾，
// 不在 Assets 底下，打包後也讀寫得到）。
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public const int SlotCount = 3;

    [Header("新遊戲的起始場景／出生點")]
    [SerializeField] private string newGameScene = "Forest_Village";
    [SerializeField] private string newGameSpawnID = "default";

    [Header("存檔用的 ID 對照表（新增道具/任務記得拖進這份資產）")]
    [SerializeField] private GameDatabase database;

    // -1 = 目前沒有進行中的遊戲（例如在標題畫面）
    public int CurrentSlot { get; private set; } = -1;

    // 遊玩時間：playtimeBase 是讀檔當下存檔裡原本就有的秒數，sessionElapsed 是這次讀檔以來累積的。
    // 存檔時兩個相加寫回去；不特別判斷選單開關，開著背包/任務/設定也照樣計時
    // （多數 JRPG 的「遊玩時數」就是這個存檔開著遊戲的總時間，不用為了暫停邏輯多繞一層）
    float playtimeBaseSeconds;
    float sessionElapsedSeconds;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (CurrentSlot >= 0)
            sessionElapsedSeconds += Time.deltaTime;
    }

    string SlotPath(int slot) => Path.Combine(Application.persistentDataPath, $"save_slot{slot}.json");

    public bool SlotExists(int slot) => File.Exists(SlotPath(slot));

    // 給存檔槽選單顯示用：讀出存檔內容但不套用到遊戲裡
    public SaveData PeekSlot(int slot)
    {
        if (!SlotExists(slot)) return null;
        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(SlotPath(slot)));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"讀取存檔槽 {slot} 失敗，當成空槽處理: {e.Message}");
            return null;
        }
    }

    // ---- 存檔 ----

    // 自動存檔／存檔點都呼叫這個：存到「目前這一局」使用的槽位
    public void SaveToCurrentSlot()
    {
        if (CurrentSlot < 0) return;   // 不在遊戲中（例如標題畫面）就不用存
        SaveToSlot(CurrentSlot);
    }

    void SaveToSlot(int slot)
    {
        var player = FindFirstObjectByType<PlayerController>();
        var data = new SaveData
        {
            sceneName = SceneTransitionManager.Instance.CurrentGameplayScene,
            playerX = player != null ? player.transform.position.x : 0f,
            playerY = player != null ? player.transform.position.y : 0f,
            quests = QuestManager.Instance.ExportSave(),
            inventory = Inventory.Instance.ExportSave(),
            collectedItemIDs = WorldStateManager.Instance.ExportCollectedItems(),
            eventFlags = WorldStateManager.Instance.ExportEventFlags(),
            harvestedCropCells = WorldStateManager.Instance.ExportHarvestedCropCells(),
            unlockedIntelIDs = IntelManager.Instance.ExportSave(),
            savedAtUtc = DateTime.UtcNow.ToString("o"),
            totalPlaytimeSeconds = playtimeBaseSeconds + sessionElapsedSeconds,
        };

        File.WriteAllText(SlotPath(slot), JsonUtility.ToJson(data, true));
    }

    public void DeleteSlot(int slot)
    {
        if (SlotExists(slot)) File.Delete(SlotPath(slot));
    }

    // ---- 開始遊戲 ----

    // 存檔槽是空的：清空所有進度、切到新遊戲的起始場景
    public void StartNewGame(int slot)
    {
        QuestManager.Instance.ResetAll();
        Inventory.Instance.ResetAll();
        WorldStateManager.Instance.ResetAll();
        IntelManager.Instance.ResetAll();
        CurrentSlot = slot;
        playtimeBaseSeconds = 0f;
        sessionElapsedSeconds = 0f;

        SceneTransitionManager.Instance.TransitionToScene(newGameScene, newGameSpawnID, () =>
        {
            var player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (player != null) player.gameObject.SetActive(true);

            SaveToSlot(slot);   // 立刻存一次，避免玩家馬上關遊戲、槽位還是顯示空的
        });
    }

    // 存檔槽有記錄：讀資料、切到記錄的場景，再用精確座標蓋過出生點
    public void LoadSlotAndEnterGame(int slot)
    {
        var data = PeekSlot(slot);
        if (data == null)
        {
            Debug.LogWarning($"存檔槽 {slot} 讀取失敗，無法繼續");
            return;
        }

        QuestManager.Instance.ImportSave(data.quests, database);
        Inventory.Instance.ImportSave(data.inventory, database);
        WorldStateManager.Instance.ImportSave(data.collectedItemIDs, data.eventFlags, data.harvestedCropCells);
        IntelManager.Instance.ImportSave(data.unlockedIntelIDs, database);
        CurrentSlot = slot;
        playtimeBaseSeconds = data.totalPlaytimeSeconds;
        sessionElapsedSeconds = 0f;

        // spawnID 傳 null：不要用出生點定位，讀檔要的是存檔裡的精確座標
        SceneTransitionManager.Instance.TransitionToScene(data.sceneName, null, () =>
        {
            var player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (player != null)
            {
                player.gameObject.SetActive(true);
                player.transform.position = new Vector3(data.playerX, data.playerY, 0f);
            }
        });
    }

    // ---- 回標題 ----

    public void ReturnToTitle(string titleSceneName = "TitleScreen")
    {
        // 已經在標題畫面了就不用再切一次（例如標題畫面本身也能打開設定，
        // 不小心按到「回到標題」按鈕不該讓 TitleScreen 卸載又重載一次）
        if (SceneTransitionManager.Instance.CurrentGameplayScene == titleSceneName) return;

        SaveToCurrentSlot();   // 離開前先存一次，不要讓玩家白白弄丟這幾分鐘的進度

        SceneTransitionManager.Instance.TransitionToScene(titleSceneName, null, () =>
        {
            var player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (player != null) player.gameObject.SetActive(false);

            CurrentSlot = -1;
        });
    }
}
