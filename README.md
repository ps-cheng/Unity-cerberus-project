# Cerberus Project
2D橫向卷軸遊戲原型 - 三頭犬

從沉睡中醒來的地獄三頭犬，在被企業統治的賽博龐克城市裡找回自己的主人。三顆頭是同一個身體裡的三個個性，玩家可以隨時切換：

| 頭 | 個性 | 戰鬥風格 | 特殊技 |
|---|---|---|---|
| 賽 Cer | 古板、嚴肅 | 防禦型、中距離，拐杖 | 格擋（時機抓準可完美格檔） |
| 伯特 Ber | 桀驁不遜 | 速度型、遠距離，手槍 | 衝刺（無敵） |
| 洛斯 Rus | 野性，不會講人話 | 力量型、近距離，爪子 | 狂暴（傷害提高，受傷也提高） |

世界觀和美術規則見 `Docs/world.md`、`Docs/art-style.md`。

## Demo 內容

第一關「赫托斯重工保管庫」，約 2～4 分鐘：

1. 開始畫面 → 開場文字卡（可跳過）
2. 起點：教學招牌（移動、跳躍）
3. 巡邏街道：地面保全機器人 → 第一架無人機（提示切到伯特用槍打）→ 混合
4. 重生點 → 保管庫大門
5. 系統廣播，Boss「塔洛斯」登場。打腳踝的弱點
6. 擊敗塔洛斯 → 取得冥河硬幣 → 結束畫面

死亡會回到最後經過的重生點；在 Boss 戰中死亡，Boss 戰從頭開始。

## 操作

| 動作 | 鍵盤 | 手把 |
|---|---|---|
| 移動 | A / D 或 ← / → | 左搖桿 |
| 跳躍 | Space | A |
| 輕攻擊 | J / Z / 滑鼠左鍵 | X |
| 重攻擊 | K / X / 滑鼠右鍵 | Y |
| 特殊技 | L / C | LB |
| 切換頭 | ↑ / ↓ | LT / RT |
| 暫停 | Esc | Start |

## 執行

- **Unity 版本**：6000.6.7f1（URP 2D）。用 Unity Hub 開啟專案資料夾。
- **編輯器裡玩**：開啟 `Assets/Scenes/Level01.unity`，按 Play。
- **打包**：
  - File → Build Profiles → Windows。
  - Build Settings 裡只啟用 `Level01`（`SampleScene`、`Level02` 是原型期的場景，保留但不打包）。
  - 輸出到 `Builds/`（不進版本控制）。

## 專案結構

```
Assets/
  _Project/
    Scripts/
      Player/      玩家移動、戰鬥、換頭、特殊技
      Enemies/     地面機器人、無人機、雷射、塔洛斯（Boss/）
      Combat/      傷害、招式資料、子彈
      Level/       關卡機關：重生點、教學招牌、保管庫大門、Boss 戰流程、視差背景、平台外觀
      Feedback/    打擊感：hit-stop、鏡頭震動、特效（CombatVfx）、閃白、暫停狀態
      Audio/       音效播放（SfxPlayer）和音效表（SfxLibrary）
      UI/          開始／暫停／結束畫面、換頭台詞、系統廣播、換頭轉盤
    Data/          招式、頭、特殊技、音效表等 ScriptableObject
    Editor/SpriteGenImport/  sprite-gen 角色圖集匯入工具
    Shaders/       閃白（SpriteFlash）和加亮（Additive）shader
  Art/             角色圖集、場景、敵人、Boss、道具、特效、UI 圖
  Audio/SFX/       暫用音效（程式合成）
  Fonts/NotoSansTC/  中文字型（SIL OFL 1.1）
Docs/              世界觀、美術規則、demo 計畫和進度
Tools/Audio/       音效合成腳本
```

## 調整數值的地方

- **招式、冷卻、移動**：`Assets/_Project/Data/` 底下的資產，以及 Player 身上的元件。
- **音量**：`Assets/_Project/Data/Audio/SfxLibrary.asset`（總音量和每個音效的音量）。
- **特效外觀**：
  - 場景 `Systems/Presentation` 上的 `CombatVfx`（顏色、數量）。
  - Player 上的 `PlayerPresentation`（出招軌跡的位置和大小、三顆頭的識別色）。
- **換頭台詞**：場景 `Characters/HeadSwitchBark`。
- **按鍵表文字**：`Systems/UI/ScreensCanvas` 上的 `GameFlowUI` → Controls Text。
- **Boss 戰鏡頭**：`Environment/Vault/BossArena` 上的 `BossEncounter`（鏡頭範圍、拉遠的大小）。

## 美術和音效流程

- **角色圖集**：
  - 用 sprite-gen 產生（`D:\Codex\sprite-work`）。
  - 用 `Tools/Cerberus/Import Sprite-Gen Atlases` 匯入，或在 `CerberusCharacters` 資產上按按鈕。
  - 會切格、建動畫，並填入每顆頭的 Animator Override。
- **場景素材**：用 sprite-gen 生成後去背、裁切，原始檔留在 `D:\Codex\sprite-work\env\`。
- **音效**：
  - `python Tools/Audio/synth_sfx.py` 重新產生全部 12 個暫用音效。
  - 只用 Python 標準函式庫，結果固定。

## 已知問題

- 主角圖放大成 1.75 倍，比碰撞體大，所以敵人和塔洛斯看起來比設定小，伯特的槍口火光也比圖上的槍低。要不要改回 1 倍待決定（見 `Docs/demo-progress.md`）。
- 洛斯攻擊動畫的出招格比判定晚 0.11～0.2 秒。
- 音效是程式合成的暫用版，沒有背景音樂。

## 授權

- 中文字型 Noto Sans TC 採用 SIL Open Font License 1.1，說明在 `Assets/Fonts/NotoSansTC/LICENSE.md`。
- TextMesh Pro 附帶的 Liberation Sans 也是 OFL。
