using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public List<Quest> Quests { get; private set; } = new List<Quest>();

    public delegate void QuestsChanged();
    public event QuestsChanged OnQuestsChanged;

    public delegate void QuestEvent(QuestData quest);
    public event QuestEvent OnQuestAccepted;   // 接到新任務時觸發（給通知系統用）
    public event QuestEvent OnQuestCompleted;  // 完成任務時觸發（給通知系統用）

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool AcceptQuest(QuestData data)
    {
        if (data == null) return false;

        if (Quests.Exists(q => q.data == data))
        {
            Debug.Log($"任務已接取: {data.questName}");
            return false;
        }

        Quests.Add(new Quest(data, QuestStatus.InProgress));
        Debug.Log($"接取新任務: {data.questName}");
        OnQuestsChanged?.Invoke();
        OnQuestAccepted?.Invoke(data);
        return true;
    }

    public bool CompleteQuest(QuestData data)
    {
        Quest quest = Quests.Find(q => q.data == data);
        if (quest == null)
        {
            Debug.Log($"尚未接取此任務，無法完成: {(data != null ? data.questName : "null")}");
            return false;
        }

        if (quest.status == QuestStatus.Completed)
            return true;

        quest.status = QuestStatus.Completed;

        // 完成時自動跳到最後一個階段，任務日誌就會顯示收尾的目標文字
        if (data.stageObjectives != null && data.stageObjectives.Length > 0)
            quest.currentStage = data.stageObjectives.Length - 1;

        Debug.Log($"任務完成: {data.questName}");
        OnQuestsChanged?.Invoke();
        OnQuestCompleted?.Invoke(data);
        return true;
    }

    // 推進 / 設定任務階段（決定任務日誌顯示哪一句目標文字）
    // 階段沒變時直接跳過，避免重複觸發 OnQuestsChanged 造成 UI 反覆重建
    public void SetStage(QuestData data, int stage)
    {
        Quest quest = Quests.Find(q => q.data == data);
        if (quest == null || quest.currentStage == stage) return;

        // 索引設錯不會真的壞掉（Quest.CurrentObjective 會安全 Clamp），
        // 但會安靜地顯示錯的目標文字，不提醒的話很難發現是設定檔的問題
        if (data.stageObjectives != null && (stage < 0 || stage >= data.stageObjectives.Length))
        {
            Debug.LogWarning($"任務「{data.questName}」的階段索引 {stage} 超出 stageObjectives 範圍" +
                              $"（共 {data.stageObjectives.Length} 個階段），畫面會顯示被夾住範圍的階段文字。");
        }

        quest.currentStage = stage;
        OnQuestsChanged?.Invoke();
    }

    public bool IsQuestAccepted(QuestData data)
    {
        return Quests.Exists(q => q.data == data);
    }

    public bool IsQuestCompleted(QuestData data)
    {
        Quest quest = Quests.Find(q => q.data == data);
        return quest != null && quest.status == QuestStatus.Completed;
    }

    // ---- 存檔 ----

    public List<QuestSaveEntry> ExportSave()
    {
        var list = new List<QuestSaveEntry>();
        foreach (var q in Quests)
        {
            if (q.data == null) continue;   // 理論上不會發生，防呆
            list.Add(new QuestSaveEntry { questID = q.data.questID, status = (int)q.status, currentStage = q.currentStage });
        }
        return list;
    }

    // 讀檔用：用 GameDatabase 把 questID 查回真正的 QuestData 資產參照
    public void ImportSave(List<QuestSaveEntry> entries, GameDatabase database)
    {
        Quests.Clear();
        if (entries != null)
        {
            foreach (var entry in entries)
            {
                QuestData data = database.FindQuest(entry.questID);
                if (data == null)
                {
                    Debug.LogWarning($"存檔裡的任務 ID「{entry.questID}」在 GameDatabase 裡找不到對應資產，這條任務進度會遺失");
                    continue;
                }
                var quest = new Quest(data, (QuestStatus)entry.status);
                quest.currentStage = entry.currentStage;
                Quests.Add(quest);
            }
        }
        OnQuestsChanged?.Invoke();
    }

    // 新遊戲／回標題用：清空目前的任務進度
    public void ResetAll()
    {
        Quests.Clear();
        OnQuestsChanged?.Invoke();
    }
}
