using Cerberus.Level;
using UnityEngine;

namespace Cerberus.UI
{
    /// <summary>通關訊息：平常完全透明，抵達終點後在畫面中央淡入並從大縮回原尺寸。</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class StageClearUI : MonoBehaviour
    {
        [SerializeField] private LevelGoal goal;
        [Tooltip("淡入時要縮放的部分（文字），背景帶不縮放")]
        [SerializeField] private RectTransform content;
        [SerializeField] private float fadeDuration = 0.35f;
        [SerializeField] private float popScale = 1.4f;

        private CanvasGroup canvasGroup;
        private float shownAt = float.NegativeInfinity;
        private bool isShown;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        private void OnEnable() => goal.Reached += HandleReached;

        private void OnDisable() => goal.Reached -= HandleReached;

        private void HandleReached()
        {
            isShown = true;
            shownAt = Time.unscaledTime;
        }

        private void Update()
        {
            if (!isShown)
                return;

            float t = Mathf.Clamp01((Time.unscaledTime - shownAt) / fadeDuration);
            float easeOut = 1f - (1f - t) * (1f - t);
            canvasGroup.alpha = t;
            content.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, easeOut);
        }
    }
}
