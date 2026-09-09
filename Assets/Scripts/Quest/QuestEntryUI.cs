using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class QuestEntryUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI objectiveText;   // 目前階段的目標小字
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject highlight;            // 選取時亮起（比照背包格子）

    private static readonly Color CompletedColor = new Color(0.45f, 0.85f, 0.45f); // 綠色
    private static readonly Color InProgressColor = new Color(1f, 0.82f, 0.3f);    // 黃色

    private int questIndex;
    private QuestUI questUI;

    public void Setup(Quest quest, int index, QuestUI ui)
    {
        questIndex = index;
        questUI = ui;

        nameText.text = quest.data.questName;
        typeText.text = quest.data.type == QuestType.Main ? "主線" : "支線";

        bool completed = quest.status == QuestStatus.Completed;
        statusText.text = completed ? "已完成" : "進行中";
        statusText.color = completed ? CompletedColor : InProgressColor;

        if (objectiveText != null)
            objectiveText.text = quest.CurrentObjective;

        SetHighlighted(false);
    }

    public void SetHighlighted(bool state)
    {
        if (highlight != null)
            highlight.SetActive(state);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        questUI?.SelectQuest(questIndex);
    }
}
