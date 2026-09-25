# Todo：森林留客

> **接手須知（2026-09-25，換新對話前補）**
> - **進度**：T1～T9 加上 T9.5（對白第四版、NPC 移位、頭頂標記）以及 T10（清理）都已完成並 commit。**下一個是 Checkpoint B，接著 T11。**
> - **對白改動**：一律照 `tasks/dialogue-draft.md` 的規則 1～6。使用者對嚴謹度要求很高，改一處就要全面自查。
> - **驗證工具在 `tasks/tools/`**：
>   - `talk.cs`：模擬對話。把 `__NPC__`、`__CHOICE__` 換掉後用 eval_file 執行
>   - `verify_boundary.cs`：洪水填充檢查邊界和落點
>   - `struct_diff.py`：比對任務結構。要搭配 `before_*.json`、`after_*.json`，這兩份用 `EditorJsonUtility.ToJson` 產生
>   - `find_spot.cs`：找 NPC 站位，條件是站得下、不擋門和出生點、不被遮住
>   - `markers.cs`：列出每個 NPC 頭頂的「!」狀態
>   - `render_map.cs`：暫時建一台相機，把地圖渲染成 PNG
>   - `dump2.py`：從磁碟解碼 `.asset`
>   - 這些腳本裡寫死的輸出路徑指向舊對話的 scratchpad，用之前要改
> - **環境注意事項**：
>   - 開 Play mode 前要先 `open_scene Persistent`（非 additive），否則常駐管理器不存在
>   - Play 剛開始先等 2～3 秒再操作
>   - 門的位置要用 `Door_To_House_*` 的 `BoxCollider2D.bounds.center`
>   - 連續幾次 eval 的編輯會併成同一個 Undo 群組，每一步前後都要 `Undo.IncrementCurrentGroup()`
>   - 測試只用存檔槽 slot 2，測完要刪掉 `save_slot2.json`
> - **旗標一覽**：`Yaer_SwordReturned`、`Yaer_SwordKept`（既有）；`Yaer_WaitsForLuke`、`Luke_Helped`、`DemoCompletionNoticeShown`（「路開了」的判斷依據）

> 規格：`SPEC.md`｜計畫：`tasks/plan.md`
> 共用驗證規則：
> - 不在 Play mode 對真實場景物件做破壞性操作
> - 編譯後 console 的 error 數要是 0
> - 驗證要給實測數字
> - 結果全對或全錯時，先懷疑驗證腳本

---

## Phase 1：開場與邊界

### T1 `intro`：開新遊戲時播三張卡
**說明**：在 `SaveManager` 新增 `introLines` 欄位。在 `StartNewGame` 的轉場 callback 裡，玩家出現、存檔完成後，呼叫 `RiftCutscene.Instance.Play(introLines)`。
**驗收**
- [x] 用空的存檔槽開新遊戲：三張卡依序出現，播放時玩家不能動，播完可以動（實測：轉場結束後黑幕 alpha 1.00、`canMove=False`；三張卡各截一張；播完 `canMove=True`）
- [x] 讀取既有存檔：不播（實測：讀檔 4 秒後 `IsPlaying=False`、`canMove=True`）
- [x] `introLines` 留空時不播，也不報錯（程式有防護：`introLines != null && Length > 0`）
- [x] 另外修掉：`RiftCutscenePanel` 的 scale 是 0.6，黑底沒蓋滿畫面；小地圖畫在黑幕之上。改成錨點撐到 −1/3～4/3（四角實測跟 Canvas 完全重合 (0,0)～(1683,919)），並移到 `[Canvas]` 最後一個子物件
- 實際多改的檔案：`SceneTransitionManager.cs`（新增 `IsTransitioning`）、`RiftCutscene.cs`（新增 `startBlack`、等轉場結束才鎖玩家）
**驗證**：Play mode 從 `TitleScreen` 開一局新遊戲，每張卡用 `ScreenCapture` 截一張（共 3 張），再讀檔確認沒有播
**相依**：無
**檔案**：`Assets/Scripts/Save/SaveManager.cs`、`Assets/Scenes/Persistent.unity`
**規模**：S

