using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 共用的分頁列。兩種用法：
//
// 1) 視窗層級（資訊視窗：背包／任務／情報）——每個分頁是一個獨立的 MenuPanelBase 面板，
//    切換 = 關掉舊的、開啟新的（交給 UIPanelManager 的互斥機制處理）。
//    可以綁快捷鍵：沒開時按 = 開啟並停在該頁；已經停在該頁再按 = 關閉；停在別頁按 = 切過去。
//    整個視窗關閉時分頁列要跟著隱藏，所以 alwaysHasSelection = false。
//
// 2) 面板內的子分頁（設定：音量／鍵位）——每個分頁只是一個 GameObject，SetActive 切換，
//    而且永遠會有一頁被選中（alwaysHasSelection = true）。
public class TabGroup : MonoBehaviour
{
    [System.Serializable]
    public class Tab
    {
        public string label;
        public Button button;
        public GameObject highlight;      // 選中時亮起（沒有就用文字顏色代替）

        [Tooltip("子分頁模式：要 SetActive 切換的頁面")]
        public GameObject page;

        [Tooltip("視窗模式：改用這個面板的 Open/ForceClose（必須實作 IMenuPanel）")]
        public MonoBehaviour menuPanel;

        [Tooltip("書本 UI：這一頁被選中時，書籤（緞帶）上要顯示的圖示")]
        public Sprite ribbonIcon;

        [Tooltip("勾選才會吃快捷鍵（子分頁通常不用）")]
        public bool hasHotkey;
        public KeyBindings.GameAction hotkey;

        public IMenuPanel Panel => menuPanel as IMenuPanel;
    }

    [SerializeField] private GameObject tabBarRoot;   // 分頁列本體；視窗模式下沒有頁開著時要隱藏
    [SerializeField] private Tab[] tabs;

    [Tooltip("勾選 = 永遠有一頁被選中（面板內的子分頁）。取消 = 可以全部關閉（獨立視窗）")]
    [SerializeField] private bool alwaysHasSelection;

    [Tooltip("alwaysHasSelection 時一開始停在第幾頁。分頁的排列順序是美術決定的（決定翻頁方向），不見得第一頁就是預設頁")]
    [SerializeField] private int defaultIndex;

    [Header("分頁文字顏色")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.45f);

    [Header("音效（留空 = 不播放）")]
    [SerializeField] private AudioClip selectSound;

    [Header("書本 UI（留空 = 直接切頁，不播翻頁動畫）")]
    [Tooltip("指定之後，切分頁會先翻一頁過去，翻到一半才換頁面內容")]
    [SerializeField] private BookMenuPanel book;
    [Tooltip("書籤（緞帶）上的圖示，會換成目前這一頁的 ribbonIcon")]
    [SerializeField] private Image ribbonIcon;

    [Header("標題畫面模式（只有書本那一份要勾）")]
    [Tooltip("勾選 = 在標題畫面把書打開時，藏掉所有側標籤、停用分頁快捷鍵，並固定停在 titleTabIndex 那一頁。" +
             "不管是設定按鈕、Esc 還是快捷鍵開的書都一視同仁，所以判斷放在這裡而不是開書的那一邊")]
    [SerializeField] private bool tabsLockedAtTitle;
    [SerializeField] private int titleTabIndex;
    [SerializeField] private string titleSceneName = "TitleScreen";

    private int activeIndex = -1;
    private bool titleModeActive;

    public bool IsOpen => activeIndex >= 0;

