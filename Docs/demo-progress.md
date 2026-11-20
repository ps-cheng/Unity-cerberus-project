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

## 階段 3：把美術接進遊戲 ✅

新程式（都只管外觀，沒有改任何戰鬥、移動邏輯和數值）：

| 檔案 | 用途 |
|---|---|
| `Scripts/Level/ParallaxLayer.cs` | 視差背景。`followFactor` 在 Inspector 調：0 = 固定在世界，1 = 黏在畫面上，負值 = 比平台移動更快（近景） |
| `Scripts/Level/PlatformSkin.cs` | 平台外觀依碰撞體寬度自動鋪圖（Tiled，左右端點不拉伸），頂面對齊碰撞體頂面。編輯器裡改大小即時更新 |
| `Scripts/Enemies/EnemyMotionWobble.cs` | 程式做的晃動：機器人行進顛簸、無人機懸浮和傾斜 |
| `Scripts/Enemies/LaserChargeRing.cs` | 無人機槍口上的倒數圈，瞄準時出現、充能時收縮到槍口，鎖定時變白 |
| `Scripts/Enemies/Boss/TalosRig.cs` | 塔洛斯零件動畫：呼吸、走路擺腿、持盾手臂擺動、腳踝弱點在破綻時發光 |

- **視差背景**（Level01）：
  - 三層 `followFactor` 分別是：遠景天際線 (0.85, 0.9)、中景街道 (0.55, 0.6)、近景剪影 (−0.3, 1)。
  - 中景用顏色壓暗到 0.42，讓角色和平台比背景亮。
  - Level02（Boss 房）改用保管庫背景。
- **平台**：
  - 地面類（Floor、StartStep、Plateau、Cliff）頂面鋪街道路面，下方是暗色本體。
  - 三個浮空平台各用一種新平台圖。
  - 位置和大小都沒動，原本用 Transform 縮放的寫法保留。
- **敵人**：
  - 機器人和無人機換上新圖，原本的色塊零件（眼睛、手臂、翅膀）改成停用，沒有刪除。
  - 無人機的 FirePoint 移到上方槍口，並且跟著機身翻面和浮動，雷射從槍口射出。
- **塔洛斯**：
  - 用零件組成 `Visual/Rig`（後腿、前腿、軀幹、持刀手臂、持盾手臂）。
  - 原本程式控制的 SwordPivot、Shield、Tentacles 掛到新的軀幹底下，所以揮刀角度、舉盾、觸手伸縮和雷射邏輯都沿用。
  - 舉盾時的 Shield 改成盾前方的橘色能量光。
  - 觸手換上觸手節和雷射發射口。
  - 已在 Play Mode 確認破綻狀態：刀垂下、身體下沉、腳踝發光。
- 敵人和 Boss 預製物的 `Debug` 子物件（偵測範圍線、攻擊範圍色塊）改成停用，demo 畫面才乾淨。要除錯時再打開。

### 發現的問題
- **主角比敵人大很多**：
  - 敵人和 Boss 的圖照碰撞體大小設定，也就是 art-style 的比例。
  - 但主角圖放大了 1.75 倍，畫面上約 2.8 單位高，所以機器人只有主角的一半、塔洛斯只有主角的 1.4 倍，跟設定的 0.8 倍、2.5 倍不同。
- **受擊閃白看不到**：
  - 換成圖片後，敵人底色是白色，EnemyBase 的受擊「閃白」看不出來。
  - 前搖變色（染紅）和硬直變色（染藍）仍然看得到。
  - 階段 5 會改用材質做真正的閃白。

## 階段 4：關卡 ✅

第一關合併成一個場景 `Level01`：街道 + 保管庫，總長約 143 單位。原本的 `Level02`（Boss 房）保留沒刪，但不再使用。

### 關卡路線

| 區段 | x 範圍 | 內容 |
|---|---|---|
| 起點 | 0–14 | 沒有敵人。招牌教學：移動、跳躍 |
| 巡邏街道 | 14–34 | 招牌教學：輕／重攻擊，接著第一隻地面機器人（x 24） |
| 第一架無人機 | 27–46 | 招牌教學：切換頭（提示切到伯特用槍打），無人機在高台上方（42, 5.5） |
| 混合區 | 46–100 | 招牌教學：特殊技。地面機器人 x 54／61／83，無人機（79, 8），原本的平台和斷崖 |
| 重生點 | 105 | 經過時指示燈從暗轉青，之後死亡回到這裡 |
| 保管庫大門 | 111 | 靠近時圓門往上滾開；上方牆掛赫托斯標誌 |
| 保管庫 | 111–143 | 換成保管庫背景；街道近景剪影淡出 |

