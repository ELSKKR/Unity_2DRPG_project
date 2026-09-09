using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 修正「同一行字裡有些字上下位置對不齊」的問題。
//
// 成因：主字型 Silver 缺很多字（漢字、數字 4/5/7/9、部分英文字母），缺的字會 fallback 到 Cubic。
// 兩套字型「字框相對於基線的位置」天生就不一樣：
//   Silver 的漢字／數字剛好坐在基線上（bearingY == height，也就是 bottom = 0）
//   Cubic 的漢字整個往下垂 15 個字型單位、數字垂 7.5
// 疊在同一行就會看到有些字偏高、有些偏低。這跟之前修過的「字比較大」是不同的問題
// （那個是 faceInfo.scale，只影響大小、不影響位置）。
//
// 為什麼是改 glyph 的 metrics：TMP 沒有「fallback 字型垂直偏移」這種設定。
// faceInfo.baseline 實測過**完全沒有作用**（改成 15 前後量出來的頂/底座標一模一樣），
// faceInfo.scale／pointSize 只能縮放、沒辦法平移。真正決定每個字畫在哪個高度的
// 只有 glyph.metrics.horizontalBearingY，所以只能直接改它。
//
// 為什麼需要這個元件常駐、而不是改完存檔就好：Cubic 是 Dynamic 圖集模式，
// 遇到沒看過的字才即時從字型檔補進來，補進來的是原始（沒對齊過的）數值。
// 所以要在執行期盯著，一有新字就順手修掉。
//
// 這個規則是「冪等」的（已經對齊過的字再跑一次不會有任何變化），
// 所以重複執行、跨場景、跨 Play Mode 都不會累加偏移。
[ExecuteAlways]
public class FontBaselineAligner : MonoBehaviour
{
    [Header("要對齊的字型（通常是 fallback 字型，例如 Cubic SDF）")]
    [SerializeField] private TMP_FontAsset[] fontsToAlign;

    [Tooltip("本來就該垂到基線以下的字元（英文的 g/j/p/q/y、分號括號之類），不要把它們拉上來。" +
             "主字型有的字根本不會用到這裡的 fallback，列進來只是保險。")]
    [SerializeField] private string descenderExceptions = "gjpqy,;()[]{}@$_";

    // 記住上次看到的 glyph 數量，數量變多 = 有新字被補進圖集，要再修一次
    private readonly Dictionary<TMP_FontAsset, int> lastGlyphCount = new Dictionary<TMP_FontAsset, int>();

    void OnEnable() => AlignAll();

    void LateUpdate()
    {
        if (fontsToAlign == null) return;

        foreach (var font in fontsToAlign)
        {
            if (font == null) continue;
            if (lastGlyphCount.TryGetValue(font, out int count) && count == font.glyphTable.Count) continue;

            Align(font);
        }
    }

    [ContextMenu("立刻對齊所有字型")]
    public void AlignAll()
    {
        if (fontsToAlign == null) return;

        foreach (var font in fontsToAlign)
            if (font != null) Align(font);
    }

    void Align(TMP_FontAsset font)
    {
        // 例外字的 glyph 先挑出來，等一下整批跳過
        var skip = new HashSet<uint>();
        if (!string.IsNullOrEmpty(descenderExceptions))
        {
            foreach (char c in descenderExceptions)
                if (font.characterLookupTable.TryGetValue(c, out var character) && character.glyph != null)
                    skip.Add(character.glyph.index);
        }

        int fixedCount = 0;
        foreach (var glyph in font.glyphTable)
        {
            if (skip.Contains(glyph.index)) continue;

            var m = glyph.metrics;
            if (Mathf.Approximately(m.horizontalBearingY, m.height)) continue;   // 已經坐在基線上了

            glyph.metrics = new UnityEngine.TextCore.GlyphMetrics(
                m.width, m.height, m.horizontalBearingX, m.height, m.horizontalAdvance);
            fixedCount++;
        }

        lastGlyphCount[font] = font.glyphTable.Count;

        // 有動到才通知 TMP 重新排版，否則每幀都會白白重建一次所有文字
        if (fixedCount > 0)
        {
            TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, font);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(font);
#endif
        }
    }
}
