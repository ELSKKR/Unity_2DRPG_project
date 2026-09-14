using UnityEngine;

// 小地圖系統：右上角常駐的即時小地圖，以及按 M 開啟的全螢幕大地圖。
//
// 兩種模式共用同一台相機與同一張 RenderTexture——切換時只改正交尺寸與相機位置。
// 之所以不做成兩套，是因為兩者永遠不會同時顯示（大地圖開著時角落那個會隱藏），
// 多養一台相機只是讓兩邊的設定有機會不同步，沒有換到任何好處。
//
// 繼承 BookMenuPanel：除了 MenuPanelBase 那些既有規則（對話中不能開、跟背包互斥、
// Esc 關閉、鎖玩家移動、開關音效），還多拿到「有開闔動畫的面板」那一套。
// 素材包的地圖是一卷會展開的卷軸，跟書本是同一種演出，所以直接沿用同一個類別——
// 欄位名字雖然叫 bookFrame / pageContent，實際上就是「畫動畫的那張圖」與「動畫播完才顯示的內容」。
//
// 角落小地圖跟著玩家跑，所以在任何場景都能用（進室內就顯示室內）；
// 大地圖則是固定框住整個村莊的「村莊地圖」，不跟著玩家，因此玩家在室內時
// 標記位置沒有意義，會把標記藏起來。
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

    [Header("大地圖")]
    [Tooltip("大地圖框住的世界座標中心（依村莊實際內容範圍量出來的）")]
    [SerializeField] private Vector2 bigMapCenter = new Vector2(3.3f, 4.5f);
    [Tooltip("大地圖的正交尺寸：要能把整個村莊含邊界密林都框進去")]
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
            // 標題畫面沒有世界可以畫，整個角落小地圖要收起來
            if (cornerRoot != null) cornerRoot.SetActive(InGameplayScene());
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
        if (bigMapPlayerMarker == null || bigMapImage == null || minimapCamera == null) return;

        // 大地圖畫的是村莊。玩家在室內時，他在村莊地圖上的位置沒有意義，藏起來比亂指一個位置好
        bool inVillage = SceneTransitionManager.Instance != null
                         && SceneTransitionManager.Instance.CurrentGameplayScene == villageSceneName;

        bigMapPlayerMarker.gameObject.SetActive(inVillage);
        if (!inVillage) return;

        Transform tf = PlayerTransform;
        if (tf == null) return;

        // 世界座標 → 大地圖 UI 座標。
        // 相機看得到的範圍是「高 = 2×正交尺寸、寬 = 高×aspect」，
        // 先把玩家相對於地圖中心的偏移換成 -0.5～0.5 的比例，再乘上圖面實際像素尺寸。
        float halfHeight = bigMapOrthoSize;
        float halfWidth = bigMapOrthoSize * minimapCamera.aspect;

        Vector2 offset = (Vector2)tf.position - bigMapCenter;
        Vector2 normalized = new Vector2(offset.x / halfWidth, offset.y / halfHeight) * 0.5f;

        Rect rect = bigMapImage.rect;
        bigMapPlayerMarker.anchoredPosition = new Vector2(normalized.x * rect.width,
                                                          normalized.y * rect.height);
    }

    protected override void OnOpened()
    {
        if (cornerRoot != null) cornerRoot.SetActive(false);
        ApplyBigMapCamera();
        UpdateBigMapMarker();   // 開啟當下就先擺好標記，不要等下一幀才跳到正確位置
    }

    protected override void OnClosed()
    {
        ApplyCornerCamera();
        if (cornerRoot != null) cornerRoot.SetActive(InGameplayScene());
    }

    void ApplyCornerCamera()
    {
        if (minimapCamera == null) return;

        minimapCamera.ResetAspect();   // 回到 RenderTexture 的原生比例（正方形），配正方形的角落框
        minimapCamera.orthographicSize = cornerOrthoSize;
    }

    void ApplyBigMapCamera()
    {
        if (minimapCamera == null) return;

        // 村莊的實際內容不是正方形（密林牆 + 河道約 115.5×123.6），用正方形取景的話
        // 多出來的寬度會露出密林牆外的空草地。這裡直接把相機 aspect 對齊圖面的比例：
        // RenderTexture 仍是正方形，渲染時畫面會被水平壓縮存進去，再顯示到同比例的圖面上
        // 剛好還原，四邊都能貼齊邊界。
        // （標記換算用的是 minimapCamera.aspect，所以這裡改完座標換算會自動跟著對）
        if (bigMapImage != null)
        {
            Rect r = bigMapImage.rect;
            if (r.width > 0f && r.height > 0f)
                minimapCamera.aspect = r.width / r.height;
        }

        minimapCamera.orthographicSize = bigMapOrthoSize;
        Vector3 camPos = minimapCamera.transform.position;
        minimapCamera.transform.position = new Vector3(bigMapCenter.x, bigMapCenter.y, camPos.z);
    }
}
