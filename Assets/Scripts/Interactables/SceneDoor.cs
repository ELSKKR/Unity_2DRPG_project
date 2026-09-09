using UnityEngine;

public class SceneDoor : MonoBehaviour, IInteractable
{
    [Header("場景切換設定")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnID = "default";
    [SerializeField] private string interactionText = "進入";

    [Header("鑰匙需求（留空 = 不需要鑰匙）")]
    [SerializeField] private ItemData requiredKeyItem;
    [SerializeField] private string lockedMessage = "沒有鑰匙";

    [Header("音效（留空 = 用 AudioManager 的共用預設；只有這扇門想用不一樣的音效才需要設）")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip lockedSound;

    private Collider2D doorCollider;

    public string InteractionPrompt => interactionText;
    public bool CanInteract => true;

    // 門的碰撞範圍是手工調過的，collider 的 offset 不會反映到 transform 上，
    // 所以錨在 transform 會讓提示偏離門口 offset 那麼多。改用 collider 的實際中心，
    // 之後再怎麼調整範圍，提示都會自己跟著門口跑
    public Vector3 PromptWorldPosition =>
        doorCollider != null ? doorCollider.bounds.center : transform.position;

    void Awake()
    {
        doorCollider = GetComponent<Collider2D>();
    }

    public void Interact()
    {
        if (requiredKeyItem != null && !HasRequiredKey())
        {
            Debug.Log(lockedMessage);
            AudioClip locked = lockedSound != null ? lockedSound : AudioManager.Instance.DoorLockedSound;
            if (locked != null)
                AudioManager.Instance.PlaySFX(locked);
            return;
        }

        AudioClip open = openSound != null ? openSound : AudioManager.Instance.DoorOpenSound;
        if (open != null)
            AudioManager.Instance.PlaySFX(open);

        // 進出場景是自動存檔的時機點之一（另一個是存檔點），到了新場景才存，
        // 這樣存檔裡的場景名稱／座標才是門的另一邊，不是門這一側
        SceneTransitionManager.Instance.TransitionToScene(targetSceneName, targetSpawnID,
            () => SaveManager.Instance?.SaveToCurrentSlot());
    }

    private bool HasRequiredKey()
    {
        return Inventory.Instance != null && Inventory.Instance.GetItemCount(requiredKeyItem) > 0;
    }
}
