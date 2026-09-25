// 收集「打包後玩家可能看到的所有字」，檢查 Silver／Cubic 兩個字型資產缺哪些字
// 來源：7 個建置場景的所有相依資產（.unity/.asset/.prefab，\uXXXX 解碼後取非 ASCII 字）
//     ＋ Assets/Scripts 裡的字串常值（去掉 // 註解、跳過 Debug.Log 那幾行）＋ ASCII 可見字元
// BAKE = true 時：先餵 Silver，Silver 還缺的再餵 Cubic（跟執行期 fallback 順序一致），最後跑對齊
bool BAKE = __BAKE__;
var chars = new System.Collections.Generic.SortedSet<char>();
for (char c = (char)32; c < 127; c++) chars.Add(c);
var esc = new System.Text.RegularExpressions.Regex(@"\\u([0-9A-Fa-f]{4})");
var scenes = new System.Collections.Generic.List<string>();
foreach (var s in UnityEditor.EditorBuildSettings.scenes) if (s.enabled) scenes.Add(s.path);
int files = 0;
foreach (var d in UnityEditor.AssetDatabase.GetDependencies(scenes.ToArray(), true)) {
    if (!(d.EndsWith(".unity") || d.EndsWith(".asset") || d.EndsWith(".prefab"))) continue;
    if (d.Contains("/Fonts/") || d.Contains("TextMesh Pro")) continue;
    files++;
    var text = esc.Replace(System.IO.File.ReadAllText(d), m => ((char)System.Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
    foreach (var c in text) if (c >= 0x80 && !char.IsSurrogate(c) && !char.IsControl(c)) chars.Add(c);
}
var lit = new System.Text.RegularExpressions.Regex("\"((?:[^\"\\\\]|\\\\.)*)\"");
int scripts = 0;
foreach (var p in System.IO.Directory.GetFiles("Assets/Scripts", "*.cs", System.IO.SearchOption.AllDirectories)) {
    if (p.Replace('\\', '/').Contains("/Editor/")) continue;
    scripts++;
    foreach (var raw in System.IO.File.ReadAllLines(p)) {
        // 只看「一行裡有 = 或 { 開頭的字串」：欄位預設值、陣列初始值、label = "…"。
        // 跳過屬性（[Header]/[Tooltip]…）、Log 訊息、以及上一行延續下來、直接以字串開頭的行（多行 Tooltip／警告訊息）
        var t = raw.TrimStart();
        if (t.StartsWith("[") || t.StartsWith("\"") || t.StartsWith("$\"") || t.StartsWith("+") || raw.Contains("Log") || raw.Contains("Tooltip") || raw.Contains("Header(")) continue;
        var line = raw; int ci = line.IndexOf("//"); if (ci >= 0) line = line.Substring(0, ci);
        foreach (System.Text.RegularExpressions.Match m in lit.Matches(line))
            foreach (var c in m.Groups[1].Value) if (c >= 0x80) chars.Add(c);
    }
}
var silver = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Silver SDF.asset");
var cubic = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/Cubic SDF.asset");
System.Func<string> report = () => {
    int inS = 0, inC = 0; var miss = new System.Text.StringBuilder();
    foreach (var c in chars) {
        if (silver.characterLookupTable.ContainsKey(c)) inS++;
        else if (cubic.characterLookupTable.ContainsKey(c)) inC++;
        else miss.Append(c);
    }
    return "字元 " + chars.Count + "：Silver " + inS + "、Cubic " + inC + "、缺 " + miss.Length + " [" + miss + "]"
        + " | 資產字元數 Silver " + silver.characterTable.Count + "、Cubic " + cubic.characterTable.Count
        + " | Silver 模式 " + silver.atlasPopulationMode ;
};
string before = "掃了 " + files + " 個資產、" + scripts + " 支腳本\n前：" + report();
if (!BAKE) return before;
var all = new string(new System.Collections.Generic.List<char>(chars).ToArray());
silver.TryAddCharacters(all, out string silverMissing);
string cubicMissing = "";
if (!string.IsNullOrEmpty(silverMissing)) cubic.TryAddCharacters(silverMissing, out cubicMissing);
foreach (var a in UnityEngine.Object.FindObjectsByType<FontBaselineAligner>(FindObjectsInactive.Include, FindObjectsSortMode.None)) a.AlignAll();
int unaligned = 0;
foreach (var g in cubic.glyphTable) if (!Mathf.Approximately(g.metrics.horizontalBearingY, g.metrics.height)) unaligned++;
UnityEditor.EditorUtility.SetDirty(silver); UnityEditor.EditorUtility.SetDirty(cubic);
UnityEditor.AssetDatabase.SaveAssets();
return before + "\n後：" + report() + "\nCubic 也沒有的字 [" + cubicMissing + "]；Cubic 未對齊 glyph（含例外字）" + unaligned;
