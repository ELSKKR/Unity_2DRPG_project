// 一個對話選項：顯示文字 + 選了之後接著說的反應台詞
// 反應台詞留空 = 選了就直接結束對話，不多說什麼
[System.Serializable]
public class DialogChoice
{
    public string label;

    [UnityEngine.TextArea(2, 4)]
    public string[] reactionLines;

    [UnityEngine.Tooltip("勾選 = 選了這個選項就接下任務。委託對話裡可以多個選項都勾（例如「好」跟「先問清楚再答應」）")]
    public bool acceptsQuest;

    [UnityEngine.Tooltip("勾選 = 交任務對話裡選了這個選項才算真的完成任務。沒有選項的交任務對話不受這個影響（一進去就算完成）")]
    public bool completesHandIn;

    [UnityEngine.Tooltip("選了這個選項就標記一個世界事件旗標（留空 = 不標記）。不限定特定情境，任何對話的選項都能用")]
    public string markEventID;

    [UnityEngine.Tooltip("要先解鎖這則情報才會顯示這個選項（留空 = 一直顯示）")]
    public IntelData requiredIntel;

    [UnityEngine.Tooltip("選了這個選項就解鎖這則情報（留空 = 不解鎖）")]
    public IntelData grantsIntel;
}
