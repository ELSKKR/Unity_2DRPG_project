using UnityEngine;

// 一則情報（八卦/線索）的資料：跟 ItemData/QuestData 同等級的資產，
// 存讀檔靠 GameDatabase 用 intelID 查回來，不是用 AssetDatabase。
[CreateAssetMenu(fileName = "NewIntel", menuName = "JRPG/Intel")]
public class IntelData : ScriptableObject
{
    public string intelID;
    public string title;

    [TextArea(3, 8)]
    public string body;
}
