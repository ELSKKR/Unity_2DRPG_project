using UnityEngine;

// 一段可播放的對話內容：純聊天用（reminder/handIn/completed），
// 或帶選項的分支對話（intro 這種需要玩家決定要不要接的場合）
[System.Serializable]
public class DialogSegment
{
    [TextArea(2, 4)]
    public string[] lines;

    [Header("選項（留空 = 沒有選項，播完直接結束對話）")]
    public DialogChoice[] choices;

    [Header("音效（留空 = 用 DialogManager 的預設）")]
    public AudioClip typingSound;
    public AudioClip startSound;
}