- 敵人的配置原本就是「先地面、再無人機、再混合」，所以位置沒動。
- 原本的終點門（x 104）停用，沒有刪除。
- 原型期的除錯 HUD（`ActionHintHud`，左上角操作說明）在這個場景停用。階段 5 會換成正式的介面。

### Boss 戰流程（`BossEncounter`）
1. 玩家走進保管庫：大門關上並鎖住，鏡頭鎖在房間內。
2. 畫面上方的系統廣播逐字打出「赫托斯重工 保全單位 塔洛斯，啟動。」，期間玩家不能操作。
3. 塔洛斯從上方落下，落地時鏡頭震動，玩家恢復操作。
4. 玩家死亡：重生時 Boss 移除、大門解鎖、鏡頭範圍還原。再走進去，Boss 戰從頭開始。
5. 擊敗塔洛斯：1.2 秒後冥河硬幣出現在背景的玻璃展示櫃裡（漂浮、發光）。走上展示櫃前的台階就算撿到，進入通關畫面。

Boss 本身的程式和數值都沒動。場景裡的 Boss 是停用的樣板，每次開戰複製一份。

### 新增的程式
- `Level/PlayerTriggerZone`：觸發區的共同基底（OverlapBox，跟 LevelGoal 同一個做法）。
- `Level/Checkpoint`：設定重生點。`PlayerDamageReceiver` 加了 `SetRespawnPoint`。
- `Level/TutorialPrompt`：玩家進入範圍時招牌亮起，做過一次指定的操作後永久熄滅。
- `Level/VaultGate`：可上鎖的圓形大門。
- `Level/BossEncounter`：Boss 戰流程（見上）。
- `Level/CameraRangeVisibility`：攝影機在指定範圍外時淡出圖層。
- `Level/FloatingPickupVisual`：硬幣的彈出、漂浮和光暈。
- `UI/BroadcastBanner`：系統廣播的橫幅。
- `CameraFollow` 加了 `SetBounds` 和 `ResetBounds`。

### 字型
- 匯入 TMP Essential Resources。
- 中文字型用 Noto Sans TC（Regular、Bold），授權是 SIL OFL 1.1，可以隨遊戲散布。
- 字型檔來自系統安裝的字型（使用者字型資料夾），授權說明在 `Assets/Fonts/NotoSansTC/LICENSE.md`。
- 做成動態 SDF 字型（只在用到時產生字），並設為 TMP 預設字型。

### 測試（Play Mode）
- 重生點啟用 → 大門打開 → 走進保管庫：大門鎖上，廣播出現，Boss 落地。
- 戰鬥中死亡：回到重生點（105, 0.8），Boss 移除，大門解鎖。
- 再進去並擊敗 Boss：硬幣出現，走到台階前觸發通關。
- Console 沒有錯誤。

### 發現的問題
- 金庫裡的展示櫃道具（display-case）太小，跟背景畫的大玻璃櫃重複。所以停用道具，硬幣直接放在背景的玻璃櫃裡。
- 通關畫面還是原型期的「STAGE CLEAR」，階段 5 會換成結局畫面。
- Build Settings 裡還有 `SampleScene` 和 `Level02`，階段 6 打包時會整理。

## 階段 5：回饋和畫面 ✅

這個階段沒有生圖，次數維持 14 次。

### 特效
全部用 Particle System、LineRenderer、TrailRenderer 在程式裡組出來，沒有生圖。

- **產生器**：`Feedback/CombatVfx`。
  - 放在場景的 `Systems/Presentation`。
  - 顏色、數量都露出在 Inspector。
- **切頭**（最用心的部分），依序疊上：
  - 白色閃光；
  - 識別色光圈；
  - 往外噴的閃點；
  - 一圈煙把身體包住，遮住換裝的瞬間；
  - 角色整張閃白 0.18 秒。
  - 切到洛斯時，西裝、襯衫、領帶顏色的布料碎片會炸開，旋轉著飄落。
- **出招**：在判定開始的那一幀播出，跟實際打到的時間一致。
  - 賽：拐杖的弧形軌跡（外層光暈 + 內層亮芯，尾端收掉）。
  - 伯特：槍口火光加火花。子彈從黃色方塊改成發亮的子彈加拖尾。
  - 洛斯：三道爪痕依序劃下。
  - 位置和大小在 Player 的 `PlayerPresentation` → Attacks 調整。
- **擊中**：
  - 火花往打擊方向噴出，帶一點目前那顆頭的識別色。
  - 受擊閃白改用材質做（新的 `Cerberus/SpriteFlash` shader + `SpriteFlash` 元件），白色為底的圖也看得出閃白。
  - 地面機器人、無人機、塔洛斯的零件都已套用。
- **敵人死亡**：閃光、火花、帶重力的碎片、一團煙。規模依敵人圖的大小，塔洛斯的爆炸比較大。
- **雷射蓄力**（`LaserPresentation`，無人機和塔洛斯觸手都有）：
  - 發射口的光暈從小長大、越閃越快；
  - 鎖定後變白；
  - 原本的蓄力圈保留。
