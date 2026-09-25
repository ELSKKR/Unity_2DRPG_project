// 模擬玩家跟 NPC 對話：Interact → 推進台詞 → 選第 CHOICE 個選項 → 推進反應句到結束
// 用法：把下面兩個常數換掉再執行（CHOICE = -1 表示這段沒有選項或不選）
string NPC = "__NPC__";
int CHOICE = __CHOICE__;

var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
var dm = DialogManager.Instance;
var T = typeof(DialogManager);
NPCDialog npc = null;
foreach (var n in UnityEngine.Object.FindObjectsByType<NPCDialog>(UnityEngine.FindObjectsSortMode.None)) if (n.name == NPC) npc = n;
if (npc == null) return "找不到 NPC " + NPC + "（目前場景 " + SceneTransitionManager.Instance.CurrentGameplayScene + "）";

var sb = new System.Text.StringBuilder();
npc.Interact();
if (!dm.IsDialogActive) return NPC + "：Interact 後沒有開啟對話";
var speaker = (T.GetField("speakerNameText", F).GetValue(dm) as TMPro.TMP_Text).text;
sb.Append("[" + speaker + "] ");
int guard = 0;
while (dm.IsDialogActive && guard++ < 50) {
    var lines = T.GetField("currentLines", F).GetValue(dm) as string[];
    int idx = (int)T.GetField("currentLineIndex", F).GetValue(dm);
    bool choosing = (bool)T.GetField("isChoosing", F).GetValue(dm);
    if (choosing) {
        var choices = T.GetField("currentChoices", F).GetValue(dm) as DialogChoice[];
        sb.Append("\n  選項: ");
        for (int k = 0; k < choices.Length; k++) {
            bool locked = (bool)T.GetMethod("IsChoiceLocked", F).Invoke(dm, new object[] { choices[k] });
            sb.Append(k + "." + (locked ? "？？？" : choices[k].label) + "  ");
        }
        if (CHOICE < 0) { T.GetMethod("EndDialog", F).Invoke(dm, null); break; }
        T.GetField("selectedChoiceIndex", F).SetValue(dm, CHOICE);
        sb.Append("\n  → 選 " + CHOICE + "：");
        T.GetMethod("ConfirmChoice", F).Invoke(dm, null);
        continue;
    }
    sb.Append("「" + lines[idx] + "」");
    if (T.GetField("typingCoroutine", F).GetValue(dm) is UnityEngine.Coroutine co) dm.StopCoroutine(co);
    T.GetField("isTyping", F).SetValue(dm, false);
    T.GetMethod("AdvanceLine", F).Invoke(dm, null);
}
sb.Append("\n  結束 dialog=" + dm.IsDialogActive);
return sb.ToString();
