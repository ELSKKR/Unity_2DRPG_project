# Todo：森林留客

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
- [ ] 三個 NPC 都能互動並播出對話
- [ ] 進出房子讓 `Forest_Village` 重新載入後，魯克仍在原位
- [ ] 匯入設定符合鐵則二（列出每張圖的 PPU、Filter、Compression）
- [ ] 排序正確：pivot 在腳底，站到樹後面會被樹擋住
**驗證**：Play mode 走到每個 NPC 旁邊互動，用 `ScreenCapture` 截圖；讀 meta 核對匯入設定
**相依**：T2
**檔案**：3 個 prefab 或 Animator controller、`NPCConversation_Nina.asset`／`NPCConversation_Luke.asset`（新）、`House_Interior_C.unity`、`Forest_Village.unity`
**規模**：M

### T6 對白稿（使用者審）
**說明**：把所有新增和改寫的台詞寫進 `tasks/dialogue-draft.md`，每句標註屬於哪個 NPC、哪一段（stage／chat）、是哪個選項、會給哪筆情報。內容包括：
- 五筆情報的標題和內文
- 路標字條、`RiftZone_Lake` 的新台詞、結尾文字卡
**驗收**
- [ ] 六個 NPC 每人至少有一個跟森林有關的細節或選項
- [ ] 五筆情報各有明確的觸發路徑
- [ ] 魯克的 chats 列出四段，依序是：預設、Kept、Returned、`Luke_Helped` 之後
- [ ] 台詞裡不再出現「三十年」「神社」「老貝」「葛倫」
- [ ] 使用者核准定稿
**相依**：無（可以和 T3～T5 同時進行）
**檔案**：`tasks/dialogue-draft.md`（新）
**規模**：S

### T7 阿茉、妮娜的對話和情報
**說明**：照定稿寫入。
- 阿茉：改寫 `NPCConversation_Amo`，把第二段的條件從葛倫的任務改成新的條件
- 妮娜：寫入她的對話
- 新增三筆情報 `forest_rule`、`nina_origin`、`forest_things`，並登錄到 `GameDatabase.allIntel`
**驗收**
- [ ] 三筆情報都能在 Play mode 實際拿到
- [ ] `GameDatabase.allIntel` 的數量從 2 變成 5
- [ ] 阿茉的第二段對話在新條件下會出現
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
- [ ] 修改前後比對每個 stage 的 `quest`、各項條件欄位、`turnInItem(s)`、`consume*`、`reward*`、`unlockEventID`、`stageIndex*`，以及既有選項的 `acceptsQuest`、`completesHandIn`、`markEventID`、`requiredIntel`、`grantsIntel`：**差異是 0 筆**
- [ ] 新增的選項都沒有勾 `acceptsQuest` 或 `completesHandIn`
- [ ] `luke_three_years` 能實際拿到
**驗證**：用腳本比對修改前後的 YAML 欄位（改之前先把原檔複製到 scratchpad）；然後在 Play mode 把尋藥、配方、採集、佩劍四條任務從頭跑到尾
**相依**：T6
**檔案**：`NPCConversation_Ella.asset`、`NPCConversation_Yaer.asset`、`NPCConversation_Sailor.asset`、`Intel_LukeThreeYears.asset`（新）、`GameDatabase.asset`
**規模**：M

### T9 魯克的對話與 `Luke_Helped`
**說明**：
- 照定稿寫入魯克的四段 chats；Kept、Returned 兩段的選項會標記 `Luke_Helped`
- 新增情報 `yaer_chase`
**驗收**：以下三種順序各實際跑一次，都要拿到 `Luke_Helped`
- [ ] 順序 1：留著劍（選「路過而已」）→ 找魯克
- [ ] 順序 2：把劍還給亞爾 → 找魯克
- [ ] 順序 3：先留著劍 → 之後又還給亞爾 → 找魯克（應該顯示 Returned 那段）
- [ ] 拿到 `Luke_Helped` 之後，魯克改播「之後」那段
**驗證**：每種順序都在 Play mode 用新遊戲狀態重跑，記錄每一步的旗標
**相依**：T5、T6
**檔案**：`NPCConversation_Luke.asset`、`Intel_YaerChase.asset`（新）、`GameDatabase.asset`
**規模**：S

### T10 清理既有問題
**說明**：
- `RiftZone_Lake`：清空 `requiredEventID`，換上定稿的新台詞
- `Pickup_OldItem`：改寫描述
- 全專案 grep「三十年」「神社」「老貝」「葛倫」，確認建置場景會用到的資產裡已經沒有
**驗收**
- [ ] 走到湖邊會播新的演出，而且只播一次
- [ ] grep 的結果列出來，每一筆剩下的都說明為什麼無害（例如只存在於沒放進場景的資產裡）
**相依**：T6
**檔案**：`Forest_Village.unity`、`OldTrinket.asset`
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
