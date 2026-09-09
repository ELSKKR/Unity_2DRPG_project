using UnityEngine;

// 每個「可探索場景」自己放一個，宣告相機不該拍出去的世界範圍。
//
// 為什麼要獨立成一個場景內的元件、而不是把數值寫在 CameraFollow 上：
// 相機跟玩家都住在常駐的 Persistent 場景裡，換地圖時它們不會被重載，
// 所以邊界值必須由「當下載入的那個場景」自己帶進來。
[ExecuteAlways]
public class CameraBounds : MonoBehaviour
{
    // 目前生效的邊界。場景被卸載時元件跟著消失，Current 會自動回到 null（＝不夾制）
    public static CameraBounds Current { get; private set; }

    [Header("相機可以拍到的世界範圍（左下 / 右上）")]
    [SerializeField] private Vector2 min = new Vector2(-60f, -55f);
    [SerializeField] private Vector2 max = new Vector2(60f, 55f);

    public Vector2 Min => min;
    public Vector2 Max => max;

    void OnEnable() => Current = this;

    void OnDisable()
    {
        if (Current == this) Current = null;
    }

    // 在 Scene 視窗畫出邊界方便調整
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.9f);
        Vector3 bl = new Vector3(min.x, min.y);
        Vector3 br = new Vector3(max.x, min.y);
        Vector3 tr = new Vector3(max.x, max.y);
        Vector3 tl = new Vector3(min.x, max.y);
        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }
}
