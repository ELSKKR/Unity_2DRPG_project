# Implementation Plan：森林留客

> 依據：`SPEC.md`｜任務清單：`tasks/todo.md`｜建立：2026-09-24

## Overview

把「森林留客」落地成可以發表的試玩版，整條流程是：
開新遊戲播 intro → 玩家想離開，被森林的鬼打牆送回村裡 → 跟六個 NPC 對話拼出森林的規矩、完成現有任務 → 魯克做了一件好事 → 森林的路打開 → 在邊界選「走」或「留」。

四個模組依序建置：`intro` → `forest-loop` → `cast` → `ending`。其中 `cast` 分成「對白稿先給你審」和「寫進資產」兩步，因為台詞是劇情內容，改資產之前要先定稿。

## Architecture Decisions

- **intro 接在 `SaveManager.StartNewGame` 的轉場 callback 裡。** 台詞放在 `SaveManager` 新增的 `[SerializeField] string[] introLines`。只有開新遊戲會走這個 callback，讀檔（`LoadSlotAndEnterGame`）不會，所以「讀檔不播」不需要另外判斷。
- **邊界用一支新腳本 `ForestLoopZone`（放在 `World/`），一面邊界掛一個，落點＝進入點往村子方向退 N 格、被擋就往內找（T3 定案，原本規劃的固定回位點 `Transform` 不做）。** 路還沒開時把玩家送回村裡，路開了就問走或留。不另外再寫一支結尾專用的腳本。
- **傳送要在畫面全黑的時候做，玩家才看不到。** 第一次觸發用 `RiftCutscene.Play`，並替它加一個可選的 `onFullyBlack` callback，在背景淡入到全黑、第一行字出現之前傳送。之後的觸發不播全黑演出：直接傳送，再用 `DialogManager` 跳一行「……回過神來，你又回到原處。」。相機是 `CameraFollow`（會直接貼齊玩家，不是 Lerp 平滑跟隨），所以瞬移不會拖出一段鏡頭掃過去的畫面，而「一眨眼換了位置」本身就符合劇情。
- **「路開了」的判斷依據就是 `DemoCompletionNoticeShown` 這個旗標。** 它本來就會寫進存檔，不再另外開一個旗標。`DemoCompletionNotice` 新增一個 `requiredAllFlags` 欄位，放 `Luke_Helped`。
- **`Luke_Helped` 完全用現有的 `ConditionalChat.requiredEventID` 做，`ConditionalChat` 一行都不改。** 魯克的 chats 由前往後排：預設 → 以 `Yaer_SwordKept` 為條件 → 以 `Yaer_SwordReturned` 為條件 → 以 `Luke_Helped` 為條件。因為 `GetActiveChat` 是從最後一筆往前找，第一個條件成立的就採用，所以排在越後面的優先權越高。
- **現有 NPC 加入森林內容的方式：在既有 `stages` 的對話段落裡新增選項，選項只帶台詞和 `grantsIntel`，不帶 `acceptsQuest` 或 `completesHandIn`。** 亞爾、愛拉、賽勒都有 stage，而有 stage 的 NPC 永遠不會播 chats，所以情報選項只能加在他們的 intro、reminder、completed 這幾段對話裡。
- **選「走」時的順序：** 先把玩家移到回位點，再播結尾卡，最後呼叫 `ReturnToTitle()`（它會先存檔）。

## 相依關係

```
T1 intro ──────────────────────────────────────────────┐
T2 佈局選項（使用者決定）─┬─ T3 ForestLoopZone 核心 ── T4 邊界改造 ─┐
                         └─ T5 NPC 素材與放置 ─┐                   │
T6 對白稿（使用者審）────────────────────────┼─ T7 新 NPC 對話 ──┤
                                              ├─ T8 既有 NPC 對話 ─┤
                                              └─ T9 魯克與 Luke_Helped ─┤
                         T10 清理既有問題 ─────────────────────────┤
                                                   T11 ending ─────┤
                                                   T12 全流程驗收、建置、文件
```

## Task List

