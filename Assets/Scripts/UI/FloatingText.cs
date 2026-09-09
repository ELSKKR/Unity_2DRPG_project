using System.Collections;
using UnityEngine;
using TMPro;

// 單一則飄字：釘在一個世界座標上，一邊往上飄一邊淡出，播完自己銷毀。
// 刻意沒有背景圖（只有文字），所以靠 TMP 的描邊來確保在草地之類的亮背景上讀得到。
[RequireComponent(typeof(CanvasGroup))]
public class FloatingText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    [Header("動態")]
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private float riseWorldUnits = 0.8f;   // 整段期間往上飄多少「世界單位」
    [Tooltip("一開始就往上抬多少，避免文字壓在角色或物品的圖上")]
    [SerializeField] private float startWorldOffset = 1.1f;
    [SerializeField] private AnimationCurve fade = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    private CanvasGroup group;
    private Camera cam;
    private Vector3 worldAnchor;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        cam = Camera.main;
    }

    // extraRise：短時間內連續獲得道具時往上多墊一段，避免文字疊在一起
    public void Play(string message, Vector3 worldPosition, float extraRise)
    {
        if (label != null) label.text = message;
        worldAnchor = worldPosition + Vector3.up * (startWorldOffset + extraRise);
        gameObject.SetActive(true);
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);

            // 每幀重新換算螢幕座標：相機會跟著玩家移動，只算一次的話文字會黏在畫面上不動
            if (cam == null) cam = Camera.main;
            if (cam != null)
                transform.position = cam.WorldToScreenPoint(worldAnchor + Vector3.up * (riseWorldUnits * k));

            group.alpha = fade.Evaluate(k);
            yield return null;
        }
        Destroy(gameObject);
    }
}
