---
name: pixel-ui-measure
description: 用像素丈量把素材包的展示圖反推成精確的 Unity 座標。要照著素材包的官方版面做 UI（書本頁面、地圖、任何有展示圖的素材包）時使用，不要用眼睛抄圖。也用於「這個框是哪張素材拼的」這種辨識問題，以及 UI 版面重疊的程式化驗證。
---

# 素材包 UI 的像素丈量

像素風素材包的**展示圖就是官方版面規格書**。用眼睛抄會差 1~4 像素，在像素風上一眼就看得出來；而且「這個框是哪張圖拼的」用猜的必錯——同一種轉角常有 16px 和 32px 兩組，選錯整條邊會變兩倍粗。

本專案用的素材包在 `Assets/Sprites/UI/Super Asset Bundle #5 Mini Pocket Status 2.1/1 - Brown/`。
已經量完的結果（座標公式、素材對應表、band 線條位置）在 `Docs/系統開發歷程.md` 系統 11，**先去看那裡有沒有現成答案，不要重量一次**。

---

## 步驟 1：取出展示圖

展示圖通常是素材本體同一個 PNG 裡的子 sprite。從 `.meta` 讀 rect：

```bash
grep -n "name:\|rect:\|  x:\|  y:\|width:\|height:" "<sheet>.png.meta" | head -80
```

**注意 meta 的 rect 是「y 從下算」**，PIL 是從上算，要換：`top = 圖高 - y - height`。

```python
from PIL import Image
im = Image.open(sheet).convert("RGBA")
W, H = im.size
im.crop((x, H - y - h, x + w, H - y)).save("show.png")
```

## 步驟 2：對位

素材本體與展示圖比例 1:1，用 alpha bounding box 對齊最快：

```python
import numpy as np
def bbox(a):
    ys, xs = np.where(np.array(a)[:, :, 3] > 0)
    return xs.min(), xs.max(), ys.min(), ys.max()
```

本專案的結果：素材 `x144..631 y160..471`（488×312）、展示圖 `x1..519 y1..312`（519×312）。
**高度一樣 → 直接對齊**（`ref_y + 159 = 素材_y`）；寬度差 31 是展示圖多畫了側標籤 → **靠左邊界對齊**（`ref_x + 143 = 素材_x`）。

不放心就跑暴力比對驗證（只比素材有墨水的像素，算 RGB 相同的比例）。本專案跑出 **dx=143 dy=159、86.5% 相同**，跟 bbox 推導一致。
**⚠️ 521×314 vs 768×640 的暴力比對會跑超過 120 秒，要丟背景跑。**

## 步驟 3：換算成引擎座標

素材圖是 768×640、貼在 pivot 置中的 768×640 RectTransform 上，所以：

```
ref_x = local_x + 241        local_x = ref_x - 241
ref_y = 161 - local_y        local_y = 161 - ref_y
```

**一定要驗證公式**：拿場景裡已存在的物件用 `GetWorldCorners()` 換算回 ref，跟展示圖上量到的位置比對。本專案實測 `LeftPage` 落在 `ref x16..234 y13..294`，跟推導完全吻合才往下走。

擺東西時（anchor/pivot 取上方置中）：`anchoredPosition.y = -(ref_y - 頁面頂端的 ref_y)`。

## 步驟 4：量線

抓墨水色遮罩（本專案深藍 `(27,34,54)`，容差 `sum(|Δrgb|) < 50`）：

```python
ink = (np.abs(np.array(im.convert("RGB")).astype(int) - [27,34,54]).sum(2) < 50)
for y in range(y0, y1):                      # 掃列找橫線
    if ink[y, x0:x1].sum() >= 60: print(y, ink[y, x0:x1].sum())
```

再把局部**印成 ASCII** ——這是後面認素材的基礎：

```python
for y in range(y0, y1):
    print("%3d" % y, "".join("#" if ink[y, x] else "." for x in range(x0, x1)))
```

## 步驟 5：認出是哪張素材（決定性的一步）

