# Spec：森林留客

> 狀態：草稿，等使用者審核 ｜ 建立：2026-09-24 ｜ 發表：下週
> 一份檔案、四個模組，節名即模組 ID（`intro`／`forest-loop`／`cast`／`ending`），之後的 plan／tasks 用這四個 ID 指涉。

---

## Objective

把林溪村的試玩內容包成一個有頭有尾的輕奇幻短篇：「森林留客」。

**世界觀一句話**：林溪村四面都是森林，走進來的旅人想離開，走著走著就會回到村裡；村人說這很正常——「等村子捨不得你了，路就開了。」

**調性**：輕奇幻，有說不清楚的神祕感，但不沉重、不走悲劇，重點在村人之間的關係。

**使用者**：成果發表的觀眾（第一次玩的人）＋之後的開發方向依據。

**成功的樣子**：玩家看完 intro 會想試著走出去 → 發現走不出去 → 從村人口中拼出森林的規矩 → 幫完大家、森林的路打開 → 自己選擇走或留。

### 明確不做
- 戰鬥系統、舊 GDD 世界觀（Morphael／夢獵人等）、老人角色（葛倫、老貝）
- 「三十年前湖邊出過事」這條線，以及 `RiftZone_Lake` 原本那三句台詞
- **任務架構不動**：不新增、不刪除、不改現有任務的階段與條件（尋藥、賽勒的配方、採集作物、亞爾的佩劍）。台詞可以改。

### 角色與住處

| 角色 | 住處 | 素材 | 身份 |
|---|---|---|---|
| 愛拉 | A 屋 | 既有 | 生病的村民，現有尋藥線的委託人 |
| 亞爾 | B 屋 | 既有 | 退役護衛。前陣子追著想偷溜出森林的魯克進林子，被林子裡的東西所傷、弄丟佩劍 |
| 阿茉 | C 屋 | `Npc's/Citizen_F/Tavern_B`（`.aseprite`，**只有 Side 動畫**，所以是站著不動的 NPC） | 土生土長的村民，五年前收留了妮娜 |
| 妮娜 | C 屋 | `Npc's/Citizen_F/Peasant_A` | 五年前被森林留下的旅人，後來乾脆住下。「被留下不是壞事」的活證明，也是森林規矩的主要情報來源 |
| 賽勒 | D 屋 | 既有 | 煉藥的人，現有配方／採集線的委託人 |
| 魯克 | 露宿（建議南邊林道口，實際位置由使用者決定） | `Npc's/Rogue` | 被困三年的流氓。從不幫人，所以森林一直不放他走 |

---

## 模組

### `intro`：新遊戲開場文字卡

**行為**
- 只在「開始新遊戲」時播，讀檔進遊戲**不播**。
- 場景切換完成、玩家出現後，用現有的 `RiftCutscene.Play(lines)` 播三張卡，播完才交回操控權：
  ```
  林溪村四面都是森林。
  每個走進來的旅人，都說自己只是路過。
  森林不這麼認為。
  ```
- 台詞放在 Inspector 的欄位，不寫死在程式裡。

**驗收**
- [ ] 空存檔槽開新遊戲：三張卡依序出現，播放期間玩家不能移動，播完可移動
- [ ] 讀取已有存檔：不播
- [ ] 播放途中遊戲沒有卡住、沒有 console error

---

### `forest-loop`：鬼打牆邊界

**要刪除、改動的既有內容**（動到使用者既有東西，執行前逐項確認）
- 刪除村口柵欄 Tilemap `[Grid]/Boundary_Fence`
- 刪除 `[Boundary]/Gate`（含 `Gate_ReadZone`）
- 南邊缺口（x −27～−11，y≈−54）改成往森林深處延伸的**林道**：兩側種樹，盡頭是牆
- 路標：實際上是 `[Boundary]/Gate/Gate_ReadZone`，一個**看不見**的 `SignPost` 查看區，台詞在講柵欄。預計改成旅人字條，把玩家引向 C 屋（例如：「如果你也繞回來了，別慌。去找住在東邊的人。」），但因為它沒有可見的物件，要刪除還是補一個看得見的東西，T4 開始時由使用者決定
- 三棟房子自己的院子柵欄（`[Structures]/House_*/Fence`）**保留不動**

