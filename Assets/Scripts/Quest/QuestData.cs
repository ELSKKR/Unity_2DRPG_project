using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "JRPG/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("基本資訊")]
    public string questID;
    public string questName;
    [TextArea(2, 4)]
    public string description;

    [Header("任務設定")]
    public QuestType type = QuestType.Side;

    [Header("任務階段（依序填寫，玩家目前在哪一階段就顯示哪一句目標）")]
    [TextArea(1, 2)]
    public string[] stageObjectives;
}
