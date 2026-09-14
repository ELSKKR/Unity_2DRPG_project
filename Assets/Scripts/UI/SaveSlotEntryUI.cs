using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 存檔槽選單裡的其中一格。陽春版：只顯示「空／場景名稱＋存檔時間」文字，
// 之後要加縮圖/任務名稱/遊玩時數都是加欄位就好，不用改這個元件的骨架。
public class SaveSlotEntryUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI label;

    [Header("刪除（留空 = 不支援刪除；空存檔槽不會顯示這顆鈕）")]
    [SerializeField] private Button deleteButton;
    [SerializeField] private TextMeshProUGUI deleteButtonLabel;
    [SerializeField] private float deleteConfirmWindow = 3f;   // 按第一次後，幾秒內沒按第二次就恢復原狀

    const string DeleteLabelDefault = "刪除";
    const string DeleteLabelConfirm = "確定？";

    int slotIndex;
    System.Action<int> onClicked;
    System.Action<int> onDeleteConfirmed;
    AudioClip selectSound;   // 由 TitleScreenUI.Init() 傳入，跟主選單按鈕共用同一顆音效，不用每格各拖一次

    bool awaitingDeleteConfirm;
    float deleteConfirmDeadline;

    public void Init(int slot, System.Action<int> callback, System.Action<int> deleteCallback, AudioClip selectSound)
    {
        slotIndex = slot;
        onClicked = callback;
        onDeleteConfirmed = deleteCallback;
        this.selectSound = selectSound;
        button.onClick.AddListener(() => { PlaySelectSound(); onClicked?.Invoke(slotIndex); });

        if (deleteButton != null)
            deleteButton.onClick.AddListener(() => { PlaySelectSound(); OnDeleteButtonClicked(); });
    }

    void PlaySelectSound()
    {
        if (selectSound != null)
            AudioManager.Instance.PlaySFX(selectSound);
    }

    void Update()
    {
        // 按第一次後放著不管太久，確認狀態要自己過期，避免使用者隔了很久回來誤按到「確定？」
        if (awaitingDeleteConfirm && Time.unscaledTime >= deleteConfirmDeadline)
            ResetDeleteConfirm();
    }

    public void Display(SaveData data)
    {
        ResetDeleteConfirm();

        if (data == null)
        {
            label.text = $"存檔槽 {slotIndex + 1}\n（空，點擊開始新遊戲）";
            if (deleteButton != null) deleteButton.gameObject.SetActive(false);
            return;
        }

        string when = "未知時間";
        if (System.DateTime.TryParse(data.savedAtUtc, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var savedAt))
        {
            when = savedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
        }

        // 只顯示時/分，不顯示秒——秒數對「大概玩了多久」這種用途沒有意義，只會讓畫面看起來雜亂
        int totalMinutes = Mathf.FloorToInt(data.totalPlaytimeSeconds / 60f);
        string playtime = $"{totalMinutes / 60}小時{totalMinutes % 60}分";

        label.text = $"存檔槽 {slotIndex + 1}\n{data.sceneName}　{when}\n遊玩時間 {playtime}";

        if (deleteButton != null) deleteButton.gameObject.SetActive(true);
    }

    void OnDeleteButtonClicked()
    {
        if (!awaitingDeleteConfirm)
        {
            awaitingDeleteConfirm = true;
            deleteConfirmDeadline = Time.unscaledTime + deleteConfirmWindow;
            if (deleteButtonLabel != null) deleteButtonLabel.text = DeleteLabelConfirm;
            return;
        }

        // 時限內按了第二次，才是真的要刪除
        ResetDeleteConfirm();
        onDeleteConfirmed?.Invoke(slotIndex);
    }

    void ResetDeleteConfirm()
    {
        awaitingDeleteConfirm = false;
        if (deleteButtonLabel != null) deleteButtonLabel.text = DeleteLabelDefault;
    }
}