### T2 佈局選項（使用者決定）
**說明**：畫出林道、魯克露宿點、回位點的 2～3 種做法，用逐像素疊圖輸出 PNG 給使用者挑。這一步只出圖，不改場景。
**驗收**
- [x] 至少 2 種林道做法：一種在現有牆內，一種把牆、相機下限、草地往南推，各自標出林道長度（格數）（A：約 25 格，其中進樹帶約 9 格；B：約 38 格，進森林約 22 格，要多補約 90×13 格的樹）
- [x] 魯克的露宿點 2～3 個候選位置（林道口 (−14,−38)、河邊 (30,−2)、西北林邊 (−45,30)）
- [x] 回位點預設往邊界內退 4 格，圖上標示出來
- [x] 使用者選定：**林道 A（現有範圍內）**、**魯克在林道口 (−14,−38)**
- 定案數據（給 T4 用）：
  - 牆的內緣：西 x=−60、北 y=56（x 到 11 為止）、南 y=−55（x 到 24 為止）
  - 觸發帶：牆內側 2 格
  - 林道路徑：(−19,−29) → (−17,−36) → (−21,−43) → (−17,−49) → (−19,−54)；盡頭觸發區 x −21～−17、y −55～−53；林道回位點約在 (−17,−44)
  - 原本的缺口 x −27～−10 要補樹，讓林道兩側夠密（樹的擺法做完後給使用者看）
  - 草圖：scratchpad 的 `T2_overview.png`、`T2_trail_AB.png`
**驗證**：讀回 PNG 確認標示正確
**相依**：無
**檔案**：只有 scratchpad
**規模**：S

### T3 `ForestLoopZone` 核心
**說明**：新增 `Assets/Scripts/World/ForestLoopZone.cs`，給 `RiftCutscene.Play` 加一個可選的 `onFullyBlack` callback。先只在西面放一個觸發區和一個回位點，把完整流程走通：
- 第一次觸發：播全黑演出，在全黑時傳送
- 之後觸發：直接傳送，再跳一行字
- 已觸發過的旗標 `forest_loop_seen` 要進存檔
- 對話中或演出中被觸發時不處理
- 回位點沒設定時，用 `Debug.LogWarning` 發中文警告
**驗收**
- [x] 第一次進觸發區：播完整演出，全黑期間傳送到回位點（實測：x −58.6 → −54.6，黑幕 1.00、`canMove=False`、`seen=True`，有截圖）
- [x] 第二次：直接傳送，只跳一行（實測：沒有播演出，`dialog=True`，玩家從 −58.6 → −54.6，有截圖）
- [x] 存檔後讀檔，仍然只跳一行（實測：存檔 JSON 含 `forest_loop_seen`；讀檔後再進入：`cutscene=False`、`dialog=True`）
- [x] 現有的 `RiftZone` 呼叫 `RiftCutscene.Play` 時不帶新參數，行為不變（實測：只傳台詞，呼叫當下黑幕 alpha 是 0.006，從透明淡入）
- **設計變更**：回位點不再用固定的 `Transform`，改成「從進入點往 `inwardDirection` 退 `returnDistance` 格，被擋住就繼續往內找，最多 `maxExtraSteps` 格」。這樣一面邊界只需要一個觸發區，驗收時也能沿整條觸發帶每一格都檢查
- 西面觸發帶 `[Boundary]/ForestLoop/ForestLoop_West`（x −60～−58，y −55～56）逐格檢查：110 個進入點的落點全部站得下，其中 12 個多往內退，最遠退到 x −49。反向驗證 `IsStandable`：牆、南牆、柵欄、樹帶內都回傳 false，村中空地回傳 true
- 存場景後，`Forest_Village.unity` 的 diff 約 24 萬行，原因是 Unity 重新編了 Tilemap 調色盤的索引。重新渲染全圖和 T2 比對，1,164,800 個像素中只有 19 個不同，都在兩個小 sprite 上（同排序值的繪製順序不固定），Tilemap 沒有差異
- **給 T4 的更正**：場景裡沒有 `SignPost_SouthRoad`，也沒有路標 sprite。所謂的路標是 `[Boundary]/Gate/Gate_ReadZone`：一個看不見的 `SignPost` 查看區，位置在 (−19,−52.8)，說話者是「凱蘭」，台詞是「（柵欄把往南的路封住了）這條路通往眠雪鎮……看來現在過不去。」柵欄拆掉後這段台詞就不成立了，**T4 開始前要請使用者決定：刪除它，還是換成旅人字條（需要一個看得見的物件）**
**驗證**：Play mode 用 eval 移動玩家進觸發區，記錄每次傳送後的座標；截圖確認演出
**相依**：T2（回位點距離）
**檔案**：`ForestLoopZone.cs`（新）、`RiftCutscene.cs`、`Forest_Village.unity`
**規模**：M

