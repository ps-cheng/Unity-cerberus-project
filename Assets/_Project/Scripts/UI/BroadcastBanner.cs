using TMPro;
using UnityEngine;

namespace Cerberus.UI
{
    /// <summary>
    /// 系統廣播：畫面上方的橫幅，文字逐字打出，停留一段時間後淡出。用於塔洛斯登場之類的劇情提示。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class BroadcastBanner : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [Tooltip("每秒打出幾個字")]
        [SerializeField] private float charactersPerSecond = 18f;
        [SerializeField] private float fadeDuration = 0.3f;

        private CanvasGroup canvasGroup;
        private float shownAt = float.NegativeInfinity;
        private float hideAt = float.NegativeInfinity;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        /// <param name="holdDuration">文字打完後停留的秒數</param>
        public void Show(string message, float holdDuration)
        {
            label.text = message;
            label.ForceMeshUpdate();
            shownAt = Time.unscaledTime;
            hideAt = shownAt + message.Length / charactersPerSecond + holdDuration;
        }

        /// <summary>打完全部文字需要的秒數，方便劇情配合時間</summary>
        public float GetTypingDuration(string message) => message.Length / charactersPerSecond;

        private void Update()
        {
            float now = Time.unscaledTime;
            label.maxVisibleCharacters = Mathf.FloorToInt((now - shownAt) * charactersPerSecond);

            float fadeIn = Mathf.Clamp01((now - shownAt) / fadeDuration);
            float fadeOut = Mathf.Clamp01((hideAt - now) / fadeDuration);
            canvasGroup.alpha = Mathf.Min(fadeIn, fadeOut);
        }
    }
}
