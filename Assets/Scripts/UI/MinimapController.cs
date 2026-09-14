using UnityEngine;

// 小地圖系統：右上角常駐的即時小地圖，以及按 M 開啟的全螢幕大地圖。
//
// 角落小地圖是即時相機，只在村莊裡顯示——玩家一進房子（不管哪一間）就收起來，
// 因為室內空間小、走沒兩步就到頭，常駐小地圖沒有意義，反而擋畫面。
// 大地圖則是一張事先烘焙好的靜態村莊全景圖（VillageMap_Baked.png），不是即時相機拍的——
// 玩家進房子時 Forest_Village 會整個被 Unload（見 SceneTransitionManager），
// 這時候如果大地圖還是靠相機即時拍村莊，畫面會是空的，因為村莊物件根本不在記憶體裡。
// 靜態圖不依賴任何場景是否載入，室內室外按 M 都能看到完整村莊。
// bigMapCenter / bigMapOrthoSize 保留下來純粹當「這張圖對應的世界座標範圍」，
// 換算玩家在圖上的位置用（見 UpdateBigMapMarker），不再拿去移動相機。
//
// 繼承 BookMenuPanel：除了 MenuPanelBase 那些既有規則（對話中不能開、跟背包互斥、
// Esc 關閉、鎖玩家移動、開關音效），還多拿到「有開闔動畫的面板」那一套。
// 素材包的地圖是一卷會展開的卷軸，跟書本是同一種演出，所以直接沿用同一個類別——
// 欄位名字雖然叫 bookFrame / pageContent，實際上就是「畫動畫的那張圖」與「動畫播完才顯示的內容」。
//
// 大地圖不跟著玩家，因此玩家在室內時標記位置沒有意義，會把標記藏起來。
public class MinimapController : BookMenuPanel
{
    [Header("渲染")]
    [Tooltip("專門畫小地圖的正交相機。targetTexture 要指向小地圖用的 RenderTexture")]
    [SerializeField] private Camera minimapCamera;

    [Header("角落小地圖")]
    [Tooltip("角落小地圖的整個容器，大地圖打開時會隱藏")]
    [SerializeField] private GameObject cornerRoot;
    [Tooltip("角落模式的正交尺寸：數字越大，看到的周圍範圍越廣")]
    [SerializeField] private float cornerOrthoSize = 18f;

    [Header("大地圖（靜態圖，見檔頭註解）")]
    [Tooltip("VillageMap_Baked.png 這張烘焙圖對應的世界座標中心，換算玩家標記位置用")]
    [SerializeField] private Vector2 bigMapCenter = new Vector2(3.3f, 4.5f);
    [Tooltip("烘焙這張圖時用的正交尺寸（半高），換算玩家標記位置用")]
    [SerializeField] private float bigMapOrthoSize = 68f;
    [Tooltip("大地圖的圖面本身，用來把世界座標換算成 UI 座標")]
    [SerializeField] private RectTransform bigMapImage;
    [Tooltip("玩家在大地圖上的位置標記（要是 bigMapImage 的子物件、錨點置中）")]
    [SerializeField] private RectTransform bigMapPlayerMarker;

    [Header("場景名稱")]
    [Tooltip("大地圖畫的是這個場景；玩家不在這裡時不顯示位置標記")]
    [SerializeField] private string villageSceneName = "Forest_Village";
    [Tooltip("在標題畫面時整個小地圖都要隱藏（那裡沒有世界可以畫）")]
    [SerializeField] private string titleSceneName = "TitleScreen";

    private Transform playerTf;
    private bool warnedNoPlayer;

    // 玩家角色在標題畫面期間是「關閉」狀態（BootstrapLoader 關的），
    // 用預設的 FindFirstObjectByType 找不到會安靜地不跟隨、也不會有任何錯誤訊息，
    // 所以一定要含 inactive。這個坑專案裡已經踩過一次（見 SceneTransitionManager.PlacePlayerAtSpawn）
    private Transform PlayerTransform
    {
        get
        {
            if (playerTf != null) return playerTf;

            var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (pc != null)
            {
                playerTf = pc.transform;
            }
            else if (!warnedNoPlayer)
            {
                warnedNoPlayer = true;
                Debug.LogWarning("小地圖找不到 PlayerController，相機不會跟隨玩家。");
            }
            return playerTf;
        }
    }

