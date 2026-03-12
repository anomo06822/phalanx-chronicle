# 地形視覺製作指南

## 目標
- 風格基準：`現代化曹操傳`
- 核心原則：`像素清晰 > 花俏特效`、`戰棋辨識 > 寫實細節`
- 地形不是單一色塊，要有 `材質 / 邊界 / 變體 / stage 語言`

## 資產規格
- 格式：`PNG`
- 畫布：`64x64`
- 背景：透明
- 匯入：
  - Texture Type: `Sprite (2D and UI)`
  - Sprite Mode: `Single`
  - Pixels Per Unit: `64`
  - Filter Mode: `Point`
  - Compression: `None`
  - Pivot: `Center`

## 命名規則
- 位置：`Assets/Resources/Terrain/<palette-id>/`
- 命名：
  - `plain_a_base.png`
  - `plain_a_overlay.png`
  - `plain_a_prop.png`
  - `forest_b_base.png`
  - `fort_c_overlay.png`
  - `hazard_c_prop.png`
  - `blocked_a_base.png`

## 變體規則
- 每個 terrain type 至少 `3` 個 base 變體：`a / b / c`
- overlay 與 prop 也必須對應 `a / b / c`
- runtime 會依 `位置 / 鄰格 / palette` 選變體，所以變體差異要看得出來，但不能破壞可讀性

## palette 套件
- `guangzong-smoke`
  - 焦土、灰燼、木柵、黃巾旗、火點
- `jiangxia-bridges`
  - 濕地、木橋、水痕、蘆葦、渡口樁
- `luocheng-gate`
  - 石板、牆根、門樓陰影、軍旗、街口障礙
- `changban-river`
  - 河灘、濕泥、水紋、蘆叢、木樁
- `dingjun-stone`
  - 山石、苔痕、冷土、矮松、碎石
- `frontier-plain`
  - 通用平原、草痕、木柵、燒痕

## 生產建議
- 優先工具：`Aseprite`
- 地圖排布驗證：`Tiled`
- 不要先靠 shader、後處理、粒子去補地形質感
- 先把 tile 本身做對，再談特效

## 目前自動化腳本
- 角色 battle sprite：`python3 scripts/generate_srpg_battle_art.py`
- terrain tile：`python3 scripts/generate_terrain_art.py`

這兩支腳本目前用於 repo 內正式 placeholder 與 fallback 強化，不取代最終手工像素美術。