- **塔洛斯弱點**：露出弱點的瞬間有一圈亮光和往外散的閃點（加在 `TalosRig`）。

### 音效
- **合成**：`Tools/Audio/synth_sfx.py`（只用 Python 標準函式庫，固定亂數種子，可以重新產生）。
- **檔案**：12 個 wav 在 `Assets/Audio/SFX`：
  - 拐杖揮擊、槍聲、爪擊、擊中；
  - 敵人爆炸、切頭；
  - 雷射蓄力、雷射發射；
  - 格擋成功、受傷；
  - 拿到硬幣、介面按鍵。
- **音量集中在一個地方**：`Assets/_Project/Data/Audio/SfxLibrary.asset`。
  - 總音量 + 每個音效的音量、音高浮動、最短間隔（避免同一幀疊加）。
- **播放**：`Audio/SfxPlayer`，任何地方呼叫 `SfxPlayer.Play(SfxId.Hit)`。
- **對應**：
  - 重攻擊用低一點的音高；
  - 一般格擋用低音高、完美格檔用高音高。

### 切頭台詞
- `UI/HeadSwitchBark`：切頭時角色頭上跳出深色對話框。
- 外框和名字用識別色，約 1 秒後淡出。
- 對話框不跟著角色翻面。
- 台詞在 Inspector 設定，內容照 `world.md`。

### 畫面
全部放在 `Systems/UI/ScreensCanvas`，由 `UI/GameFlowUI` 控制。
- **開始畫面**：
  - 遊戲名稱 CERBERUS、「第一關 赫托斯重工保管庫 · DEMO」、開始遊戲按鈕、完整按鍵表（鍵盤和手把）。
  - Enter、Space、手把 A／Start 或滑鼠都可以開始。
- **開場文字卡**：黑底，兩行字依序淡入，約 7 秒後自動進入遊戲；任意鍵跳過。
- **暫停畫面**：Esc 或手把 Start，有繼續、重新開始和按鍵表。
- **結束畫面**：
  - 撿到硬幣 1.2 秒後出現：硬幣圖、「取得：冥河硬幣」「DEMO END 感謝遊玩」，以及重新開始按鈕。
  - 重新開始會重新載入場景，直接進入遊戲（不再顯示開始畫面和文字卡）。
- **共用**：
  - 按鍵表的文字寫在 `GameFlowUI` 的 controlsText，開始畫面和暫停畫面共用。
  - 暫停狀態統一由 `GamePause` 管理。`HitStop` 結束時不會把暫停解除。暫停中 `PlayerController` 不讀輸入。

### 改到的既有程式（只加東西，沒有改數值）
- `EnemyBase`：加了 `IsHitFlashing` 和靜態事件 `AnyDied`。
- `HitStop`：結束時如果在暫停中，保持 timeScale = 0。
- `PlayerController`：暫停中直接跳過 Update。
- `TalosRig`：露出弱點時呼叫閃光特效。

### 測試（Play Mode）
- 開始畫面 → 文字卡 → 遊戲 → 暫停／繼續 → 撿硬幣 → 結束畫面 → 重新開始：都正常。重新開始會直接進入遊戲。
- 三顆頭切換的特效和台詞、三種出招特效、擊中和死亡爆散、無人機雷射蓄力光芒：都已截圖確認。
- 塔洛斯弱點閃光和觸手雷射的光芒沒有截圖確認，階段 6 完整跑一遍時會看到。
- Console 沒有錯誤。

### 發現的問題
- **除錯外框**：Player 身上的除錯判定框（`CombatZoneVisualizer`）在遊戲中會畫出紅色方塊，所以場景裡的除錯顯示元件全部停用。要除錯時在 Inspector 打開。
- **伯特的槍口位置**：判定上的槍口位置比圖上的槍低。主角圖放大 1.75 倍造成，跟「主角大小」那題是同一件事。火光只往上移了 0.15，太多會跟子彈出現的位置對不上。

## 需要你決定的事

- **主角大小**：要不要把 Player 的 `Visual/ArtRoot` 縮放從 1.75 改回 1？改回 1 的話主角跟碰撞體一樣高，敵人和 Boss 的比例就會符合設定。目前沒有改。
- **三顆頭的識別色**：文件沒有指定，我先訂成賽 淺藍 `#73BFFF`、伯特 橘 `#FFB061`、洛斯 紅 `#FF524D`。換頭特效、台詞名字、擊中火花都用這組。要改的話在 Player 的 `PlayerPresentation` → Heads。
- 洛斯攻擊的出招格比判定晚 0.11～0.2 秒（見階段 1 的表格）：要調程式的前搖，還是調動畫格的時間？

（目前沒有）
