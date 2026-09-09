using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

// 情報清單裡的其中一列。陽春版：只顯示標題，選了才在旁邊詳情欄顯示內文。
public class IntelEntryUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private GameObject highlight;   // 選取時亮起（比照任務清單）

    private int intelIndex;
    private IntelUI intelUI;

    public void Setup(IntelData intel, int index, IntelUI ui)
    {
        intelIndex = index;
        intelUI = ui;

        titleText.text = intel.title;

        SetHighlighted(false);
    }

    public void SetHighlighted(bool state)
    {
        if (highlight != null)
            highlight.SetActive(state);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        intelUI?.SelectIntel(intelIndex);
    }
}