### T4 邊界改造
**說明**：照 T2 選定的做法改場景：
- 刪除 `[Grid]/Boundary_Fence` 的所有 tile，以及 `[Boundary]/Gate`（含 `Gate_ReadZone`）
- 依選定的樣式種樹做林道
- 在西、北、南三面的樹帶和林道盡頭鋪滿觸發區與回位點
- `SignPost_SouthRoad` 的文字改成旅人的字條
**動到的既有內容**（先列清單，做完再回報一次）：`Boundary_Fence`、`Gate`、`Gate_ReadZone`、路標文字；如果選了往南推的做法，還有 `Wall_South`、`CameraBounds`、草地 Tilemap
**驗收**
- [x] `Boundary_Fence` 的 tile 數是 0；`Gate` 和 `Gate_ReadZone` 不存在（刪之前柵欄 17 格；場景檔裡兩者的名稱計數都是 0）
- [x] 洪水填充的可達範圍和改之前一致：這次可達 7,445 格，範圍 x[−60,39]、y[−55,55]，牆外可達格 = 0。跟記憶中的數字差 1 格，是因為起點和格子中心的取法不同
- [x] 每個觸發區都至少有一格可達：西 124、南 73、北 105，共 302 格
- [x] 落點檢查：302 格逐格計算落點，302 格全部合格（站得下、在可達範圍內、不在任何觸發帶裡）。第一輪抓到北面 (10.5,54.5) 不合格，那格緊貼河北端的塞子，往南都是河，所以北面觸發帶縮到 x ≤ 10
- [x] 林道中心線取樣 32 點，全部可達；疊圖 `T4_verify_overlay.png`、`T4_verify_south.png`
- [x] Play mode 實測：林道盡頭 y −53.6 → −49.6（第一次，播演出）；北面 y 55.2 → 51.05（只跳一行）
- 使用者決定：`Gate_ReadZone` 刪除；林道用草徑（只用樹留出走廊，不挖泥土路），缺口補了 16 棵邊界樹（固定亂數種子，離林道中心線 3.4 格以上）；玩家名沿用「凱蘭」
- **使用者試玩後調整（2026-09-24）**：每次都播完整演出（拿掉只跳一行的分支和 `forest_loop_seen` 旗標）；退回距離 4 → 6 格。改成 6 格後北面 x 8.5、9.5 會退進河裡，所以 `FindReturnPosition` 在往內都找不到時，改找比較短的距離。重跑驗證：302 格全部合格（北 105、南 73、西 124），牆外 0 格；Play mode 實測 x −58.6 → −52.6、y 55.2 → 49.05，第二次也播全黑演出
- 踩到的坑：MCP 連續幾次 eval 的編輯會被併進同一個 Undo 群組，一次 `PerformUndo` 就把刪柵欄、種樹、挖路全部還原了。之後每一步前後都要 `Undo.IncrementCurrentGroup()`
**驗證**：洪水填充腳本加上逐像素疊圖 PNG（標出觸發區、回位點、可達範圍），讀回來看
**相依**：T2、T3
**檔案**：`Forest_Village.unity`
**規模**：M

