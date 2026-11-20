using System.Collections;
using Cerberus.Audio;
using Cerberus.Feedback;
using Cerberus.Level;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cerberus.UI
{
    /// <summary>
    /// demo 的畫面流程：開始畫面 → 開場文字卡 → 遊戲 ⇄ 暫停 → 結束畫面。
    /// 開始畫面、文字卡、暫停期間遊戲暫停（GamePause）。重新開始會重新載入場景並直接進入遊戲，不再顯示開始畫面。
    /// 按鍵表的文字在 Inspector 的 controlsText，開始畫面和暫停畫面共用。
    /// </summary>
    public class GameFlowUI : MonoBehaviour
    {
        private enum State { Title, OpeningCard, Playing, Paused, Ending }

        // 重新開始時跳過開始畫面和文字卡（場景重新載入後仍保留）
        private static bool skipIntroOnNextLoad;

        [Header("Screens")]
        [SerializeField] private CanvasGroup titleScreen;
        [SerializeField] private CanvasGroup openingCard;
        [SerializeField] private CanvasGroup pauseScreen;
        [SerializeField] private CanvasGroup endingScreen;

        [Header("Title")]
        [SerializeField] private Button startButton;

        [Header("Opening Card")]
        [SerializeField] private TextMeshProUGUI[] openingLines;
        [SerializeField] private TextMeshProUGUI skipHint;
        [Tooltip("每行淡入的時間")]
        [SerializeField] private float lineFadeDuration = 0.8f;
        [Tooltip("兩行之間的間隔")]
        [SerializeField] private float lineInterval = 1.4f;
        [Tooltip("全部顯示後停留多久自動進入遊戲")]
        [SerializeField] private float cardHoldDuration = 2.2f;

        [Header("Pause")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRestartButton;

        [Header("Ending")]
        [SerializeField] private LevelGoal goal;
        [SerializeField] private Button endingRestartButton;
        [Tooltip("撿到硬幣後多久出現結束畫面")]
        [SerializeField] private float endingDelay = 1.2f;

        [Header("Controls Table")]
        [SerializeField] private TextMeshProUGUI[] controlsLabels;
        [TextArea(6, 12)]
        [SerializeField] private string controlsText =
            "<b>動作</b><pos=28%><b>鍵盤</b><pos=72%><b>手把</b>\n" +
            "移動<pos=28%>A / D　或　← / →<pos=72%>左搖桿\n" +
            "跳躍<pos=28%>Space<pos=72%>A\n" +
            "輕攻擊<pos=28%>J / Z / 滑鼠左鍵<pos=72%>X\n" +
            "重攻擊<pos=28%>K / X / 滑鼠右鍵<pos=72%>Y\n" +
            "特殊技<pos=28%>L / C<pos=72%>LB\n" +
            "切換頭<pos=28%>↑ / ↓<pos=72%>LT / RT\n" +
            "暫停<pos=28%>Esc<pos=72%>Start";

        [SerializeField] private float screenFadeDuration = 0.25f;

        private State state;
        private float cardStartedAt;
        private Coroutine cardRoutine;

        private void Awake()
        {
            foreach (TextMeshProUGUI label in controlsLabels)
                label.text = controlsText;

            startButton.onClick.AddListener(() => { Click(); BeginOpeningCard(); });
            resumeButton.onClick.AddListener(() => { Click(); Resume(); });
            pauseRestartButton.onClick.AddListener(() => { Click(); Restart(); });
            endingRestartButton.onClick.AddListener(() => { Click(); Restart(); });

            HideInstant(titleScreen);
            HideInstant(openingCard);
            HideInstant(pauseScreen);
            HideInstant(endingScreen);
        }

        private void OnEnable() => goal.Reached += HandleGoalReached;

        private void OnDisable() => goal.Reached -= HandleGoalReached;

        private void Start()
        {
            if (skipIntroOnNextLoad)
            {
                skipIntroOnNextLoad = false;
                EnterPlaying();
                return;
            }

            state = State.Title;
            GamePause.SetPaused(true);
            Show(titleScreen, startButton);
        }

        private void Update()
        {
            switch (state)
            {
                case State.Title:
                    // 開始畫面：Enter、Space、手把 A／Start 都可以開始（滑鼠點按鈕也行）
                    if (WasPressed(Key.Enter) || WasPressed(Key.Space) || WasPadPressed(pad => pad.startButton))
                    {
                        Click();
                        BeginOpeningCard();
                    }
                    break;

                case State.OpeningCard:
                    // 開場一小段時間內不接受跳過，避免開始畫面那一下直接把文字卡跳掉
                    if (Time.unscaledTime - cardStartedAt > 0.4f && AnyKeyPressed())
                        FinishOpeningCard();
                    break;

                case State.Playing:
                    if (PausePressed())
                        Pause();
                    break;

                case State.Paused:
                    if (PausePressed())
                        Resume();
                    break;
            }
        }

        // ── 開場文字卡 ─────────────────────────────────

        private void BeginOpeningCard()
        {
            if (state != State.Title)
                return;

            state = State.OpeningCard;
            cardStartedAt = Time.unscaledTime;
            Hide(titleScreen);
            Show(openingCard, null);
            cardRoutine = StartCoroutine(PlayOpeningCard());
        }

        private IEnumerator PlayOpeningCard()
        {
            foreach (TextMeshProUGUI line in openingLines)
                line.alpha = 0f;
            skipHint.alpha = 0f;

            yield return new WaitForSecondsRealtime(0.4f);

            for (int i = 0; i < openingLines.Length; i++)
            {
                yield return FadeText(openingLines[i], lineFadeDuration);
                if (i == 0)
                    StartCoroutine(FadeText(skipHint, 0.5f, 0.5f));
                yield return new WaitForSecondsRealtime(lineInterval);
            }

            yield return new WaitForSecondsRealtime(cardHoldDuration);
            FinishOpeningCard();
        }

        private void FinishOpeningCard()
        {
            if (state != State.OpeningCard)
                return;

            if (cardRoutine != null)
                StopCoroutine(cardRoutine);
            Hide(openingCard);
            EnterPlaying();
        }

        // ── 暫停 ─────────────────────────────────────

        private void EnterPlaying()
        {
            state = State.Playing;
            GamePause.SetPaused(false);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        private void Pause()
        {
            state = State.Paused;
            GamePause.SetPaused(true);
            Show(pauseScreen, resumeButton);
        }

        private void Resume()
        {
            if (state != State.Paused)
                return;

            Hide(pauseScreen);
            EnterPlaying();
        }

        private void Restart()
        {
            skipIntroOnNextLoad = true;
            GamePause.SetPaused(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ── 結束畫面 ─────────────────────────────────

        private void HandleGoalReached()
        {
            SfxPlayer.Play(SfxId.Coin);
            StartCoroutine(ShowEnding());
        }

        private IEnumerator ShowEnding()
        {
            yield return new WaitForSecondsRealtime(endingDelay);

            // 撿到硬幣後暫停中的話先關掉暫停畫面
            HideInstant(pauseScreen);
            state = State.Ending;
            GamePause.SetPaused(true);
            Show(endingScreen, endingRestartButton);
        }

        // ── 共用 ─────────────────────────────────────

        private void Show(CanvasGroup screen, Button firstSelected)
        {
            screen.gameObject.SetActive(true);
            screen.interactable = true;
            screen.blocksRaycasts = true;
            StartCoroutine(FadeGroup(screen, 1f));

            if (firstSelected != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
        }

        private void Hide(CanvasGroup screen)
        {
            screen.interactable = false;
            screen.blocksRaycasts = false;
            StartCoroutine(FadeGroup(screen, 0f));
        }

        private static void HideInstant(CanvasGroup screen)
        {
            screen.alpha = 0f;
            screen.interactable = false;
            screen.blocksRaycasts = false;
            screen.gameObject.SetActive(false);
        }

        private IEnumerator FadeGroup(CanvasGroup group, float target)
        {
            float start = group.alpha;
            for (float t = 0f; t < screenFadeDuration; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, t / screenFadeDuration);
                yield return null;
            }

            group.alpha = target;
            if (target <= 0f)
                group.gameObject.SetActive(false);
        }

        private static IEnumerator FadeText(TextMeshProUGUI text, float duration, float target = 1f)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                text.alpha = Mathf.Lerp(0f, target, t / duration);
                yield return null;
            }

            text.alpha = target;
        }

        private static void Click() => SfxPlayer.Play(SfxId.UiClick);

        private static bool WasPressed(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;

        private static bool WasPadPressed(System.Func<Gamepad, UnityEngine.InputSystem.Controls.ButtonControl> button) =>
            Gamepad.current != null && button(Gamepad.current).wasPressedThisFrame;

        private static bool PausePressed() => WasPressed(Key.Escape) || WasPadPressed(pad => pad.startButton);

        private static bool AnyKeyPressed() =>
            (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
            WasPadPressed(pad => pad.buttonSouth) || WasPadPressed(pad => pad.startButton);
    }
}