把候選素材也印成 ASCII，**逐格比對圖案**。素材有紙面填色，所以要分三種：墨水 `#`、不透明但非墨水 `+`、透明 `.`。

```python
a = np.array(Image.open(p).convert("RGBA")).astype(int)
d = (np.abs(a[:,:,:3] - [27,34,54]).sum(2) < 60) & (a[:,:,3] > 0)
for y in range(a.shape[0]):
    print(y, "".join("#" if d[y,x] else ("+" if a[y,x,3] > 0 else ".") for x in range(a.shape[1])))
```

本專案就是這樣一次確定的——展示圖 `ref y73..80` 的斜線跟 `Holders/34` 的第 5~12 列**逐格完全相同**，所以是 16px 組不是 32px 組。

**同時要量出每張圖在自己畫布裡的墨水偏移**（ink bbox）。素材常常不是置中的，例如 `Progress Bars/28` 的墨水在 32×32 畫布的 `x8..19 y13..19`——**這個偏移就是擺放的間距依據**（音量條是 32×32 的 Image 以間距 11 重疊擺）。

## 步驟 6：注意展示圖是手繪 mockup

會有量不出來的地方（本專案的設定頁 band：展示圖 18 列高，但素材 16 列且斜線在垂直中段不能拉伸）。

**衝突時選「素材原生尺寸、像素完美」，不要為了貼合 mockup 去拉伸有細節的區域。** 差 2px 沒人看得出來，糊掉的斜線一眼就看得出來。

反過來，**被拉伸的那段如果是純色就完全無損**——設 `spriteBorder` 把有細節的列框進上下邊界即可（`Holders/35` 設成 `(0,3,0,1)`：上 1 列頂線、下 3 列雙底線、中間 12 列純紙面，所以能做成任意高度）。
`spriteBorder` 的順序是 **`(x=左, y=下, z=右, w=上)`**，貼圖 y=0 在**下面**。

---

## 版面驗證（不要看截圖）

算出每個元件在 ref 座標的實際範圍，程式化比對。

**band 的線條位置是相對頂端的固定偏移**：16 高 → `0/13/15`；18 高 → `0/15/17`；41 高 → `0/38/40`。拿這些去比對每段文字的**實際墨水範圍**。

```csharp
tmp.ForceMeshUpdate();
var rt = (RectTransform)tmp.transform;
float inkTop = refY_of_rect_top + (rt.rect.yMax - tmp.textBounds.max.y);
float inkBot = refY_of_rect_top + (rt.rect.yMax - tmp.textBounds.min.y);
```

**三個必須排除的假陽性**（本專案第一版驗證腳本全中，跳出 10 個假衝突，補完之後真實衝突是 0）：

1. **只比 Y 會把左頁的 band 跟右頁的文字配成一對** → 先檢查 X 有沒有交集。
2. **互斥的子分頁**（音訊／鍵位）永遠不會同時顯示 → 排除跨子分頁的組合。
3. **框的 cell 會超出可見範圍**：9 片拼的框，角落那片的 cell 會伸出邊界 5px，但**墨水不會**。用 cell 邊界判斷會誤報。

> 數值出現「全部都對」或「全部都錯」時，**先懷疑驗證腳本本身**。本專案在這一章被自己的測試腳本騙過兩次。

**文字高度陷阱**：同一個 fontSize，墨水高度會差 1.5 倍（11pt 中文實測 8.0~12.2，看字有沒有 fallback 到另一套字型）。**絕對不能用 fontSize 推算**，而且要用真正會顯示的那串字去量。排在 band 上時要對齊「band 內部（頂線與底線之間）的中心」，不是包住它的按鈕中心。

## 截圖

Screen Space - Overlay 的 UI **不會**被 `Camera.Render()` 抓進 RenderTexture，MCP 的 `capture_game_view`／`screenshot` 拍出來沒有 UI。要用：

```csharp
ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(
    System.IO.Directory.GetCurrentDirectory(), "Docs/screenshots/_x.png"));
```

**它是非同步的**——要輪詢檔案大小等它寫完再讀，也要等開書動畫播完（約 0.32 秒）才拍，否則拍到書還在飛。
