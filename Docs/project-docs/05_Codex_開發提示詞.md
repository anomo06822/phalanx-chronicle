# Codex 開發提示詞

## Prompt：建立 Unity SRPG MVP 專案骨架

請用 Unity C# 幫我建立一個 2D 戰棋 / SRPG 遊戲 MVP 的程式架構，需求如下：

- 類型：類似三國英傑傳的方格戰棋遊戲
- 地圖：10x10 方格
- 單位：玩家 2 名、敵人 3 名
- 回合制：玩家回合 / 敵方回合
- 行動：移動、攻擊、等待
- 移動範圍：MoveRange = 3
- 攻擊距離：AttackRange = 1
- 傷害公式：max(1, ATK - DEF)
- 勝利條件：所有敵人死亡
- 失敗條件：所有玩家死亡

請先建立以下類別空殼與基本責任分工：
- GameManager
- BattleManager
- TurnManager
- GridManager
- GridCell
- Unit
- UnitDefinition
- UnitRuntimeState
- RangeCalculator
- MoveSystem
- CombatSystem
- AIController
- BattleHUD
- ActionMenuPanel
- StageDefinition
- UnitSpawnData

要求：
1. 每個類別加上清楚註解
2. 避免把所有邏輯塞進 MonoBehaviour
3. 靜態資料與執行期資料分開
4. 程式碼要可擴充
