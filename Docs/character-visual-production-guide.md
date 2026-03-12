# 角色視覺製作指南

這份文件是《方陣戰記》目前 Unity 專案的角色資產交接規格。目標不是做理論上的最佳流程，而是讓美術、企劃、Unity 端可以直接用同一套規格落地。

## 1. 現況與接線位置

目前角色視覺的接線點如下：

- 正式角色資產入口：`Assets/Scripts/Data/UnitVisualDefinition.cs`
- 角色資料載入：`Assets/Scripts/Presentation/UnitVisualCatalog.cs`
- 戰場角色顯示：`Assets/Scripts/Battle/Units/Unit.cs`
- Fallback 程式生成圖：`Assets/Scripts/Presentation/RuntimeSpriteLibrary.cs`

結論：

- `RuntimeSpriteLibrary` 只應保留為缺圖 fallback。
- 正式角色畫面應改由 `UnitVisualDefinition` 指定 `portraitSprite`、`battleSprite`、`weaponIcon`。
- `UnitVisualDefinition` 必須放在 `Assets/Resources/UnitVisualDefinitions/`，因為專案目前用 `Resources.LoadAll("UnitVisualDefinitions")` 載入。
- Unity 端可以直接使用選單 `Phalanx Chronicle/Visuals/Prepare Character Art Pipeline` 批次建立資料夾與 `UnitVisualDefinition`。

## 2. 視覺方向

角色風格固定如下：

- 題材：三國題材 2D SRPG
- 主方向：現代水墨戰棋
- 角色比例：半寫實小比例
- 戰場讀性優先於細節量
- 主角與 Boss 以 `俊逸、史詩、輪廓明確` 為核心

陣營配色語言：

- 玩家軍：`苔綠 / 黛青 / 舊金`
- 黃巾軍：`赭黃 / 焦褐 / 煙橘`
- 魏軍追兵：`鋼藍 / 灰銀 / 深墨`

不要做的事：

- 不做 Q 版大頭比例
- 不把外框、陰影、陣營圈直接畫進 battle sprite
- 不把 UI 底框直接畫進 portrait
- 不把細碎飾品堆到遠景無法辨識

## 3. 目錄結構

建議維持以下目錄：

```text
ArtSource/
  Characters/
    Heroes/
    Bosses/
    Enemies/
    Weapons/

Assets/
  Art/
    Characters/
      Portraits/
        Heroes/
        Bosses/
        Enemies/
      Battle/
        Heroes/
        Bosses/
        Enemies/
    UI/
      Weapons/
  Resources/
    UnitVisualDefinitions/
```

說明：

- `ArtSource/` 放 PSD、Clip、Krita 原始檔，不參與 runtime。
- `Assets/Art/...` 放 Unity 使用的輸出 PNG。
- `Assets/Resources/UnitVisualDefinitions/` 放每個 `unitId` 的 `.asset`。

## 4. 檔名規則

正式輸出檔名固定用下列規則：

- 立繪：`<asset-key>__portrait.png`
- 戰場角色：`<asset-key>__battle.png`
- 武器圖示：`<weapon-id>__icon.png`
- ScriptableObject：`<unit-id>.asset`

範例：

- `player-liu-bei__portrait.png`
- `player-liu-bei__battle.png`
- `vermilion-jian__icon.png`
- `player-liu-bei.asset`
- `archetype-wei-guardian__portrait.png`
- `archetype-wei-guardian__battle.png`

## 5. 資產規格

### 5.1 Portrait

- 格式：`PNG`
- 畫布：`768x768`
- 背景：透明
- 構圖：`3/4 胸像`
- 安全區：四邊各留至少 `32px`
- 重要資訊不得超出安全區：
  - 頭頂裝飾
  - 眉眼
  - 鬍型輪廓
  - 主武器靠近臉部的部分
- 建議姿態：
  - 頭部略轉，避免純正面證件照
  - 肩線與披風有方向性
  - 兵器可入鏡，但不要吃掉臉部可讀性

專案限制：

- `Assets/Scripts/UI/BattleHUD.cs` 目前將頭像放在 `128x128` 的框內，內容區只有 `96% x 96%`。
- 因此美術稿若超出外側約 `4%`，進 UI 後會顯得擁擠。

### 5.2 Battle Sprite

- 格式：`PNG`
- 畫布：`128x128` 優先，次選 `96x96`
- 背景：透明
- 構圖：站姿正視角略偏 `3/4`，符合戰棋遠距辨識
- 必須先做 silhouette，再做紋理
- 禁止內建：
  - 地板底盤
  - 陰影
  - 陣營圈
  - 選取框

目前專案最佳匯入設定：

- `128x128` 圖請設 `Pixels Per Unit = 64`
- `96x96` 圖請設 `Pixels Per Unit = 48`

原因：

- 專案目前 fallback 角色是 `48px @ 24 PPU`，等效世界尺寸是 `2.0`
- 若正式圖沿用這個世界尺寸，`128/64 = 2.0`，`96/48 = 2.0`
- 這樣可最大程度對齊 `Assets/Scripts/Battle/Units/Unit.cs` 內既有縮放與裝飾位置

Pivot 規則：

- `v1 一律使用 Center Pivot`
- 不建議直接改成 `Bottom Center`

原因：

- `Unit.cs` 目前的陰影、陣營圈、血條、名字、選取框，都是圍繞中心錨點配置
- 若改成 `Bottom Center`，需要再補一輪程式修正

### 5.3 Weapon Icon

- 格式：`PNG`
- 畫布：`128x128`
- 背景：透明
- 只保留武器主體，不加外框，不加字
- 建議方向：斜向 `20 - 35` 度，利於 icon 辨識

