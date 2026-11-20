# Demo 製作進度

照 `Docs/demo-task.md` 的階段執行。分支：`demo/art-pass`（只 commit，不 push）。

## 生圖次數

上限 20 次。

**目前用了 14 次**，沒有失敗、沒有重生。全部用 `sprite-gen gen --provider codex`，綠底素材加 `--transparent --alpha-mode chroma --chroma-key green` 去背。

| # | 素材 | 結果 | 備註 |
|---|---|---|---|
| 1 | 中景：街道建築 | 成功 | 風格基準。細電線有綠色溢色，後製已去除 |
| 2 | 遠景：城市天際線 | 成功 | 中央高樓頂端有赫托斯標誌 |
| 3 | 近景：前景雜物 | 成功 | 原圖不夠暗，後製壓暗成剪影 |
| 4 | 保管庫內部 | 成功 | 中央有展示台 |
| 5 | 地面平台：街道路面 | 成功 | 兩端多畫了路燈和招牌，後製裁掉只留路面 |
| 6 | 浮空平台三種 | 成功 | 警示條紋鋼樑、鷹架踏板、霓虹招牌頂 |
| 7 | 赫托斯重工標誌 | 成功 | |
| 8 | 地面巡邏機器人 | 成功 | 履帶、前方撞擊鏟 |
| 9 | 飛行無人機 | 成功 | 兩個朝前的槍口 |
| 10 | 塔洛斯完整圖 | 成功 | |
| 11 | 塔洛斯零件 | 成功 | 拆成 8 個零件 |
| 12 | 冥河硬幣 | 成功 | |
| 13 | 保管庫道具 | 成功 | 圓形金庫門、空的展示櫃（玻璃是透明的） |
| 14 | 街道道具 | 成功 | 貨箱、空白霓虹招牌框、垃圾袋、路障 |

## 階段 0：準備 ✅

- 開始前 `main` 上有上一輪刪除方塊的 4 個未 commit 檔案，經同意先 commit 成「Remove placeholder block visuals from the player」，之後 git 乾淨。
- 建立分支 `demo/art-pass`。
- `.gitignore` 已排除 `Library/`、`Temp/`、`Obj/`、`Logs/`、`UserSettings/`、`Build/`、`Builds/`，不用補。
- 環境：codex-cli 0.169.0（ChatGPT 登入）、sprite-gen venv、ffmpeg、系統已有 Noto Sans TC（OFL）。

## 階段 1：角色圖集匯入工具 ✅

### 工具
- 程式在 `Assets/_Project/Editor/SpriteGenImport/`：
  - `SpriteGenManifest.cs`：讀 manifest（用 Newtonsoft，已在 `Packages/manifest.json` 明確加上 `com.unity.nuget.newtonsoft-json`）。
  - `SpriteGenAtlasImporter.cs`：複製圖集和 manifest、設定貼圖、切圖、建 clip、更新 Controller 和 Override。
  - `SpriteGenImportProfile.cs` 和 `SpriteGenImportProfileEditor.cs`：設定資產和 Inspector 按鈕。
- 設定資產 `Assets/_Project/Editor/SpriteGenImport/CerberusCharacters.asset`。用法：
  - 在 Inspector 按「匯入全部角色」，或用選單 `Tools > Cerberus > Import Sprite-Gen Atlases`。
- 重新匯入是安全的：
  - 同名 sprite 沿用原 ID，引用不會斷。
  - clip 只覆寫 sprite 曲線，所以 Ber 衝刺手動加的位移和透明度曲線會保留。
  - Controller 已有的狀態只更新 clip 和速度設定。
- 錨點用 manifest 的 `cell.safe_margin_y` 換算：地面線 = 256 − 24 − 1 = 231，pivot = (0.5, 25/256 ≈ 0.0977)。
- 貼圖設定：Bilinear、CompressedHQ（原本是不壓縮，依任務改成高品質壓縮）、無 mipmap、Max Size 4096。

### 大小（PPU）
- PPU = **128**，三顆頭共用。站姿 204 px ÷ 128 ≈ **1.6 單位**，等於碰撞體高度 1.6（CapsuleCollider2D 0.8×1.6）。
- 但依你之前的決定，Player 的 `Visual/ArtRoot` 放大 **1.75 倍**，畫面上角色約 2.8 單位高，比碰撞體高。要跟碰撞體一致，把 ArtRoot 的 scale 改回 1 即可。

