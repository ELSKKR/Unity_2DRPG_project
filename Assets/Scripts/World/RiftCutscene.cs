using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 裂縫演出：黑底淡入 → 逐行文字 → 淡出，回到正常場景
// 目前是最陽春的黑底＋文字版本，星座連線視覺之後再加（掛在同一個 root 底下即可）
public class RiftCutscene : MonoBehaviour
{
    public static RiftCutscene Instance { get; private set; }

    [Header("UI 參照")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI lineText;

    [Header("節奏")]
    [SerializeField] private float backgroundFadeDuration = 0.8f;
    [SerializeField] private float lineFadeDuration = 0.6f;
    [SerializeField] private float lineHoldDuration = 1.8f;
    [SerializeField] private float holdBeforeExit = 0.6f;

    [Header("音效（留空 = 不播放）")]
    [SerializeField] private AudioClip startSound;

    public bool IsPlaying { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (root != null) root.SetActive(false);
    }

    // startBlack = 呼叫當下黑幕就直接蓋滿，不做淡入。用在轉場黑畫面還沒退的時候接著播，
    // 兩層黑幕無縫銜接，玩家不會先看到場景又被蓋黑
    public void Play(string[] lines, System.Action onComplete = null, bool startBlack = false)
    {
        if (IsPlaying || lines == null || lines.Length == 0) return;
        StartCoroutine(PlayRoutine(lines, onComplete, startBlack));
    }

    IEnumerator PlayRoutine(string[] lines, System.Action onComplete, bool startBlack)
    {
        IsPlaying = true;

        root.SetActive(true);
        SetAlpha(background, startBlack ? 1f : 0f);
        SetAlpha(lineText, 0f);
        lineText.text = "";

        // 轉場收尾會把玩家移動打開，在它之前鎖住會被蓋掉，所以等轉場做完再鎖
        while (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsTransitioning)
            yield return null;

        var player = FindFirstObjectByType<PlayerController>();
        player?.SetCanMove(false);
        global::InteractionPrompt.Instance?.Hide();

        if (startSound != null)
            AudioManager.Instance.PlaySFX(startSound);

        if (!startBlack)
            yield return Fade(background, 0f, 1f, backgroundFadeDuration);

        foreach (var line in lines)
        {
            lineText.text = line;
            yield return Fade(lineText, 0f, 1f, lineFadeDuration);
            yield return new WaitForSeconds(lineHoldDuration);
            yield return Fade(lineText, 1f, 0f, lineFadeDuration);
        }

        yield return new WaitForSeconds(holdBeforeExit);
        yield return Fade(background, 1f, 0f, backgroundFadeDuration);

        root.SetActive(false);
        player?.SetCanMove(true);

        IsPlaying = false;
        onComplete?.Invoke();
    }

    IEnumerator Fade(Graphic target, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetAlpha(target, Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetAlpha(target, to);
    }

    void SetAlpha(Graphic target, float a)
    {
        if (target == null) return;
        Color c = target.color;
        c.a = a;
        target.color = c;
    }
}
