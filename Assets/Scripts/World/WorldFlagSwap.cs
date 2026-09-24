using UnityEngine;

// 世界旗標成立時切換一組物件（例如 NPC 從 A 地點移到 B 地點）。
// 旗標在玩家眼前成立：播一段黑幕演出，黑幕蓋滿時才切換，玩家看不到物件憑空消失。
// 場景載入時旗標早就成立（讀檔、進出房子）：直接套用結果，不重播演出。
// 這個元件要掛在「不會被自己關掉」的物件上：被 SetActive(false) 的物件收不到事件（鐵則三）
public class WorldFlagSwap : MonoBehaviour
{
    [Header("觸發的世界旗標")]
    [SerializeField] private string flagID;

    [Header("旗標成立後關掉／打開的物件")]
    [SerializeField] private GameObject[] hideWhenSet;
    [SerializeField] private GameObject[] showWhenSet;

    [Header("切換時的黑幕演出（留空 = 直接切換）")]
    [TextArea(1, 3)]
    [SerializeField] private string[] cutsceneLines;

    private bool applied;

    void Start()
    {
        if (string.IsNullOrEmpty(flagID))
            Debug.LogWarning($"{name}：沒有設定旗標 ID，物件永遠不會切換");

        foreach (var go in hideWhenSet) if (go != null && go.transform.IsChildOf(transform))
            Debug.LogWarning($"{name}：要關掉的 {go.name} 是這個元件的子物件，關掉時會連元件一起停掉");

        // 場景剛載入：旗標已經成立就直接套用，沒成立就維持場景檔裡的初始狀態
        if (WorldStateManager.Instance != null && WorldStateManager.Instance.HasEvent(flagID))
            Apply();

        if (WorldStateManager.Instance != null)
            WorldStateManager.Instance.OnWorldStateChanged += OnWorldStateChanged;
    }

    void OnDestroy()
    {
        if (WorldStateManager.Instance != null)
            WorldStateManager.Instance.OnWorldStateChanged -= OnWorldStateChanged;
    }

    void OnWorldStateChanged()
    {
        if (applied || !WorldStateManager.Instance.HasEvent(flagID)) return;

        // 演出系統正在播別的東西時 Play 會直接忽略，這時退而求其次直接切換，不能讓切換漏掉
        bool canPlay = RiftCutscene.Instance != null && !RiftCutscene.Instance.IsPlaying
            && cutsceneLines != null && cutsceneLines.Length > 0;

        if (canPlay)
        {
            applied = true;   // 先標記，避免演出期間其他旗標變動又觸發一次
            RiftCutscene.Instance.Play(cutsceneLines, null, onFullyBlack: Apply);
        }
        else
        {
            Apply();
        }
    }

    void Apply()
    {
        applied = true;
        foreach (var go in hideWhenSet) if (go != null) go.SetActive(false);
        foreach (var go in showWhenSet) if (go != null) go.SetActive(true);
    }
}
