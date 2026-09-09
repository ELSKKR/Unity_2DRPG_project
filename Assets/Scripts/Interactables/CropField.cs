using UnityEngine;
using UnityEngine.Tilemaps;

// 一格磚對應一種可採收的作物；同一塊田如果有生長階段（例如胡蘿蔔幼苗→熟成），
// 只有列在這裡的磚（通常只有「熟成」那個階段）才能採收，其餘磚互動時不會有反應
[System.Serializable]
public class CropYield
{
    public string tileName;
    public ItemData item;
    [Min(1)] public int amount = 1;
}

// 掛在作物 Tilemap 所在的 GameObject 上：玩家面對這塊田按互動鍵，
// 會採收「玩家面前那一格」，不是整塊田一次採光
[RequireComponent(typeof(Tilemap))]
public class CropField : MonoBehaviour, IInteractable
{
    [Header("這塊田的作物磚 → 道具對應（磚不在列表裡 = 還沒熟，採不到）")]
    [SerializeField] private CropYield[] yields;

    [SerializeField] private string interactionText = "採收";

    [Header("音效（留空 = 用 AudioManager 的共用預設）")]
    [SerializeField] private AudioClip harvestSound;

    private Tilemap tilemap;
    private PlayerController player;
    private string fieldID;

    public string InteractionPrompt => interactionText;
    public Vector3 PromptWorldPosition => player != null ? player.transform.position : transform.position;

    // 田的 collider 是整塊一起判定，但玩家面前那一格不一定熟了；
    // 只有這格真的能採到東西時才顯示提示 UI，避免站在田邊卻按了沒反應
    public bool CanInteract => TryGetHarvestableCell(out _, out _);

    void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        player = FindFirstObjectByType<PlayerController>();
        fieldID = BuildHierarchyPath(transform);

        RestoreHarvestedCells();
    }

    // 進出房子等場景切換會把 Forest_Village 整個 Unload 再重新 Load，
    // 採收清掉的 tile 只是執行期狀態，重新載入會被場景檔的原始資料蓋掉變回滿的。
    // 開場先照 WorldStateManager 記錄的清單，把已經採過的格子重新清空一次
    void RestoreHarvestedCells()
    {
        if (WorldStateManager.Instance == null) return;

        tilemap.CompressBounds();
        foreach (var pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.GetTile(pos) == null) continue;
            if (WorldStateManager.Instance.IsCropHarvested(CellKey(pos)))
                tilemap.SetTile(pos, null);
        }
    }

    public void Interact()
    {
        if (!TryGetHarvestableCell(out Vector3Int cell, out CropYield yield)) return;

        bool success = Inventory.Instance.AddItem(yield.item, yield.amount);
        if (!success) return;   // 背包滿了，磚留著，道具也沒扣

        tilemap.SetTile(cell, null);   // 只清掉作物層這一格，底下的耕地磚是另一層，會自然露出來
        WorldStateManager.Instance?.MarkCropHarvested(CellKey(cell));

        AudioClip clip = harvestSound != null ? harvestSound : AudioManager.Instance.ItemPickupSound;
        if (clip != null)
            AudioManager.Instance.PlaySFX(clip);
    }

    string CellKey(Vector3Int cell) => $"{fieldID}:{cell.x},{cell.y}";

    static string BuildHierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    // 玩家面前那一格（不是整塊田）是否有熟成、列在 yields 裡的作物磚。
    // 田的碰撞是 trigger、玩家走得進去，人可能就站在熟成作物上面——
    // 面向那一格沒磚時（例如站在整排最後一格、面向繼續往外走出田），
    // 退回檢查玩家自己站的那一格，避免明明腳下就是熟成作物卻判定不能互動
    bool TryGetHarvestableCell(out Vector3Int cell, out CropYield yield)
    {
        cell = default;
        yield = null;

        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (player == null) return false;

        Vector3Int facingCell = tilemap.WorldToCell(player.transform.position + (Vector3)player.FacingDirection);
        if (TryYieldAt(facingCell, out yield))
        {
            cell = facingCell;
            return true;
        }

        Vector3Int currentCell = tilemap.WorldToCell(player.transform.position);
        if (TryYieldAt(currentCell, out yield))
        {
            cell = currentCell;
            return true;
        }

        cell = facingCell;   // 兩格都沒有時，維持回傳面向那格，方便外部除錯看得出玩家想指向哪
        return false;
    }

    bool TryYieldAt(Vector3Int cell, out CropYield yield)
    {
        TileBase tile = tilemap.GetTile(cell);
        if (tile == null) { yield = null; return false; }   // 這格沒磚（田裡的空格，或站得太遠）

        yield = System.Array.Find(yields, y => y.tileName == tile.name);
        return yield != null;   // 這格還沒熟（生長階段磚不在列表裡）時 yield 會是 null
    }
}
