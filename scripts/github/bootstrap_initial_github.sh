#!/usr/bin/env bash
set -euo pipefail

if ! command -v gh >/dev/null 2>&1; then
  echo "gh CLI is required." >&2
  exit 1
fi

REPO="${1:-}"
if [[ -z "$REPO" ]]; then
  REPO="$(gh repo view --json nameWithOwner --jq '.nameWithOwner' 2>/dev/null || true)"
fi

if [[ -z "$REPO" ]]; then
  echo "Unable to determine repository. Pass OWNER/REPO as the first argument." >&2
  exit 1
fi

OWNER="${REPO%%/*}"
PROJECT_TITLE="Phalanx Chronicle MVP"

ensure_label() {
  local name="$1"
  local color="$2"
  local description="$3"
  gh label create "$name" --repo "$REPO" --color "$color" --description "$description" --force >/dev/null
}

ensure_milestone() {
  local title="$1"
  local description="$2"
  local existing
  existing="$(gh api "repos/$REPO/milestones?state=all&per_page=100" --jq ".[] | select(.title == \"$title\") | .number" | head -n1 || true)"
  if [[ -z "$existing" ]]; then
    gh api "repos/$REPO/milestones" --method POST -f title="$title" -f description="$description" >/dev/null
  fi
}

find_issue_number() {
  local title="$1"
  gh issue list --repo "$REPO" --state all --limit 200 --json number,title --search "\"$title\" in:title" \
    --jq ".[] | select(.title == \"$title\") | .number" | head -n1 || true
}

ensure_issue() {
  local title="$1"
  local milestone="$2"
  local body_file="$3"
  shift 3

  local existing
  existing="$(find_issue_number "$title")"
  if [[ -n "$existing" ]]; then
    echo "$existing"
    return 0
  fi

  local args=()
  local label
  for label in "$@"; do
    args+=(--label "$label")
  done

  gh issue create \
    --repo "$REPO" \
    --title "$title" \
    --milestone "$milestone" \
    --body-file "$body_file" \
    "${args[@]}" >/tmp/phalanx_issue_url.txt

  local issue_url issue_number
  issue_url="$(cat /tmp/phalanx_issue_url.txt)"
  issue_number="${issue_url##*/}"
  echo "$issue_number"
}

has_project_scope() {
  gh project list --owner "$OWNER" >/dev/null 2>&1
}

ensure_project() {
  if ! has_project_scope; then
    echo "Project scope is unavailable. Skipping project creation." >&2
    return 0
  fi

  local project_number
  project_number="$(gh project list --owner "$OWNER" --format json --jq ".projects[] | select(.title == \"$PROJECT_TITLE\") | .number" | head -n1 || true)"

  if [[ -z "$project_number" ]]; then
    project_number="$(gh project create --owner "$OWNER" --title "$PROJECT_TITLE" --format json --jq '.number')"
  fi

  gh project link "$project_number" --owner "$OWNER" --repo "$REPO" >/dev/null 2>&1 || true
  echo "$project_number"
}

maybe_add_issue_to_project() {
  local project_number="$1"
  local issue_number="$2"

  if [[ -z "$project_number" ]]; then
    return 0
  fi

  gh project item-add "$project_number" --owner "$OWNER" --url "https://github.com/$REPO/issues/$issue_number" >/dev/null 2>&1 || true
}

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir" /tmp/phalanx_issue_url.txt' EXIT

ensure_label "type:epic" "5319E7" "Large delivery theme or milestone"
ensure_label "type:task" "0E8A16" "Concrete implementation task"
ensure_label "type:bug" "D73A4A" "Behavior does not match expectation"
ensure_label "area:core" "1D76DB" "Core battle rules and turn flow"
ensure_label "area:battle" "0052CC" "Battle scene, grid, units, interaction"
ensure_label "area:ui" "FBCA04" "HUD, panels, and battle presentation"
ensure_label "area:ai" "C2E0C6" "Enemy AI logic"
ensure_label "area:data" "BFD4F2" "Definitions and stage data"
ensure_label "area:test" "5319E7" "Automated or manual test work"
ensure_label "area:docs" "0075CA" "Documentation and process"
ensure_label "priority:p0" "B60205" "Must-have for MVP"
ensure_label "priority:p1" "D93F0B" "Important but after p0"
ensure_label "priority:p2" "FBCA04" "Useful but lower urgency"

