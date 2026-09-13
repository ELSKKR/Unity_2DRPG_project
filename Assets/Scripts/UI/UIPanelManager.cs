using UnityEngine;

// 管理「選單類」面板之間的互斥開關：背包、任務、設定...未來新增的選單面板
// 只要繼承 MenuPanelBase，就會自動在開啟時呼叫 NotifyOpening，把上一個開著的面板關掉。
// 也統一處理 Esc 鍵：有面板開著就當「返回」關掉它，都沒開著就打開 defaultEscPanel（通常是設定選單）。
// 對話進行中禁止開啟的判斷交給 MenuPanelBase.Open() 自己處理，這裡不用重複判斷。
public class UIPanelManager : MonoBehaviour
{
    public static UIPanelManager Instance { get; private set; }

    [Header("Esc 鍵：目前沒有面板開著時，按 Esc 要打開哪個面板（通常是設定選單，留空 = 不開任何東西）")]
    [Tooltip("必須是實作 IMenuPanel 的元件（例如 SettingsUI）")]
    [SerializeField] private MonoBehaviour defaultEscPanel;

    private IMenuPanel currentOpenPanel;
    private IMenuPanel DefaultPanel => defaultEscPanel as IMenuPanel;

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
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // 正在等玩家按下新鍵時，Esc 交給 KeybindSettingsUI 當「取消」處理，不要順手把視窗關掉。
        // 問的是靜態旗標而不是指定的那一份：鍵位頁現在有兩個地方會出現（標題畫面、書本內頁）
        if (KeybindSettingsUI.AnyCapturing) return;

        if (currentOpenPanel != null)
            CloseCurrent();
        else
            DefaultPanel?.Open();
    }

    // 面板要開啟時呼叫：如果有別的面板正開著，先把它強制關掉
    public void NotifyOpening(IMenuPanel panel)
    {
        if (currentOpenPanel != null && currentOpenPanel != panel)
            currentOpenPanel.ForceClose();

        currentOpenPanel = panel;
    }

    // 面板自己關閉時呼叫，清掉目前開啟中的記錄
    public void NotifyClosed(IMenuPanel panel)
    {
        if (currentOpenPanel == panel)
            currentOpenPanel = null;
    }

    // 關掉目前開著的面板（給 Esc 這種「返回」用）
    public void CloseCurrent()
    {
        if (currentOpenPanel == null) return;

        currentOpenPanel.ForceClose();
        currentOpenPanel = null;
    }

    public bool IsAnyPanelOpen => currentOpenPanel != null;
}
