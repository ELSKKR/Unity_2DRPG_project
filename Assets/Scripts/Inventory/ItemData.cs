using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "JRPG/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("基本資訊")]
    [Tooltip("存檔用的穩定 ID，改 itemName／改檔名都不會影響存檔資料")]
    public string itemID;
    public string itemName;
    public Sprite icon;
    [TextArea(2, 4)]
    public string description;

    [Header("堆疊設定")]
    public bool isStackable = true;
    public int maxStack = 99;
}
