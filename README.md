# 方陣戰記（Phalanx Chronicle）

這個目錄現在不只是規格包，已經包含一套可在 Unity 中打開的《方陣戰記》SRPG MVP 原型實作。

## Demo
- [觀看示範影片](./phalanx-chronicle_demo.mp4)

## 畫面預覽
戰場指揮與行動範圍預覽：

![戰場指揮畫面](./pc-image1.png)

戰役入口與作戰卡片：

![戰役入口畫面](./pc-image2.png)

武將裝備與升階介面：

![裝備與升階畫面](./pc-image3.png)

## 已完成內容
- 10x10 方格戰場
- 玩家單位 2 名、敵方單位 3 名
- 滑鼠點擊選角、移動、攻擊、等待
- 藍色移動範圍、紅色攻擊範圍
- 玩家回合 / 敵方回合切換
- 基本敵方 AI：尋找最近玩家、移動、攻擊、等待
- 傷害公式 `max(1, ATK - DEF)`
- 死亡移除、勝利 / 失敗結果面板
- 可用 `dotnet test` 驗證的純 C# 核心規則層

## 主要結構
- `Assets/Scripts/Core/`：不依賴 Unity 的核心戰鬥邏輯
- `Assets/Scripts/Battle/`：BattleManager、GridManager、Unit、狀態機
- `Assets/Scripts/Data/`：`StageDefinition`、`UnitDefinition` 等靜態資料
- `Assets/Scripts/UI/`：HUD 與行動選單
- `Assets/Scripts/Editor/`：建立戰鬥場景的 Unity Editor 選單
- `Tests/Headless/`：headless 規則測試

## 如何啟動
1. 用 Unity 開啟這個目錄。
2. 直接進 Play Mode 也可以啟動。
3. 如果想建立固定場景，使用 Unity 選單 `Phalanx Chronicle/Create Battle Scene` 產生 `Assets/Scenes/Battle.unity`。

## 驗證
- Headless 規則測試：

```bash
dotnet test Tests/Headless/PhalanxChronicle.Headless.Tests.csproj
```

## 原始需求文件
- `01_MVP_規格書.md`
- `02_技術架構設計.md`
- `03_資料結構與類別設計.md`
- `04_開發里程碑.md`
- `05_Codex_開發提示詞.md`
- `06_驗收測試清單.md`
- `07_建議資料夾結構.md`
- `08_GitHub_需求管理流程.md`
- `09_GitHub_初始_Issue_清單.md`
- `10_GitHub_Project_建立說明.md`

## 視覺製作文件
- `Docs/character-visual-production-guide.md`
- `Docs/terrain-visual-production-guide.md`
- `Docs/character-asset-manifest.csv`
- `Docs/templates/character-visual-brief-template.md`
- Unity 選單：`Phalanx Chronicle/Visuals/Prepare Character Art Pipeline`
- 角色美術生成器：`python3 scripts/generate_character_art.py`
- SRPG 戰場像素角色：`python3 scripts/generate_srpg_battle_art.py`
- 地形像素資產生成器：`python3 scripts/generate_terrain_art.py`

## GitHub 協作
- 已補上 `.gitignore`，可避免 Unity 產生檔進版控
- 已補上 `.github/ISSUE_TEMPLATE/` 與 `PULL_REQUEST_TEMPLATE.md`
- 建議將 `04_開發里程碑.md` 的每個 milestone 建成一張 Epic issue，再往下拆成 Feature / Task
- 可用 `scripts/github/bootstrap_initial_github.sh` 建立 labels、milestones 與初始 issues