### Phase 1：開場和邊界（風險最高，放最前面）
- [x] T1 `intro`：開新遊戲時播三張卡
- [x] T2 佈局選項：林道、魯克的露宿點、回位點（**使用者決定**：林道 A、魯克在林道口）
- [x] T3 `ForestLoopZone` 核心：先只做一個觸發區，走通「第一次播完整演出、之後只跳一行」
- [x] T4 邊界改造：刪除柵欄與 Gate，做出林道，鋪上全部觸發區，改寫路標

### Checkpoint A（T1～T4 做完）
- [ ] 洪水填充：邊界封閉，每個觸發區都走得到，每個回位點都安全
- [ ] 退出 Play mode 後重開場景，清點關鍵物件都還在
- [ ] 使用者實際玩過鬼打牆，確認手感

### Phase 2：角色和對話
- [ ] T5 三個新 NPC 的素材、Animator、prefab，並放進場景（對話先放佔位台詞）
- [ ] T6 對白稿：六個人的新增和改寫台詞、五筆情報（**使用者審**）
- [ ] T7 阿茉、妮娜的對話和情報寫進資產
- [ ] T8 愛拉、亞爾、賽勒的台詞改寫和情報選項
- [ ] T9 魯克的對話，以及 `Luke_Helped` 的三種順序
- [ ] T10 清理：`RiftZone_Lake`、`Pickup_OldItem`、全專案 grep 舊設定

### Checkpoint B（T5～T10 做完）
- [ ] 現有四條任務從頭跑到尾都正常，`stages` 結構修改前後比對結果為零差異
- [ ] 每一筆新情報都有一條實際觸發得到的路徑
- [ ] 存檔讀檔後，情報數和旗標數都一致

### Phase 3：結尾和收尾
- [ ] T11 `ending`：「路開了」的判定，以及走／留的選項
- [ ] T12 從標題畫面走完整個流程、建置、補寫系統開發歷程

### Checkpoint：完成
- [ ] SPEC 的七條 Success Criteria 全部附上實測數字
- [ ] 使用者從標題畫面玩一次完整流程

## Risks and Mitigations

| 風險 | 影響 | 對策 |
|---|---|---|
| 林道沒辦法太深。`Wall_South` 的中心在 (−18, −56.5)、尺寸 96×3，相機下限是 y −55，所以林道頂多往森林裡延伸約 6 格 | 中 | T2 提出兩種做法讓使用者選：一是林道留在原本範圍內，靠兩側樹木的密度營造深度；二是把牆、相機下限、草地往南推，讓林道更深 |
| `ReturnToTitle` 先存檔，座標落在觸發區裡 | 高（讀檔後馬上又被問一次） | T11 先傳送再回標題；驗收時實際讀檔確認 |
| 觸發區在對話中或演出中又被觸發 | 中 | `ForestLoopZone` 在 `RiftCutscene.IsPlaying`、`DialogManager.IsDialogActive` 為真時不處理 |
| 玩家沿著邊界探索時一直被打斷 | 中 | 觸發區放在樹帶深處、牆的前面，不放在樹帶邊緣；第二次以後只跳一行 |
| `Tavern_B` 只有側面動畫，而且是 `.aseprite` 格式 | 低 | 讓 NPC 站著不動、朝向固定；T5 先確認 aseprite 匯入器產生的動畫片段能用 |
| `Idle-Sheet.png.meta` 裡有 `textureCompression: 1` | 低 | T5 先確認這個值屬於哪個平台設定；如果是生效中的設定，改成 None，並回報改了什麼 |
| Play mode 期間銷毀的場景物件會被寫回場景檔（鐵則一） | 高 | 刪除柵欄、Gate 這類操作都在 Edit mode 做；Play mode 只移動玩家（玩家在常駐場景，不會被寫回）。每個 checkpoint 都要清點關鍵物件 |
| 對白寫完才被退回 | 中 | T6 先交對白稿給使用者審，定稿後才寫進資產 |

## Open Questions

- 林道的形狀、魯克的位置、回位點的距離：T2 會提出選項。
- 結尾文字卡的最終台詞：放在 T6 對白稿裡一起定稿。
- SPEC 的 Open Questions 1、2（`RiftZone_Lake`、`Pickup_OldItem`）沒有異議就照預設做，在 T10 處理。
