// 洪水填充：從村子中央出發，1 格網格，OverlapCircle 0.4 忽略 trigger（跟 ForestLoopZone.IsStandable 同一套判定）
UnityEngine.Physics2D.SyncTransforms();
int X0 = -70, X1 = 78, Y0 = -64, Y1 = 64;
int W = X1 - X0 + 1, H = Y1 - Y0 + 1;
var stand = new bool[W, H];
var reach = new bool[W, H];
for (int x = X0; x <= X1; x++) for (int y = Y0; y <= Y1; y++)
    stand[x - X0, y - Y0] = ForestLoopZone.IsStandable(new UnityEngine.Vector2(x + 0.5f, y + 0.5f));

var q = new System.Collections.Generic.Queue<(int, int)>();
int sx = -9, sy = -9;
if (!stand[sx - X0, sy - Y0]) return "起點站不下，驗證腳本有問題";
reach[sx - X0, sy - Y0] = true; q.Enqueue((sx, sy));
int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
int count = 0, minX = 999, maxX = -999, minY = 999, maxY = -999;
while (q.Count > 0) {
    var (cx, cy) = q.Dequeue(); count++;
    if (cx < minX) minX = cx; if (cx > maxX) maxX = cx; if (cy < minY) minY = cy; if (cy > maxY) maxY = cy;
    for (int k = 0; k < 4; k++) {
        int nx = cx + dx[k], ny = cy + dy[k];
        if (nx < X0 || nx > X1 || ny < Y0 || ny > Y1) continue;
        if (reach[nx - X0, ny - Y0] || !stand[nx - X0, ny - Y0]) continue;
        reach[nx - X0, ny - Y0] = true; q.Enqueue((nx, ny));
    }
}
var sb = new System.Text.StringBuilder();
sb.AppendLine($"可達格數={count} bbox x[{minX},{maxX}] y[{minY},{maxY}]（碰到網格邊界就代表漏了：網格 x[{X0},{X1}] y[{Y0},{Y1}]）");

// 牆外可達格：x<-60、y<-55、y>=56
int outside = 0;
for (int x = X0; x <= X1; x++) for (int y = Y0; y <= Y1; y++)
    if (reach[x - X0, y - Y0] && (x < -60 || y < -55 || y >= 56)) outside++;
sb.AppendLine($"牆外可達格={outside}");

var zones = UnityEngine.Object.FindObjectsByType<ForestLoopZone>(UnityEngine.FindObjectsSortMode.None);
var boxes = new System.Collections.Generic.List<UnityEngine.Bounds>();
foreach (var z in zones) boxes.Add(z.GetComponent<UnityEngine.BoxCollider2D>().bounds);
System.Func<UnityEngine.Vector2, bool> inAnyZone = p => { foreach (var b in boxes) if (b.Contains(new UnityEngine.Vector3(p.x, p.y, 0))) return true; return false; };

int totalIn = 0, totalBad = 0;
foreach (var z in zones) {
    var b = z.GetComponent<UnityEngine.BoxCollider2D>().bounds;
    int inZone = 0, good = 0, extraMax = 0; string bad = "";
    for (int x = X0; x <= X1; x++) for (int y = Y0; y <= Y1; y++) {
        var c = new UnityEngine.Vector2(x + 0.5f, y + 0.5f);
        if (!reach[x - X0, y - Y0] || !b.Contains(new UnityEngine.Vector3(c.x, c.y, 0))) continue;
        inZone++;
        var r = z.FindReturnPosition(c);
        int rx = UnityEngine.Mathf.FloorToInt(r.x), ry = UnityEngine.Mathf.FloorToInt(r.y);
        bool ok = ForestLoopZone.IsStandable(r) && rx >= X0 && rx <= X1 && ry >= Y0 && ry <= Y1 && reach[rx - X0, ry - Y0] && !inAnyZone(r);
        if (ok) { good++; extraMax = System.Math.Max(extraMax, (int)UnityEngine.Mathf.Round((r - c).magnitude)); }
        else if (bad.Length < 200) bad += $"({c.x},{c.y})->({r.x:F1},{r.y:F1}) ";
    }
    totalIn += inZone; totalBad += inZone - good;
    sb.AppendLine($"{z.name}: 觸發帶內可達格={inZone} 落點合格={good} 最遠退={extraMax} 格 不合格={bad}");
}
sb.AppendLine($"總計：觸發帶內可達格={totalIn} 不合格={totalBad}");

// 林道：沿中心線每格是否可達
var trail = new[] { (-19f, -29f), (-17f, -36f), (-21f, -43f), (-17f, -49f), (-19f, -54f) };
int tOk = 0, tAll = 0; string tBad = "";
for (int i = 0; i < trail.Length - 1; i++) for (int s = 0; s < 8; s++) {
    float t = s / 8f; float px = trail[i].Item1 + (trail[i + 1].Item1 - trail[i].Item1) * t, py = trail[i].Item2 + (trail[i + 1].Item2 - trail[i].Item2) * t;
    int cx = UnityEngine.Mathf.FloorToInt(px), cy = UnityEngine.Mathf.FloorToInt(py); tAll++;
    if (reach[cx - X0, cy - Y0]) tOk++; else tBad += $"({px:F1},{py:F1}) ";
}
sb.AppendLine($"林道中心線取樣 {tAll} 點，可達={tOk} 不可達={tBad}");

// 輸出可達地圖給疊圖用
var lines = new System.Text.StringBuilder();
for (int y = Y1; y >= Y0; y--) { for (int x = X0; x <= X1; x++) lines.Append(reach[x - X0, y - Y0] ? '1' : (stand[x - X0, y - Y0] ? '2' : '0')); lines.Append('\n'); }
System.IO.File.WriteAllText(@"C:\Users\User\AppData\Local\Temp\claude\D--Unity-MyJRPG\2ccd218b-cd9a-493d-9afd-111502726170\scratchpad\reach.txt", lines.ToString());
return sb.ToString();
