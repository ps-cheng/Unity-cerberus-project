using UnityEngine;

namespace Cerberus.Feedback
{
    /// <summary>
    /// 命中瞬間把 Time.timeScale 設為 0，短暫凍結畫面來強調打擊感。
    /// 用真實時間計時，所以凍結期間也會正常解除；連續觸發時取較晚結束的那個，不會疊加變長。
    /// </summary>
    public class HitStop : MonoBehaviour
    {
        public bool IsActive { get; private set; }

        private float resumeAtRealtime;

        public void Trigger(float duration)
        {
            if (duration <= 0f)
                return;

            resumeAtRealtime = Mathf.Max(resumeAtRealtime, Time.unscaledTime + duration);
            IsActive = true;
            Time.timeScale = 0f;
        }

        private void Update()
        {
            if (IsActive && Time.unscaledTime >= resumeAtRealtime)
                Resume();
        }

        private void OnDisable()
        {
            if (IsActive)
                Resume();
        }

        private void Resume()
        {
            IsActive = false;
            Time.timeScale = 1f;
        }
    }
}
