using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 素材包的選單範例裡，被選中的按鈕左右兩側會各出現一個箭頭指向它。
// 這支就是那對箭頭：掛在放按鈕的容器上，自己找出「現在是哪一顆」再飛過去。
//
// 為什麼用輪詢而不是事件：事件版本要在每一顆按鈕上都掛一個回報用的元件，
// 多一層要維護的接線，而且漏掛一顆的症狀是「箭頭偶爾不會動」——很難查。
// 選單一頁最多六顆按鈕，每幀做六次矩形判定的成本可以直接忽略。
public class MenuChevrons : MonoBehaviour
{
    [Header("兩側的箭頭（右邊那個請把 Scale X 設成 -1 來鏡射）")]
    [SerializeField] private RectTransform leftChevron;
    [SerializeField] private RectTransform rightChevron;

    [Tooltip("箭頭和按鈕邊緣之間留多寬")]
    [SerializeField] private float gap = 8f;

    [Tooltip("留空 = 自動抓子物件裡所有的按鈕")]
    [SerializeField] private Selectable[] targets;

    [Tooltip("沒有任何按鈕被指到時，是否退回指著第一顆")]
    [SerializeField] private bool fallbackToFirst = true;

    RectTransform self;

    void Awake()
    {
        self = (RectTransform)transform;
        if (targets == null || targets.Length == 0)
            targets = GetComponentsInChildren<Selectable>(true);
    }

    void OnEnable()
    {
        // 面板重新打開時，滑鼠可能停在上一次的位置，先擺好再顯示，避免第一幀閃到舊座標
        LateUpdate();
    }

    void LateUpdate()
    {
        if (leftChevron == null || rightChevron == null) return;

        RectTransform target = FindTarget();
        bool show = target != null;
        if (leftChevron.gameObject.activeSelf != show) leftChevron.gameObject.SetActive(show);
        if (rightChevron.gameObject.activeSelf != show) rightChevron.gameObject.SetActive(show);
        if (!show) return;

        // 用世界座標換算，這樣按鈕和箭頭不必是同一層、錨點也不必一樣
        float half = target.rect.width * 0.5f;
        Vector3 leftEdge = self.InverseTransformPoint(target.TransformPoint(new Vector3(-half, 0f, 0f)));
        Vector3 rightEdge = self.InverseTransformPoint(target.TransformPoint(new Vector3(half, 0f, 0f)));

        leftChevron.localPosition = new Vector3(
            leftEdge.x - gap - leftChevron.rect.width * 0.5f, leftEdge.y, 0f);
        rightChevron.localPosition = new Vector3(
            rightEdge.x + gap + rightChevron.rect.width * 0.5f, rightEdge.y, 0f);
    }

    RectTransform FindTarget()
    {
        // 專案其他地方（KeyBindings、PlayerController）都走舊版 Input，這裡跟著走，
        // 省得同一個專案裡兩套輸入 API 混用
        Vector2 pointer = Input.mousePosition;
        Camera cam = CanvasCamera();

        RectTransform first = null;
        foreach (Selectable s in targets)
        {
            if (s == null || !s.IsInteractable() || !s.gameObject.activeInHierarchy) continue;
            RectTransform rt = (RectTransform)s.transform;
            if (first == null) first = rt;
            if (RectTransformUtility.RectangleContainsScreenPoint(rt, pointer, cam))
                return rt;
        }

        // 滑鼠不在任何按鈕上時，改看鍵盤／手把選到的那顆
        GameObject selected = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject : null;
        if (selected != null)
        {
            foreach (Selectable s in targets)
                if (s != null && s.gameObject == selected && s.IsInteractable())
                    return (RectTransform)s.transform;
        }

        return fallbackToFirst ? first : null;
    }

    Camera CanvasCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        // Overlay 模式要傳 null，傳相機進去算出來的座標會是錯的
        return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }
}