**行為**
- 西、北、南三面的樹帶，在牆（`[Boundary]/Walls`）的前面放觸發區；林道盡頭也放一個。東面是河，不放。
- 玩家走進觸發區（且森林的路還沒開）：
  1. 玩家鎖住移動
  2. **每次**都用 `RiftCutscene` 播完整演出「……回過神來，你又回到原處。」，在黑幕蓋滿時瞬移（使用者 2026-09-24 改：原本第二次起只跳一行）
  3. 玩家從進入點往村子方向退 6 格（使用者改，原本 4 格）；落點被擋住就繼續往內找，往內都找不到（河岸轉彎）再改找較短的距離。不用固定的回位點 `Transform`。不能卡在任何碰撞體裡
- 森林的路打開以後，同一批觸發區改交給 `ending` 模組處理。

**驗收**
- [ ] `Boundary_Fence` 的 tile 數量＝0；`Gate`、`Gate_ReadZone` 已不在場景中
- [ ] **洪水填充**：從村子中央開始，可達範圍的邊界跟刪柵欄前一樣被封住（前一次實測可達範圍 x −59～40，y −54～55，允許因為林道往外延伸幾格）；沒有任何格子能走出牆外
- [ ] 每一個觸發區都至少有一格是玩家可達的（用同一次洪水填充的結果逐一核對，列出數量）
- [ ] 沿每一條觸發帶逐格計算落點：用 `Physics2D.OverlapCircle(半徑0.4)` 檢查沒有碰到阻擋物，而且位在洪水填充的可達範圍內
- [ ] 每次進觸發區都播完整演出，退 6 格

---

### `cast`：新 NPC、對話、情報

**新增 NPC**
- 阿茉、妮娜放進 `House_Interior_C`；魯克放進 `Forest_Village`
- 三個人都用 `NPCDialog`＋`NPCConversationData`，跟現有 NPC 一致
- 阿茉用既有的 `NPCConversation_Amo` 改寫
- 素材進場前確認匯入設定（鐵則二）。`Rogue`、`Peasant_A` 已確認是 PPU 16、Point 濾鏡；**`Idle-Sheet.png.meta` 有兩個 `textureCompression` 值（0 和 1），要確認哪一個平台設定是 1，如果是有效設定就改成 None**。`Tavern_B` 是 `.aseprite`，另外確認

**對話改寫原則**
- 任務架構不動：所有 `stages` 的任務、條件、交付物品、`markEventID`、`requiredIntel`／`grantsIntel` 的**既有引用**都保留
- 可以改台詞內容；可以新增對話選項（只帶台詞與新情報，不接任務）
- 每個 NPC 至少提供一個跟森林有關的自然細節或選項
- 刪除、改寫所有跟「三十年前」「舊神社」「老貝」「葛倫」有關的內容

**新情報（約 4～5 筆，都要登錄到 `GameDatabase.allIntel`）**

| ID | 標題（暫定） | 來源 |
|---|---|---|
| `forest_rule` | 森林的規矩 | 妮娜 |
| `nina_origin` | 妮娜的來歷 | 阿茉 |
| `luke_three_years` | 魯克的三年 | 賽勒或愛拉 |
| `yaer_chase` | 亞爾那天追的人 | 魯克或亞爾 |
| `forest_things` | 林子裡的東西 | 阿茉（貓和雞對著林子叫、晚上林子裡的光） |

既有情報 `ella_illness`、`yaer_temper` 的 ID 與流程上的角色（它們是賽勒、亞爾任務選項的解鎖條件）**不能變**，內文可以微調。

**魯克的好事：旗標 `Luke_Helped`**
- 玩家帶著亞爾的佩劍去找魯克時：魯克認出那把劍（亞爾是追他才受傷的），勸玩家把劍還回去，或承認是自己害的，選項標記 `Luke_Helped`
- **三種順序都必須拿得到 `Luke_Helped`**（原則五：內容和架構的接縫）：
  1. 先給魯克看劍，再還給亞爾
  2. 先還給亞爾（劍已經被收走），再找魯克 → 魯克要有另一段對話也能標記
  3. 選擇把劍留著（`Yaer_SwordKept`）→ 劍還在身上，魯克照樣能看到
