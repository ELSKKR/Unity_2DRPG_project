using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 書本 UI 的「設定」頁。左頁是分類按鈕（音訊／鍵位／回到標題），右頁是被選中的那一類。
//
// 這個類別只負責音訊那一頁的資料來回：分頁切換交給同一個物件上的 TabGroup（子分頁模式），
// 鍵位那一頁的行為則完全沿用既有的 KeybindSettingsUI，這裡不碰。
//
// 刻意不繼承 MenuPanelBase：開關、鎖玩家移動、互斥那些是整本書（BookMenuPanel）的事，
// 這一頁只是書裡的一張紙，被 SetActive 切換而已。
public class BookSettingsUI : MonoBehaviour
{
    [Header("音量條")]
    [SerializeField] private SegmentedBar bgmBar;
    [SerializeField] private SegmentedBar sfxBar;

    [Header("靜音")]
    [SerializeField] private Button muteButton;
    [Tooltip("打勾的那個勾勾圖，靜音時才顯示")]
    [SerializeField] private GameObject muteCheck;

    [Header("重設音量")]
    [SerializeField] private Button resetButton;

    [Header("回到標題（沒有進行中的遊戲時會自己隱藏）")]
    [SerializeField] private Button returnToTitleButton;

    [Header("左頁說明欄")]
    [SerializeField] private TextMeshProUGUI infoText;
    [Tooltip("用來判斷重新翻回來時停在哪一個子分頁，說明欄才不會固定顯示音訊那一段")]
    [SerializeField] private GameObject keybindPage;
    [TextArea] [SerializeField] private string audioInfo = "音量會即時生效，關上書本時自動儲存。";
    [TextArea] [SerializeField] private string keybindInfo = "點一列再按下新按鍵即可更改。Esc 取消。";

    [Header("音效（留空 = 不播放）")]
    [Tooltip("拖 SFX 音量條時的試聽音")]
    [SerializeField] private AudioClip sfxPreviewSound;
    [Tooltip("按下按鈕時的音效")]
    [SerializeField] private AudioClip selectSound;

    [Header("設定")]
    [SerializeField] private float previewInterval = 0.12f;   // 試聽音的最短間隔，避免拖動時爆音

    private float lastPreviewTime;

    void Awake()
    {
        if (bgmBar != null) bgmBar.OnValueChanged += OnBGMChanged;
        if (sfxBar != null) sfxBar.OnValueChanged += OnSFXChanged;
        if (muteButton != null) muteButton.onClick.AddListener(OnMuteClicked);
        if (resetButton != null) resetButton.onClick.AddListener(OnResetClicked);
        if (returnToTitleButton != null) returnToTitleButton.onClick.AddListener(OnReturnToTitleClicked);
    }

    void OnDestroy()
    {
        if (bgmBar != null) bgmBar.OnValueChanged -= OnBGMChanged;
        if (sfxBar != null) sfxBar.OnValueChanged -= OnSFXChanged;
        if (muteButton != null) muteButton.onClick.RemoveListener(OnMuteClicked);
        if (resetButton != null) resetButton.onClick.RemoveListener(OnResetClicked);
        if (returnToTitleButton != null) returnToTitleButton.onClick.RemoveListener(OnReturnToTitleClicked);
    }

    void OnEnable()
    {
        // 每次翻到這一頁都重新同步一次：音量可能在別的地方被改過
        Refresh();

        // 子分頁的選擇是留著的（TabGroup 只在第一次 Start 時選一次），
        // 說明欄要跟著現在停著的那一頁，不能固定顯示音訊那一段
        SetInfo(keybindPage != null && keybindPage.activeSelf ? keybindInfo : audioInfo);

        // 標題畫面按設定時 CurrentSlot 是 -1（沒有進行中的遊戲），
        // 這顆是「存檔並回到標題」，沒有存檔可存就不該出現
        if (returnToTitleButton != null)
            returnToTitleButton.gameObject.SetActive(SaveManager.Instance != null && SaveManager.Instance.CurrentSlot >= 0);
    }

    // 翻走這一頁才把設定寫進硬碟：拖動時每幀都寫太重（AudioManager 只有改記憶體）
    void OnDisable()
    {
        AudioManager.Instance?.SaveVolumeSettings();
    }

    // 左頁的分類按鈕會透過 TabGroup 直接切頁面，說明文字沒有人會更新，
    // 所以額外掛在按鈕的 onClick 上（Inspector 指定），由分頁自己報自己的說明
    public void ShowAudioInfo() => SetInfo(audioInfo);
    public void ShowKeybindInfo() => SetInfo(keybindInfo);

    void SetInfo(string text)
    {
        if (infoText != null) infoText.text = text;
    }

    void Refresh()
    {
        var audio = AudioManager.Instance;
        if (audio == null) return;

        if (bgmBar != null) bgmBar.Value = audio.BGMVolume;
        if (sfxBar != null) sfxBar.Value = audio.SFXVolume;
        if (muteCheck != null) muteCheck.SetActive(audio.Muted);
    }

    void OnBGMChanged(float value)
    {
        AudioManager.Instance?.SetBGMVolume(value);
        // BGM 本來就在播，音量變化馬上聽得到，不需要額外試聽音
    }

    void OnSFXChanged(float value)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SetSFXVolume(value);

        // 音效平常沒在響，不放個試聽音玩家會不知道自己調到多大聲
        if (sfxPreviewSound != null && Time.unscaledTime - lastPreviewTime >= previewInterval)
        {
            lastPreviewTime = Time.unscaledTime;
            AudioManager.Instance.PlaySFX(sfxPreviewSound);
        }
    }

    void OnMuteClicked()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SetMuted(!AudioManager.Instance.Muted);
        if (muteCheck != null) muteCheck.SetActive(AudioManager.Instance.Muted);
        PlaySelect();
    }

    void OnResetClicked()
    {
        AudioManager.Instance?.ResetVolumesToDefault();
        Refresh();
        PlaySelect();
    }

    void OnReturnToTitleClicked()
    {
        PlaySelect();

        // 先把書關起來再切場景：書是常駐場景的物件，不關的話回到標題畫面它還攤在那裡
        GetComponentInParent<BookMenuPanel>(true)?.Close();
        SaveManager.Instance?.ReturnToTitle();
    }

    void PlaySelect()
    {
        if (selectSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(selectSound);
    }
}
