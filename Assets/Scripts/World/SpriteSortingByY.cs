using UnityEngine;

// Gives the 2.5D illusion: sprites lower on screen render in front of sprites higher up.
// [ExecuteAlways]：沒有這個標籤時 LateUpdate 只會在 Play mode 執行，導致場景編輯畫面
// （Scene View、非播放狀態下的截圖）裡所有物件的 sortingOrder 永遠停在生成當下的預設值，
// 樹木、草叢密集重疊時就會出現排序錯亂、硬切邊的畫面。
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSortingByY : MonoBehaviour
{
    [SerializeField] float sortingOffset = 0f;

    SpriteRenderer sr;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void LateUpdate()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        float baseY = transform.position.y + sortingOffset;
        sr.sortingOrder = Mathf.RoundToInt(-baseY * 100f);
    }
}
