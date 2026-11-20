using UnityEngine;

namespace Cerberus.Feedback
{
    /// <summary>
    /// 全遊戲共用的暫停狀態（開始畫面、開場文字卡、暫停選單）。
    /// 暫停時 timeScale = 0；HitStop 結束時會檢查這裡，不會把暫停解除。
    /// </summary>
    public static class GamePause
    {
        public static bool IsPaused { get; private set; }

        public static void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => IsPaused = false;
    }
}
