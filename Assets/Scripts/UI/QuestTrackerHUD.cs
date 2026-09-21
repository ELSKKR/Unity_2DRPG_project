using System.Collections;
using UnityEngine;
using TMPro;

// 畫面角落常駐的追蹤目標字幕（左下角）。
// 純手動：只顯示玩家在任務頁按過「追蹤」的那個任務，不會自動接去追別的。
//
// 三種畫面狀態：
// - 有追蹤中的任務 -> 顯示任務名 + 目前階段目標
// - 沒追蹤，但還有其他進行中的任務 -> 顯示提示文字，引導玩家去任務頁選
// - 一個進行中任務都沒有（還沒接過任何任務，或全部做完了）-> 整個面板收起來
//
// 對話中、書本選單開著、或不在遊戲場景（標題畫面／場景切換中）時也收起來，
// 這部分跟畫面內容無關，所以獨立在 Update() 判斷，不跟 OnQuestsChanged 混在一起。
//
// 這個元件本身要掛在常駐、不會被關掉的物件上（跟 NotificationToast 同一個做法：
// 腳本掛在場景根物件，panelRoot 另外指向 [Canvas] 底下真正會被開關的那塊面板）。
// 千萬不要把它掛在 panelRoot 自己身上——Update() 一旦把 panelRoot 關掉，
// 掛在上面的這個元件也會跟著被停用，Update() 從此不會再跑，面板就永遠打不開了
// （這正是專案鐵則第 3 條講的那個坑）
public class QuestTrackerHUD : MonoBehaviour
{
    [Header("整個面板（含背景），三種收起條件共用同一個開關。不要指到這個元件自己所在的物件")]
    [SerializeField] private GameObject panelRoot;
    [Tooltip("有追蹤任務／只顯示提示，面板兩種寫死的尺寸（不用自動依內容縮放：中文字換字型 fallback 時，同一個 fontSize 量出來的實際高度可能差到 1.5 倍，自動量測不可靠，這裡只有兩種固定狀態，寫死尺寸更省事也更準）")]
    [SerializeField] private Vector2 contentSize = new Vector2(210, 56);
    [SerializeField] private Vector2 hintSize = new Vector2(150, 32);

    [Header("有追蹤任務時顯示")]
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI objectiveText;

    [Header("沒追蹤、但還有其他進行中任務時顯示")]
    [SerializeField] private GameObject hintRoot;
    [SerializeField] private string hintMessage = "尚未追蹤任何任務";

    [Header("標題畫面場景名稱（這個場景一律收起）")]
    [SerializeField] private string titleSceneName = "TitleScreen";

    [Header("目標推進時的提示（留空 = 不播音效）")]
    [SerializeField] private AudioClip stageAdvanceSound;
    [SerializeField] private float flashScale = 1.15f;
    [SerializeField] private float flashDuration = 0.15f;

    private bool hasAnyInProgress;
    private string lastTrackedID;
    private int lastStage = -1;
    private Coroutine flashRoutine;
    private Vector3 contentBaseScale = Vector3.one;
    private RectTransform panelRect;

    // Start（不是 OnEnable）：這個元件掛在常駐、不會被關掉的物件上（見類別註解），
    // 用 Start 是為了保證 QuestManager.Awake() 已經跑完、Instance 一定不是 null
    // ——Unity 保證同一幀所有物件的 Awake 都跑完才會開始跑 Start，OnEnable 沒有這個保證，
    // 兩者的 Awake 順序沒定義的話，訂閱可能悄悄地被跳過
    void Start()
    {
        if (panelRoot != null) panelRect = panelRoot.GetComponent<RectTransform>();
        if (contentRoot != null) contentBaseScale = contentRoot.transform.localScale;

        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestsChanged += RefreshContent;

        RefreshContent();
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestsChanged -= RefreshContent;
    }

    void Update()
    {
        bool contextOK = InGameplayScene()
            && (DialogManager.Instance == null || !DialogManager.Instance.IsDialogActive)
            && (UIPanelManager.Instance == null || !UIPanelManager.Instance.IsAnyPanelOpen);

        if (panelRoot != null) panelRoot.SetActive(contextOK && hasAnyInProgress);
    }

    bool InGameplayScene()
    {
        var stm = SceneTransitionManager.Instance;
        if (stm == null) return false;

        string scene = stm.CurrentGameplayScene;
        return !string.IsNullOrEmpty(scene) && scene != titleSceneName;
    }

    void RefreshContent()
    {
        var manager = QuestManager.Instance;
        if (manager == null) return;

        hasAnyInProgress = manager.Quests.Exists(q => q.status == QuestStatus.InProgress);

        string trackedID = manager.TrackedQuestID;
        Quest tracked = string.IsNullOrEmpty(trackedID)
            ? null
            : manager.Quests.Find(q => q.data.questID == trackedID && q.status == QuestStatus.InProgress);

        if (tracked != null)
        {
            if (panelRect != null) panelRect.sizeDelta = contentSize;
            if (contentRoot != null) contentRoot.SetActive(true);
            if (hintRoot != null) hintRoot.SetActive(false);
            if (nameText != null) nameText.text = tracked.data.questName;
            if (objectiveText != null) objectiveText.text = tracked.CurrentObjective;

            // 同一個任務、階段數字變了才算「推進」，觸發閃一下+音效；
            // 剛切換追蹤目標（換了任務或剛完成重讀存檔）不算，不然一選任務就閃一次很煩
            bool stageAdvanced = trackedID == lastTrackedID && tracked.currentStage != lastStage;
            if (stageAdvanced) PlayFlash();

            lastTrackedID = trackedID;
            lastStage = tracked.currentStage;
        }
        else
        {
            lastTrackedID = null;
            lastStage = -1;

            if (contentRoot != null) contentRoot.SetActive(false);
            if (hintRoot != null)
            {
                hintRoot.SetActive(hasAnyInProgress);
                var hintText = hintRoot.GetComponentInChildren<TextMeshProUGUI>();
                if (hintText != null) hintText.text = hintMessage;
            }
            if (panelRect != null && hasAnyInProgress) panelRect.sizeDelta = hintSize;
        }
    }

    void PlayFlash()
    {
        if (stageAdvanceSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(stageAdvanceSound);

        if (contentRoot == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        Vector3 bigScale = contentBaseScale * flashScale;
        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.unscaledDeltaTime;
            contentRoot.transform.localScale = Vector3.Lerp(bigScale, contentBaseScale, t / flashDuration);
            yield return null;
        }
        contentRoot.transform.localScale = contentBaseScale;
        flashRoutine = null;
    }
}
