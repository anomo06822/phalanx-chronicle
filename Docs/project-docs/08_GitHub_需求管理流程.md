# GitHub 需求管理流程

## 目標
使用 GitHub Issues + Project + Board 管理《方陣戰記》需求、開發進度與驗收狀態，避免需求只停留在文件中，沒有被拆解成可執行工作。

## 管理單位
### Epic
- 用來描述一個較大的主題或里程碑，例如「移動系統」、「敵方 AI」、「勝敗與 UI」。
- 一個 Epic 可以拆成多個 Feature 或 Task。

### Feature
- 用來描述一個可交付功能，例如「顯示可移動範圍」、「角色攻擊結算」、「顯示戰鬥結果面板」。
- Feature 必須附上驗收條件。

### Task
- 用來描述具體工程工作，例如「實作 BFS 移動範圍計算」、「補 BattleHUD 單位資訊更新」、「撰寫 AI 移動測試」。
- Task 要能在一次 PR 內完成。

### Bug
- 用來記錄實際行為與預期不符的缺陷。

## GitHub 對應方式
- `Epic`：使用 `Epic` issue template
- `Feature`：使用 `Feature` issue template
- `Task`：使用 `Task` issue template
- `Bug`：使用 `Bug` issue template
- `PR`：所有程式變更透過 Pull Request 合併，並在描述中連結 issue

## 建議 Project 欄位
在 GitHub Project 建立以下欄位：

### Status
- `Backlog`
- `Ready`
- `In Progress`
- `Review`
- `Done`

### Type
- `Epic`
- `Feature`
- `Task`
- `Bug`
- `Chore`

### Priority
- `P0`
- `P1`
- `P2`
- `P3`

### Area
- `Core`
- `Battle`
- `UI`
- `AI`
- `Data`
- `Test`
- `Docs`
- `Build`

### Milestone
- `M1 專案骨架`
- `M2 地圖與單位`
- `M3 移動系統`
- `M4 攻擊系統`
- `M5 回合系統`
- `M6 敵方 AI`
- `M7 勝敗與 UI`

### Estimate
- `XS`
- `S`
- `M`
- `L`

## 建議 Board 視圖
### 1. 需求看板
- 依 `Status` 分欄
- 用來看目前哪些工作還在 Backlog、哪些正在開發、哪些待 review

### 2. 里程碑視圖
- 依 `Milestone` 分組
- 用來確認 MVP 路線是否完整覆蓋

### 3. 區域視圖
- 依 `Area` 篩選
- 用來讓程式、企劃、測試快速聚焦特定模組

## 實際使用流程
1. 先用 `Epic` 或 `Feature` 建立需求。
2. 將需求拆成 1 到多個 `Task`。
3. 每個 issue 都加入 GitHub Project。
4. 開發前把卡片從 `Backlog` 移到 `Ready`。
5. 開始開發時移到 `In Progress`，並建立對應 branch。
6. 開 PR 時在描述中寫 `Closes #編號`，讓合併後自動關閉 issue。
7. PR review 完成後移到 `Done`。

## Issue 命名建議
- Epic：`[Epic] 移動系統`
- Feature：`[Feature] 顯示可移動範圍`
- Task：`[Task] 實作 RangeCalculator 的 BFS 搜尋`
- Bug：`[Bug] 敵人回合不會自動結束`

## Branch 命名建議
- `feature/issue-12-move-range`
- `task/issue-18-ai-target-selection`
- `bugfix/issue-27-turn-end-stuck`

## PR 命名建議
- `feat: 顯示可移動範圍`
- `feat: 新增敵方 AI 選目標邏輯`
- `fix: 修正玩家回合無法正常結束`

## 與目前文件的對應
建議把現有里程碑拆成以下 Epic：

| 現有文件 | 對應 Epic |
| --- | --- |
| `04_開發里程碑.md` Milestone 1 | `[Epic] 專案骨架` |
| `04_開發里程碑.md` Milestone 2 | `[Epic] 地圖與單位` |
| `04_開發里程碑.md` Milestone 3 | `[Epic] 移動系統` |
| `04_開發里程碑.md` Milestone 4 | `[Epic] 攻擊系統` |
| `04_開發里程碑.md` Milestone 5 | `[Epic] 回合系統` |
| `04_開發里程碑.md` Milestone 6 | `[Epic] 敵方 AI` |
| `04_開發里程碑.md` Milestone 7 | `[Epic] 勝敗與 UI` |

## 初始化建議
如果要正式切到 GitHub 管理，建議順序如下：

1. 建立 Git repository 並推到 GitHub
2. 啟用 Issues
3. 建立一個 GitHub Project
4. 建立上方欄位與三個視圖
5. 先建立 7 個 Epic
6. 再把每個 Epic 往下拆成 Feature / Task

## MVP 第一批建議卡片
- `[Epic] 專案骨架`
- `[Epic] 地圖與單位`
- `[Epic] 移動系統`
- `[Epic] 攻擊系統`
- `[Epic] 回合系統`
- `[Epic] 敵方 AI`
- `[Epic] 勝敗與 UI`
- `[Task] 補齊 headless 規則測試`
- `[Task] 建立戰鬥場景生成流程`
- `[Task] 補 README 啟動與驗證方式`
