using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 標題畫面的動態背景：把真正的遊玩場景載進來當底圖，相機緩慢平移，
// 水流、NPC 待機動畫都是活的，比靜態圖有生氣。
//
// 這個元件放在 TitleScreen 場景裡，所以玩家一離開標題、場景被卸載時，
// OnDestroy 就會把相機還給遊戲，不需要別人來通知它。
//
// 為什麼要先關掉主相機上的兩個元件：
// - CameraFollow 的 target 是玩家。標題畫面時玩家雖然是關閉狀態，但 Transform 參照還在，
//   LateUpdate 會照樣把相機釘在玩家身上，蓋掉這裡設的取景與平移。
// - PixelPerfectCamera 會自己接管 orthographicSize（依 320x180 的參考解析度算出約 7.87），
//   不關掉就拉不出俯瞰整個村莊的廣角。
//
// 背景場景「不是」目前遊玩場景，所以要跟 SceneTransitionManager 登記，
// 讓它在轉場離開標題時連同背景一起卸載——否則玩家按下開始遊戲、
// 載入的剛好就是同一個村莊時，世界會被載入兩份。
public class TitleBackground : MonoBehaviour
{
    [Header("要當背景的場景")]
    [SerializeField] private string backgroundScene = "Forest_Village";

    [Header("取景")]
    [SerializeField] private Vector2 viewCenter = new Vector2(-5f, -5f);
    [SerializeField] private float viewSize = 18f;

    [Header("平移")]
    [Tooltip("以取景中心為基準，水平／垂直各往兩側移動多少世界單位")]
    [SerializeField] private Vector2 panRange = new Vector2(6f, 3f);
    [Tooltip("水平來回一趟的秒數。垂直方向會用一半的頻率，軌跡才不會是單調的直線來回")]
    [SerializeField] private float panPeriod = 40f;

    private Camera cam;
    private CameraFollow follow;
    private UnityEngine.U2D.PixelPerfectCamera pixelPerfect;
    private float originalOrthoSize;
    private bool takenOver;

    IEnumerator Start()
    {
        // 從遊戲中回到標題時，轉場已經把舊場景卸載掉了，正常會走到這裡重新載入一次。
        // 但還是先檢查一次：重複載入同一個場景會得到兩份世界，而且不會有任何錯誤訊息。
        if (!SceneManager.GetSceneByName(backgroundScene).isLoaded)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(backgroundScene, LoadSceneMode.Additive);
            if (op == null)
            {
                // 場景名稱打錯或忘記加進 Build Settings。標題畫面還是能用（只是背景是純色），
                // 所以不讓它崩掉，但一定要留訊息——不然只會看到「背景怎麼沒出現」卻查不到原因
                Debug.LogWarning($"標題背景載入失敗：找不到場景「{backgroundScene}」，" +
                                 "請確認名稱是否正確、是否已加入 Build Settings。標題畫面將維持純色背景。");
                yield break;
            }
            while (!op.isDone)
                yield return null;
        }

        SceneTransitionManager.Instance?.SetBackgroundScene(backgroundScene);

        TakeOverCamera();
    }

    void TakeOverCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("標題背景找不到主相機，背景不會平移。");
            return;
        }

        follow = cam.GetComponent<CameraFollow>();
        pixelPerfect = cam.GetComponent<UnityEngine.U2D.PixelPerfectCamera>();

        if (follow != null) follow.enabled = false;
        if (pixelPerfect != null) pixelPerfect.enabled = false;

        originalOrthoSize = cam.orthographicSize;
        cam.orthographicSize = viewSize;
        cam.transform.position = new Vector3(viewCenter.x, viewCenter.y, cam.transform.position.z);

        takenOver = true;
    }

    void Update()
    {
        if (!takenOver || cam == null) return;

        // 用正弦波而不是線性來回：兩端會自然減速再折返，不會有突然轉向的頓點。
        // 垂直用一半的頻率，兩軸相位不同步，軌跡才是緩慢的橢圓而不是來回直線。
        float t = Time.unscaledTime / Mathf.Max(panPeriod, 0.01f) * Mathf.PI * 2f;
        float x = viewCenter.x + Mathf.Sin(t) * panRange.x;
        float y = viewCenter.y + Mathf.Sin(t * 0.5f) * panRange.y;

        cam.transform.position = new Vector3(x, y, cam.transform.position.z);
    }

    void OnDestroy()
    {
        if (!takenOver) return;

        // 把相機還給遊戲。位置不用還原——接下來 CameraFollow 的 LateUpdate
        // 會自己把相機貼回玩家身上
        if (follow != null) follow.enabled = true;
        if (pixelPerfect != null) pixelPerfect.enabled = true;
        if (cam != null) cam.orthographicSize = originalOrthoSize;
    }
}
