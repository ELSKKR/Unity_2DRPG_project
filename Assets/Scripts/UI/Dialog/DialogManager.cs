using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance { get; private set; }

    [Header("UI 參照")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogText;

    [Header("打字效果")]
    [SerializeField] private float typingSpeed = 0.03f;

    [Header("預設音效（DialogSegment 沒指定時使用）")]
    [SerializeField] private AudioClip defaultTypingSound;
    [SerializeField] private AudioClip defaultStartSound;

    [Header("選項 UI")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Transform choiceListContainer;
    [SerializeField] private GameObject choiceEntryPrefab;

    private string currentSpeaker;
    private string[] currentLines;
    private DialogChoice[] currentChoices;
    // currentChoices 是隱藏過的清單；回報給呼叫端的必須是原始陣列的索引（NPCDialog 用它回查 acceptsQuest）
    private int[] currentChoiceSourceIndex;
    private AudioClip currentTypingSound;
    private AudioClip currentStartSound;

    private int currentLineIndex;
    private bool isTyping;
    private bool justStarted;
    private Coroutine typingCoroutine;

    private bool isChoosing;
    private int selectedChoiceIndex;
    private int pendingChoiceIndex = -1;
    private DialogChoice pendingChoice;   // 選項的通用副作用要延後到 EndDialog 才生效，這裡先記住選了哪個
    private readonly List<GameObject> choiceEntryObjs = new List<GameObject>();

    private PlayerController player;
    private System.Action onComplete;
    private System.Action<int> onChoiceComplete;

    // 對話進行中時，其他 UI 不該搶走玩家的移動鎖定
    public bool IsDialogActive => dialogPanel != null && dialogPanel.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        dialogPanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        player = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        if (!dialogPanel.activeSelf) return;

        if (justStarted)
        {
            justStarted = false;
            return;
        }

        if (isChoosing)
        {
            HandleChoiceInput();
            return;
        }

        if (KeyBindings.GetKeyDown(KeyBindings.GameAction.Interact))
        {
            if (isTyping)
                CompleteLine();
            else
                AdvanceLine();
        }
    }

    // ---------- 對外播放介面 ----------

    // 沒有選項的簡單對話（reminder / handIn / completed 都走這個）
    public void StartDialog(string speaker, DialogSegment segment, System.Action onComplete = null)
    {
        BeginSegment(speaker, segment);
        this.onComplete = onComplete;
        this.onChoiceComplete = null;
    }

    // 帶選項的對話（intro 這種需要玩家決定的場合）
    public void StartDialogWithChoice(string speaker, DialogSegment segment, System.Action<int> onChoiceComplete)
    {
        BeginSegment(speaker, segment);
        this.onComplete = null;
        this.onChoiceComplete = onChoiceComplete;
    }

    // ---------- 內部流程 ----------

    void BeginSegment(string speaker, DialogSegment segment)
    {
        currentSpeaker = speaker;
        currentLines = segment.lines;
        FilterChoices(segment.choices);
        currentTypingSound = segment.typingSound != null ? segment.typingSound : defaultTypingSound;
        currentStartSound = segment.startSound != null ? segment.startSound : defaultStartSound;
        currentLineIndex = 0;
        pendingChoiceIndex = -1;
        pendingChoice = null;
        isChoosing = false;
        if (choicePanel != null) choicePanel.SetActive(false);

        dialogPanel.SetActive(true);
        speakerNameText.text = currentSpeaker;
        justStarted = true;

        player?.SetCanMove(false);   // 對話中鎖移動

        if (currentStartSound != null)
            AudioManager.Instance.PlaySFX(currentStartSound);

        ShowLine();
    }

    // 拿掉 hideIfEventID 已成立的選項，並記住每個留下來的選項在原始陣列裡的索引
    void FilterChoices(DialogChoice[] source)
    {
        if (source == null) { currentChoices = null; currentChoiceSourceIndex = null; return; }

        var kept = new List<DialogChoice>();
        var index = new List<int>();
        for (int i = 0; i < source.Length; i++)
        {
            var c = source[i];
            bool hidden = !string.IsNullOrEmpty(c.hideIfEventID)
                && WorldStateManager.Instance != null && WorldStateManager.Instance.HasEvent(c.hideIfEventID);
            if (hidden) continue;
            kept.Add(c);
            index.Add(i);
        }
        currentChoices = kept.ToArray();
        currentChoiceSourceIndex = index.ToArray();
    }

    // 選項設了 requiredIntel、又還沒解鎖那則情報的話，這個選項就是「鎖住」的：
    // 還是會顯示（讓玩家知道這裡有東西、勾起好奇心），只是選了問不出所以然
    bool IsChoiceLocked(DialogChoice choice)
    {
        return choice.requiredIntel != null
            && !(IntelManager.Instance != null && IntelManager.Instance.HasIntel(choice.requiredIntel));
    }

    void ShowLine()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLine(currentLines[currentLineIndex]));
    }

    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogText.text = "";

        foreach (char c in line)
        {
            dialogText.text += c;

            if (currentTypingSound != null)
                AudioManager.Instance.PlaySFX(currentTypingSound);

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    void CompleteLine()
    {
        StopCoroutine(typingCoroutine);
        dialogText.text = currentLines[currentLineIndex];
        isTyping = false;
    }

    void AdvanceLine()
    {
        currentLineIndex++;

        if (currentLineIndex < currentLines.Length)
        {
            ShowLine();
            return;
        }

        // 這段文字放完了：有選項就顯示選項，沒有就直接結束
        if (currentChoices != null && currentChoices.Length > 0)
            ShowChoices();
        else
            EndDialog();
    }

    // ---------- 選項 ----------

    void ShowChoices()
    {
        isChoosing = true;
        selectedChoiceIndex = 0;

        if (choicePanel != null) choicePanel.SetActive(true);

        // 倒著走並先脫離父物件，沿用跟 QuestUI/InventoryUI 一致的清單重建寫法
        for (int i = choiceListContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = choiceListContainer.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        choiceEntryObjs.Clear();

        foreach (var choice in currentChoices)
        {
            GameObject entry = Instantiate(choiceEntryPrefab, choiceListContainer);
            var label = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                bool locked = IsChoiceLocked(choice);
                // 鎖住的選項不顯示真正的問題文字（會提前暴露劇情），統一顯示「？？？」，
                // 半透明表示選了也問不出所以然——但還是要看得到、選得到，勾起玩家去打聽情報的動機
                label.text = locked ? "？？？" : choice.label;
                label.color = new Color(label.color.r, label.color.g, label.color.b, locked ? 0.5f : 1f);
            }
            choiceEntryObjs.Add(entry);
        }

        UpdateChoiceHighlight();
    }

    void HandleChoiceInput()
    {
        // 選選項直接沿用「移動」的上下鍵：玩家用什麼走路就用什麼選，不用另外記一組，
        // 也避免兩個動作預設綁同一顆鍵害改鍵時的衝突偵測失效
        if (Input.GetKeyDown(KeyCode.UpArrow) || KeyBindings.GetKeyDown(KeyBindings.GameAction.MoveUp))
            MoveChoiceSelection(-1);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || KeyBindings.GetKeyDown(KeyBindings.GameAction.MoveDown))
            MoveChoiceSelection(1);
        else if (KeyBindings.GetKeyDown(KeyBindings.GameAction.Interact))
            ConfirmChoice();
    }

    void MoveChoiceSelection(int delta)
    {
        int count = currentChoices.Length;
        selectedChoiceIndex = (selectedChoiceIndex + delta + count) % count;
        UpdateChoiceHighlight();
    }

    void UpdateChoiceHighlight()
    {
        for (int i = 0; i < choiceEntryObjs.Count; i++)
        {
            Transform hl = choiceEntryObjs[i].transform.Find("Highlight");
            if (hl != null) hl.gameObject.SetActive(i == selectedChoiceIndex);
        }
    }

    void ConfirmChoice()
    {
        DialogChoice chosen = currentChoices[selectedChoiceIndex];

        if (IsChoiceLocked(chosen))
        {
            // 選了鎖住的選項：只跳提示，對話跟選項清單都留在原地，玩家可以繼續選別的或直接離開
            NotificationToast.Instance?.ShowLockedChoiceHint();
            return;
        }

        pendingChoiceIndex = currentChoiceSourceIndex[selectedChoiceIndex];
        pendingChoice = chosen;   // 標記事件／解鎖情報延後到 EndDialog 才生效，不要一選完就跳通知

        isChoosing = false;
        if (choicePanel != null) choicePanel.SetActive(false);

        currentChoices = null;   // 反應句播完後不會再進 ShowChoices，直接結束對話

        if (chosen.reactionLines != null && chosen.reactionLines.Length > 0)
        {
            currentLines = chosen.reactionLines;
            currentLineIndex = 0;
            ShowLine();
        }
        else
        {
            EndDialog();
        }
    }

    // ---------- 結束 ----------

    void EndDialog()
    {
        dialogPanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);

        player?.SetCanMove(true);

        var cb = onComplete;
        var choiceCb = onChoiceComplete;
        int chosenIndex = pendingChoiceIndex;
        var chosen = pendingChoice;

        onComplete = null;
        onChoiceComplete = null;
        pendingChoiceIndex = -1;
        pendingChoice = null;

        // 選項的通用副作用（標記世界事件／解鎖情報）放在對話真正結束、反應句都播完之後才生效，
        // 不然選完的當下反應句都還沒看完，通知就先跳出來會很奇怪（要跟任務接取/完成通知的時機一致）
        if (chosen != null)
        {
            if (!string.IsNullOrEmpty(chosen.markEventID))
                WorldStateManager.Instance?.MarkEvent(chosen.markEventID);

            if (chosen.grantsIntel != null)
                IntelManager.Instance?.UnlockIntel(chosen.grantsIntel);
        }

        cb?.Invoke();                  // 對話結束後的自訂事件，例如交任務
        choiceCb?.Invoke(chosenIndex); // 帶選項的對話：回報玩家選了哪個
    }
}
