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

        [Tooltip("勾選才會吃快捷鍵（子分頁通常不用）")]
        public bool hasHotkey;
        public KeyBindings.GameAction hotkey;

        public IMenuPanel Panel => menuPanel as IMenuPanel;
    }

    [SerializeField] private GameObject tabBarRoot;   // 分頁列本體；視窗模式下沒有頁開著時要隱藏
    [SerializeField] private Tab[] tabs;

    [Tooltip("勾選 = 永遠有一頁被選中（面板內的子分頁）。取消 = 可以全部關閉（獨立視窗）")]
    [SerializeField] private bool alwaysHasSelection;

    [Header("分頁文字顏色")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.45f);

    [Header("音效（留空 = 不播放）")]
    [SerializeField] private AudioClip selectSound;

    private int activeIndex = -1;

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
        if (alwaysHasSelection) Select(0, playSound: false);
        else CloseAll();
    }

    void Update()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            if (!tabs[i].hasHotkey) continue;
            if (!KeyBindings.GetKeyDown(tabs[i].hotkey)) continue;

            // 已經停在這一頁 → 再按一次是關閉；否則就是開啟／切過來（都是玩家操作，要出聲）
            if (activeIndex == i) CloseAll(playSound: true);
            else Select(i, playSound: true);
            return;
        }

        // 面板可能被 Esc（UIPanelManager）從外面關掉，這裡要同步分頁列的狀態
        if (!alwaysHasSelection && activeIndex >= 0)
        {
            var panel = tabs[activeIndex].Panel;
            if (panel != null && !panel.IsOpen) CloseAll();
        }
    }

    public void Select(int index) => Select(index, true);

    public void Select(int index, bool playSound)
    {
        if (index < 0 || index >= tabs.Length) return;

        if (playSound && selectSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(selectSound);

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

        activeIndex = index;
        if (tabBarRoot != null) tabBarRoot.SetActive(true);
        UpdateVisuals();
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

    void UpdateVisuals()
    {
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
