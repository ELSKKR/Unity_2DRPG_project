using UnityEngine;
using TMPro;
using System.Collections.Generic;

// 情報面板：清單 + 選取顯示詳情，結構直接照抄 QuestUI（陽春版沒有分類/搜尋）。
public class IntelUI : MenuPanelBase
{
    public static IntelUI Instance { get; private set; }

    [SerializeField] private Transform intelListContainer;
    [SerializeField] private GameObject intelEntryPrefab;
    [SerializeField] private GameObject emptyHintText;

    [Header("情報詳情")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private TextMeshProUGUI detailTitleText;
    [SerializeField] private TextMeshProUGUI detailBodyText;

    private int selectedIndex = -1;
    private readonly List<IntelEntryUI> entryUIs = new List<IntelEntryUI>();

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    protected override void Start()
    {
        base.Start();
        if (detailPanel != null) detailPanel.SetActive(false);

        IntelManager.Instance.OnIntelChanged += RefreshUI;
    }

    void OnDestroy()
    {
        if (IntelManager.Instance != null)
            IntelManager.Instance.OnIntelChanged -= RefreshUI;
    }

    // 開關的快捷鍵由資訊視窗的 TabGroup 統一處理（要判斷「切分頁」還是「關視窗」），這裡不再自己監聽

    protected override void OnOpened() => RefreshUI();

    protected override void OnClosed() => ClearSelection();

    void RefreshUI()
    {
        // 倒著走並先脫離父物件，沿用跟 QuestUI 一致的清單重建寫法
        for (int i = intelListContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = intelListContainer.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        entryUIs.Clear();

        var intel = IntelManager.Instance.UnlockedIntel;

        if (emptyHintText != null)
            emptyHintText.SetActive(intel.Count == 0);

        for (int i = 0; i < intel.Count; i++)
        {
            GameObject entryObj = Instantiate(intelEntryPrefab, intelListContainer);
            IntelEntryUI entryUI = entryObj.GetComponent<IntelEntryUI>();
            entryUI.Setup(intel[i], i, this);
            entryUIs.Add(entryUI);
        }

        if (selectedIndex >= intel.Count)
            selectedIndex = -1;

        UpdateHighlights();
        UpdateDetailPanel();
    }

    public void SelectIntel(int index)
    {
        if (index < 0 || index >= IntelManager.Instance.UnlockedIntel.Count) return;

        // 再次點擊同一列 -> 取消選取，收起詳情
        selectedIndex = (selectedIndex == index) ? -1 : index;

        UpdateHighlights();
        UpdateDetailPanel();
    }

    void ClearSelection()
    {
        selectedIndex = -1;
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    void UpdateHighlights()
    {
        for (int i = 0; i < entryUIs.Count; i++)
            entryUIs[i].SetHighlighted(i == selectedIndex);
    }

    void UpdateDetailPanel()
    {
        if (detailPanel == null) return;

        if (selectedIndex < 0)
        {
            detailPanel.SetActive(false);
            return;
        }

        var intel = IntelManager.Instance.UnlockedIntel[selectedIndex];
        detailPanel.SetActive(true);

        if (detailTitleText != null) detailTitleText.text = intel.title;
        if (detailBodyText != null) detailBodyText.text = intel.body;
    }
}
