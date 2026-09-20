using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 鍵位設定頁裡的一列：左邊動作名稱、右邊一顆顯示目前按鍵的按鈕。
// 按鈕按下去會請 KeybindSettingsUI 進入「等待輸入」狀態。
public class KeybindRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI actionLabel;
    [SerializeField] private Button keyButton;
    [SerializeField] private TextMeshProUGUI keyLabel;
    [Tooltip("等待輸入時常駐亮起，比照任務清單「查看詳情」按鈕同一套 highlight")]
    [SerializeField] private GameObject highlight;

    private KeyBindings.GameAction action;
    private KeybindSettingsUI owner;

    public KeyBindings.GameAction Action => action;

    public void Setup(KeyBindings.GameAction action, string label, KeybindSettingsUI owner)
    {
        this.action = action;
        this.owner = owner;
        actionLabel.text = label;
        keyButton.onClick.AddListener(() => this.owner.BeginCapture(this));
        Refresh();
    }

    public void Refresh()
    {
        keyLabel.text = KeyBindings.Get(action).ToString();
        if (highlight != null) highlight.SetActive(false);
    }

    // 等待玩家按鍵時顯示提示，讓玩家知道現在該按了
    public void ShowCapturing()
    {
        keyLabel.text = "按下新按鍵…";
        if (highlight != null) highlight.SetActive(true);
    }
}
