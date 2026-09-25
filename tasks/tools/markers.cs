var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sb = new System.Text.StringBuilder("scene=" + SceneTransitionManager.Instance.CurrentGameplayScene + " | ");
foreach (var n in UnityEngine.Object.FindObjectsByType<NPCDialog>(UnityEngine.FindObjectsSortMode.None)) {
    var qm = typeof(NPCDialog).GetField("questMarker", f).GetValue(n) as QuestMarker;
    var txt = qm != null ? typeof(QuestMarker).GetField("markerText", f).GetValue(qm) as UnityEngine.Behaviour : null;
    sb.Append(n.name + "=" + (txt != null && txt.enabled ? "!" : "·") + "  ");
}
return sb.ToString();
