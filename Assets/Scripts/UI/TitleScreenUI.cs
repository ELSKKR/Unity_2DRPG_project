using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 標題畫面（獨立場景 TitleScreen.unity 專用，不是 MenuPanelBase 那一套遊戲內選單，
// 沒有「Esc 返回」、沒有玩家可以鎖移動，所以不繼承 MenuPanelBase）。
// 兩層面板：主選單（開始遊戲／設定／離開）→ 按「開始遊戲」切到存檔槽選單。
//
// 「設定」改成直接開常駐場景那本書、翻到設定頁——舊的 SettingsPanel／SettingsUI
// 已經退役刪掉了，書本 UI 是現在唯一一套設定介面。書活在 Persistent 場景的
// [Canvas]/BookWindow 底下，跨場景所以用 GameObject.Find 找，不是 Inspector 拖引用。
public class TitleScreenUI : MonoBehaviour
{
    [Header("面板")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject saveSlotPanel;

    [Header("主選單按鈕")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("存檔槽選單")]
    [SerializeField] private Button backButton;
    [SerializeField] private SaveSlotEntryUI[] slotEntries;   // 依序對應存檔槽 0、1、2

    [Header("音效（留空 = 不播放）")]
    [SerializeField] private AudioClip selectSound;   // 按鈕點選音效，存檔槽格子也共用這顆（往下傳給 SaveSlotEntryUI）

    // 書本側標籤的排列順序（美術決定，見 TabGroup 上的 tabs 陣列）：
    // 0 任務／1 背包／2 裝備／3 情報／4 存檔／5 設定
    const int BookSettingsTabIndex = 5;

    private TabGroup bookTabGroup;

    void Start()
    {
        mainMenuPanel.SetActive(true);
        saveSlotPanel.SetActive(false);

        bookTabGroup = GameObject.Find("[Canvas]")?.transform.Find("BookWindow")?.GetComponent<TabGroup>();
        if (bookTabGroup == null)
            Debug.LogWarning("TitleScreenUI 找不到 Persistent 場景的 BookWindow，標題畫面的「設定」按鈕不會有反應。");

        startGameButton.onClick.AddListener(() => { PlaySelectSound(); ShowSaveSlots(); });
        settingsButton.onClick.AddListener(() => { PlaySelectSound(); bookTabGroup?.ToggleTab(BookSettingsTabIndex); });
        quitButton.onClick.AddListener(() => { PlaySelectSound(); QuitGame(); });
        backButton.onClick.AddListener(() => { PlaySelectSound(); ShowMainMenu(); });

        for (int i = 0; i < slotEntries.Length; i++)
            slotEntries[i].Init(i, OnSlotClicked, OnDeleteClicked, selectSound);
    }

    void PlaySelectSound()
    {
        if (selectSound != null)
            AudioManager.Instance.PlaySFX(selectSound);
    }

    void ShowSaveSlots()
    {
        mainMenuPanel.SetActive(false);
        saveSlotPanel.SetActive(true);
        RefreshSlots();
    }

    void ShowMainMenu()
    {
        saveSlotPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    void RefreshSlots()
    {
        for (int i = 0; i < slotEntries.Length; i++)
        {
            SaveData data = SaveManager.Instance.PeekSlot(i);
            slotEntries[i].Display(data);
        }
    }

    void OnSlotClicked(int slot)
    {
        SaveData data = SaveManager.Instance.PeekSlot(slot);
        if (data == null)
            SaveManager.Instance.StartNewGame(slot);
        else
            SaveManager.Instance.LoadSlotAndEnterGame(slot);
    }

    void OnDeleteClicked(int slot)
    {
        SaveManager.Instance.DeleteSlot(slot);
        RefreshSlots();
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
