using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MenuPanelBase
{
    public static SettingsUI Instance { get; private set; }

    [Header("音量滑桿")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TextMeshProUGUI bgmValueText;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI sfxValueText;

    [Header("音效（留空 = 不播放）")]
    [SerializeField] private AudioClip sfxPreviewSound;   // 拖 SFX 滑桿時的試聽音

    [Header("回到標題（留空 = 不顯示這個功能）")]
    [SerializeField] private Button returnToTitleButton;

    [Tooltip("按下「儲存並回到標題」時的音效（留空 = 不播放）")]
    [SerializeField] private AudioClip selectSound;

    [Header("設定")]
    [SerializeField] private float previewInterval = 0.12f;   // 試聽音的最短間隔，避免拖動時爆音

    private float lastPreviewTime;

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    protected override void Start()
    {
        base.Start();

        // 用 SetValueWithoutNotify 初始化，否則會在還沒開面板時就觸發一次 onValueChanged
        bgmSlider.SetValueWithoutNotify(AudioManager.Instance.BGMVolume);
        sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SFXVolume);
        UpdateValueTexts();

        bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);

        if (returnToTitleButton != null)
            returnToTitleButton.onClick.AddListener(OnReturnToTitleClicked);
    }

    void OnDestroy()
    {
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(OnBGMChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
        if (returnToTitleButton != null) returnToTitleButton.onClick.RemoveListener(OnReturnToTitleClicked);
    }

    void OnReturnToTitleClicked()
    {
        if (selectSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(selectSound);

        Close();
        SaveManager.Instance?.ReturnToTitle();
    }

    // Esc 鍵的開關/返回行為現在統一交給 UIPanelManager 處理，這裡不用再自己判斷

    protected override void OnOpened()
    {
        // 面板打開時同步一次，避免其他地方改過音量後滑桿位置不同步
        bgmSlider.SetValueWithoutNotify(AudioManager.Instance.BGMVolume);
        sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SFXVolume);
        UpdateValueTexts();

        // 標題畫面按「設定」時 CurrentSlot 是 -1（沒有進行中的遊戲），
        // 這個按鈕是「存檔並回到標題」，標題畫面本身不需要顯示
        if (returnToTitleButton != null)
            returnToTitleButton.gameObject.SetActive(SaveManager.Instance != null && SaveManager.Instance.CurrentSlot >= 0);
    }

    protected override void OnClosed()
    {
        AudioManager.Instance.SaveVolumeSettings();
    }

    void OnBGMChanged(float value)
    {
        AudioManager.Instance.SetBGMVolume(value);
        UpdateValueTexts();
        // BGM 本來就在播，音量變化馬上聽得到，不需要額外試聽音
    }

    void OnSFXChanged(float value)
    {
        AudioManager.Instance.SetSFXVolume(value);
        UpdateValueTexts();

        // 音效平常沒在響，不放個試聽音玩家會不知道自己調到多大聲
        if (sfxPreviewSound != null && Time.unscaledTime - lastPreviewTime >= previewInterval)
        {
            lastPreviewTime = Time.unscaledTime;
            AudioManager.Instance.PlaySFX(sfxPreviewSound);
        }
    }

    void UpdateValueTexts()
    {
        if (bgmValueText != null) bgmValueText.text = Mathf.RoundToInt(bgmSlider.value * 100f) + "%";
        if (sfxValueText != null) sfxValueText.text = Mathf.RoundToInt(sfxSlider.value * 100f) + "%";
    }
}