### ✅ Checkpoint A
- [ ] T1～T4 的驗收全部通過
- [ ] 退出 Play mode 後重開 `Forest_Village`，清點愛拉、亞爾、兩個 Pickup、`RiftZone_Lake`、所有 `ForestLoopZone` 都還在
- [ ] 使用者實際玩過鬼打牆，確認手感

---

## Phase 2：角色與對話

### T5 新 NPC 的素材與放置
**說明**：
- 先確認 `Rogue`、`Peasant_A`、`Tavern_B` 的匯入設定。`textureCompression: 1` 如果是生效中的設定，就改成 None，並回報改了什麼
- 參考 `NPC1`、`NPC2` 做出三個 NPC（Animator 用 Idle 動畫）
- 阿茉、妮娜放進 `House_Interior_C`，魯克放在 T2 選定的位置
- 阿茉接上既有的 `NPCConversation_Amo`；妮娜、魯克新建對話資產，先放佔位台詞
**驗收**
- [x] 三個 NPC 都能互動並播出對話（實測 `Interact()`：阿茉說出「唉，最近家裡的雞都不太安穩……」，妮娜、魯克說出佔位台詞，說話者名稱都正確；沒有任務的 NPC 頭上的「!」確實隱藏）
- [x] 進出房子讓 `Forest_Village` 重新載入後，魯克仍在原位（進 C 屋再出來，魯克還在 (−16.5,−31)，有遊戲內截圖）
- [x] 匯入設定符合鐵則二：三張圖都是 PPU 16、Point、RGBA32 不壓縮。`Rogue`、`Peasant_A` 的 `textureCompression: 1` 屬於 Standalone 平台，而且 `overridden: 0` 沒有生效，所以不用改。**有改的地方**：`Tavern_B` 的 aseprite 匯入器原本是 PPU 100、以畫布為 pivot 基準（pivot 落在腳下 16px），改成 PPU 16、Local 底部中央；`Rogue` 的 Idle 切圖 pivot 從中心改成底部中央。兩份素材改之前 grep GUID，引用數都是 0，不需要補償
- [x] 排序正確：pivot 都在腳底，`sortingOrder` 由 y 計算（魯克 3100 ＝ −y×100）
- 位置調整：魯克原本放在 (−14,−38)，但被南邊一棵樹的樹冠整個蓋住（只看得到頭頂的「!」）。改用計算找位置，條件是離林道 1.8～4 格、站得下、遮擋低於 5%，只有 (−16.5,−31) 符合，遮擋 3%。重跑邊界驗證：可達 7,444 格，302 個觸發格全部合格，林道 32 點全可達
- C 屋：新增 `[NPC]` 根物件；阿茉 (−4.6,12.3) 在地毯上、妮娜 (−6.2,15.0) 在床邊（遊戲內截圖 `houseC_ingame.png`），位置可以再調
- 新增資產：`Assets/Animations/Amo|Nina|Luke/`（Idle 動畫，8fps、4 格、循環，加 controller）、`NPCConversation_Nina`、`NPCConversation_Luke`（佔位台詞）
**驗證**：Play mode 走到每個 NPC 旁邊互動，用 `ScreenCapture` 截圖；讀 meta 核對匯入設定
**相依**：T2
**檔案**：3 個 prefab 或 Animator controller、`NPCConversation_Nina.asset`／`NPCConversation_Luke.asset`（新）、`House_Interior_C.unity`、`Forest_Village.unity`
**規模**：M

