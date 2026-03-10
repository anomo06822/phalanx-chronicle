# GitHub Project 建立說明

目前 repo 內已經有：
- GitHub issue templates
- PR template
- 初始 issue 清單
- `scripts/github/bootstrap_initial_github.sh`

## 已可直接做的事
- 建立 labels
- 建立 milestones
- 建立 Epic / Task issues

## Project / Board 的前置條件
GitHub Project 需要 `project` scope。若目前 `gh auth status` 看不到 `project`，先執行：

```bash
gh auth refresh -s project
```

## 建立順序
1. 確認 repo 已存在於 GitHub
2. 執行 bootstrap 腳本建立 labels、milestones、issues
3. 補上 `project` scope
4. 再次執行 bootstrap 腳本，腳本會嘗試建立或重用 `Phalanx Chronicle MVP` project，並把 issue 加入 project

## 範例
```bash
./scripts/github/bootstrap_initial_github.sh OWNER/REPO
```

## 建議後續在 GitHub UI 裡做的設定
- 建立 Board view，按 `Status` 分欄
- 將 `Todo` 改為 `Backlog`
- 新增 `Ready`、`Review`
- 建立 Milestone 或 Area 的分組視圖

CLI 可以建立 project 並加 item，但 view 與欄位細節在 GitHub UI 裡調整會更快。