    void Start()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;   // 閉包要抓副本，不然全部都會抓到最後一個 i
            if (tabs[i].button != null)
                tabs[i].button.onClick.AddListener(() => Select(index));
        }

        // 初始化時不出聲：玩家還沒做任何操作
        if (alwaysHasSelection) Select(defaultIndex, playSound: false);
        else CloseAll();
    }

    void Update()
    {
        if (tabsLockedAtTitle && book != null) UpdateTitleMode();

        for (int i = 0; i < tabs.Length; i++)
        {
            if (titleModeActive) break;   // 標題畫面開的書只能看設定頁，不能靠快捷鍵翻到背包／任務
            if (!tabs[i].hasHotkey) continue;
            if (!KeyBindings.GetKeyDown(tabs[i].hotkey)) continue;

            ToggleTab(i);
            return;
        }

        // 面板可能被 Esc（UIPanelManager）從外面關掉，這裡要同步分頁列的狀態
        if (!alwaysHasSelection && activeIndex >= 0)
        {
            var panel = tabs[activeIndex].Panel;
            if (panel != null && !panel.IsOpen) CloseAll();
        }
    }

    // 「書開著」而且「人在標題畫面」才進標題模式；書關了或離開標題就還原。
    // 還原一定要有，因為書跟這個 TabGroup 都住在常駐場景，標題畫面卸載後它們還在，
    // 沒還原的話進遊戲後側標籤就永遠消失了
    void UpdateTitleMode()
    {
        bool atTitle = SceneTransitionManager.Instance != null
            && SceneTransitionManager.Instance.CurrentGameplayScene == titleSceneName;

        if (!titleModeActive)
        {
            if (book.IsOpen && atTitle) EnterTitleMode();
        }
        else if (!book.IsOpen || !atTitle)
        {
            ExitTitleMode();
        }
    }

    void EnterTitleMode()
    {
        titleModeActive = true;
        SetTabButtonsActive(false);

        // 直接換頁不翻頁：這時候多半還在開書動畫中，頁面內容本來就是藏著的
        if (activeIndex != titleTabIndex)
        {
            activeIndex = titleTabIndex;
            UpdateVisuals();
            ApplyPages(titleTabIndex);
        }
    }

    void ExitTitleMode()
    {
        titleModeActive = false;
        SetTabButtonsActive(true);
    }

    void SetTabButtonsActive(bool active)
    {
        foreach (var t in tabs)
            if (t.button != null) t.button.gameObject.SetActive(active);
    }

    // 按下某一頁的快捷鍵時該發生什麼事。拉成獨立的 public 方法有兩個原因：
    // 一是 Update 裡那段狀態判斷沒辦法從外面測（不能模擬按鍵），二是之後若要做
    // 手把或選單導航，會需要同一個入口。
    public void ToggleTab(int index)
    {
        // 書本模式：分頁永遠有一頁選著，所以「關閉」關的是整本書，不是取消選取。
        // 順序是先選頁再開書——書還沒開的時候 FlipPage 不會播動畫，內容會直接換好，
        // 這樣開書動畫播完攤開時看到的就已經是正確那一頁
        if (book != null)
        {
            if (!book.IsOpen) { Select(index, playSound: true); book.Open(); }
            else if (activeIndex == index) book.Close();
            else Select(index, playSound: true);
            return;
        }

        // 已經停在這一頁 → 再按一次是關閉；否則就是開啟／切過來（都是玩家操作，要出聲）
        if (activeIndex == index) CloseAll(playSound: true);
        else Select(index, playSound: true);
    }

    public void Select(int index) => Select(index, true);

    public void Select(int index, bool playSound)
    {
        if (index < 0 || index >= tabs.Length) return;

        if (playSound && selectSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(selectSound);

        int previous = activeIndex;

        // 標籤本身要立刻亮起來。等翻頁動畫播完才變的話，點下去有將近 0.2 秒沒反應，
        // 玩家會以為沒點到而再點一次
        activeIndex = index;
        if (tabBarRoot != null) tabBarRoot.SetActive(true);
        UpdateVisuals();

        // 書本 UI：頁面內容要等紙張翻到一半、把新舊兩頁都擋住時才換。
        // 第一次選取（previous < 0）是開視窗，不算翻頁
        if (book != null && previous >= 0 && previous != index)
            book.FlipPage(index > previous, () => ApplyPages(index));
        else
            ApplyPages(index);
    }

    void ApplyPages(int index)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            bool on = i == index;

            if (tabs[i].page != null) tabs[i].page.SetActive(on);

            var panel = tabs[i].Panel;
            if (panel != null)
            {
                // Open 內部會通知 UIPanelManager 把其他面板關掉，不用自己處理互斥
                if (on && !panel.IsOpen) panel.Open();
                else if (!on && panel.IsOpen) panel.ForceClose();
            }
        }
    }

    public void CloseAll() => CloseAll(false);

    public void CloseAll(bool playSound)
    {
        if (alwaysHasSelection) return;

        if (playSound && selectSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(selectSound);

        foreach (var t in tabs)
        {
            if (t.page != null) t.page.SetActive(false);
            var panel = t.Panel;
            if (panel != null && panel.IsOpen) panel.ForceClose();
        }

        activeIndex = -1;
        if (tabBarRoot != null) tabBarRoot.SetActive(false);
    }

    // 給標籤出現動畫用：它播完之後要請這裡重畫一次，
    // 「哪一格該亮」的判斷只有這裡有，動畫自己猜會跟這裡不同步
    public void RefreshVisuals() => UpdateVisuals();

    void UpdateVisuals()
    {
        // 書籤上的圖示跟著目前這一頁換。沒有任何一頁被選中時（視窗關著）就不畫，
        // 免得留著上一次的圖示，下次開書時閃一下才更新
        if (ribbonIcon != null)
        {
            Sprite s = activeIndex >= 0 ? tabs[activeIndex].ribbonIcon : null;
            ribbonIcon.sprite = s;
            ribbonIcon.enabled = s != null;
        }

        for (int i = 0; i < tabs.Length; i++)
        {
            bool on = i == activeIndex;
            if (tabs[i].highlight != null) tabs[i].highlight.SetActive(on);

            if (tabs[i].button != null)
            {
                var label = tabs[i].button.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.color = on ? activeColor : inactiveColor;
            }
        }
    }
}
