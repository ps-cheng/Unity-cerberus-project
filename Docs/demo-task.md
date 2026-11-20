# Demo 製作任務

目標：把 Cerberus 第一關做成可以展示的 demo。玩家能從開始畫面一路玩到結束畫面，第一次玩的人不需要額外說明。

開始之前先讀：

- `Docs/world.md`：世界觀、第一關的內容、demo 會用到的文字
- `Docs/art-style.md`：美術風格和素材清單
- `D:\Codex\sprite-work\CLAUDE.md`：角色動畫素材和 Codex 生圖的規則（這個檔案不在目前的專案資料夾，不會自動讀取，要手動打開）

## 資料夾分工

- **`D:\Unity\Cerberus Project`**：Unity 專案，有 git。只放最終要用的素材和程式。
- **`D:\Codex\sprite-work`**：所有生圖的工作區，沒有 git。原始生成圖、中間檔、備份都留在這裡，不要放進 Unity 專案。
  - 角色動畫圖集在 `runs\Cer`、`runs\Ber`、`runs\Rus`。
  - 這次新生成的場景素材放在 `env\`，每項素材一個子資料夾。

## 執行方式

- **自己往下做，不用每一步問我。** 照這份文件和你的判斷執行，遇到下面「停下來的情況」才停。
- **不改我已經調好的數值**：攻擊的前搖、判定、後搖時間、技能冷卻、移動速度、Boss 的參數。新增的參數一律露出到 Inspector 讓我調。
- **不刪除既有的素材和程式**。被取代的舊素材移到 `Assets/Art/_Legacy/`。
- **每個階段結束時 commit 一次**，只 commit 不 push。
- **每個階段結束時更新 `Docs/demo-progress.md`**：完成了什麼、用了幾次生圖、遇到什麼問題、需要我決定的事。我回來時會先看這個檔案。
- 用 Unity MCP 做大改動之前先存場景。MCP 斷線或被 Unity 的彈出視窗卡住時，等一下再重試幾次；還是不行就記錄下來，先做不需要 Unity 的工作（例如生圖、寫程式、寫文件）。

## 停下來的情況

遇到以下任何一種，停下來並更新 `Docs/demo-progress.md`：

- Codex 生圖額度用完，或生圖次數達到上限（20 次）。這時先把不需要生圖的階段做完，再停。
- git 出現衝突，或需要刪除、覆蓋我寫的程式或設定。
- Unity MCP 斷線且無法恢復。
- 同一個階段失敗兩次。

---

## 階段 0：準備

1. 確認 `git status` 是乾淨的。不乾淨就停下來問我。
2. 開一個新分支 `demo/art-pass`，之後的 commit 都在這個分支上。
3. 檢查 `.gitignore` 有排除 `Library/`、`Temp/`、`Obj/`、`Logs/`、`UserSettings/`、`Build/`、`Builds/`，缺的補上。
4. 建立 `Docs/demo-progress.md`。
5. Commit：`docs: add demo plan, world and art style docs`。

## 階段 1：角色圖集匯入工具（不花生圖額度）

做一個 Unity Editor 工具，把 sprite-gen 產出的角色圖集匯入 Unity。這同時是給面試官看的「自製 Editor 工具」作品，程式要寫得乾淨、有註解。

1. 讀 `runs\<名稱>\manifest.json` 和 `sprite-sheet-alpha.png`，複製到 `Assets/Art/Characters/<名稱>/`。
2. 依 manifest 的 frame_layout 自動切圖。這是手繪風格，不是像素圖：Filter Mode 用 Bilinear，壓縮用高品質。
3. 錨點設在腳底。sprite-gen 的格子是 384×256、地面線在 y=231，換算錨點時以 manifest 的資料為準。
4. Pixels Per Unit：先查目前玩家碰撞體的大小，選一個讓角色看起來跟碰撞體一樣高的值，三顆頭用同一個值。把數值記錄下來。
5. 依 manifest 每個狀態的 fps 和是否循環，自動建立 Animation Clip。
6. 建立一個共用的 Animator Controller（狀態：`idle`、`run`、`light-attack`、`heavy-attack`），每顆頭各一個 Animator Override Controller。
7. 接到遊戲裡：
   - 移動時播 run，停下播 idle，攻擊時播對應的攻擊，播完回到 idle 或 run。
   - 角色往左走時水平翻轉。
   - **切頭時接續動畫進度**：記下目前的狀態和 normalized time，換完 Override Controller 後從同樣的位置繼續，不能從頭開始。
   - **攻擊時間以程式為準**：調整攻擊狀態的播放速度，讓動畫總長度等於程式裡的攻擊時間。把每個攻擊「出招那一格」的時間點和程式開啟判定的時間點差多少，記在進度檔裡，先不要改程式。
   - run 的播放速度跟移動速度連動，露出一個倍率參數。
8. 有圖集的頭就匯入；還沒有圖集的頭，先用 `D:\Codex\sprite-work\bases\` 裡的母版靜態圖去背後頂著，並記在進度檔裡。
9. Commit：`feat: sprite-gen atlas importer and character animation`。

## 階段 2：場景素材生成（在 `D:\Codex\sprite-work` 進行）

1. 照 `Docs/art-style.md` 的素材清單和順序生成，每次都加上「共通風格描述」。
2. 單張素材的生成方式：先看 sprite-gen 的文件有沒有適合單張圖的流程（例如 `workflow --kind image`），沒有的話直接用 `codex exec` 搭配 `$imagegen`。
3. 遵守 `D:\Codex\sprite-work\CLAUDE.md` 的額度規則：每次最多 5 張參考圖、同一項失敗最多重生一次。這次總上限 20 次，每次生成都記在進度檔裡（第幾次、生成什麼、成功或失敗）。
4. 後製：綠幕去背、裁掉多餘邊界、背景圖處理成左右無縫接續、平台切出 9-slice 的左中右、零件圖拆成獨立的檔案。
5. 完成的素材複製到 Unity 專案：`Assets/Art/Environment/`、`Assets/Art/Enemies/`、`Assets/Art/Boss/`、`Assets/Art/Props/`。原始檔留在 `D:\Codex\sprite-work\env\`。
6. 某一項做不出來時，在進度檔記下來並繼續下一項，之後在遊戲裡用色塊頂著。
7. Commit：`art: add environment, enemy, boss and prop assets`。

## 階段 3：把美術接進遊戲

1. **視差背景**：遠景、中景、近景三層，跟著攝影機用不同速度移動，速度比例露出到 Inspector。保管庫房間改用保管庫背景。
2. **平台**：Sprite 的 Draw Mode 設成 Sliced，碰撞體跟著寬度調整。現有的平台換成新素材，位置和大小先不動。
3. **敵人**：地面巡邏機器人和飛行無人機換上新素材，原本的行為邏輯不改。移動時的晃動、浮動用程式做。無人機的雷射倒數圈對齊到槍口位置。
4. **塔洛斯**：用零件在 Unity 裡組起來，待機的晃動、攻擊動作用程式驅動零件，不需要逐格動畫。觸手雷射沿用現有的雷射邏輯。腳踝的弱點平時暗著，進入破綻狀態時發光。
5. Commit：`feat: integrate environment, enemies and Talos visuals`。

## 階段 4：關卡

照 `Docs/world.md` 的關卡路線組成第一關。現有的小怪區和 Boss 房可以重新排列、延伸。

1. **起點**：沒有敵人的一小段路。
2. **巡邏街道**：地面和上層平台，敵人一次一種地出現：先一隻慢慢走的地面機器人，再出現第一隻無人機，之後才混合配置。
3. **重生點**：放在保管庫大門前。死亡後回到最近的重生點，Boss 戰從頭開始。
4. **保管庫**：大門開啟，顯示系統廣播，塔洛斯登場。擊敗後，冥河硬幣出現在展示櫃，玩家碰到就拿到，進入結束畫面。
5. **操作教學**：在關卡裡放觸發區，玩家第一次走到時跳出提示，用過一次就不再出現。提示寫在空白的霓虹招牌上，在 Unity 裡疊字。按鍵照程式實際綁定的鍵顯示，不要用猜的。至少要有：移動和跳躍（起點）、攻擊（第一隻地面敵人前）、切換頭（第一隻無人機出現時，提示可以切到伯特用射擊）。
6. **中文字型**：TextMeshPro 預設字型沒有中文。用 Noto Sans TC（OFL 授權，可以商用）建立字型資源；拿不到的話停下來告訴我，不要用其他來源不明的字型。
7. 整段長度約 2～4 分鐘。
8. Commit：`feat: assemble level 1 with tutorial, checkpoint and ending`。

## 階段 5：回饋和畫面

1. **特效**（全部用 Unity 的 Particle System、Trail、LineRenderer 做，不用生圖）：
   - 切頭特效：一閃加煙霧；切到洛斯時多一個布料碎片炸開的效果。這是遊戲的核心概念，要做得最用心。
   - 賽的拐杖揮擊軌跡、伯特的槍口火光、洛斯的爪痕
   - 擊中火花、敵人死亡爆散
   - 塔洛斯雷射的蓄力光芒、腳踝弱點的閃光
2. **音效**：用程式合成暫用音效（wav），不要從網路下載：拐杖揮擊、槍聲、爪擊、擊中、敵人爆炸、切頭、雷射蓄力、雷射發射、格擋成功、受傷、拿到硬幣、介面按鍵。音量統一調整過，露出到一個集中管理的地方。
3. **切頭台詞**：切頭時在角色頭上跳出小對話框，顯示 `Docs/world.md` 裡那三句，名字用各自的識別色，約 1 秒後消失。
4. **開場文字卡**：黑底兩行字（內容在 `Docs/world.md`），可以按鍵跳過。
5. **開始畫面**：遊戲名稱、開始遊戲、完整按鍵表。
6. **暫停畫面**：繼續、重新開始、按鍵表。
7. **結束畫面**：內容在 `Docs/world.md`，可以重新開始。
8. 背景音樂這次不做。
9. Commit：`feat: VFX, SFX, head-switch barks and UI screens`。

## 階段 6：收尾

1. 用 Play Mode 從開始畫面完整玩一次：開場、教學提示、敵人、重生、Boss、拿硬幣、結束畫面，每一段都要能動。把遇到的問題列在進度檔。
2. 輸出 Windows 版本到 `Builds/`（不進 git）。
3. 寫 `README.md` 草稿，給面試官看：
   - 一段遊戲介紹和操作方式
   - 架構：三顆頭的模組化設計、切換機制、Boss 狀態機、角色圖集匯入工具
   - AI 協作流程：Unity MCP 加 Claude Code 開發、Codex 加 sprite-gen 生成美術、用 CLAUDE.md 管理生成規則
   - 接下來打算做什麼
4. Commit：`docs: add README and demo build notes`。
5. 在 `Docs/demo-progress.md` 寫最終報告：每個階段的結果、總共用了幾次生圖、已知問題、需要我決定或手動調整的地方（例如 Boss 手感、攻擊時間的落差）。
