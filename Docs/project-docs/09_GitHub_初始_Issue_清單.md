# GitHub 初始 Issue 清單

這份清單對應目前 MVP 規格與里程碑，作為 GitHub 初始需求池。建議先建立 7 張 Epic，再建立第一批 Task。

## Epic

### [Epic] 專案骨架
- Milestone：`M1 專案骨架`
- Labels：`type:epic` `area:core` `priority:p0`
- 目標：
  建立可持續開發的 Unity SRPG MVP 專案骨架，包含場景生成、核心規則層與基本文件。
- 驗收條件：
  - 可在 Unity 成功開啟專案
  - 有可進入的戰鬥場景
  - 有可執行的 headless 規則測試
  - README 有啟動與驗證說明

### [Epic] 地圖與單位
- Milestone：`M2 地圖與單位`
- Labels：`type:epic` `area:battle` `priority:p0`
- 目標：
  完成 10x10 方格地圖與玩家 / 敵方單位生成，讓戰鬥場景具備基本可玩狀態。
- 驗收條件：
  - 正確建立 10x10 格地圖
  - 玩家單位 2 名生成正確
  - 敵方單位 3 名生成正確
  - 單位位置與佔位同步正確

### [Epic] 移動系統
- Milestone：`M3 移動系統`
- Labels：`type:epic` `area:battle` `priority:p0`
- 目標：
  完成玩家單位移動流程，包含可移動範圍顯示、合法性檢查與佔位更新。
- 驗收條件：
  - 可顯示藍色移動範圍
  - 不可移動到非法格
  - 合法移動後位置與佔位更新正確

### [Epic] 攻擊系統
- Milestone：`M4 攻擊系統`
- Labels：`type:epic` `area:battle` `priority:p0`
- 目標：
  完成基礎近戰攻擊流程，包含攻擊範圍、傷害結算與死亡移除。
- 驗收條件：
  - 可顯示紅色攻擊範圍
  - 傷害公式正確
  - HP <= 0 時單位會被移除

### [Epic] 回合系統
- Milestone：`M5 回合系統`
- Labels：`type:epic` `area:core` `priority:p0`
- 目標：
  完成玩家 / 敵方回合切換與每回合行動次數控制。
- 驗收條件：
  - 玩家與敵方回合切換正確
  - 每單位每回合只能行動一次
  - 新回合開始時狀態能正確重置

### [Epic] 敵方 AI
- Milestone：`M6 敵方 AI`
- Labels：`type:epic` `area:ai` `priority:p1`
- 目標：
  完成基本敵方 AI，能尋找最近玩家、移動並在可攻擊時發動攻擊。
- 驗收條件：
  - AI 可找到最近玩家
  - 可執行移動與攻擊
  - 敵方回合可完整結束

### [Epic] 勝敗與 UI
- Milestone：`M7 勝敗與 UI`
- Labels：`type:epic` `area:ui` `priority:p1`
- 目標：
  完成 HUD、行動選單、勝敗判定與結果面板。
- 驗收條件：
  - HUD 顯示目前回合與選中單位資訊
  - 勝利 / 失敗條件正確判定
  - 結果面板可正常顯示

## 第一批 Task

### [Task] 建立戰鬥場景生成流程
- Milestone：`M1 專案骨架`
- Labels：`type:task` `area:battle` `priority:p0`
- 上層 Epic：`[Epic] 專案骨架`

### [Task] 補齊 headless 規則測試
- Milestone：`M1 專案骨架`
- Labels：`type:task` `area:test` `priority:p0`
- 上層 Epic：`[Epic] 專案骨架`

### [Task] 補 README 啟動與驗證方式
- Milestone：`M1 專案骨架`
- Labels：`type:task` `area:docs` `priority:p2`
- 上層 Epic：`[Epic] 專案骨架`

### [Task] 實作 10x10 格子初始化與座標映射
- Milestone：`M2 地圖與單位`
- Labels：`type:task` `area:battle` `priority:p0`
- 上層 Epic：`[Epic] 地圖與單位`

### [Task] 實作玩家與敵方單位生成
- Milestone：`M2 地圖與單位`
- Labels：`type:task` `area:data` `priority:p0`
- 上層 Epic：`[Epic] 地圖與單位`

### [Task] 實作 RangeCalculator 的 BFS 移動範圍
- Milestone：`M3 移動系統`
- Labels：`type:task` `area:core` `priority:p0`
- 上層 Epic：`[Epic] 移動系統`

### [Task] 實作移動合法性檢查與佔位更新
- Milestone：`M3 移動系統`
- Labels：`type:task` `area:battle` `priority:p0`
- 上層 Epic：`[Epic] 移動系統`

### [Task] 實作傷害公式與死亡移除
- Milestone：`M4 攻擊系統`
- Labels：`type:task` `area:core` `priority:p0`
- 上層 Epic：`[Epic] 攻擊系統`

### [Task] 實作玩家 / 敵方回合切換與行動重置
- Milestone：`M5 回合系統`
- Labels：`type:task` `area:core` `priority:p0`
- 上層 Epic：`[Epic] 回合系統`

### [Task] 實作敵人選最近目標並移動攻擊
- Milestone：`M6 敵方 AI`
- Labels：`type:task` `area:ai` `priority:p1`
- 上層 Epic：`[Epic] 敵方 AI`

### [Task] 實作勝敗判定與結果面板
- Milestone：`M7 勝敗與 UI`
- Labels：`type:task` `area:ui` `priority:p1`
- 上層 Epic：`[Epic] 勝敗與 UI`

### [Task] 顯示 HUD 回合與選中單位資訊
- Milestone：`M7 勝敗與 UI`
- Labels：`type:task` `area:ui` `priority:p1`
- 上層 Epic：`[Epic] 勝敗與 UI`