### 動畫
- 共用 Controller 的狀態改成跟 manifest 同名：`idle`、`run`、`light-attack`、`heavy-attack`，另外保留 `jump`、`special`。
  - 原本的 `walk`、`attack_light`、`attack_heavy` 已改名；Base clip 也跟著改名，GUID 不變。
  - 速度參數 `WalkSpeed` 改名為 `RunSpeed`。
  - `HeadConfig.walkAnimationSpeedMultiplier` 改名為 `runAnimationSpeedMultiplier`，有加 FormerlySerializedAs，原本的數值保留。
- 三顆頭的 Override 沿用 `Head1/2/3.overrideController`。
- 已接進遊戲並測試（Play Mode）：跑 / 待機、攻擊後回到移動、往左翻轉、切頭接續進度、攻擊播放速度縮放。這些上一輪就做好了，這次確認改名後仍正常。
- 三顆頭都有圖集，所以「用母版靜態圖頂著」不需要。

### 出招格和判定時間的落差（沒有改程式）
動畫已縮放到等於程式的總時間（前搖 + 判定 + 後搖）。「出招格」是武器第一次伸到最遠的那一格。正值 = 動畫比判定晚。

| 招式 | 程式 前搖/判定/後搖 (s) | 總長 | 格數 | 出招格 | 出招格時間 | 判定開始 | 落差 |
|---|---|---|---|---|---|---|---|
| 賽 輕攻擊 | 0.06 / 0.10 / 0.14 | 0.30 | 4 | 第 1 格 | 0.075 | 0.06 | +0.015 |
| 賽 重攻擊 | 0.28 / 0.16 / 0.50 | 0.94 | 8 | 第 2 格 | 0.235 | 0.28 | −0.045 |
| 伯特 輕攻擊 | 0.05 / 0.03 / 0.10 | 0.18 | 4 | 第 1 格 | 0.045 | 0.05 | −0.005 |
| 伯特 重攻擊 | 0.22 / 0.05 / 0.35 | 0.62 | 8 | 第 2 格 | 0.155 | 0.22 | −0.065 |
| 洛斯 輕攻擊 | 0.10 / 0.10 / 0.22 | 0.42 | 4 | 第 2 格 | 0.210 | 0.10 | **+0.110** |
| 洛斯 重攻擊 | 0.36 / 0.16 / 0.60 | 1.12 | 6 | 第 3 格 | 0.560 | 0.36 | **+0.200** |

洛斯兩招的出招格明顯比判定晚，畫面上會像是爪子還沒揮到就打中了。可以調整程式的前搖時間，或在 clip 裡讓前面幾格播快一點，由你決定。

## 階段 2：場景素材生成 ✅

- 工作區 `D:\Codex\sprite-work\env\`：
  - 每項一個資料夾，裡面有原圖、`.raw.png`、`report.json`、`gen.out`。
  - 生成腳本在 `env\scripts\`，紀錄在 `env\gen-log.tsv`。
  - 後製完成的圖在 `env\out\`。
- 後製：
  - 去背：生成時由 sprite-gen 完成。
  - 背景左右無縫：sprite-gen `background-tile`，週期 2076 px、重疊 96 px。它的自動檢查回報 needs-review，我把兩張接在一起目視檢查過，接縫看不出來。
  - 去綠色溢色、壓暗、裁邊、拆零件：`env\scripts\post.py`。依透明區塊自動拆，零件順序和數量都對。
- 匯入 Unity：
  - `Assets/Art/Environment/`：`Backgrounds/` 背景 4 張、`Platforms/` 平台 4 種、赫托斯標誌。
  - `Assets/Art/Enemies/`：地面機器人、無人機。
  - `Assets/Art/Boss/`：塔洛斯完整圖和 8 個零件。
  - `Assets/Art/Props/`：硬幣、金庫門、展示櫃、貨箱、霓虹招牌框、垃圾袋、路障。
- 大小：
  - `art-style.md` 的相對大小，剛好對應現有碰撞體（主角 1.6、地面機器人 1.4、塔洛斯 4），所以敵人和 Boss 的圖照碰撞體大小設定。
  - 機器人 1.4 高、無人機 1.2 寬、塔洛斯 4 高、金庫門 2.9 高、硬幣約 0.45（比清單的 0.15 大，不然撿取物太小看不到）。
- 平台設定了 9-slice 的左右邊界（左端、可重複的中段、右端），在 Unity 用 Tiled 模式橫向接續。

## 需要你決定的事

- 洛斯攻擊的出招格比判定晚 0.11～0.2 秒（見階段 1 的表格）：要調程式的前搖，還是調動畫格的時間？

（目前沒有）
