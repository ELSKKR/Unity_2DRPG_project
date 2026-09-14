using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("畫面淡入淡出")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.4f;

    [Header("音效（留空 = 不播放）")]
    [SerializeField] private AudioClip transitionSound;

    private string currentGameplayScene;
    private string pendingSpawnID;
    private System.Action pendingOnComplete;
    private string backgroundScene;

    // 存檔系統要知道「玩家現在在哪個場景」才能存檔；回標題時也要判斷目前算不算「遊玩中」
    public string CurrentGameplayScene => currentGameplayScene;

    // 標題畫面拿真實場景當背景時用（TitleBackground）。那個場景不是「目前遊玩場景」——
    // 玩家不在裡面、也沒有做出生點定位——但轉場離開標題時一樣要卸載掉，
    // 否則接下來 LoadSceneAsync 載同一個場景會出現第二份，整個世界疊兩層。
    public void SetBackgroundScene(string sceneName) => backgroundScene = sceneName;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 給 BootstrapLoader 開機載入標題畫面時用：只記錄「目前是這個場景」，
    // 不做出生點定位（標題畫面沒有 PlayerSpawnPoint，玩家角色這時候也是關閉的）。
    // 這樣之後從標題畫面 TransitionToScene 離開時，這個場景才會被正確卸載掉，
    // 不會一直疊在遊玩場景底下。
    public void SetCurrentSceneWithoutSpawn(string sceneName)
    {
        currentGameplayScene = sceneName;
    }

    public void TransitionToScene(string sceneName, string spawnID, System.Action onComplete = null)
    {
        pendingSpawnID = spawnID;
        pendingOnComplete = onComplete;
        StartCoroutine(DoTransition(sceneName));
    }

    IEnumerator DoTransition(string newSceneName)
    {
        var player = FindFirstObjectByType<PlayerController>();
        player?.SetCanMove(false);
        InteractionPrompt.Instance?.Hide();

        // 平常這張黑幕完全透明、大家會忘記它還在——但它的 raycastTarget 預設是開著的，
        // 代表看不見歸看不見，滑鼠事件全部被它吃掉。只有真的在轉場的這段時間才需要擋，
        // 平常必須關掉，不然疊在它下面的 UI（包含標題畫面）永遠點不到
        fadeImage.raycastTarget = true;

        if (transitionSound != null)
            AudioManager.Instance.PlaySFX(transitionSound);

        yield return StartCoroutine(Fade(0f, 1f)); // 淡出（畫面轉黑）

        string previousScene = currentGameplayScene;

        // 卸載目前的遊玩場景
        if (!string.IsNullOrEmpty(currentGameplayScene))
        {
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentGameplayScene);
            while (unloadOp != null && !unloadOp.isDone)
                yield return null;
        }

        // 標題畫面的背景場景也要卸載，而且一定要在載入新場景「之前」做完：
        // 玩家通常就是從標題直接進入那個背景場景（新遊戲的起始場景），
        // 沒先卸載的話同一個場景會被載入第二份。
        //
        // 這裡刻意「卸載再重新載入」而不是沿用已經載好的那份：CropField／ItemPickup
        // 這些是在 Awake 跟 WorldStateManager 對帳的，背景載入時就已經跑過一次，
        // 比它們晚匯入的存檔進度會套不上去（見附錄 A 第一條：執行期 vs 持久化狀態）。
        if (!string.IsNullOrEmpty(backgroundScene))
        {
            AsyncOperation bgUnloadOp = SceneManager.UnloadSceneAsync(backgroundScene);
            while (bgUnloadOp != null && !bgUnloadOp.isDone)
                yield return null;
            backgroundScene = null;
        }

        // 附加載入新的遊玩場景
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(newSceneName, LoadSceneMode.Additive);

        if (loadOp == null)
        {
            // 場景名稱打錯字、或忘記加進 Build Settings：LoadSceneAsync 會回傳 null。
            // 這裡不能什麼都不做直接往下走，不然 while(!loadOp.isDone) 會直接 NullReferenceException，
            // 協程死在畫面全黑、玩家永久卡住的狀態，且玩家完全看不到任何錯誤提示。
            Debug.LogError($"場景切換失敗：找不到場景「{newSceneName}」，請確認名稱是否正確、是否已加入 Build Settings。");

            // 盡量把剛卸載的舊場景載回來，讓玩家至少能繼續玩，而不是卡在空場景裡
            if (!string.IsNullOrEmpty(previousScene))
            {
                AsyncOperation recoverOp = SceneManager.LoadSceneAsync(previousScene, LoadSceneMode.Additive);
                if (recoverOp != null)
                {
                    while (!recoverOp.isDone)
                        yield return null;
                    currentGameplayScene = previousScene;
                }
            }

            yield return StartCoroutine(Fade(1f, 0f));
            fadeImage.raycastTarget = false;
            player = FindFirstObjectByType<PlayerController>();
            player?.SetCanMove(true);
            yield break;
        }

        while (!loadOp.isDone)
            yield return null;

        currentGameplayScene = newSceneName;

        yield return null; // 等一幀，確保新場景的初始化完成

        // spawnID 留空代表呼叫端不需要（或不想要）用出生點定位——
        // 例如讀檔要用存檔裡的精確座標蓋過去、或切到沒有出生點的標題畫面
        if (!string.IsNullOrEmpty(pendingSpawnID))
            PlacePlayerAtSpawn();

        // 讓呼叫端（例如存檔系統要蓋回精確座標）在畫面還是黑的時候做完，玩家才不會看到瞬間跳動
        var completeCallback = pendingOnComplete;
        pendingOnComplete = null;
        completeCallback?.Invoke();

        yield return StartCoroutine(Fade(1f, 0f)); // 淡入（畫面轉回來）
        fadeImage.raycastTarget = false;

        player = FindFirstObjectByType<PlayerController>();
        player?.SetCanMove(true);
    }

    void PlacePlayerAtSpawn()
    {
        var spawnPoints = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
        foreach (var sp in spawnPoints)
        {
            if (sp.SpawnID == pendingSpawnID)
            {
                // 一定要含 inactive：新遊戲流程裡，玩家角色這個當下還沒被 SaveManager 打開
                // （SetActive 是在這個方法之後的 onComplete callback 才做），
                // 用預設的 FindFirstObjectByType 找不到會直接 silently 跳過定位、也不會有警告
                var player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
                if (player != null)
                    player.transform.position = sp.transform.position;
                return;
            }
        }
        Debug.LogWarning($"找不到 spawnID: {pendingSpawnID}");
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        Color c = fadeImage.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = to;
        fadeImage.color = c;
    }
}
