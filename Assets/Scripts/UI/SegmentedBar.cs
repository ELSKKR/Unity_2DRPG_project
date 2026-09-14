using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 素材包的音量條：14 個各自獨立的格子，不是一條會伸縮的長條。
//
// 為什麼不用內建的 Slider：Slider 是靠 fillRect 把一張圖「裁短」來表示進度，
// 但這組素材每一格都是完整的一張圖，而且頭尾兩格跟中間那些長得不一樣
// （頭尾 12 寬、中間 9 寬），裁短會把某一格切成半個。改成整格開關就沒這問題，
// 順便也讓音量變成 14 段的離散值——像素風的 UI 本來就不適合無段調整。
//
// 格子之間是重疊擺放的（間距 11，但每格的圖是 32x32），這是素材本身的設計：
// 每張圖在自己的 32x32 畫布裡是偏移的，照間距排才會接起來。
public class SegmentedBar : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Tooltip("由左到右的格子。第一格與最後一格會用頭尾專用的圖")]
    [SerializeField] private Image[] cells;

    [Header("格子圖（亮起 / 熄滅）")]
    [SerializeField] private Sprite filledStart;
    [SerializeField] private Sprite filledMiddle;
    [SerializeField] private Sprite filledEnd;
    [SerializeField] private Sprite emptyStart;
    [SerializeField] private Sprite emptyMiddle;
    [SerializeField] private Sprite emptyEnd;

    [Tooltip("指在「目前最後一格亮著的格子」下面的箭頭。留空 = 不顯示")]
    [SerializeField] private RectTransform marker;

    [Tooltip("格子之間的間距，跟擺放時用的數字要一致")]
    [SerializeField] private float cellPitch = 11f;
    [Tooltip("第一格的圖在自己畫布裡往右縮排了幾格像素，點擊換算要扣掉")]
    [SerializeField] private float firstCellInset = 8f;
    [Tooltip("箭頭要對齊格子中心用的偏移。箭頭的圖在自己的 16x16 畫布裡也是偏的，不是 0")]
    [SerializeField] private float markerOffset = 8f;

    public event Action<float> OnValueChanged;

    private int filled;

    public int SegmentCount => cells != null ? cells.Length : 0;

    // 0~1。設進來的值會被量化成格數，讀回去的也是量化後的值
    public float Value
    {
        get => SegmentCount > 0 ? (float)filled / SegmentCount : 0f;
        set => SetFilled(Mathf.RoundToInt(Mathf.Clamp01(value) * SegmentCount), notify: false);
    }

    void SetFilled(int count, bool notify)
    {
        if (cells == null || cells.Length == 0) return;

        count = Mathf.Clamp(count, 0, cells.Length);
        bool changed = count != filled;
        filled = count;
        Redraw();

        if (notify && changed) OnValueChanged?.Invoke(Value);
    }

    void Redraw()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == null) continue;
            bool on = i < filled;
            bool first = i == 0;
            bool last = i == cells.Length - 1;

            cells[i].sprite = on
                ? (first ? filledStart : last ? filledEnd : filledMiddle)
                : (first ? emptyStart : last ? emptyEnd : emptyMiddle);
        }

        if (marker == null) return;

        // 音量 0 時沒有任何一格亮著，箭頭沒有可以指的地方，就整個收起來
        marker.gameObject.SetActive(filled > 0);
        if (filled > 0)
        {
            var p = marker.anchoredPosition;
            p.x = (filled - 1) * cellPitch + markerOffset;
            marker.anchoredPosition = p;
        }
    }

    public void OnPointerDown(PointerEventData e) => Apply(e);
    public void OnDrag(PointerEventData e) => Apply(e);

    void Apply(PointerEventData e)
    {
        if (cells == null || cells.Length == 0) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform, e.position, e.pressEventCamera, out Vector2 local))
            return;

        // local 是以 pivot 為原點，換成「距離左邊界幾格」
        var rect = ((RectTransform)transform).rect;
        float fromLeft = local.x - rect.xMin - firstCellInset;

        // +1：點在第 0 格上代表「亮一格」。往左拖出格子外就是靜音（0 格）
        SetFilled(Mathf.FloorToInt(fromLeft / cellPitch) + 1, notify: true);
    }
}
