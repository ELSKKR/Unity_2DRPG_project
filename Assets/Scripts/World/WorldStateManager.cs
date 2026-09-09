using System.Collections.Generic;
using UnityEngine;

public class WorldStateManager : MonoBehaviour
{
    public static WorldStateManager Instance { get; private set; }

    private HashSet<string> collectedItemIDs = new HashSet<string>();

    // 一次性世界事件旗標（例如 riftUnlocked、某段演出已播過）
    private HashSet<string> eventFlags = new HashSet<string>();

    // 已採收的作物格子（key 是 CropField 自己組的「田地ID:格子座標」，見 CropField.cs）
    private HashSet<string> harvestedCropCells = new HashSet<string>();

    public delegate void WorldStateChanged();
    public event WorldStateChanged OnWorldStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool IsItemCollected(string itemID)
    {
        return collectedItemIDs.Contains(itemID);
    }

    public void MarkItemCollected(string itemID)
    {
        collectedItemIDs.Add(itemID);
        OnWorldStateChanged?.Invoke();
    }

    // ---- 已採收的作物格子 ----
    // 場景（例如 Forest_Village）在進出房子時會被整個 Unload 再重新 Load，
    // CropField 用 tilemap.SetTile(cell, null) 做的「清空這格作物」是純執行期的 tilemap 狀態，
    // 不會存回場景檔，重新載入就會被場景檔裡「作物還在」的原始資料蓋掉——這裡負責跨場景重載記住哪些格子已經採過

    public bool IsCropHarvested(string cellKey)
    {
        return harvestedCropCells.Contains(cellKey);
    }

    public void MarkCropHarvested(string cellKey)
    {
        harvestedCropCells.Add(cellKey);
    }

    // ---- 一次性事件旗標 ----

    public bool HasEvent(string eventID)
    {
        return !string.IsNullOrEmpty(eventID) && eventFlags.Contains(eventID);
    }

    // 回傳 true 代表這次是「第一次」標記，可用來確保演出只觸發一次
    public bool MarkEvent(string eventID)
    {
        if (string.IsNullOrEmpty(eventID)) return false;

        bool isFirstTime = eventFlags.Add(eventID);
        if (isFirstTime)
        {
            Debug.Log($"世界事件觸發: {eventID}");
            OnWorldStateChanged?.Invoke();
        }
        return isFirstTime;
    }

    // ---- 存檔 ----
    // 三個 HashSet 本來就是字串，不用另外轉換，直接包成 List 給 JsonUtility 序列化

    public List<string> ExportCollectedItems() => new List<string>(collectedItemIDs);
    public List<string> ExportEventFlags() => new List<string>(eventFlags);
    public List<string> ExportHarvestedCropCells() => new List<string>(harvestedCropCells);

    public void ImportSave(List<string> collected, List<string> events, List<string> harvestedCrops)
    {
        collectedItemIDs = new HashSet<string>(collected ?? new List<string>());
        eventFlags = new HashSet<string>(events ?? new List<string>());
        harvestedCropCells = new HashSet<string>(harvestedCrops ?? new List<string>());
        OnWorldStateChanged?.Invoke();
    }

    // 新遊戲／回標題用：清空所有世界狀態
    public void ResetAll()
    {
        collectedItemIDs.Clear();
        eventFlags.Clear();
        harvestedCropCells.Clear();
        OnWorldStateChanged?.Invoke();
    }
}