ensure_milestone "M1 專案骨架" "專案骨架、場景生成、文件與規則測試"
ensure_milestone "M2 地圖與單位" "10x10 地圖與玩家 / 敵方單位生成"
ensure_milestone "M3 移動系統" "移動範圍、合法移動與佔位更新"
ensure_milestone "M4 攻擊系統" "攻擊範圍、傷害公式與死亡移除"
ensure_milestone "M5 回合系統" "玩家 / 敵方回合切換與行動限制"
ensure_milestone "M6 敵方 AI" "敵人目標選擇、移動與攻擊"
ensure_milestone "M7 勝敗與 UI" "HUD、行動選單、勝敗判定與結果面板"

project_number="$(ensure_project || true)"

cat >"$tmp_dir/epic_m1.md" <<'EOF'
## 目標
建立可持續開發的 Unity SRPG MVP 專案骨架，包含場景生成、核心規則層與基本文件。

## 範圍
包含：
- 可在 Unity 成功開啟專案
- 可進入戰鬥場景
- 可執行 headless 規則測試
- README 有啟動與驗證說明

不包含：
- 新增額外遊戲系統
- 擴充多關卡流程

## 驗收條件
- [ ] 可在 Unity 成功開啟專案
- [ ] 有可進入的戰鬥場景
- [ ] 有可執行的 headless 規則測試
- [ ] README 有啟動與驗證說明

## 預計拆解的 Feature / Task
- [ ] 建立戰鬥場景生成流程
- [ ] 補齊 headless 規則測試
- [ ] 補 README 啟動與驗證方式

## 對應 Milestone
M1 專案骨架
EOF
epic_m1="$(ensure_issue "[Epic] 專案骨架" "M1 專案骨架" "$tmp_dir/epic_m1.md" "type:epic" "area:core" "priority:p0")"
maybe_add_issue_to_project "$project_number" "$epic_m1"

cat >"$tmp_dir/epic_m2.md" <<'EOF'
## 目標
完成 10x10 方格地圖與玩家 / 敵方單位生成，讓戰鬥場景具備基本可玩狀態。

## 範圍
包含：
- 10x10 格地圖
- 玩家單位 2 名生成
- 敵方單位 3 名生成
- 單位位置與佔位同步

不包含：
- 地形效果
- 額外兵種差異

## 驗收條件
- [ ] 正確建立 10x10 格地圖
- [ ] 玩家單位 2 名生成正確
- [ ] 敵方單位 3 名生成正確
- [ ] 單位位置與佔位同步正確

## 預計拆解的 Feature / Task
- [ ] 實作 10x10 格子初始化與座標映射
- [ ] 實作玩家與敵方單位生成

## 對應 Milestone
M2 地圖與單位
EOF
epic_m2="$(ensure_issue "[Epic] 地圖與單位" "M2 地圖與單位" "$tmp_dir/epic_m2.md" "type:epic" "area:battle" "priority:p0")"
maybe_add_issue_to_project "$project_number" "$epic_m2"

cat >"$tmp_dir/epic_m3.md" <<'EOF'
## 目標
完成玩家單位移動流程，包含可移動範圍顯示、合法性檢查與佔位更新。

## 範圍
包含：
- 顯示藍色移動範圍
- 只能移動到合法格子
- 移動後更新佔位

不包含：
- 位移技能
- 地形移動成本

## 驗收條件
- [ ] 可顯示藍色移動範圍
- [ ] 不可移動到非法格
- [ ] 合法移動後位置與佔位更新正確

## 預計拆解的 Feature / Task
- [ ] 實作 RangeCalculator 的 BFS 移動範圍
- [ ] 實作移動合法性檢查與佔位更新

## 對應 Milestone
M3 移動系統
EOF
epic_m3="$(ensure_issue "[Epic] 移動系統" "M3 移動系統" "$tmp_dir/epic_m3.md" "type:epic" "area:battle" "priority:p0")"
maybe_add_issue_to_project "$project_number" "$epic_m3"

cat >"$tmp_dir/epic_m4.md" <<'EOF'
## 目標
完成基礎近戰攻擊流程，包含攻擊範圍、傷害結算與死亡移除。

## 範圍
包含：
- 顯示紅色攻擊範圍
- 套用傷害公式
- HP <= 0 時死亡移除

不包含：
- 暴擊
- 反擊
- 技能傷害

## 驗收條件
- [ ] 可顯示紅色攻擊範圍
- [ ] 傷害公式正確
- [ ] HP <= 0 時單位會被移除