### T6 對白稿（使用者審）
**說明**：把所有新增和改寫的台詞寫進 `tasks/dialogue-draft.md`，每句標註屬於哪個 NPC、哪一段（stage／chat）、是哪個選項、會給哪筆情報。內容包括：
- 五筆情報的標題和內文
- 路標字條、`RiftZone_Lake` 的新台詞、結尾文字卡
**驗收**
- [x] 六個 NPC 每人至少有一個跟森林有關的細節或選項
- [x] 五筆情報各有明確的觸發路徑（稿子第五節附稽核表）
- [x] 魯克的 chats：定稿為五段，依序是預設、Kept、Returned、`Yaer_WaitsForLuke`、`Luke_Helped`
- [x] 台詞裡不再出現「三十年」「神社」「老貝」「葛倫」（只剩稿子的說明文字）
- [x] 使用者核准定稿（第三版，2026-09-24）
- 審稿過程：第二版修正了「講了情報內容卻沒給情報」的漏洞，並補完亞爾追魯克的動機（光是另一回事）。第三版改成由玩家在魯克和亞爾之間傳話，而且選項的出現條件一律要來自另一個 NPC
**相依**：無（可以和 T3～T5 同時進行）
**檔案**：`tasks/dialogue-draft.md`（新）
**規模**：S

### T7 阿茉、妮娜的對話和情報
**說明**：照定稿寫入。
- 阿茉：改寫 `NPCConversation_Amo`，把第二段的條件從葛倫的任務改成新的條件
- 妮娜：寫入她的對話
- 新增三筆情報 `forest_rule`、`nina_origin`、`forest_lights`（定稿時從 `forest_things` 改名），並登錄到 `GameDatabase.allIntel`
**驗收**
- [x] 三筆情報都能在 Play mode 實際拿到：模擬玩家選選項，走 `DialogManager` 的 `AdvanceLine`、`ConfirmChoice`、`EndDialog`，跟實際按鍵是同一條路徑。阿茉選 0 → `forest_lights`、阿茉選 1 → `nina_origin`、妮娜選 0 → `forest_rule`；妮娜選 1 沒有多給情報
- [x] `GameDatabase.allIntel` 的數量從 2 變成 5（直接讀磁碟上的資產檔確認）
- [x] 阿茉的第二段對話在新條件下會出現：尋藥任務完成後，開場換成「聽說愛拉好多了？」。妮娜在 `DemoCompletionNoticeShown` 成立後，換成「路開了吧？」
- [x] 存檔讀檔：存檔 JSON 和讀檔後的情報都是 [forest_lights, nina_origin, forest_rule]，共 3 筆
- **寫的時候補的漏洞**：`GetActiveChat` 只取一段 chat，所以後面的 chat 一成立，前面那段就再也不會出現。如果第二段沒有選項，沒問過的玩家就永遠拿不到情報。所以阿茉、妮娜的第二段都帶同一組情報選項
- 資產內容直接從磁碟解碼，跟定稿第三版逐字比對一致
**驗證**：Play mode 逐個選項走一遍；存檔後讀檔，比對已解鎖的情報數
**相依**：T5、T6
**檔案**：`NPCConversation_Amo.asset`、`NPCConversation_Nina.asset`、3 個 `Intel_*.asset`（新）、`GameDatabase.asset`
**規模**：M

