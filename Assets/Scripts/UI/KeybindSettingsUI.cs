using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 設定視窗的「鍵位」分頁。列出所有可改的動作，點按鍵進入等待輸入狀態，
// 按下新鍵就套用（衝突時 KeyBindings.Rebind 會自動把兩個動作的鍵對調）。
public class KeybindSettingsUI : MonoBehaviour
{
    [SerializeField] private Transform rowContainer;
    [SerializeField] private GameObject rowPrefab;
    [SerializeField] private Button resetButton;
    [SerializeField] private TextMeshProUGUI hintText;   // 「已與XX對調」之類的提示

    private readonly List<KeybindRowUI> rows = new List<KeybindRowUI>();
    private KeybindRowUI capturing;

    // 場景裡會同時存在兩份鍵位頁（標題畫面的舊設定面板、遊戲中的書本頁），
    // UIPanelManager 只需要知道「現在有沒有人在等玩家按鍵」，不必認得是哪一份。
    // 兩份不可能同時顯示（面板本來就是互斥的），所以一個靜態指標就夠
    static KeybindSettingsUI activeCapturer;
    public static bool AnyCapturing => activeCapturer != null;

    // 這些鍵不開放綁定：Esc 在等待輸入時是「取消」，滑鼠鍵在這個遊戲沒有對應操作
    static readonly KeyCode[] Forbidden =
    {
        KeyCode.Escape,
        KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Mouse2,
        KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5, KeyCode.Mouse6,
    };

    void Awake()
    {
        BuildRows();
        if (resetButton != null) resetButton.onClick.AddListener(ResetAll);
    }

    void OnEnable()
    {
        CancelCapture();
        RefreshAll();
        SetHint("");
    }

    void OnDisable() => CancelCapture();

    void BuildRows()
    {
        foreach (var pair in KeyBindings.RebindableActions)
        {
            var obj = Instantiate(rowPrefab, rowContainer);
            obj.SetActive(true);
            var row = obj.GetComponent<KeybindRowUI>();
            row.Setup(pair.action, pair.label, this);
            rows.Add(row);
        }
    }

    public void BeginCapture(KeybindRowUI row)
    {
        CancelCapture();
        capturing = row;
        activeCapturer = this;
        row.ShowCapturing();
        SetHint("按下要指定的按鍵，Esc 取消");
    }

    void CancelCapture()
    {
        if (activeCapturer == this) activeCapturer = null;
        if (capturing == null) return;
        capturing.Refresh();
        capturing = null;
    }

    void Update()
    {
        if (capturing == null) return;

        // Esc 在等待輸入時的意思是「取消這次改鍵」，不是關掉設定視窗，
        // 所以這裡攔下來自己處理（UIPanelManager 也監聽 Esc，見它的 IsRebinding 判斷）
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelCapture();
            SetHint("已取消");
            return;
        }

        if (!Input.anyKeyDown) return;

        foreach (KeyCode code in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(code)) continue;
            if (System.Array.IndexOf(Forbidden, code) >= 0) continue;

            var target = capturing;
            capturing = null;
            activeCapturer = null;

            var swapped = KeyBindings.Rebind(target.Action, code);
            RefreshAll();

            if (swapped.HasValue)
            {
                string swappedLabel = LabelOf(swapped.Value);
                SetHint($"這顆鍵原本是「{swappedLabel}」，已經和它對調");
            }
            else SetHint("");
            return;
        }
    }

    // 等待輸入時要擋住 Esc 關視窗（UIPanelManager 會查上面那個靜態版本）
    public bool IsCapturing => capturing != null;

    void ResetAll()
    {
        CancelCapture();
        KeyBindings.ResetAllToDefault();
        RefreshAll();
        SetHint("已還原為預設值");
    }

    void RefreshAll()
    {
        foreach (var r in rows) r.Refresh();
    }

    void SetHint(string msg)
    {
        if (hintText != null) hintText.text = msg;
    }

    static string LabelOf(KeyBindings.GameAction action)
    {
        foreach (var pair in KeyBindings.RebindableActions)
            if (pair.action == action) return pair.label;
        return action.ToString();
    }
}