## 預計拆解的 Feature / Task
- [ ] 實作傷害公式與死亡移除

## 對應 Milestone
M4 攻擊系統
EOF
epic_m4="$(ensure_issue "[Epic] 攻擊系統" "M4 攻擊系統" "$tmp_dir/epic_m4.md" "type:epic" "area:battle" "priority:p0")"
maybe_add_issue_to_project "$project_number" "$epic_m4"

cat >"$tmp_dir/epic_m5.md" <<'EOF'
## 目標
完成玩家 / 敵方回合切換與每回合行動次數控制。

## 範圍
包含：
- 玩家 / 敵方回合切換
- 每單位每回合只能行動一次
- 新回合時重置行動狀態

不包含：
- 速度值排序
- 額外回合

## 驗收條件
- [ ] 玩家與敵方回合切換正確
- [ ] 每單位每回合只能行動一次
- [ ] 新回合開始時狀態能正確重置

## 預計拆解的 Feature / Task
- [ ] 實作玩家 / 敵方回合切換與行動重置

## 對應 Milestone
M5 回合系統
EOF
epic_m5="$(ensure_issue "[Epic] 回合系統" "M5 回合系統" "$tmp_dir/epic_m5.md" "type:epic" "area:core" "priority:p0")"
maybe_add_issue_to_project "$project_number" "$epic_m5"

cat >"$tmp_dir/epic_m6.md" <<'EOF'
## 目標
完成基本敵方 AI，能尋找最近玩家、移動並在可攻擊時發動攻擊。

## 範圍
包含：
- AI 選最近玩家
- 若可攻擊則攻擊
- 否則移動到最近位置後再判斷攻擊

不包含：
- 戰術權重
- 技能施放

## 驗收條件
- [ ] AI 可找到最近玩家
- [ ] 可執行移動與攻擊
- [ ] 敵方回合可完整結束

## 預計拆解的 Feature / Task
- [ ] 實作敵人選最近目標並移動攻擊

## 對應 Milestone
M6 敵方 AI
EOF
epic_m6="$(ensure_issue "[Epic] 敵方 AI" "M6 敵方 AI" "$tmp_dir/epic_m6.md" "type:epic" "area:ai" "priority:p1")"
maybe_add_issue_to_project "$project_number" "$epic_m6"

cat >"$tmp_dir/epic_m7.md" <<'EOF'
## 目標
完成 HUD、行動選單、勝敗判定與結果面板。

## 範圍
包含：
- HUD 顯示回合與選中單位
- 行動選單
- 勝利 / 失敗判定
- 結果面板

不包含：
- 劇情演出
- 複雜 UI 動畫

## 驗收條件
- [ ] HUD 顯示目前回合與選中單位資訊
- [ ] 勝利 / 失敗條件正確判定
- [ ] 結果面板可正常顯示

## 預計拆解的 Feature / Task
- [ ] 實作勝敗判定與結果面板
- [ ] 顯示 HUD 回合與選中單位資訊

## 對應 Milestone
M7 勝敗與 UI
EOF
epic_m7="$(ensure_issue "[Epic] 勝敗與 UI" "M7 勝敗與 UI" "$tmp_dir/epic_m7.md" "type:epic" "area:ui" "priority:p1")"
maybe_add_issue_to_project "$project_number" "$epic_m7"

cat >"$tmp_dir/task_scene.md" <<EOF
## 工作內容
建立可重複使用的戰鬥場景生成流程，讓專案可快速產出基礎 Battle 場景。

## 完成定義
- [ ] 可透過 Unity Editor 產生 Battle 場景
- [ ] 產生後可直接進 Play Mode
- [ ] 場景初始化流程穩定

## 測試方式
- 使用 Unity 選單產生 Battle 場景
- 直接進 Play Mode 確認場景可正常啟動

## 上層 Feature 或 Epic
#${epic_m1} [Epic] 專案骨架
EOF
task_scene="$(ensure_issue "[Task] 建立戰鬥場景生成流程" "M1 專案骨架" "$tmp_dir/task_scene.md" "type:task" "area:battle" "priority:p0")"
maybe_add_issue_to_project "$project_number" "$task_scene"

cat >"$tmp_dir/task_tests.md" <<EOF
## 工作內容
補齊 headless 規則測試，覆蓋移動、攻擊、回合與勝敗核心規則。

## 完成定義
- [ ] `dotnet test` 可執行
- [ ] 核心規則有代表性測試案例
- [ ] 新增測試不依賴 Unity 執行環境

