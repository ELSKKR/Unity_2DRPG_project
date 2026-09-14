using UnityEngine;

// 所有「選單類」面板（背包 / 任務 / 設定...）的共用邏輯：
// 跟 UIPanelManager 互斥註冊、鎖/解鎖玩家移動、播放開關音效、快取 PlayerController、
// 對話進行中禁止開啟。子類別只需要處理自己的顯示內容（清單重建、選取狀態、滑桿同步等），
// 覆寫 OnOpened / OnClosed 掛進開關流程即可，不用再各自重寫一份 Open/Close/ForceClose。
public abstract class MenuPanelBase : MonoBehaviour, IMenuPanel
{
    [Header("面板本體")]
    [SerializeField] private GameObject panelRoot;

    [Header("音效（留空 = 不播放）")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    public bool IsOpen { get; private set; }
    protected PlayerController Player { get; private set; }

    protected virtual void Awake()
    {
        Player = FindFirstObjectByType<PlayerController>();
    }

    protected virtual void Start()
    {
        SetPanelVisible(false);
    }

    public void Open()
    {
        if (IsOpen) return;

        // 對話進行中不能開選單面板：兩邊都會各自鎖/解玩家移動，疊在一起會互相打架
        // （例如面板先關掉把移動解鎖了，但對話其實還沒結束）
        if (DialogManager.Instance != null && DialogManager.Instance.IsDialogActive) return;

        UIPanelManager.Instance?.NotifyOpening(this);

        IsOpen = true;
        SetPanelVisible(true);

        OnOpened();

        if (openSound != null)
            AudioManager.Instance.PlaySFX(openSound);

        Player?.SetCanMove(false);
    }

    // 玩家自己按鍵關閉：要通知 UIPanelManager 目前沒有面板開著了
    public void Close()
    {
        if (!IsOpen) return;

        CloseInternal();
        UIPanelManager.Instance?.NotifyClosed(this);
    }

    // 被 UIPanelManager 要求強制關閉（開了別的面板、或 Esc 返回）：不用再通知它，因為就是它呼叫的
    public void ForceClose()
    {
        if (!IsOpen) return;
        CloseInternal();
    }

    void CloseInternal()
    {
        IsOpen = false;
        SetPanelVisible(false);

        OnClosed();

        if (closeSound != null)
            AudioManager.Instance.PlaySFX(closeSound);

        Player?.SetCanMove(true);
    }

    // 面板實際顯示／隱藏的動作。預設就是直接切 active，跟以前一樣。
    //
    // 之所以拉成獨立的虛擬方法，是為了讓「有開關動畫」的面板（書本 UI）有地方介入：
    // 關閉流程走的是 CloseInternal → 先隱藏再 OnClosed，等到 OnClosed 時物件已經 inactive、
    // 協程根本跑不起來，動畫沒有機會播。覆寫這裡就能自己決定「什麼時候才真的隱藏」。
    protected virtual void SetPanelVisible(bool visible)
    {
        if (panelRoot != null) panelRoot.SetActive(visible);
    }

    // 面板打開/關閉時要做的額外事情（重建清單、清掉選取狀態、同步滑桿、存設定...）
    protected virtual void OnOpened() { }
    protected virtual void OnClosed() { }
}
