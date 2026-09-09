using UnityEngine;

// 指定任務完成後，才會出現在 NPC 身上的隨身物件（例如亞爾拿回佩劍後就一直帶著它）。
// 掛在 NPC 底下的道具子物件上：執行時先把自己藏起來，任務完成才顯示。
//
// 刻意「不」加 [ExecuteAlways]：編輯器裡要看得到這個道具才好調位置，
// 藏起來的行為只在 Play mode 生效。
[RequireComponent(typeof(SpriteRenderer))]
public class QuestHeldItem : MonoBehaviour
{
    [Header("這條任務完成後才顯示")]
    [SerializeField] private QuestData requiredQuest;

    [Header("本體的 SpriteRenderer（跟著 idle 動畫上下起伏；留空 = 不同步）")]
    [SerializeField] private SpriteRenderer body;

    [Tooltip("本體最高的那一格 idle 圖有幾像素高。目前這格比它矮多少，道具就跟著往下移多少。" +
             "設太小會自動往上修正，不用算得很精準。")]
    [SerializeField] private int bodyTallestFramePixels = 29;

    [Tooltip("相對本體的 sortingOrder 差：負的畫在本體後面，正的畫在前面。")]
    [SerializeField] private int sortingOrderOffset = -2;

    private SpriteRenderer sr;
    private Vector3 restPosition;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // 位置在編輯器裡擺好，執行期只在這個基準上做動畫補正，不能反過來被補正值污染
        restPosition = transform.localPosition;

        // 先套一次再進 Start，避免任務還沒完成時道具閃現一格
        Apply();
    }

    void Start()
    {
        // 任務是在別的 NPC 身上完成的（亞爾自己收劍），所以不能等玩家來對話才更新，
        // 要跟著 QuestManager 的變更事件走
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestsChanged += Apply;

        Apply();
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestsChanged -= Apply;
    }

    void Apply()
    {
        sr.enabled = requiredQuest != null
                     && QuestManager.Instance != null
                     && QuestManager.Instance.IsQuestCompleted(requiredQuest);
    }

    // 這組 idle 動畫是「腳踩在原地不動、上半身整個往下縮」——實測手臂位移量
    // 跟整張 sprite 的高度差完全一致（29/27/27/28 對應 0/-2/-2/-1 像素），
    // 所以直接用高度差就能推出這一格該往下多少，不必另外開動畫曲線，
    // 之後換掉 idle 動畫也會自動跟上。
    void LateUpdate()
    {
        if (body == null || body.sprite == null) return;

        // 排序直接跟著本體走，不要自己掛 SpriteSortingByY：道具通常掛在比本體高的地方，
        // 用自己的世界 Y 去算會被當成「站在本體後方好幾格的東西」，
        // 剛好有人站在那個位置時就會穿幫蓋住道具
        sr.sortingLayerID = body.sortingLayerID;
        sr.sortingOrder = body.sortingOrder + sortingOrderOffset;

        // 遇到更高的一格就把基準拉高，省得手動填精確值（填太小只會第一輪短暫偏移）
        int height = Mathf.RoundToInt(body.sprite.rect.height);
        if (height > bodyTallestFramePixels)
            bodyTallestFramePixels = height;

        float dropPixels = height - bodyTallestFramePixels;
        transform.localPosition = restPosition + new Vector3(0f, dropPixels / body.sprite.pixelsPerUnit, 0f);
    }
}
