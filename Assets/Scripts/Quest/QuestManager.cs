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

    // 追蹤中的任務：任務頁詳情按鈕、畫面角落的 QuestTrackerHUD 都在用，
    // 兩邊都是自己訂閱 OnQuestsChanged 即時刷新，這裡不需要專屬事件。
    public string TrackedQuestID { get; private set; }

    public bool IsTracked(QuestData data) => data != null && TrackedQuestID == data.questID;

    public void ToggleTracked(QuestData data)
    {
        if (data == null) return;
        TrackedQuestID = IsTracked(data) ? null : data.questID;

        // QuestUI 自己在按下追蹤鈕後會直接呼叫 UpdateDetailPanel()，不靠這個事件
        // （避免只是切換追蹤就整批重建清單，見 QuestUI.OnTrackButtonClicked 的註解）。
        // 但這裡還是要發事件，不然 QuestTrackerHUD 這種只靠事件刷新畫面的訂閱者，
        // 追蹤狀態一變就沒人通知，字幕會卡在切換前的內容
        OnQuestsChanged?.Invoke();
    }

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

        // 正在追蹤的任務完成了：追蹤目標已經沒意義，清掉。
        // 純手動追蹤，不會自動接去追別的任務——角落 HUD 會落回「尚未追蹤」的提示
        if (IsTracked(data)) TrackedQuestID = null;

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

    // 讀檔用：還原追蹤中的任務。要在 ImportSave(quests) 之後呼叫，
    // 否則 Quests 清單還是空的，會被下面的檢查當成無效直接丟掉。
    // 存檔裡的 ID 已經不在進行中任務清單裡（任務被移除、或資料被改過）就當沒追蹤，
    // 不要讓 HUD 掛著一個查無此任務的追蹤目標
    public void ImportTrackedQuestID(string questID)
    {
        TrackedQuestID = !string.IsNullOrEmpty(questID) && Quests.Exists(q => q.data.questID == questID)
            ? questID
            : null;
    }

    // 新遊戲／回標題用：清空目前的任務進度
    public void ResetAll()
    {
        Quests.Clear();
        TrackedQuestID = null;
        OnQuestsChanged?.Invoke();
    }
}
