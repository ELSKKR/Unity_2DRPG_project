using UnityEngine;

// 手動存檔點：站到旁邊按互動鍵就存檔。跟進出場景的自動存檔是同一套
// SaveManager.SaveToCurrentSlot()，差別只在觸發時機——這個給玩家自己決定要不要存。
public class SavePoint : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionText = "儲存進度";

    [Header("音效（留空 = 不播放）")]
    [SerializeField] private AudioClip saveSound;

    public string InteractionPrompt => interactionText;
    public Vector3 PromptWorldPosition => transform.position;
    public bool CanInteract => SaveManager.Instance != null && SaveManager.Instance.CurrentSlot >= 0;

    public void Interact()
    {
        SaveManager.Instance?.SaveToCurrentSlot();

        if (saveSound != null)
            AudioManager.Instance.PlaySFX(saveSound);

        Debug.Log("已儲存進度");
    }
}
