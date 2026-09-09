using UnityEngine;

// 「獲得道具」的飄字提示。訂閱 Inventory.OnItemAdded，所以不管是從地上撿的、
// 還是任務獎勵給的，都會有一致的回饋，不用逐個來源去接。
//
// 跟 NotificationToast 是兩條不同的管道：那個是左上角有黑底的系統訊息（任務/情報），
// 這個是現場的輕量回饋——只有文字、會飄會淡、不排隊、不搶注意力。
public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private Transform container;          // 生成在哪個 Canvas 底下

    [Header("沒有指定世界座標時，提示浮在玩家頭上多高")]
    [SerializeField] private float playerHeadOffset = 1.2f;

    [Header("連續獲得時的錯開")]
    [Tooltip("這段時間內再次出現，就往上多墊一層，避免文字重疊")]
    [SerializeField] private float stackWindow = 0.8f;
    [SerializeField] private float stackStep = 0.45f;

    private Transform player;
    private float lastSpawnTime = -999f;
    private int stackCount;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnItemAdded += HandleItemAdded;
    }

    void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnItemAdded -= HandleItemAdded;
    }

    void HandleItemAdded(ItemData item, int amount, Vector3? worldPosition)
    {
        if (item == null) return;

        // 數量 1 就不寫「×1」，看起來比較乾淨
        string message = amount > 1 ? $"{item.itemName} ×{amount}" : item.itemName;
        Show(message, worldPosition);
    }

    public void Show(string message, Vector3? worldPosition)
    {
        if (floatingTextPrefab == null || container == null) return;

        Vector3 pos = worldPosition ?? GetPlayerHeadPosition();

        // 短時間內連續出現就往上疊，不然會糊成一團
        stackCount = (Time.time - lastSpawnTime < stackWindow) ? stackCount + 1 : 0;
        lastSpawnTime = Time.time;

        var obj = Instantiate(floatingTextPrefab, container);
        obj.GetComponent<FloatingText>().Play(message, pos, stackCount * stackStep);
    }

    Vector3 GetPlayerHeadPosition()
    {
        // 玩家在常駐場景、而且開場是關閉狀態，所以要含 inactive 找、找到再快取
        if (player == null)
        {
            var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (pc != null) player = pc.transform;
        }
        return player != null ? player.position + Vector3.up * playerHeadOffset : Vector3.zero;
    }
}
