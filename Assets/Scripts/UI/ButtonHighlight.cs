using UnityEngine;
using UnityEngine.EventSystems;

// 書本 UI 按鈕的點擊提示：按下時亮起 highlight 子物件。
// 滑鼠移入的「變暗」提示交給 Button 自己的 ColorTint（Highlighted 顏色），不需要額外程式。
public class ButtonHighlight : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private GameObject highlight;
    [Tooltip("勾選：按下後常駐亮著，不會因為放開滑鼠熄滅（用在按下去畫面就會切走的一次性按鈕）")]
    [SerializeField] private bool sticky;

    public void OnPointerDown(PointerEventData eventData) => SetState(true);
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!sticky) SetState(false);
    }

    void SetState(bool state)
    {
        if (highlight != null)
            highlight.SetActive(state);
    }
}
