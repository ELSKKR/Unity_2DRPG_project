// 找 NPC 站位：站得下、離錨點 [MIN,MAX] 格、離門 >= DOORMIN、被腳底更南的 sprite 遮擋 < 10%
UnityEngine.Physics2D.SyncTransforms();
var fv = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Forest_Village");
NPCDialog luke = null, yaer = null;
foreach (var n in UnityEngine.Object.FindObjectsByType<NPCDialog>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None)) { if (n.name == "魯克") luke = n; if (n.name == "亞爾") yaer = n; }
var size = luke.GetComponent<UnityEngine.SpriteRenderer>().bounds.size;
var renderers = new System.Collections.Generic.List<UnityEngine.SpriteRenderer>();
foreach (var sr in UnityEngine.Object.FindObjectsByType<UnityEngine.SpriteRenderer>(UnityEngine.FindObjectsSortMode.None))
    if (sr.gameObject.scene == fv && sr.GetComponent<SpriteSortingByY>() != null && sr.gameObject.activeInHierarchy) renderers.Add(sr);
var sb = new System.Text.StringBuilder();
var doorC = UnityEngine.GameObject.Find("Door_To_House_C").GetComponent<UnityEngine.Collider2D>().bounds.center;
var doorB = UnityEngine.GameObject.Find("Door_To_House_B").GetComponent<UnityEngine.Collider2D>().bounds.center;
sb.Append("C門=" + doorC + " B門=" + doorB + " 亞爾=" + yaer.transform.position + "\n");
foreach (var (label, anchor, min, max, door) in new[] {
    ("妮娜@C門", (UnityEngine.Vector2)doorC, 1.2f, 2.8f, (UnityEngine.Vector2)doorC),
    ("魯克@亞爾", (UnityEngine.Vector2)yaer.transform.position, 1.2f, 2.2f, (UnityEngine.Vector2)doorB) }) {
    var list = new System.Collections.Generic.List<(float, string)>();
    for (float y = anchor.y - 3f; y <= anchor.y + 1f; y += 0.25f) for (float x = anchor.x - 3f; x <= anchor.x + 3f; x += 0.25f) {
        var p = new UnityEngine.Vector2(x, y); float d = UnityEngine.Vector2.Distance(p, anchor);
        if (d < min || d > max) continue;
        if (UnityEngine.Vector2.Distance(p, door) < 1.6f) continue;
        bool nearSpawn = false; foreach (var sp in UnityEngine.Object.FindObjectsByType<PlayerSpawnPoint>(UnityEngine.FindObjectsSortMode.None)) if (UnityEngine.Vector2.Distance(p, sp.transform.position) < 1.8f) nearSpawn = true;
        if (nearSpawn) continue;
        if (!ForestLoopZone.IsStandable(p)) continue;
        var body = new UnityEngine.Bounds(new UnityEngine.Vector3(x, y + size.y / 2f, 0), new UnityEngine.Vector3(size.x, size.y, 40));
        float cov = 0f;
        foreach (var sr in renderers) {
            if (sr.transform.position.y >= y) continue;
            if (label.StartsWith("魯克") && sr.transform.IsChildOf(yaer.transform)) continue;
            var b = sr.bounds; b.extents = new UnityEngine.Vector3(b.extents.x, b.extents.y, 40);
            if (b.Intersects(body)) cov += UnityEngine.Mathf.Max(0, UnityEngine.Mathf.Min(b.max.x, body.max.x) - UnityEngine.Mathf.Max(b.min.x, body.min.x)) * UnityEngine.Mathf.Max(0, UnityEngine.Mathf.Min(b.max.y, body.max.y) - UnityEngine.Mathf.Max(b.min.y, body.min.y));
        }
        float r = cov / (size.x * size.y);
        if (r < 0.1f) list.Add((UnityEngine.Mathf.Abs(y - anchor.y) + UnityEngine.Mathf.Abs(d - (min + max) / 2f), "(" + x.ToString("F2") + "," + y.ToString("F2") + ") 距=" + d.ToString("F1") + " 遮擋=" + (r * 100).ToString("F0") + "%"));
    }
    list.Sort((a, b) => a.Item1.CompareTo(b.Item1));
    sb.Append(label + " 候選=" + list.Count + "：");
    for (int i = 0; i < System.Math.Min(8, list.Count); i++) sb.Append(list[i].Item2 + " | ");
    sb.Append("\n");
}
return sb.ToString();