### T8 愛拉、亞爾、賽勒的台詞改寫和情報選項
**說明**：
- 照定稿改寫台詞，在既有的 intro／reminder／completed 段落加上森林選項
- 新增情報 `luke_three_years`
- 視需要微調 `Intel_EllaIllness`、`Intel_YaerTemper` 的內文，但 ID 不改
**驗收**
- [x] 修改前後比對每個 stage 的 `quest`、各項條件欄位、`turnInItem(s)`、`consume*`、`reward*`、`unlockEventID`、`stageIndex*`，以及既有選項的 `acceptsQuest`、`completesHandIn`、`markEventID`、`requiredIntel`、`grantsIntel`：**差異是 0 筆**（用 `EditorJsonUtility` 取修改前後的快照，愛拉、亞爾、賽勒共比對 181 個結構欄位。反向測試：故意改壞 4 處，4 處都抓到）
- [x] 新增的選項都沒有勾 `acceptsQuest` 或 `completesHandIn`（9 個新選項逐一列出確認）
- [x] `luke_three_years` 能實際拿到（賽勒第一階段 intro、第二階段 reminder 各實測一次）
- [x] Play mode 四條任務從頭跑到尾：尋藥接取 → 配方交付後接上採集 → 作物 2/3/5 全數被收走、拿到藥水 → 愛拉完成 → 亞爾的佩劍選項（需要 `yaer_temper`）解鎖 → 還劍，四條任務全部 done，旗標 `Yaer_SwordReturned`
- 範圍調整：亞爾的新選項「魯克託我問你：腿還好嗎？」需要 `yaer_that_night`，還會標記 `Yaer_WaitsForLuke`，屬於魯克線，**移到 T9**。T8 裡亞爾的資產沒動（差異 0）
- **實測發現**：賽勒的 reminder 和 completed 原本沒有選項，加了新選項後，玩家每次搭話都會被迫再聽一次魯克的故事。所以這三段補上「（離開）」
- 情報內文改寫：`ella_illness`（刪掉「村子西側」）、`yaer_temper`（跟愛拉改寫後的台詞對齊）；`GameDatabase.allIntel` 從 5 變成 6
**驗證**：用腳本比對修改前後的 YAML 欄位（改之前先把原檔複製到 scratchpad）；然後在 Play mode 把尋藥、配方、採集、佩劍四條任務從頭跑到尾
**相依**：T6
**檔案**：`NPCConversation_Ella.asset`、`NPCConversation_Yaer.asset`、`NPCConversation_Sailor.asset`、`Intel_LukeThreeYears.asset`（新）、`GameDatabase.asset`
**規模**：M

### T9 魯克線（玩家當中間人）與 `Luke_Helped`
**說明**（照定稿第三版，範圍比原本規劃大）：
- 魯克寫五段 chats：預設、Kept、Returned、`Yaer_WaitsForLuke`、`Luke_Helped`
- 新增情報 `yaer_that_night`
- 亞爾的 handIn、completed 加上「魯克託我問你：腿還好嗎？」〈需要 `yaer_that_night`〉，選了會標記 `Yaer_WaitsForLuke`（從 T8 移過來）
- 新腳本：旗標成立的當下播 `RiftCutscene`，並在黑幕中開關物件；場景載入時如果旗標已經成立，就直接套用結果、不播演出。這支腳本要掛在不會被關掉的物件上（鐵則三）
- 在亞爾家門口放第二個魯克（預設關閉），跟第一個魯克共用同一份對話資產
**驗收**
- [x] 三種劍的順序各跑一次，都能走完「魯克帶話 → 亞爾 → 魯克起身 → 亞爾家門口的魯克」：
  - 順序 1：留著劍（亞爾在 handIn 狀態，帶話選項是第 3 個）。走完後佩劍任務仍未完成、劍還在背包，確認帶話選項不會把劍交出去
  - 順序 2：還了劍。完整走到亞爾家門口的魯克播 chat 4，另外做了存檔讀檔測試
  - 順序 3：先留著、後來又還了。兩個旗標都成立，魯克播的是 chat 2「那老頭腰上的劍回來了」，後續走完