    protected override void Start()
    {
        base.Start();   // 把大地圖面板關起來

        if (minimapCamera == null)
        {
            Debug.LogWarning("MinimapController 沒有指定 minimapCamera，小地圖不會運作。");
            return;
        }
        ApplyCornerCamera();
    }

    void Update()
    {
        if (KeyBindings.GetKeyDown(KeyBindings.GameAction.ToggleMap))
        {
            // 已經開著就關掉；沒開才開啟。Open() 內部會擋掉「對話進行中」的情況，
            // 這裡不用重複判斷
            if (IsOpen) Close();
            else if (InGameplayScene()) Open();
            return;
        }

        if (IsOpen)
        {
            UpdateBigMapMarker();
        }
        else
        {
            // 只在村莊裡顯示；標題畫面、室內都收起來
            if (cornerRoot != null) cornerRoot.SetActive(InVillage());
            FollowPlayer();
        }
    }

    // 目前算不算「在可以顯示地圖的遊玩場景裡」。標題畫面與還沒載入任何場景時都不算
    bool InGameplayScene()
    {
        var stm = SceneTransitionManager.Instance;
        if (stm == null) return false;

        string scene = stm.CurrentGameplayScene;
        return !string.IsNullOrEmpty(scene) && scene != titleSceneName;
    }

    // 玩家目前在不在村莊本體（不是室內、也不是標題畫面）。
    // 角落小地圖的顯示、跟大地圖上玩家標記的顯示都是同一個判斷——兩者都只在村莊裡有意義。
    bool InVillage()
    {
        return SceneTransitionManager.Instance != null
            && SceneTransitionManager.Instance.CurrentGameplayScene == villageSceneName;
    }

    void FollowPlayer()
    {
        if (minimapCamera == null) return;

        Transform tf = PlayerTransform;
        if (tf == null) return;

        Vector3 camPos = minimapCamera.transform.position;
        minimapCamera.transform.position = new Vector3(tf.position.x, tf.position.y, camPos.z);
    }

    void UpdateBigMapMarker()
    {
        if (bigMapPlayerMarker == null || bigMapImage == null) return;

        // 大地圖畫的是村莊。玩家在室內時，他在村莊地圖上的位置沒有意義，藏起來比亂指一個位置好
        bool inVillage = InVillage();

        bigMapPlayerMarker.gameObject.SetActive(inVillage);
        if (!inVillage) return;

        Transform tf = PlayerTransform;
        if (tf == null) return;

        // 世界座標 → 大地圖 UI 座標。
        // 這張圖是照 bigMapCenter/bigMapOrthoSize 烘焙出來的，圖面寬高比理當跟烘焙當下
        // 的圖面比例一致，換算比例直接用 bigMapImage 目前的寬高比就好，不用再靠相機的 aspect。
        float halfHeight = bigMapOrthoSize;
        float halfWidth = bigMapOrthoSize * (bigMapImage.rect.width / bigMapImage.rect.height);

        Vector2 offset = (Vector2)tf.position - bigMapCenter;
        Vector2 normalized = new Vector2(offset.x / halfWidth, offset.y / halfHeight) * 0.5f;

        Rect rect = bigMapImage.rect;
        bigMapPlayerMarker.anchoredPosition = new Vector2(normalized.x * rect.width,
                                                          normalized.y * rect.height);
    }

    protected override void OnOpened()
    {
        if (cornerRoot != null) cornerRoot.SetActive(false);
        UpdateBigMapMarker();   // 開啟當下就先擺好標記，不要等下一幀才跳到正確位置
    }

    protected override void OnClosed()
    {
        if (cornerRoot != null) cornerRoot.SetActive(InVillage());
    }

    void ApplyCornerCamera()
    {
        if (minimapCamera == null) return;

        minimapCamera.ResetAspect();   // 回到 RenderTexture 的原生比例（正方形），配正方形的角落框
        minimapCamera.orthographicSize = cornerOrthoSize;
    }
}