## 6. Unity 匯入設定

### 6.1 Portrait

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`
- Mesh Type: `Full Rect`
- Mip Maps: `Off`
- Compression: `None`
- Filter Mode: `Bilinear`

### 6.2 Battle Sprite

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`
- Mesh Type: `Full Rect`
- Mip Maps: `Off`
- Compression: `Normal` 或 `None`
- Filter Mode: `Bilinear`
- Pivot: `Center`
- Pixels Per Unit:
  - `128x128 -> 64`
  - `96x96 -> 48`

### 6.3 Weapon Icon

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`
- Mesh Type: `Full Rect`
- Mip Maps: `Off`
- Compression: `None`
- Filter Mode: `Bilinear`

## 7. UnitVisualDefinition 欄位標準

建立 `UnitVisualDefinition` 時請遵守下列規格：

| 欄位 | 規則 |
| --- | --- |
| `unitId` | 必須與遊戲內單位 ID 完全一致 |
| `heroProfile` | 主角與有專屬立繪的 Boss 設 `true`，一般兵設 `false` |
| `archetype` | 就算有正式圖也要選最接近的 archetype，供 fallback 使用 |
| `frameStyle` | 主角 `Hero`，Boss `Boss`，一般兵 `Common` |
| `useCustomPalette` | 正式資產建議設 `true` |
| `primaryColor` | 角色主色，用於 UI 與 fallback 協調 |
| `secondaryColor` | 甲片、布料、金屬副色 |
| `accentColor` | 高亮色，建議留給飾邊、陣營識別 |
| `frameColor` | 頭像框與選取外框色 |
| `markerColor` | 地面陣營 marker 色 |
| `portraitBackdropColor` | 頭像背板色，建議低彩低明度 |
| `battleScale` | 初值建議 `1.00`，可微調在 `0.92 - 1.08` |
| `portraitSprite` | 專屬立繪 |
| `battleSprite` | 戰場小比例角色 |
| `weaponIcon` | 武器圖示 |
| `factionMarker` | 通常先留空 |
| `selectionFrame` | 通常先留空，除非主角或 Boss 有專屬框 |
| `idleAnimationController` | 沒有輕量待機動畫就留空 |

## 8. 角色生產順序

### P0：主角與 Boss 專屬

先做這 12 個單位：

- `player-liu-bei`
- `player-guan-yu`
- `player-zhang-fei`
- `player-huang-zhong`
- `player-zhuge-liang`
- `player-zhao-yun`
- `player-ma-chao`
- `enemy-zhang-bao`
- `enemy-zhang-liang`
- `enemy-pursuit_commander`
- `enemy-xiahou-dun`
- `enemy-xiahou-yuan`

每個都要有：

- 專屬 portrait
- 專屬 battle sprite
- 專屬或半專屬 weapon icon

### P1：共用雜兵 archetype

先做 6 套即可覆蓋目前大多數敵軍：

- `archetype-yellow-turban-guardian`
- `archetype-yellow-turban-ranger`
- `archetype-yellow-turban-raider`
- `archetype-wei-guardian`
- `archetype-wei-ranger`
- `archetype-wei-raider`

選做：

- `archetype-wei-commandant`

### P2：變體與精修

- 同 archetype 的頭盔差異
- Boss 專屬 selection frame
- 輕量 idle animation
- 角色 promotion 後的新立繪

## 9. silhouette 指導

### 主角

- `劉備`：冠飾、長袍、較文雅的肩線，劍意象
- `關羽`：高冠、長鬚、長柄兵器、穩重站姿
- `張飛`：寬肩、濃鬚、重甲量感、強壓迫感
- `黃忠`：老將鬚眉、披風與弓形輪廓
- `諸葛亮`：羽扇、文士帽、長袖、輕盈輪廓
- `趙雲`：白甲、長槍、乾淨俐落、槍尖輪廓清楚
- `馬超`：西涼騎將感、長槍、披風與盔飾偏銳利

### 敵軍

- `黃巾軍`：布甲、頭巾、粗獷、低秩序感
- `魏軍守軍`：重甲、盾牆、結構穩定
- `魏軍弓兵`：細長、背弓輪廓明顯
- `魏軍騎突`：斜向動勢、槍與披風拉出速度感

## 10. 交付流程

1. 企劃或美術依 `Docs/character-asset-manifest.csv` 確認單位與資產 key。
2. 美術依 `Docs/templates/character-visual-brief-template.md` 開單。
3. 美術輸出 PNG 至 `Assets/Art/...` 對應目錄。
4. Unity 端建立 `Assets/Resources/UnitVisualDefinitions/<unit-id>.asset`。
5. 將 `portraitSprite`、`battleSprite`、`weaponIcon` 綁定到 `.asset`。
6. 在 Play Mode 內檢查：
   - 頭像是否被裁切
   - 戰場角色是否過大或過小
   - 選取框、陣營圈、血條是否對齊
7. 若比例不準，只先調 `battleScale`，不要先重畫圖。

如果已經依本文件建立 `Docs/character-asset-manifest.csv` 與 `Assets/Art/...` 目錄，可直接執行：

- `Phalanx Chronicle/Visuals/Prepare Character Art Pipeline`
- `Phalanx Chronicle/Visuals/Sync Unit Visual Definitions`

## 11. 最低可交付標準

一個角色可視為完成，至少需要：

- `portrait` 1 張
- `battle sprite` 1 張
- `UnitVisualDefinition` 1 份

完整版本再加上：

- `weapon icon`
- `idleAnimationController`
- 專屬 `selectionFrame`