- 實作方式（plan 階段確認）：通關條件本來就要求 `Yaer_SwordReturned` 或 `Yaer_SwordKept` 至少一個成立，所以魯克的對話只要以這兩個旗標為條件（`ConditionalChat.requiredEventID`），就能涵蓋所有會通關的順序，**不用修改 `ConditionalChat`**。「先給魯克看劍」這個順序改成「玩家留著劍時，魯克看到了」來呈現。`GetActiveChat` 是從後往前取第一個成立的條件，所以 chats 的順序要排成：預設 → Kept → Returned → Luke_Helped 之後

**順便修的既有問題**
1. `RiftZone_Lake` 的 `requiredEventID: riftUnlocked` 在整個專案裡沒有任何地方會標記，演出永遠播不出來 → 見 Open Questions 第 1 題
2. `NPCConversation_Amo` 第二段對話的條件綁在沒使用的葛倫神社任務上 → 改綁新的條件
3. `Pickup_OldItem` 的描述提到舊神社 → 見 Open Questions 第 2 題

**驗收**
- [ ] 三個新 NPC 在各自場景可互動，進出房子（`Forest_Village` 整個重新載入）後仍然在
- [ ] 每一筆新情報都有至少一條實際能觸發的對話路徑（逐筆列出觸發它的 NPC 和選項）
- [ ] `GameDatabase.allIntel` 數量＝2＋新增筆數；存檔後讀回，已解鎖的情報數量一致
- [ ] `Luke_Helped` 在上面三種順序中都拿得到（三種順序各實際走一次）
- [ ] 現有四條任務從頭到尾走一次，每一階段都能正常推進（證明任務架構沒被動到）
- [ ] 全專案 grep「三十年」「神社」「老貝」「葛倫」：在建置場景會用到的資產中為 0 筆（列出剩下的，並說明為什麼無害）

---

### `ending`：森林的路開了 + 走，還是留

**依賴**：`forest-loop`（觸發區）、`cast`（`Luke_Helped`）

**「路開了」的判定**
- 沿用 `Persistent` 裡的 `DemoCompletionNotice`。現在的條件是：`Quest_FindPotion` 完成，而且 `Yaer_SwordReturned`／`Yaer_SwordKept` 至少一個成立。**把劍留著也算數，這點不改。**
- 新增條件：`Luke_Helped` 成立。現有欄位只有「全部任務」和「任一旗標」，沒有「全部旗標」，所以要加一個欄位
- 通知訊息從「目前的試玩內容到這裡結束了，感謝遊玩！」改成「森林的路，開了。」
- 旗標 `DemoCompletionNoticeShown` 就是「路開了」的判斷依據，已經會進存檔

**走，還是留**
- 路開了之後，玩家進入任何一個森林觸發區：用 `DialogManager.StartDialogWithChoice` 問「走，還是留？」
  - **走**：用 `RiftCutscene` 播結尾文字卡，播完呼叫 `SaveManager.ReturnToTitle()` 回標題。**`ReturnToTitle` 會先存檔**，所以要先把玩家移到回位點再呼叫，否則讀檔後玩家會出生在觸發區裡，馬上又被問一次。結尾台詞放在 Inspector 欄位，例如：「你走出了森林。」「身後，林溪村的燈還亮著。」
  - **留**：關掉對話，把玩家推回邊界內（跟 `forest-loop` 用同一個回位點），不再問；下次再進觸發區時再問一次
- 路開了以後，就不再出現「回過神來」

**驗收**
- [ ] 條件差任何一項（藥水、佩劍、`Luke_Helped`）：邊界仍然是鬼打牆，不會出現選項
- [ ] 條件全部達成的當下，「森林的路，開了。」只跳一次；存檔讀檔後不會再跳
- [ ] 選「留」：玩家回到可達範圍內，可以再次移動；再進觸發區會再問
- [ ] 選「走」：結尾卡播完後載入 `TitleScreen`，常駐場景沒有殘留的玩家或介面（沿用 `ReturnToTitle` 既有的清理行為，實測確認）
- [ ] 回標題後再讀同一個存檔：狀態正確（路仍然是開的）