## 測試方式
- 執行 `dotnet test Tests/Headless/PhalanxChronicle.Headless.Tests.csproj`

## 上層 Feature 或 Epic
#${epic_m1} [Epic] 專案骨架
EOF
task_tests="$(ensure_issue "[Task] 補齊 headless 規則測試" "M1 專案骨架" "$tmp_dir/task_tests.md" "type:task" "area:test" "priority:p0")"
maybe_add_issue_to_project "$project_number" "$task_tests"

cat >"$tmp_dir/task_readme.md" <<EOF
## 工作內容
整理 README 的啟動方式、測試方式與文件入口，降低新加入開發者的理解成本。

## 完成定義
- [ ] README 包含 Unity 啟動方式
- [ ] README 包含 headless test 指令
- [ ] README 有需求文件與 GitHub 流程入口

## 測試方式
- 依 README 步驟實際執行一次專案啟動與測試

## 上層 Feature 或 Epic
#${epic_m1} [Epic] 專案骨架
EOF
task_readme="$(ensure_issue "[Task] 補 README 啟動與驗證方式" "M1 專案骨架" "$tmp_dir/task_readme.md" "type:task" "area:docs" "priority:p2")"
maybe_add_issue_to_project "$project_number" "$task_readme"

cat >"$tmp_dir/task_grid.md" <<EOF
## 工作內容
實作 10x10 格子初始化與座標映射，提供戰鬥場景的基礎格位資料。

## 完成定義
- [ ] 可建立 10x10 格資料
- [ ] 世界座標與格座標可互相轉換
- [ ] 格位資料可提供給移動與攻擊系統使用

## 測試方式
- headless 測試座標轉換
- Unity 場景中確認格位正確建立

## 上層 Feature 或 Epic
#${epic_m2} [Epic] 地圖與單位
EOF
task_grid="$(ensure_issue "[Task] 實作 10x10 格子初始化與座標映射" "M2 地圖與單位" "$tmp_dir/task_grid.md" "type:task" "area:battle" "priority:p0")"
maybe_add_issue_to_project "$project_number" "$task_grid"

cat >"$tmp_dir/task_spawn.md" <<EOF
## 工作內容
實作玩家與敵方單位生成流程，根據定義資料將單位放到正確格位。

## 完成定義
- [ ] 玩家 2 名生成正確
- [ ] 敵方 3 名生成正確
- [ ] 生成後 HP、ATK、DEF、位置資料正確

## 測試方式
- Play Mode 驗證單位數量與位置
- headless 測試生成資料映射

## 上層 Feature 或 Epic
#${epic_m2} [Epic] 地圖與單位
EOF
task_spawn="$(ensure_issue "[Task] 實作玩家與敵方單位生成" "M2 地圖與單位" "$tmp_dir/task_spawn.md" "type:task" "area:data" "priority:p0")"
maybe_add_issue_to_project "$project_number" "$task_spawn"

cat >"$tmp_dir/task_range.md" <<EOF
## 工作內容
實作 RangeCalculator 的 BFS 移動範圍計算，回傳所有合法移動格。

## 完成定義
- [ ] 支援 MoveRange = 3 的範圍計算
- [ ] 被單位佔用的格不可作為可停留格
- [ ] 結果可供 UI 顯示移動範圍

## 測試方式
- headless 測試 BFS 結果
- Play Mode 驗證藍色範圍顯示

## 上層 Feature 或 Epic
#${epic_m3} [Epic] 移動系統
EOF
task_range="$(ensure_issue "[Task] 實作 RangeCalculator 的 BFS 移動範圍" "M3 移動系統" "$tmp_dir/task_range.md" "type:task" "area:core" "priority:p0")"
maybe_add_issue_to_project "$project_number" "$task_range"

cat >"$tmp_dir/task_move.md" <<EOF
## 工作內容
實作移動合法性檢查與佔位更新，確保單位只能走到合法位置並同步更新地圖狀態。

## 完成定義
- [ ] 非法格不可移動
- [ ] 合法移動後位置更新正確
- [ ] 舊格與新格佔位狀態同步

## 測試方式
- headless 測試移動合法性
- Play Mode 測試單位移動與佔位更新

## 上層 Feature 或 Epic
#${epic_m3} [Epic] 移動系統
EOF
task_move="$(ensure_issue "[Task] 實作移動合法性檢查與佔位更新" "M3 移動系統" "$tmp_dir/task_move.md" "type:task" "area:battle" "priority:p0")"
maybe_add_issue_to_project "$project_number" "$task_move"

