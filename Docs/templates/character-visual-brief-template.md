# 角色視覺需求單模板

把下列欄位填完，就可以直接交給美術或外包。

## 基本資訊

- `unitId`:
- `displayName`:
- `faction`: `Player / Enemy`
- `role`: `Commander / Guardian / Ranger / Scout / Raider`
- `deliveryTier`: `P0-Unique / P1-Shared`
- `assetKey`:

## 角色定位

- 劇情定位：
- 戰場定位：
- 是否為主角或 Boss：
- 是否需要專屬頭像框：

## 視覺方向

- 主視覺關鍵字：
- 禁止事項：
- 主要配色：
- 次要配色：
- 高亮色：
- 材質語言：`布 / 甲片 / 金屬 / 毛皮 / 羽飾`

## silhouette 要求

- 必須一眼可見的識別：
- 頭部識別：
- 肩線識別：
- 兵器識別：
- 披風或下擺識別：

## Portrait 規格

- 輸出檔名：`<asset-key>__portrait.png`
- 畫布：`768x768`
- 構圖：`3/4 胸像`
- 安全區：四邊至少 `32px`
- 備註：

## Battle Sprite 規格

- 輸出檔名：`<asset-key>__battle.png`
- 畫布：`128x128` 優先，次選 `96x96`
- Pivot：`Center`
- 不可內建陰影與地板底盤
- 備註：

## Weapon Icon 規格

- 輸出檔名：`<weapon-id>__icon.png`
- 畫布：`128x128`
- 是否需要新畫：
- 備註：

## Unity 綁定

- `UnitVisualDefinition` 檔名：`<unit-id>.asset`
- `heroProfile`：
- `frameStyle`：
- `archetype`：
- `battleScale` 初值：`1.00`
- `portraitBackdropColor`：

## 驗收條件

- UI 頭像不裁切
- 戰場上 1 秒內辨識角色類型
- 與同陣營其他角色並排時不混淆
- 配色符合陣營語言
- 武器與職業一致
