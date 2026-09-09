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

    public void Play(string[] lines, System.Action onComplete = null)
    {
        if (IsPlaying || lines == null || lines.Length == 0) return;
        StartCoroutine(PlayRoutine(lines, onComplete));
    }

    IEnumerator PlayRoutine(string[] lines, System.Action onComplete)
    {
        IsPlaying = true;

        var player = FindFirstObjectByType<PlayerController>();
        player?.SetCanMove(false);
        global::InteractionPrompt.Instance?.Hide();

        root.SetActive(true);
        SetAlpha(background, 0f);
        SetAlpha(lineText, 0f);
        lineText.text = "";

        if (startSound != null)
            AudioManager.Instance.PlaySFX(startSound);

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
