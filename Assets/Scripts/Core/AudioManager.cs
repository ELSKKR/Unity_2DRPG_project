using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("音效來源")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("音量設定")]
    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    [Header("大門共用音效（只掛這裡一次，所有門預設都用這組；個別門若有自己的欄位會優先用門自己的）")]
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip doorLockedSound;
    public AudioClip DoorOpenSound => doorOpenSound;
    public AudioClip DoorLockedSound => doorLockedSound;

    [Header("撿取道具共用音效（只掛這裡一次，所有拾取物預設都用這組；個別拾取物若有自己的欄位會優先用它自己的）")]
    [SerializeField] private AudioClip itemPickupSound;
    public AudioClip ItemPickupSound => itemPickupSound;

    const string BgmVolumeKey = "BGMVolume";
    const string SfxVolumeKey = "SFXVolume";
    const string MutedKey = "AudioMuted";

    // 靜音是獨立於音量的開關，不是「把音量設成 0」——這樣取消靜音時
    // 才能把玩家原本調好的音量原封不動地還回去
    private bool muted;

    // Inspector 上填的那組值就是「預設音量」。要在讀 PlayerPrefs 之前先抄起來，
    // 不然開過一次遊戲之後它們就被存檔蓋掉，設定頁的「重設」會變成還原成上次的值
    private float defaultBgmVolume;
    private float defaultSfxVolume;

    public float BGMVolume => bgmVolume;
    public float SFXVolume => sfxVolume;
    public bool Muted => muted;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        defaultBgmVolume = bgmVolume;
        defaultSfxVolume = sfxVolume;

        // 讀回上次存的音量（沒存過就沿用 Inspector 的預設值）
        bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, bgmVolume);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume);
        muted = PlayerPrefs.GetInt(MutedKey, 0) != 0;

        bgmSource.loop = true;
        ApplyVolumes();
    }

    // 音量與靜音都只會改到這兩個 AudioSource，集中在一個地方設，
    // 免得之後又漏掉其中一邊（SetSFXVolume 就漏過一次，音量被算成平方）
    void ApplyVolumes()
    {
        bgmSource.volume = muted ? 0f : bgmVolume;
        sfxSource.volume = muted ? 0f : sfxVolume;
    }

    // 播放背景音樂（會自動循環，切換歌曲時才會重播）
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource.clip == clip) return;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // 播放一次性音效（可以同時疊多個，不會互相中斷）
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        // 音量統一由 sfxSource.volume 控制；這裡再傳一次 sfxVolume 會變成平方
        sfxSource.PlayOneShot(clip);
    }

    // 之後如果要做音量選項 UI，這兩個方法直接接就好
    public void SetBGMVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        ApplyVolumes();
        PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyVolumes();                 // 原本漏掉這行，滑桿拉低時音量會被算成平方
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
    }

    public void SetMuted(bool value)
    {
        muted = value;
        ApplyVolumes();
        PlayerPrefs.SetInt(MutedKey, muted ? 1 : 0);
    }

    public void ResetVolumesToDefault()
    {
        SetBGMVolume(defaultBgmVolume);
        SetSFXVolume(defaultSfxVolume);
        SetMuted(false);
    }

    // 滑桿拖動時每幀都寫硬碟太重，改成關閉設定面板時才真正存檔
    public void SaveVolumeSettings()
    {
        PlayerPrefs.Save();
    }
}