cat >"$tmp_dir/task_combat.md" <<EOF
## 工作內容
實作傷害公式與死亡移除，完成最小可玩的攻擊結算流程。

## 完成定義
- [ ] 傷害公式為 `max(1, ATK - DEF)`
- [ ] 受傷後 HP 正確更新
- [ ] HP <= 0 時單位自場上移除

## 測試方式
- headless 測試傷害公式與死亡條件
- Play Mode 驗證攻擊後場上狀態

## 上層 Feature 或 Epic
#${epic_m4} [Epic] 攻擊系統
EOF
task_combat="$(ensure_issue "[Task] 實作傷害公式與死亡移除" "M4 攻擊系統" "$tmp_dir/task_combat.md" "type:task" "area:core" "priority:p0")"
maybe_add_issue_to_project "$project_number" "$task_combat"

cat >"$tmp_dir/task_turn.md" <<EOF
## 工作內容
實作玩家 / 敵方回合切換與行動重置，確保每單位每回合只能行動一次。

## 完成定義
- [ ] 玩家回合能正常結束
- [ ] 敵方回合能正常開始與結束
- [ ] 新回合開始時單位行動狀態正確重置

## 測試方式
- headless 測試回合切換
- Play Mode 驗證每回合只能行動一次

## 上層 Feature 或 Epic
#${epic_m5} [Epic] 回合系統
EOF
task_turn="$(ensure_issue "[Task] 實作玩家 / 敵方回合切換與行動重置" "M5 回合系統" "$tmp_dir/task_turn.md" "type:task" "area:core" "priority:p0")"
maybe_add_issue_to_project "$project_number" "$task_turn"

cat >"$tmp_dir/task_ai.md" <<EOF
## 工作內容
實作敵人選最近目標並移動攻擊，讓敵方回合具備最小策略性。

## 完成定義
- [ ] 能找到最近玩家單位
- [ ] 若可直接攻擊則攻擊
- [ ] 否則移動到最近可行位置後再判斷攻擊

## 測試方式
- headless 測試 AI 目標選擇
- Play Mode 驗證敵方回合完整執行

## 上層 Feature 或 Epic
#${epic_m6} [Epic] 敵方 AI
EOF
task_ai="$(ensure_issue "[Task] 實作敵人選最近目標並移動攻擊" "M6 敵方 AI" "$tmp_dir/task_ai.md" "type:task" "area:ai" "priority:p1")"
maybe_add_issue_to_project "$project_number" "$task_ai"

cat >"$tmp_dir/task_result.md" <<EOF
## 工作內容
實作勝敗判定與結果面板，讓戰鬥結束後有明確反饋。

## 完成定義
- [ ] 所有敵方單位死亡時顯示勝利
- [ ] 所有玩家單位死亡時顯示失敗
- [ ] 結果面板顯示正確

## 測試方式
- Play Mode 手動驗證勝利 / 失敗流程
- 規則層測試勝敗判定

## 上層 Feature 或 Epic
#${epic_m7} [Epic] 勝敗與 UI
EOF
task_result="$(ensure_issue "[Task] 實作勝敗判定與結果面板" "M7 勝敗與 UI" "$tmp_dir/task_result.md" "type:task" "area:ui" "priority:p1")"
maybe_add_issue_to_project "$project_number" "$task_result"

cat >"$tmp_dir/task_hud.md" <<EOF
## 工作內容
顯示 HUD 回合與選中單位資訊，讓玩家可讀取目前戰鬥狀態。

## 完成定義
- [ ] 顯示 Player Turn / Enemy Turn
- [ ] 顯示選中單位名稱與 HP
- [ ] 切換選中單位時 HUD 正確更新

## 測試方式
- Play Mode 手動驗證 HUD 顯示

## 上層 Feature 或 Epic
#${epic_m7} [Epic] 勝敗與 UI
EOF
task_hud="$(ensure_issue "[Task] 顯示 HUD 回合與選中單位資訊" "M7 勝敗與 UI" "$tmp_dir/task_hud.md" "type:task" "area:ui" "priority:p1")"
maybe_add_issue_to_project "$project_number" "$task_hud"

echo "Bootstrap complete for $REPO"
if [[ -n "$project_number" ]]; then
  echo "Project created or reused: $PROJECT_TITLE (#$project_number)"
else
  echo "Project skipped because GitHub token does not currently include project scope."
fi
