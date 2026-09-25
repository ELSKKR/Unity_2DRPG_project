using System.Linq;
using UnityEngine;

public class NPCDialog : MonoBehaviour, IInteractable
{
    [Header("對話與任務資料（單一檔案，包含所有階段）")]
    [SerializeField] private NPCConversationData conversation;

    [Header("頭頂任務提示圖示")]
    [SerializeField] private QuestMarker questMarker;
    [SerializeField] private string markerSymbol = "!";
    [SerializeField] private Color canAcceptColor = new Color(1f, 0.85f, 0.2f);   // 黃：有新任務可接
    [SerializeField] private Color canHandInColor = new Color(1f, 0.55f, 0.1f);   // 橘：任務可以回報了

    [SerializeField] private string interactionText = "對話";

    public string InteractionPrompt => interactionText;
    public Vector3 PromptWorldPosition => transform.position;
    public bool CanInteract => true;

    void Start()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestsChanged += RefreshState;

        // 撿到道具、完成別的任務都可能影響解鎖狀態，不能等玩家再來對話才更新
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged += RefreshState;

        if (WorldStateManager.Instance != null)
            WorldStateManager.Instance.OnWorldStateChanged += RefreshState;

        // 沒有任務的 NPC 靠「還有沒有沒拿到的情報」決定要不要亮標記，情報變動也要重算
        if (IntelManager.Instance != null)
            IntelManager.Instance.OnIntelChanged += RefreshState;

        RefreshState();
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestsChanged -= RefreshState;

        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= RefreshState;

        if (WorldStateManager.Instance != null)
            WorldStateManager.Instance.OnWorldStateChanged -= RefreshState;

