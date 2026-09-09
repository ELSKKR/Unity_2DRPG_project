using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    private Camera cam;

    void Awake() => cam = GetComponent<Camera>();

    void LateUpdate()
    {
        if (target == null) return;

        float x = target.position.x;
        float y = target.position.y;

        // 邊界由當下載入的場景自己提供（CameraBounds），沒有的話就不夾制
        var bounds = CameraBounds.Current;
        if (bounds != null && cam != null)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            x = ClampAxis(x, bounds.Min.x, bounds.Max.x, halfWidth);
            y = ClampAxis(y, bounds.Min.y, bounds.Max.y, halfHeight);
        }

        transform.position = new Vector3(x, y, -10f);
    }

    // 地圖在這個軸上比畫面還窄時，min 會大於 max，直接丟給 Mathf.Clamp 會得到亂七八糟的值，
    // 這種情況正確的行為是「把相機釘在地圖中心」，讓兩側均勻露出外面
    static float ClampAxis(float value, float boundsMin, float boundsMax, float halfSize)
    {
        float min = boundsMin + halfSize;
        float max = boundsMax - halfSize;

        if (min > max) return (boundsMin + boundsMax) * 0.5f;
        return Mathf.Clamp(value, min, max);
    }
}
