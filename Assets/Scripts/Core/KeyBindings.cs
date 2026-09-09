using System;
using System.Collections.Generic;
using UnityEngine;

// 鍵位設定的唯一入口。所有「玩家之後可能會想自訂」的按鍵都要透過這裡查詢
// （KeyBindings.GetKeyDown(...)），不要在各自的腳本裡直接寫死 Input.GetKeyDown(KeyCode.X)。
//
// 目前還沒有改鍵位的設定畫面，這層只是先把「查詢」跟「儲存」的介面做好——
// 之後要做鍵位設定 UI，只要呼叫 Rebind() 就好，不用回頭改任何用到按鍵的腳本。
//
// 純靜態類別，不掛在場景上：鍵位是玩家裝置層級的偏好設定（跟 AudioManager 的音量
// 存取邏輯同一個道理），不屬於任何一個存檔，用 PlayerPrefs 直接存，跟遊戲進度無關。
//
// 不包在這裡的按鍵：
// - 移動（WASD/方向鍵）是透過 Unity Input Manager 的 Horizontal/Vertical 軸讀的，
//   不是逐鍵判斷，要改要去 Project Settings > Input Manager 調，這層管不到。
// - Esc（選單返回/開啟設定）跟 PlayerController 裡 G/T 這兩個「之後會刪掉的測試鍵」
//   刻意不做成可改——Esc 是幾乎所有遊戲都通用、不會給玩家改的系統鍵，G/T 只是開發階段暫用。
public static class KeyBindings
{
    public enum GameAction
    {
        MoveUp,           // 移動四向（方向鍵永遠可用，這四個是額外的可改按鍵）
        MoveDown,
        MoveLeft,
        MoveRight,
        Interact,         // 世界互動 + 對話推進／確認選項共用同一顆鍵
        Sprint,
        ToggleInventory,
        ToggleQuestLog,
        ToggleIntelLog,
    }

    static readonly Dictionary<GameAction, KeyCode> Defaults = new Dictionary<GameAction, KeyCode>
    {
        { GameAction.MoveUp, KeyCode.W },
        { GameAction.MoveDown, KeyCode.S },
        { GameAction.MoveLeft, KeyCode.A },
        { GameAction.MoveRight, KeyCode.D },
        { GameAction.Interact, KeyCode.F },
        { GameAction.Sprint, KeyCode.LeftShift },
        { GameAction.ToggleInventory, KeyCode.Tab },
        { GameAction.ToggleQuestLog, KeyCode.Q },
        { GameAction.ToggleIntelLog, KeyCode.I },
    };

    // 顯示在鍵位設定畫面上的名稱（也決定了列出來的順序）
    public static readonly (GameAction action, string label)[] RebindableActions =
    {
        (GameAction.MoveUp, "向上移動"),
        (GameAction.MoveDown, "向下移動"),
        (GameAction.MoveLeft, "向左移動"),
        (GameAction.MoveRight, "向右移動"),
        (GameAction.Sprint, "衝刺"),
        (GameAction.Interact, "互動／對話"),
        (GameAction.ToggleInventory, "背包"),
        (GameAction.ToggleQuestLog, "任務"),
        (GameAction.ToggleIntelLog, "情報"),
    };

    static readonly Dictionary<GameAction, KeyCode> current = new Dictionary<GameAction, KeyCode>();
    static bool loaded;

    const string PrefPrefix = "KeyBind_";

    static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;

        foreach (var kv in Defaults)
        {
            string saved = PlayerPrefs.GetString(PrefPrefix + kv.Key, "");
            if (!string.IsNullOrEmpty(saved) && Enum.TryParse(saved, out KeyCode savedKey))
                current[kv.Key] = savedKey;
            else
                current[kv.Key] = kv.Value;
        }
    }

    public static KeyCode Get(GameAction action)
    {
        EnsureLoaded();
        return current[action];
    }

    public static KeyCode GetDefault(GameAction action) => Defaults[action];

    // 改鍵位並存檔。如果這顆鍵已經被別的動作佔用，就把兩個動作的按鍵對調——
    // 這樣永遠不會出現「某個動作沒有鍵可按」的狀態，也不用跟玩家解釋錯誤
    // 回傳被對調掉的那個動作（沒有衝突就回傳 null），方便 UI 提示玩家
    public static GameAction? Rebind(GameAction action, KeyCode newKey)
    {
        EnsureLoaded();

        KeyCode oldKey = current[action];
        if (oldKey == newKey) return null;

        GameAction? swapped = null;
        foreach (var kv in current)
        {
            if (kv.Key == action || kv.Value != newKey) continue;
            swapped = kv.Key;
            break;
        }

        current[action] = newKey;
        Save(action);

        if (swapped.HasValue)
        {
            current[swapped.Value] = oldKey;
            Save(swapped.Value);
        }

        PlayerPrefs.Save();
        return swapped;
    }

    static void Save(GameAction action) => PlayerPrefs.SetString(PrefPrefix + action, current[action].ToString());

    public static void ResetToDefault(GameAction action) => Rebind(action, Defaults[action]);

    // 鍵位設定畫面的「還原預設值」：全部一次還原，避免逐個 Rebind 過程中互相對調弄亂
    public static void ResetAllToDefault()
    {
        EnsureLoaded();
        foreach (var kv in Defaults)
        {
            current[kv.Key] = kv.Value;
            Save(kv.Key);
        }
        PlayerPrefs.Save();
    }

    // 直接取代 Input.GetKeyDown(KeyCode.X) / Input.GetKey(KeyCode.X) 的寫法
    public static bool GetKeyDown(GameAction action) => Input.GetKeyDown(Get(action));
    public static bool GetKey(GameAction action) => Input.GetKey(Get(action));
}