---

## Tech Stack
- Unity **6000.3.14f1**，2D URP，C#，全部放在預設的 `Assembly-CSharp`（沒有 `.asmdef`）
- 已安裝的相關套件：`com.unity.2d.aseprite` 3.0.1（`Tavern_B` 會用到）
- 既有系統：`RiftCutscene`、`RiftZone`、`WorldStateManager`、`DialogManager`、`NPCDialog`／`NPCConversationData`／`ConditionalChat`、`IntelManager`／`IntelData`、`SaveManager`／`GameDatabase`、`DemoCompletionNotice`、`SceneTransitionManager`
- 編輯器操作與驗證：unity-editor-mcp（`eval`／`eval_file`、`editor_play`／`editor_stop`、`get_console_logs`）

## Commands

這個專案沒有指令列建置，也沒有測試框架，所有動作都在 Unity 編輯器裡做：

- **開啟專案**：Unity Hub → `D:\Unity\MyJRPG`（6000.3.14f1）
- **編譯確認**：MCP `recompile` → `get_console_logs`（error 數＝0）
- **Play**：MCP `editor_play` ／ `editor_stop`；從 `TitleScreen` 開始跑完整流程
- **驗證腳本**：MCP `eval_file`，腳本放在 scratchpad，不放進專案
- **遊戲畫面含 UI 截圖**：`ScreenCapture.CaptureScreenshot()`，這是非同步的，要輪詢檔案大小等它寫完
- **建置**：File → Build Profiles → Build（手動）。**不能相信 `build_status` 回報的狀態**，要去看輸出資料夾 `MyJRPG_Data/level0～level6` 是不是剛好 7 個
- **發布前**：刪除 `MyJRPG_BurstDebugInformation_DoNotShip`

## Project Structure

```
Assets/Scripts/
  World/        → RiftCutscene、WorldStateManager；新的邊界觸發腳本放這裡
  Interactables/→ RiftZone、NPCDialog、SignPost
  Quest/        → DemoCompletionNotice、ConditionalChat、Intel*
  Save/         → SaveManager（intro 從這裡的 StartNewGame 接進來）
Assets/Dialog/  → NPCConversation_*.asset、Intel_*.asset（新情報與新 NPC 的對話資產放這裡）
Assets/Prefabs/NPC/ → NPC1／NPC2 prefab（新 NPC 參考這兩個做）
Assets/Scenes/  → Forest_Village、House_Interior_C、Persistent
Assets/Save/GameDatabase.asset → 新情報要登錄
Docs/系統開發歷程.md → 做完補一節（沿用「問題→設計→踩過的坑→怎麼驗證→報告可以這樣講」模板）
SPEC.md、tasks/  → 本規格與之後的 plan／todo
```

## Code Style

照既有程式碼寫，不引入新風格：

```csharp
using UnityEngine;

// 森林邊界：玩家走進來時，路還沒開就把他送回村裡；路開了就問走或留
public class ForestLoopZone : MonoBehaviour
{
    [Header("回位點（玩家被送回的位置）")]
    [SerializeField] private Transform returnPoint;

    [Header("演出文字")]
    [TextArea(1, 3)]
    [SerializeField] private string[] firstTimeLines;

    [Header("音效（留空 = 不播放）")]
    [SerializeField] private AudioClip loopSound;

    private const string SeenFlag = "forest_loop_seen";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (WorldStateManager.Instance == null) return;

        if (returnPoint == null)
        {
            Debug.LogWarning($"{name} 沒有設定回位點，玩家不會被送回");
            return;
        }
        // ...
    }
}
```

- 識別字用英文，**註解和 `Debug.Log` 訊息一律繁體中文**
- 不用 `namespace`
- Singleton 寫法：`public static X Instance { get; private set; }`，跨系統呼叫前先檢查 null
- `[SerializeField] private` 加 camelCase
- 內容資料（台詞、情報）放在 ScriptableObject 或 Inspector 欄位，不寫死在程式裡
- 事件在 `Start` 訂閱、在 `OnDestroy` 對稱退訂
- 會「行為不對但沒有任何線索」的地方一定要加警告（原則二）
- 音效只開空的 `AudioClip` 欄位，**不指定任何音效檔**

