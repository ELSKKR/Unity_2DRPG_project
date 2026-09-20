using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 情報清單裡的其中一列。版面比照 QuestEntryUI：整列只顯示標題，
// 用旁邊的「詳情」按鈕觸發選取，不是整列都能點。
public class IntelEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private GameObject highlight;   // 選取時亮起（比照任務清單）
    [SerializeField] private Button viewButton;       // 「詳情」按鈕，改當「查看情報」

    private int intelIndex;
    private IntelUI intelUI;

    public void Setup(IntelData intel, int index, IntelUI ui)
    {
        intelIndex = index;
        intelUI = ui;

        titleText.text = intel.title;

        // RefreshUI 每次都整批 Destroy 重新 Instantiate，不會重複訂閱
        if (viewButton != null)
            viewButton.onClick.AddListener(() => intelUI.SelectIntel(intelIndex));

        SetHighlighted(false);
    }

    public void SetHighlighted(bool state)
    {
        if (highlight != null)
            highlight.SetActive(state);
    }
}
