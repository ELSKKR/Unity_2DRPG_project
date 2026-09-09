// 所有「選單類」面板（背包、任務、設定...未來的地圖/技能樹...）都實作這個介面，
// 才能被 UIPanelManager 統一管理互斥開關、以及 Esc 鍵的開啟/返回行為。
public interface IMenuPanel
{
    // 目前是不是開著。分頁列要靠這個同步狀態（面板可能被 Esc 從外面關掉）
    bool IsOpen { get; }

    // 主動開啟（例如按對應的快捷鍵、或 Esc 開啟預設面板時）
    void Open();

    // 被 UIPanelManager 要求強制關閉時呼叫（因為玩家開了別的選單面板，或按 Esc 返回）。
    // 跟玩家自己按鍵關閉不同：這裡不用再通知 UIPanelManager，因為就是它呼叫的。
    void ForceClose();
}