## Testing Strategy

沒有測試框架，驗證靠實測手法，驗證方式要對得上被驗證的東西（原則四）：

| 驗證對象 | 手法 |
|---|---|
| 邊界封閉、觸發區可達、回位點安全 | 洪水填充（`Physics2D.OverlapCircle` 半徑 0.4，1 格網格）＋逐點核對。**不用 Scene View 截圖判斷** |
| 文字卡、選項畫面 | `ScreenCapture.CaptureScreenshot()`；太短的動畫就暫時把 hold 時間拉長再截 |
| 對話路徑、情報解鎖、`Luke_Helped` 三種順序 | 在 Play mode 用 eval 直接呼叫對話與世界狀態 API，逐條走完並列出結果 |
| 存檔一致性 | 存檔 → 讀檔，比對旗標數、情報數、任務狀態 |
| 任務架構沒有被動到 | 四條任務從頭跑到尾 ＋ 比對 `stages` 欄位修改前後的 diff |

- **Play mode 鐵則**：不對真實場景物件呼叫破壞性方法。要測傳送、刪除這類邏輯，就臨時用 `new GameObject()` 建測試物件，測完 `DestroyImmediate`。退出 Play mode 後重新開場景，清點關鍵物件（愛拉、亞爾、賽勒、兩個 Pickup、`RiftZone_Lake`、新 NPC）是否都還在。
- 出現「全部都對」或「全部都錯」的結果時，先懷疑驗證腳本本身。

## Boundaries

**Always**
- 動到使用者既有的東西（刪物件、改名、改台詞、改共用資產）之前先列清單，做完再講一次
- 新情報、新物品登錄到 `GameDatabase`
- 任何「玩家改變了世界」的狀態都寫進 `WorldStateManager`
- 驗證要給實測數字
- 做完補進 `Docs/系統開發歷程.md`

**Ask first**
- 林道的形狀和樹的擺法、魯克的露宿位置、回位點的位置（關卡佈局是使用者的判斷）
- 路標要不要換 sprite、邊界要不要加視覺效果（美術判斷，給選項讓使用者挑）
- 修改 `ConditionalChat`、`DemoCompletionNotice` 這些共用腳本的欄位結構
- 改素材的匯入設定或 pivot（會影響共用資產，要先 grep GUID 找出所有引用）

**Never**
- 改變任何現有任務的階段、條件或交付物
- 在 Play mode 對真實場景物件呼叫破壞性方法
- 預先指定音效檔
- 動 `Main.unity`、`House_Interior_01/02.unity`，或拿它們當參考
- 退役標題畫面的 `SettingsPanel`，或把標題畫面換成書本介面

## Success Criteria

1. 開新遊戲會看到 intro，讀檔不會
2. 在路還沒開之前，玩家不管從西、北、南哪一面，都走不出森林，而且每次都被送回可以走的位置（洪水填充＋逐點核對的數字）
3. 六個 NPC 各有森林相關的內容，新情報全部都有實際可觸發的路徑
4. `Luke_Helped` 在三種順序下都拿得到，沒有任何玩法會讓玩家永遠出不去
5. 條件全部達成後，邊界改成問「走，還是留？」；選走就播結尾文字卡並回到標題
6. 現有四條任務從頭跑到尾都正常；存檔讀檔後所有新狀態都保持一致
7. 建置成功，`level0～level6` 共 7 個，console 零 error

## Open Questions

以下有預設做法，沒有異議就照預設做：

1. **`RiftZone_Lake` 要怎麼處理？** 舊台詞已經刪了。預設：保留這個觸發區，清空前置條件，換成跟森林有關的新台詞，例如「湖面映著森林，但倒影裡的森林少了一條路。」這樣它會成為一個不用接任務的情報點。另一個選擇是直接停用這個觸發區。
2. **`Pickup_OldItem`（舊物件）要怎麼處理？** 它原本屬於沒有放進遊戲的神社任務。預設：只改描述，改成前一個旅人留下的東西，不接任何任務。另一個選擇是從場景裡移除。
3. **結尾文字卡的完整台詞**：預設使用上面那兩句，最後定稿時再請你看。
