# 林溪村 — 專案規則

Unity **6000.3.14f1**／2D 俯視角 JRPG／個人專案。
`Assets/Scripts/` 下 **71 支 C# 腳本**，建置場景 **7 個**：
`Persistent`、`Forest_Village`（村莊本體）、`House_Interior_A~D`、`TitleScreen`。

> `Main.unity`、`House_Interior_01/02.unity` 是早期實驗場景，**不在建置清單**，流程也沒引用。不要動它們，也不要拿它們當參考。

---

## 分工

| 誰 | 負責 |
|---|---|
| Claude | 程式邏輯、系統架構、debug、資料建置、驗證測試 |
| 使用者 | 美術判斷、關卡佈局、音效選擇 |

- 使用者**不寫腳本**，程式部分完全交付。回報時講清楚改了什麼、驗證數字是多少，不要只丟程式碼。
- **音效鐵則**：只建立空的 `AudioClip` 欄位，**絕不預先指定音效檔**。使用者要自己掛。
- 美術／視覺決定要**給選項讓使用者挑**，不要自己決定。
- 動到使用者既有的內容（刪物件、改名、改共用資產）**一定要主動講明**，免得被當成誤刪。
- 問「要怎麼做 X？」是**要解說，不是要動手**。「幫我…」「請修復」才是動手信號。

---

## 程式慣例

照著現有程式碼寫，不要引入新風格。

- **識別字英文，註解一律繁體中文**。連 `Debug.LogWarning` 的訊息都是中文。
- 註解寫**「為什麼」不是「做什麼」**。範例：`NPCDialog.cs:115` 解釋「先完成任務再扣道具：反過來的話…任務階段會閃一下退回上一段」。管理器類別開頭有多行區塊註解說明職責與跨系統契約（見 `SaveManager.cs`）。
- **零 `namespace`**。全部在全域命名空間，不要新增。
- **Singleton 17 個**：`public static X Instance { get; private set; }`，`Awake` 裡守衛並銷毀重複實例。跨系統呼叫一律 `X.Instance.Method()`，**前面要加 null 防護**（`if (QuestManager.Instance != null)`）。
- 私有欄位 `[SerializeField] private` + camelCase；對外狀態用 `{ get; private set; }`。
- **內容資料用 ScriptableObject**，不要寫死在程式裡：`ItemData`、`QuestData`、`IntelData`、`NPCConversationData`、`GameDatabase`。
- **event 解耦**：`Start` 訂閱、`OnDestroy` 對稱退訂（`OnQuestsChanged`、`OnInventoryChanged`、`OnWorldStateChanged`）。
- **介面只在有多個實作時才開**。目前只有兩個：`IInteractable`（宣告在 `PlayerController.cs:185`，6 個實作）與 `IMenuPanel`／`MenuPanelBase`。

新增可互動物件時實作 `IInteractable`：`Interact()`、`InteractionPrompt`、`PromptWorldPosition`、`CanInteract`。

---

## 架構要點

- **常駐場景 + 加法式載入**：`Persistent` 永不卸載，遊戲場景加法式載入。進出房子會把 `Forest_Village` **整個 Unload 再重新 Load**。
- **世界狀態**：任何「玩家改變了世界」的事（撿過、採過、觸發過）都要寫進 `WorldStateManager`，否則場景重載會被場景檔的原始資料蓋回去。
- **存檔存 ID 不存參照**，透過 `GameDatabase` 還原。新增道具／任務要記得進資料庫，否則存檔讀回來那筆會安靜消失。
- **2D 排序**：`SpriteSortingByY` 用 `transform.position.y` 決定遮擋順序。
- **UI**：全掛在 `Persistent` 的 `[Canvas]` 底下，由 `UIPanelManager` 管理。遊戲內選單是**素材包的書本 UI**（`BookWindow`，六個分頁）——頁面座標公式、素材對應表、踩過的坑見 `Docs/系統開發歷程.md` 系統 11。標題畫面仍用舊的 `SettingsPanel`，**要退役它必須先問使用者**（等於讓標題畫面也開這本書，側標籤會露出背包／存檔那些分頁）。

---

## 五條原則

（出自 `Docs/系統開發歷程.md` 附錄 A，是十個系統做完回頭歸納的）

1. **執行期狀態 vs 持久化狀態** — 暫時的資料會在某個時間點被丟掉，你必須知道那個時間點在哪。
2. **安靜失敗比崩潰更危險** — 如果一個錯誤會導致「行為不對但沒有任何線索」，就一定要加警告。
3. **抽象層要在需要之前先做** — 鍵位系統先做查詢層，之後做 UI 完全不用回頭改腳本。
4. **驗證方式要跟被驗證的東西匹配** — 不要用「看起來對」當驗證。
5. **內容和架構的接縫最容易出問題** — 每個函式都正確，玩家體感仍可能是壞的（賽勒要講兩次話那個 bug）。

---

## 驗證要求

**要實測數字，不要「應該可以了」。** 講結果直接給數字（「1430 格水全部擋住、2928 格陸地零誤擋」）。

已驗證有效的手法，照被驗證的對象挑：

