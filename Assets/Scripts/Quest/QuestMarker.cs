using UnityEngine;
using TMPro;

// NPC 頭頂的任務提示圖示（世界空間，會跟角色一起被鏡頭縮放）
// 掛在 NPC 底下的子物件上，由 NPCDialog 依任務狀態呼叫 Show / Hide
public class QuestMarker : MonoBehaviour
{
    [Header("顯示元件（兩個都可留空，之後換美術素材時填 markerSprite 即可）")]
    [SerializeField] private TextMeshPro markerText;
    [SerializeField] private SpriteRenderer markerSprite;

    [Header("底襯（深色圓底，只跟著顯示/隱藏，不套用狀態顏色）")]
    [SerializeField] private SpriteRenderer background;

    [Header("上下浮動")]
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 2.5f;

    private Vector3 basePosition;
    private bool isShown;

    void Awake()
    {
        basePosition = transform.localPosition;

        if (markerText == null) markerText = GetComponent<TextMeshPro>();
        if (markerSprite == null) markerSprite = GetComponent<SpriteRenderer>();

        Hide();
    }

    void LateUpdate()
    {
        if (!isShown) return;

        float offset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = basePosition + new Vector3(0f, offset, 0f);
    }

    public void Show(string symbol, Color color)
    {
        isShown = true;

        if (markerText != null)
        {
            markerText.enabled = true;
            markerText.text = symbol;
            markerText.color = color;
        }

        if (markerSprite != null)
        {
            markerSprite.enabled = true;
            markerSprite.color = color;
        }

        if (background != null) background.enabled = true;
    }

    public void Hide()
    {
        isShown = false;
        transform.localPosition = basePosition;

        if (markerText != null) markerText.enabled = false;
        if (markerSprite != null) markerSprite.enabled = false;
        if (background != null) background.enabled = false;
    }
}
