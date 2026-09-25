// 暫時建一台正交相機，把 Forest_Village 指定範圍以整數倍像素渲染成 PNG（編輯模式，測完 DestroyImmediate）
float x0 = -66f, x1 = 74f, y0 = -70f, y1 = 60f;
int ppu = 8;
int w = (int)((x1 - x0) * ppu), h = (int)((y1 - y0) * ppu);
var go = new UnityEngine.GameObject("__TmpMapCam");
string result;
try {
    var cam = go.AddComponent<UnityEngine.Camera>();
    cam.orthographic = true;
    cam.orthographicSize = (y1 - y0) / 2f;
    cam.aspect = (float)w / h;
    cam.transform.position = new UnityEngine.Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, -10f);
    cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
    cam.backgroundColor = UnityEngine.Color.magenta;
    cam.cullingMask = ~0;
    var rt = new UnityEngine.RenderTexture(w, h, 24, UnityEngine.RenderTextureFormat.ARGB32);
    rt.filterMode = UnityEngine.FilterMode.Point;
    cam.targetTexture = rt;
    cam.Render();
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(w, h, UnityEngine.TextureFormat.RGBA32, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, w, h), 0, 0);
    tex.Apply();
    UnityEngine.RenderTexture.active = null;
    System.IO.File.WriteAllBytes(@"C:\Users\User\AppData\Local\Temp\claude\D--Unity-MyJRPG\2ccd218b-cd9a-493d-9afd-111502726170\scratchpad\map_raw_now.png", tex.EncodeToPNG());
    cam.targetTexture = null;
    UnityEngine.Object.DestroyImmediate(rt);
    UnityEngine.Object.DestroyImmediate(tex);
    result = "ok " + w + "x" + h;
} finally {
    UnityEngine.Object.DestroyImmediate(go);
}
return result;