| 要驗證什麼 | 用的方法 |
|---|---|
| 玩家走不出地圖 | 洪水填充，模擬玩家實際能走到哪（掃牆面證明不了「沒繞路」） |
| Tilemap／碰撞這種空間性的東西 | **逐像素疊圖輸出 PNG 再讀回來看**。Scene View 截圖非整數縮放會產生取樣假象 |
| 短動畫（1.2 秒飄字） | 暫時把時間拉長到 60 秒再截圖 |
| 字型大小 | 用全新的字測，讀真正的事實來源（快取會混淆判斷） |
| UI 有沒有重疊 | 讀元件的實際角落座標計算，不要肉眼估 |
| 打包後字型有沒有壞 | 建置完回頭數字元數量 |
| UI 文字會不會壓到框線 | `ForceMeshUpdate()` 後讀 `textBounds`。**同一個 fontSize，墨水高度會差 1.5 倍**（11pt 中文實測 8.0~12.2，看字有沒有 fallback），用 fontSize 推算一定錯 |
| 遊戲畫面含 UI 的截圖 | `ScreenCapture.CaptureScreenshot()`。Screen Space - Overlay 不經過相機，`capture_game_view`／`screenshot` 拍不到 UI；它是**非同步**的，要輪詢檔案大小等它寫完 |

**數值出現「全部都對」或「全部都錯」這種極端結果時，先懷疑驗證腳本本身，再懷疑功能。** 這個專案已經有多次是驗證腳本寫錯而不是功能壞掉。

---

## 鐵則

**1. Play Mode 絕不對真實場景物件呼叫破壞性方法**

這個 unity-editor-mcp 環境裡，**Play mode 期間銷毀的場景物件會被寫回場景檔**，不會自動還原。曾因此永久刪掉 `Pickup_OldItem`，必須手動重建。

→ 要測會刪改場景物件的邏輯，在 Play mode 臨時 `new GameObject()` 建獨立測試物件（借用相同資產參照），測完 `DestroyImmediate`。退出 Play mode 後重新開場景清點關鍵物件還在不在。

**2. 美術素材匯入三項設定**

用任何素材前先確認 inspector：`Pixels Per Unit = 16`、`Filter Mode = Point (no filter)`、`Compression = None`。不合規先修或先問。

地面站立類 props（樹、草叢、石頭）pivot 一律**底部中央 `(0.5, 0)`**——`SpriteSortingByY` 靠 `position.y` 排序，pivot 不在腳底會遮擋錯誤且半截埋進地裡。

**改 pivot 是改共用資產**：先 grep texture GUID 找出所有用到的 `.unity`／`.prefab`，改完要補償既有物件位置（`y -= h/2`）與碰撞體（`collider.offset.y += h/2`）。

**UI 素材是例外，不要照上面三項改**：`Pixels Per Unit = 100`、`Sprite Mode = Single`。
- PPU 100 才對——`Image` 的 9-slice 邊界會除以 `sprite.pixelsPerUnit / canvas.referencePixelsPerUnit`，兩邊都是 100 才會 1 像素對 1 單位。把 UI 素材「修正」成 PPU 16 會讓邊框縮成 1/6。
- `Multiple` 模式會把畫布留白裁掉，但這套素材的定位全靠畫布內偏移（音量條是 32×32 的圖以間距 11 重疊擺）。改 `spriteMode` 會改變 fileID、斷掉既有引用，動之前先 grep guid 確認零引用。

**3. 有 `Update()` 的元件不能掛在會被關掉的物件上**

被 `SetActive(false)` 的物件不執行 `Update()`，連 `Start()` 都不跑，而且**不會有任何錯誤訊息**。`TabGroup` 曾掛在「書關起來就隱藏」的側標籤上 → 快捷鍵整組失效、頁面從未初始化。掛到生命週期比它長的父物件上。

**4. `build_status` MCP 工具不可信**

會一直卡在 "building"、`elapsedMs` 顯示的是編輯器開機時間。**要確認建置好了沒，直接看輸出資料夾**：`MyJRPG_Data/level0~levelN` 的數量要對得上建置清單的場景數，再看 console 有沒有 error。

發布前記得刪 `MyJRPG_BurstDebugInformation_DoNotShip`。

---

## 現況與工具

- **測試**：沒有。零 `.asmdef`，全部編進預設 `Assembly-CSharp`。驗證靠上面那些手法，不是靠測試套件。
- **CI**：沒有。建置是手動的。
- **文件**：`Docs/系統開發歷程.md`（1211 行）記錄十一個系統，模板固定為 **問題→設計→踩過的坑→怎麼驗證→報告可以這樣講**。新系統做完要沿用這個模板補進去，不要另開一套 ADR。
- **待辦**：見 `Docs/系統開發歷程.md` 附錄 B「目前刻意沒做的事」。
- **Unity MCP**：可用。場景操作、eval、截圖都走這個。
- **Skill**：`/pixel-ui-measure` — 把素材包展示圖反推成精確座標的整套流程（對位→量線→逐格比對認素材→版面驗證）。做任何素材包 UI 版面前先跑它，不要用眼睛抄。
