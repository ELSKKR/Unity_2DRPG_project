using UnityEngine;
using UnityEngine.SceneManagement;

// 開機流程：Persistent 常駐場景一起動就會跑這個，載入標題畫面（不是直接進遊戲）。
// 玩家角色先關掉，等實際「開始遊戲」（新遊戲/讀檔）時才由 SaveManager 打開、
// 定位到正確場景與座標。
public class BootstrapLoader : MonoBehaviour
{
    [SerializeField] private string titleScene = "TitleScreen";

    void Start()
    {
        var player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player != null) player.gameObject.SetActive(false);

        SceneManager.LoadScene(titleScene, LoadSceneMode.Additive);
        SceneTransitionManager.Instance.SetCurrentSceneWithoutSpawn(titleScene);
    }
}