- [x] 起身演出播放時，林道口的魯克關掉、亞爾家門口的魯克打開：全黑期間實測 `bgA=1.00`，林道口 active=False、亞爾家 active=True，有截圖。存檔讀檔後維持切換後的狀態，`cutscene=False`，沒有重播；開新遊戲後恢復成場景的初始狀態
- [x] 每一步選「（離開）」都不會卡住：在魯克 chat 1、chat 3 各選一次「（離開）」，旗標沒有變，下次找他還是同一段
- [x] 亞爾的選項結構比對：49 個欄位差異 0 筆；新增 3 個選項，都沒有勾 `acceptsQuest` 或 `completesHandIn`
- 前置步驟用 `UnlockIntel(yaer_temper)` 和 `AddItem(Sword)` 省略（這兩步 T8 已經實測過），其餘對話全部走真實的流程
- 鐵則一檢查：Play mode 期間 `WorldFlagSwap` 會關掉真實的場景物件。檢查後 git diff 場景只有新增 546 行、0 行刪除，重開場景確認林道口的魯克 active=True、亞爾家的魯克 active=False，初始狀態沒有被寫回
- 亞爾家魯克的位置 (−22.97,−25.39)：用計算找的，離亞爾 1.5 格、不擋門、遮擋 0%（共 37 個候選位置都是 0%）
- 新腳本 `WorldFlagSwap`：旗標成立時，如果 `RiftCutscene` 正在播別的演出，就直接切換，不等演出，避免這次切換被漏掉；要關掉的物件如果是自己的子物件，會發出警告（鐵則三）
**驗證**：每種順序都在 Play mode 用新遊戲狀態重跑，記錄每一步的旗標；物件切換要截圖
**相依**：T5、T6、T8
**檔案**：`NPCConversation_Luke.asset`、`NPCConversation_Yaer.asset`、`Intel_YaerThatNight.asset`（新）、`GameDatabase.asset`、新腳本、`Forest_Village.unity`
**規模**：M

### T9.5 對白第四版與 NPC 移位（使用者試玩後回饋，2026-09-25）
- [x] 使用者自己把亞爾移到屋外的椅子前 (−19.21,−24.56)；阿茉在 C 屋內移到 (−0.14,13.97)
- [x] 妮娜從 C 屋內移到 C 屋門外 (25.01,−12.05)：用 `MoveGameObjectToScene` 搬動，元件設定完整保留。位置用計算找，條件是離門 2.1 格、避開 `Exit_House_C`、`Default` 兩個出生點、遮擋 0%。注意 `Door_To_House_*` 的 transform 座標在房子碰撞體內，真正的門要用碰撞體中心
- [x] 亞爾家魯克改到 (−17.46,−24.56)：在亞爾右邊 1.8 格，遮擋 0%
- [x] 新增 `DialogChoice.hideIfEventID`：`DialogManager` 會先過濾選項，但回報的是原始索引。亞爾的帶話選項在 handIn 和 completed 都設 `Yaer_WaitsForLuke`；completed 裡配套的「（離開）」一起隱藏。實測帶話之後再找亞爾，只播台詞、沒有任何選項
- [x] 情報去除重複 5 處（`forest_rule` 的光、`nina_origin` 的繞不出去、`luke_three_years` 的林子不放人、`yaer_that_night` 的光和被逮到）；實測妮娜、魯克的新台詞都沒有提到光
- [x] 阿茉 chat 1、妮娜 chat 1 的選項改成對得上新的開場（「北邊林子最近還好嗎？」「路為什麼會開？」「妳呢？不走嗎？」），三筆情報照樣拿得到
- [x] 結構比對：愛拉、亞爾、賽勒共 241 個欄位，差異 0；邊界洪水填充 302 格合格；console 0 error；Play mode 後場景沒有 `m_IsActive` 被寫回
- [x] 定稿更新為第四版：新增規則 4～6，稽核表新增 5-5（情報重複比對）、5-6（開場和選項對不對得上）

### T10 清理既有問題
**說明**：
- `RiftZone_Lake`：清空 `requiredEventID`，換上定稿的新台詞
- `Pickup_OldItem`：改寫描述
- 全專案 grep「三十年」「神社」「老貝」「葛倫」，確認建置場景會用到的資產裡已經沒有
**驗收**
- [x] 走到湖邊會播新的演出，而且只播一次（實測：開新遊戲後把玩家傳送進觸發區 → `IsPlaying=True`，畫面上的句子是「倒影裡的森林，有一條路。」，console 的「裂縫演出觸發」出現 1 次。走出去、讓 `Forest_Village` 整個重載後再進去 → 不播，也不跳鎖定提示。對照組：手動拿掉 `rift_played_lake_shrine` 再進去 → 會重播，證明第二次進入確實有跑觸發判定）
- [x] grep 的結果列出來，每一筆剩下的都說明為什麼無害（注意：`.asset` 裡的中文存成 `\uXXXX` 跳脫字元，直接 grep 抓不到，要先解碼再搜）
  - 改完後剩 2 個檔、共 12 筆：`NPCConversation_Glenn.asset`（6 筆）、`Quest_ShrineLake.asset`（6 筆）
  - 為什麼無害：用 `AssetDatabase.GetDependencies` 追 7 個建置場景的所有相依（共 630 個檔），這兩個檔都不在裡面（對照組 `OldTrinket.asset` 在裡面），也都不在 `Resources/` 資料夾
  - 為了做到這點，使用者選擇把 `Quest_ShrineLake` 從 `GameDatabase.allQuests` 移除（7 → 6 筆，沒有 null）。任務檔和葛倫的對話檔都保留，沒有刪
