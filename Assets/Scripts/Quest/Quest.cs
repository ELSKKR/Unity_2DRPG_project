using UnityEngine;

[System.Serializable]
public class Quest
{
    public QuestData data;
    public QuestStatus status;
    public int currentStage;

    public Quest(QuestData data, QuestStatus status = QuestStatus.InProgress)
    {
        this.data = data;
        this.status = status;
        this.currentStage = 0;
    }

    // 目前階段要顯示的目標文字（QuestData 沒填階段時回傳空字串）
    public string CurrentObjective
    {
        get
        {
            if (data == null || data.stageObjectives == null || data.stageObjectives.Length == 0)
                return "";

            int index = Mathf.Clamp(currentStage, 0, data.stageObjectives.Length - 1);
            return data.stageObjectives[index];
        }
    }
}