        if (IntelManager.Instance != null)
            IntelManager.Instance.OnIntelChanged -= RefreshState;
    }

    public void Interact()
    {
        NPCQuestStage stage = GetActiveStage();

        if (stage == null)
        {
            PlaySimple(conversation != null ? conversation.GetActiveChat() : null);
            return;
        }

        switch (EvaluateStageState(stage))
        {
            case NPCQuestState.CanAccept:
                PlayWithChoice(stage.intro, choiceIndex => TryAcceptQuest(stage, choiceIndex));
                break;

            case NPCQuestState.CanHandIn:
                if (stage.completesQuestOnHandIn)
                    PlayWithChoice(stage.handIn, choiceIndex => TryCompleteHandIn(stage, choiceIndex));
                else
                    PlaySimple(stage.handIn);   // 只是提示玩家該去找誰，不完成任務
                break;

            case NPCQuestState.InProgress:
                PlaySimple(stage.reminder);
                break;

            case NPCQuestState.Completed:
                PlaySimple(stage.completed);
                break;
        }
    }

    // 委託對話有選項時，只有被標記 acceptsQuest 的選項才會接下任務；
    // 沒有選項的委託對話則是「播完就接」
    void TryAcceptQuest(NPCQuestStage stage, int choiceIndex)
    {
        var choices = stage.intro != null ? stage.intro.choices : null;

        if (choices == null || choices.Length == 0)
        {
            QuestManager.Instance.AcceptQuest(stage.quest);
            return;
        }

        if (choiceIndex >= 0 && choiceIndex < choices.Length && choices[choiceIndex].acceptsQuest)
            QuestManager.Instance.AcceptQuest(stage.quest);
    }

    // 交任務對話有選項時，只有被標記 completesHandIn 的選項才會真的完成任務
    // （例如「把東西還給他」vs「先不說」）；沒有選項的交任務對話則是「播完就完成」
    void TryCompleteHandIn(NPCQuestStage stage, int choiceIndex)
    {
        var choices = stage.handIn != null ? stage.handIn.choices : null;

        if (choices == null || choices.Length == 0)
        {
            CompleteStage(stage);
            return;
        }

        if (choiceIndex >= 0 && choiceIndex < choices.Length && choices[choiceIndex].completesHandIn)
            CompleteStage(stage);
    }

    void CompleteStage(NPCQuestStage stage)
    {
        // 先完成任務再扣道具：反過來的話扣完道具會先被判定成「條件未達成」，
        // 任務階段會閃一下退回上一段，UI 也會多重建一次
        QuestManager.Instance.CompleteQuest(stage.quest);

        if (stage.turnInItems != null && stage.turnInItems.Length > 0)
        {
            if (stage.consumeTurnInItem)
                foreach (var req in stage.turnInItems)
                    Inventory.Instance.RemoveItem(req.item, req.amount);
        }
        else if (stage.turnInItem != null && stage.consumeTurnInItem)
        {
            Inventory.Instance.RemoveItem(stage.turnInItem, 1);
        }

        if (stage.rewardItem != null)
            Inventory.Instance.AddItem(stage.rewardItem, stage.rewardItemCount);

        if (!string.IsNullOrEmpty(stage.unlockEventID))
            WorldStateManager.Instance?.MarkEvent(stage.unlockEventID);

        TryChainNextStage(stage);
    }

    // 交完任務的當下，下一階段常常立刻就能接（例如賽勒收下配方後馬上要委託採集）。
    // 一次互動只播一段的話，玩家得再對話一次才拿得到任務，看起來像 NPC 忘了說，
    // 所以這裡把下一段委託直接接著播完，整串變成一次連續對話。
    // 之所以可以在這裡重開一段對話：DialogManager.EndDialog() 是先把面板關掉、
    // 所有狀態清空之後才 invoke callback，而 CompleteStage 正是從那個 callback 進來的。
    void TryChainNextStage(NPCQuestStage justCompleted)
    {
        NPCQuestStage next = GetActiveStage();

        // 沒有下一段、或條件還沒滿足時 GetActiveStage 會停在剛完成的這一段
        if (next == null || next == justCompleted) return;

        // 只接「可以接取新任務」的情況；其他狀態留給玩家下次主動對話
        if (EvaluateStageState(next) != NPCQuestState.CanAccept) return;

        PlayWithChoice(next.intro, choiceIndex => TryAcceptQuest(next, choiceIndex));
    }

    // 找出「目前該互動的階段」：依序往後走，直到卡在一個還沒完成、
    // 或下一階段的解鎖條件還不滿足的地方
    NPCQuestStage GetActiveStage()
    {
        if (conversation == null || conversation.stages == null || conversation.stages.Length == 0)
            return null;
        if (QuestManager.Instance == null)
            return null;

        NPCQuestStage active = null;

        foreach (var stage in conversation.stages)
        {
            if (stage == null || stage.quest == null) break;

            if (active != null && !QuestManager.Instance.IsQuestCompleted(active.quest))
                break;   // 上一階段還沒完成，不能再往後找

            if (!stage.ConditionsMet())
                break;   // 這一階段的解鎖條件還沒滿足，停在上一個已解鎖的階段

            active = stage;

            if (!QuestManager.Instance.IsQuestCompleted(stage.quest))
                break;   // 這階段還在進行中，就是它了
        }

        return active;
    }

    NPCQuestState EvaluateStageState(NPCQuestStage stage)
    {
        if (QuestManager.Instance.IsQuestCompleted(stage.quest))
            return NPCQuestState.Completed;

        if (!QuestManager.Instance.IsQuestAccepted(stage.quest))
        {
            // 不能發任務的關係人（例如收物件的對象），任務還沒接時只會說些場面話
            return stage.canAcceptQuest ? NPCQuestState.CanAccept : NPCQuestState.InProgress;
        }

        bool hasItem;
        if (stage.turnInItems != null && stage.turnInItems.Length > 0)
        {
            hasItem = Inventory.Instance != null
                && stage.turnInItems.All(req => Inventory.Instance.GetItemCount(req.item) >= req.amount);
        }
        else
        {
            hasItem = stage.turnInItem == null
                || (Inventory.Instance != null && Inventory.Instance.GetItemCount(stage.turnInItem) > 0);
        }

        return hasItem ? NPCQuestState.CanHandIn : NPCQuestState.InProgress;
    }

    void RefreshState()
    {
        NPCQuestStage stage = GetActiveStage();

        if (stage == null)
        {
            // 純閒聊的 NPC：這段對話裡還有沒拿到的情報、或會推進劇情的選項，才亮標記；問完就熄，
            // 跟任務標記一樣的理念——亮著就代表過去一定有事可做
            if (questMarker != null)
            {
                if (ActiveChatHasSomethingNew()) questMarker.Show(markerSymbol, canAcceptColor);
                else questMarker.Hide();
            }
            return;
        }

        NPCQuestState state = EvaluateStageState(stage);
        UpdateMarker(stage, state);
        UpdateQuestProgress(stage, state);
    }

    bool ActiveChatHasSomethingNew() =>
        SegmentHasSomethingNew(conversation != null ? conversation.GetActiveChat() : null);

    static bool SegmentHasSomethingNew(DialogSegment segment)
    {
        if (segment == null || segment.choices == null) return false;

        foreach (var c in segment.choices)
            if (!c.IsHidden && !c.IsLocked && c.HasSomethingNew) return true;
        return false;
    }

    void UpdateMarker(NPCQuestStage stage, NPCQuestState state)
    {
        if (questMarker == null) return;

        switch (state)
        {
            case NPCQuestState.CanAccept:
                questMarker.Show(markerSymbol, canAcceptColor);
                break;

            case NPCQuestState.CanHandIn:
                // 只有真的能在這裡交任務才亮橘燈，否則會誤導玩家
                if (stage.completesQuestOnHandIn)
                    questMarker.Show(markerSymbol, canHandInColor);
                else
                    questMarker.Hide();
                break;

            default:
                // 進行中／已完成：這一段對話裡還有沒拿到的情報或會推進劇情的選項才亮，
                // 否則不顯示，避免玩家看到圖示跑過來卻沒事可做
                var segment = state == NPCQuestState.Completed ? stage.completed : stage.reminder;
                if (SegmentHasSomethingNew(segment)) questMarker.Show(markerSymbol, canAcceptColor);
                else questMarker.Hide();
                break;
        }
    }

    void UpdateQuestProgress(NPCQuestStage stage, NPCQuestState state)
    {
        if (state == NPCQuestState.InProgress)
            QuestManager.Instance.SetStage(stage.quest, stage.stageIndexInProgress);
        else if (state == NPCQuestState.CanHandIn)
            QuestManager.Instance.SetStage(stage.quest, stage.stageIndexReadyToHandIn);
        // Completed 的階段由 QuestManager.CompleteQuest 自動跳到最後一段
    }

    void PlaySimple(DialogSegment segment, System.Action onComplete = null)
    {
        if (segment == null || segment.lines == null || segment.lines.Length == 0) return;

        questMarker?.Hide();   // 標記別擋對話框；對話真正結束後在下面還原
        DialogManager.Instance.StartDialog(SpeakerName(), segment, () =>
        {
            onComplete?.Invoke();
            // IsDialogActive 排除連續對話鏈（交任務後緊接著下一段委託）中途誤還原的情況
            if (!DialogManager.Instance.IsDialogActive) RefreshState();
        });
    }

    void PlayWithChoice(DialogSegment segment, System.Action<int> onChoiceComplete)
    {
        if (segment == null || segment.lines == null || segment.lines.Length == 0) return;

        questMarker?.Hide();
        DialogManager.Instance.StartDialogWithChoice(SpeakerName(), segment, choiceIndex =>
        {
            onChoiceComplete(choiceIndex);
            if (!DialogManager.Instance.IsDialogActive) RefreshState();
        });
    }

    string SpeakerName() => conversation != null ? conversation.speakerName : "";
}

// NPC 目前對這個玩家而言處於什麼任務狀態（只在「有解鎖出來的階段」時才有意義）
public enum NPCQuestState
{
    CanAccept,    // 有新任務可以接
    InProgress,   // 任務進行中，但還沒達成條件
    CanHandIn,    // 條件達成，可以回報任務
    Completed     // 任務已完成
}