- 實際改動：`RiftZone_Lake` 的 `requiredEventID` 清空，換成第四版的三句台詞（`riftID` 維持 `lake_shrine`，這是內部 ID，玩家看不到）；`OldTrinket` 的描述換成懷錶那句（道具名稱「舊物件」照稿不改）
**相依**：T6
**檔案**：`Forest_Village.unity`、`OldTrinket.asset`、`GameDatabase.asset`
**規模**：S

### ✅ Checkpoint B
- [ ] T5～T10 的驗收全部通過
- [ ] 四條任務從頭到尾走一遍都正常
- [ ] 存檔 → 讀檔後，情報數、旗標數、任務狀態都一致
- [ ] 清點關鍵物件都還在

---

## Phase 3：結尾與收尾

### T11 `ending`：路開了，以及走或留
**說明**：
- `DemoCompletionNotice` 新增 `requiredAllFlags` 欄位，放入 `Luke_Helped`；通知訊息改成「森林的路，開了。」
- `ForestLoopZone` 加上判斷：如果 `DemoCompletionNoticeShown` 已成立，就用 `StartDialogWithChoice` 問「走，還是留？」
  - 選「走」：先移到回位點，播結尾卡，再呼叫 `ReturnToTitle()`
  - 選「留」：移回回位點
**驗收**
- [ ] 藥水、佩劍、`Luke_Helped` 三個條件任何一個還沒達成時，邊界仍然是鬼打牆
- [ ] 條件全部達成的那一刻，通知只跳一次；讀檔後不會再跳
- [ ] 選「留」：玩家在可達範圍內、能移動；再進觸發區會再問一次
- [ ] 選「走」：結尾卡播完會回到 `TitleScreen`；再讀同一個存檔，玩家不在觸發區裡，而且路仍然是開的
**驗證**：Play mode 分別測「差一個條件」三種情況、「全部達成」一種情況，再測走和留兩條分支；回標題後，檢查常駐場景裡的玩家是否已經 inactive
**相依**：T4、T9
**檔案**：`DemoCompletionNotice.cs`、`ForestLoopZone.cs`、`Persistent.unity`、`Forest_Village.unity`
**規模**：M

### T12 全流程驗收、建置、文件
**說明**：
- 從標題畫面開新遊戲，一路玩到選「走」
- 建置遊戲
- 在 `Docs/系統開發歷程.md` 補上系統 12，沿用既有模板
**驗收**
- [ ] 完整流程沒有 console error
- [ ] 建置出來的 `MyJRPG_Data` 裡 `level0`～`level6` 共 7 個
- [ ] 刪除 `MyJRPG_BurstDebugInformation_DoNotShip`
- [ ] 打包後字型正常（建置完回頭數字元數量）
- [ ] 系統開發歷程補完「問題 → 設計 → 踩過的坑 → 怎麼驗證 → 報告可以這樣講」五段
**相依**：T1～T11
**檔案**：`Docs/系統開發歷程.md`
**規模**：S

### ✅ Checkpoint：完成
- [ ] SPEC 的七條 Success Criteria 全部附上實測數字
- [ ] 使用者從標題畫面實際玩過一次
