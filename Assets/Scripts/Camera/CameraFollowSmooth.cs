using UnityEngine;

public class CameraFollowSmooth : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 8f; // 越小延遲越明顯，建議 5~12

    void Start()
    {
        if (target == null) return;

        // 讓玩家位置在物理幀（50Hz）之間連續插值
        // 沒有這行，Lerp 的目標本身是跳的，段落感消不掉
        if (target.TryGetComponent<Rigidbody2D>(out var rb))
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = new(target.position.x, target.position.y, -10f);

        float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPos, t);
    }
}
