using System.Collections.Generic;
using UnityEngine;

// 情報系統的總管：玩家聽到/發現的「情報」（八卦、線索），解鎖之後
// 可以在情報面板回頭翻閱，也可以拿來當對話選項的解鎖條件（DialogChoice.requiredIntel）。
// 架構完全比照 QuestManager：解鎖清單 + 事件 + 存讀檔。
public class IntelManager : MonoBehaviour
{
    public static IntelManager Instance { get; private set; }

    public List<IntelData> UnlockedIntel { get; private set; } = new List<IntelData>();

    public delegate void IntelChanged();
    public event IntelChanged OnIntelChanged;

    public delegate void IntelEvent(IntelData intel);
    public event IntelEvent OnIntelUnlocked;   // 新解鎖時才觸發（給通知系統用，重複解鎖不會再跳一次）

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool UnlockIntel(IntelData intel)
    {
        if (intel == null || UnlockedIntel.Contains(intel)) return false;

        UnlockedIntel.Add(intel);
        Debug.Log($"獲得情報: {intel.title}");
        OnIntelChanged?.Invoke();
        OnIntelUnlocked?.Invoke(intel);
        return true;
    }

    public bool HasIntel(IntelData intel)
    {
        return intel != null && UnlockedIntel.Contains(intel);
    }

    // ---- 存檔 ----

    public List<string> ExportSave()
    {
        var list = new List<string>();
        foreach (var intel in UnlockedIntel)
        {
            if (intel == null) continue;   // 理論上不會發生，防呆
            list.Add(intel.intelID);
        }
        return list;
    }

    // 讀檔用：用 GameDatabase 把 intelID 查回真正的 IntelData 資產參照
    public void ImportSave(List<string> ids, GameDatabase database)
    {
        UnlockedIntel.Clear();
        if (ids != null)
        {
            foreach (var id in ids)
            {
                IntelData intel = database.FindIntel(id);
                if (intel == null)
                {
                    Debug.LogWarning($"存檔裡的情報 ID「{id}」在 GameDatabase 裡找不到對應資產，這則情報會遺失");
                    continue;
                }
                UnlockedIntel.Add(intel);
            }
        }
        OnIntelChanged?.Invoke();
    }

    // 新遊戲／回標題用：清空目前解鎖的情報
    public void ResetAll()
    {
        UnlockedIntel.Clear();
        OnIntelChanged?.Invoke();
    }
}
