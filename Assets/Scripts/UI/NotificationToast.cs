using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

// 畫面左上角的通用通知 toast：接到新任務／完成任務／獲得情報時跳出
// （原本叫 QuestNotification 只處理任務，2026-08 擴充成任務/情報共用同一顆，
// 檔案跟著搬到 Scripts/UI，場景裡的 GameObject/元件參照靠 .meta 的 GUID 保留，沒有斷掉）
//
// 排隊顯示：同一瞬間如果觸發不只一件事（例如一個對話選項同時接下任務又解鎖情報），
// 不會互相蓋掉對方的文字，而是排隊一個接一個顯示完。
//
// 「按 X 鍵看詳情」的提示是通用的，不是任務專屬：哪個鍵、開哪個面板、
// 提示文字寫什麼，都跟著各則通知自己的設定走（見 ToastRequest），
// 按鍵本身透過 KeyBindings 查詢，玩家之後改了鍵位這裡會自動跟著換。
public class NotificationToast : MonoBehaviour
{
    public static NotificationToast Instance { get; private set; }

    [SerializeField] private GameObject notificationRoot;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("「按 X 鍵看詳情」提示（留空 = 不支援這個功能）")]
    [SerializeField] private GameObject keyHintGroup;
    [SerializeField] private TextMeshProUGUI keyHintKeyText;    // 顯示按鍵本身，例如「Q」「I」
    [SerializeField] private TextMeshProUGUI keyHintLabelText;  // 顯示動作文字，例如「了解任務詳情」

    [Header("設定")]
    [SerializeField] private float displayDuration = 4f;

    [Header("音效")]
    [Tooltip("大部分通知都用這顆（接到任務、獲得情報、選項鎖住提示...），個別要不同的才特別指定")]
    [SerializeField] private AudioClip defaultSound;
    [Tooltip("留空 = 用預設通知音效")]
    [SerializeField] private AudioClip questCompletedSound;

    private struct ToastRequest
    {
        public string message;
        public AudioClip sound;
        public KeyBindings.GameAction? hintAction;   // null = 這則通知沒有「按 X 看詳情」的提示
        public string hintLabel;                     // 例如「了解任務詳情」
    }

    private readonly Queue<ToastRequest> queue = new Queue<ToastRequest>();
    private bool isShowing;
    private Coroutine hideCoroutine;
    private KeyBindings.GameAction? currentHintAction;   // 目前這一則通知，按哪個 action 對應的鍵可以提早關掉

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        notificationRoot.SetActive(false);
    }

    void Start()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted += HandleQuestAccepted;
            QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
        }

        if (IntelManager.Instance != null)
            IntelManager.Instance.OnIntelUnlocked += HandleIntelUnlocked;
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestAccepted -= HandleQuestAccepted;
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
        }

        if (IntelManager.Instance != null)
            IntelManager.Instance.OnIntelUnlocked -= HandleIntelUnlocked;
    }

    void Update()
    {
        // 顯示「按 X 看詳情」時，玩家按下對應的鍵就提早關掉通知、換排隊裡的下一則；
        // 開對應面板這件事交給 QuestUI/IntelUI 自己監聽同一個 action 處理，這裡不重複呼叫，
        // 避免同一幀互相搶著開關。
        if (currentHintAction.HasValue && notificationRoot.activeSelf && KeyBindings.GetKeyDown(currentHintAction.Value))
        {
            HideAndAdvance();
        }
    }

    void HandleQuestAccepted(QuestData quest)
    {
        Enqueue($"接收到新任務：{quest.questName}", null, KeyBindings.GameAction.ToggleQuestLog, "了解任務詳情");
    }

    void HandleQuestCompleted(QuestData quest)
    {
        Enqueue($"任務完成：{quest.questName}", questCompletedSound, null, null);
    }

    void HandleIntelUnlocked(IntelData intel)
    {
        Enqueue($"獲得情報：{intel.title}", null, KeyBindings.GameAction.ToggleIntelLog, "了解情報詳情");
    }

    // 對話裡選到鎖住的選項時跳出來的提示（DialogManager 呼叫）
    // 故意不把情報標題顯示出來（顯示成「？？？」）——講出名字就等於先暴雷了，
    // 只讓玩家知道「這裡缺一則情報」，具體是什麼要自己去打聽才知道
    public void ShowLockedChoiceHint()
    {
        Enqueue("缺少情報「？？？」，或許該先打聽點消息。", null, null, null);
    }

    // 一般用途的通知，給沒有專屬類型的訊息用（例如試玩內容結束的提示）
    public void ShowMessage(string message)
    {
        Enqueue(message, null, null, null);
    }

    void Enqueue(string message, AudioClip sound, KeyBindings.GameAction? hintAction, string hintLabel)
    {
        // 沒特別指定音效就用預設通知音效
        queue.Enqueue(new ToastRequest
        {
            message = message,
            sound = sound != null ? sound : defaultSound,
            hintAction = hintAction,
            hintLabel = hintLabel
        });

        if (!isShowing)
            ShowNext();
    }

    void ShowNext()
    {
        if (queue.Count == 0)
        {
            isShowing = false;
            return;
        }

        isShowing = true;
        ToastRequest req = queue.Dequeue();

        if (req.sound != null)
            AudioManager.Instance.PlaySFX(req.sound);

        messageText.text = req.message;

        currentHintAction = req.hintAction;
        bool showHint = req.hintAction.HasValue && keyHintGroup != null;
        if (keyHintGroup != null) keyHintGroup.SetActive(showHint);
        if (showHint)
        {
            if (keyHintKeyText != null) keyHintKeyText.text = KeyBindings.Get(req.hintAction.Value).ToString();
            if (keyHintLabelText != null) keyHintLabelText.text = req.hintLabel;
        }

        notificationRoot.SetActive(true);

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(AutoHide());
    }

    IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(displayDuration);
        HideAndAdvance();
    }

    // 收起目前這一則，接著顯示排隊裡的下一則（沒有的話就自然停在隱藏狀態）
    void HideAndAdvance()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
        notificationRoot.SetActive(false);
        currentHintAction = null;

        ShowNext();
    }
}
