using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class QuestUI : MenuPanelBase
{
    public static QuestUI Instance { get; private set; }

    [SerializeField] private Transform questListContainer;
    [SerializeField] private GameObject questEntryPrefab;
    [SerializeField] private GameObject emptyHintText;

    [Header("任務詳情")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private TextMeshProUGUI detailNameText;
    [SerializeField] private TextMeshProUGUI detailObjectiveText;
    [SerializeField] private TextMeshProUGUI detailDescriptionText;

    private int selectedIndex = -1;
    private List<QuestEntryUI> entryUIs = new List<QuestEntryUI>();

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    protected override void Start()
    {
        base.Start();
        if (detailPanel != null) detailPanel.SetActive(false);

        QuestManager.Instance.OnQuestsChanged += RefreshUI;
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestsChanged -= RefreshUI;
    }

    // 開關的快捷鍵由資訊視窗的 TabGroup 統一處理（要判斷「切分頁」還是「關視窗」），這裡不再自己監聽

    protected override void OnOpened() => RefreshUI();

    protected override void OnClosed() => ClearSelection();

    void RefreshUI()
    {
        // 倒著走並先脫離父物件：Destroy 要到這一幀結束才生效，
        // 同一幀內若再次 Refresh，舊的項目會被算進 childCount 而重複出現
        for (int i = questListContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = questListContainer.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        entryUIs.Clear();

        var quests = QuestManager.Instance.Quests;

        if (emptyHintText != null)
            emptyHintText.SetActive(quests.Count == 0);

        for (int i = 0; i < quests.Count; i++)
        {
            GameObject entryObj = Instantiate(questEntryPrefab, questListContainer);
            QuestEntryUI entryUI = entryObj.GetComponent<QuestEntryUI>();
            entryUI.Setup(quests[i], i, this);
            entryUIs.Add(entryUI);
        }

        // 任務被移除或清單重排時，原本選取的索引可能已經失效
        if (selectedIndex >= quests.Count)
            selectedIndex = -1;

        UpdateHighlights();
        UpdateDetailPanel();
    }

    public void SelectQuest(int index)
    {
        if (index < 0 || index >= QuestManager.Instance.Quests.Count) return;

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

        var quest = QuestManager.Instance.Quests[selectedIndex];
        detailPanel.SetActive(true);

        if (detailNameText != null) detailNameText.text = quest.data.questName;
        if (detailObjectiveText != null) detailObjectiveText.text = quest.CurrentObjective;
        if (detailDescriptionText != null) detailDescriptionText.text = quest.data.description;
    }
}
