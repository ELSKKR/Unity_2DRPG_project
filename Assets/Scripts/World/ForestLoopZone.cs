using UnityEngine;

// 森林邊界（鬼打牆）：玩家走進來時，把他往村子的方向送回去幾格。
// 第一次播全黑演出、在黑幕蓋滿時瞬移；之後直接瞬移再跳一行字。
// 回位點不是固定一個點，而是「從踏進來的位置往內退」，所以一整面邊界只要一個觸發區
public class ForestLoopZone : MonoBehaviour
{
    [Header("往村子的方向（西面邊界 = 往右 (1,0)）")]
    [SerializeField] private Vector2 inwardDirection = Vector2.right;

    [Header("往內退幾格")]
    [SerializeField] private float returnDistance = 4f;

    [Tooltip("落點被樹或牆擋住時，最多再往內多找幾格")]
    [SerializeField] private int maxExtraSteps = 8;

    [Header("第一次觸發：全黑演出（逐行淡入淡出）")]
    [TextArea(1, 3)]
    [SerializeField] private string[] firstTimeLines = { "……回過神來，你又回到原處。" };

    [Header("之後觸發：只跳一行")]
    [TextArea(1, 2)]
    [SerializeField] private string repeatLine = "……回過神來，你又回到原處。";

    // 所有邊界共用：只要有一面播過完整演出，之後哪一面都只跳一行
    public const string SeenFlag = "forest_loop_seen";

    // 跟洪水填充驗證用同一個半徑，兩邊對「站得下」的定義才一致
    const float PlayerRadius = 0.4f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (WorldStateManager.Instance == null) return;

        // 演出或對話進行中不處理（例如被送回的途中又擦到觸發區）
        if (RiftCutscene.Instance != null && RiftCutscene.Instance.IsPlaying) return;
        if (DialogManager.Instance != null && DialogManager.Instance.IsDialogActive) return;

        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        Vector2 target = FindReturnPosition(player.transform.position);

        bool firstTime = WorldStateManager.Instance.MarkEvent(SeenFlag);
        if (firstTime && RiftCutscene.Instance != null && firstTimeLines != null && firstTimeLines.Length > 0)
        {
            RiftCutscene.Instance.Play(firstTimeLines, null, onFullyBlack: () => Teleport(player, target));
            return;
        }

        Teleport(player, target);
        ShowRepeatLine();
    }

    // 從進入點往內退 returnDistance 格；落點站不下就一格一格往內找
    public Vector2 FindReturnPosition(Vector2 entry)
    {
        Vector2 dir = inwardDirection.normalized;
        Vector2 candidate = entry;

        for (int i = 0; i <= maxExtraSteps; i++)
        {
            candidate = entry + dir * (returnDistance + i);
            if (IsStandable(candidate)) return candidate;
        }

        Debug.LogWarning($"{name}：從 {entry} 往內找了 {maxExtraSteps} 格都站不下，玩家可能會卡在碰撞體裡");
        return candidate;
    }

    public static bool IsStandable(Vector2 p)
    {
        foreach (var c in Physics2D.OverlapCircleAll(p, PlayerRadius))
            if (!c.isTrigger) return false;
        return true;
    }

    void Teleport(PlayerController player, Vector2 target)
    {
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = target;
        }
        player.transform.position = new Vector3(target.x, target.y, player.transform.position.z);
    }

    void ShowRepeatLine()
    {
        if (string.IsNullOrEmpty(repeatLine) || DialogManager.Instance == null) return;
        DialogManager.Instance.StartDialog("", new DialogSegment { lines = new[] { repeatLine } });
    }
}
