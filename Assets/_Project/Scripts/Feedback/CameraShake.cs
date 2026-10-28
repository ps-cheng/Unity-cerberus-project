using UnityEngine;

namespace Cerberus.Feedback
{
    /// <summary>
    /// 鏡頭震動：以 localPosition 的偏移呈現，不改動鏡頭原本的位置。
    /// 之後若加入鏡頭跟隨，讓跟隨移動父物件即可，兩者互不干擾。
    /// 用真實時間計算，hit-stop 凍結畫面時鏡頭仍會震動。
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [Tooltip("震動的抖動頻率，越高越碎")]
        [SerializeField] private float frequency = 30f;

        public float CurrentAmplitude { get; private set; }

        private Vector3 restLocalPosition;
        private float amplitude;
        private float duration;
        private float elapsed;
        private float noiseSeed;

        private void Awake()
        {
            restLocalPosition = transform.localPosition;
            noiseSeed = Random.value * 100f;
        }

        /// <summary>震動中又觸發時，只有更強的震動會取代目前的</summary>
        public void Shake(float shakeAmplitude, float shakeDuration)
        {
            if (shakeAmplitude <= 0f || shakeDuration <= 0f || shakeAmplitude < CurrentAmplitude)
                return;

            amplitude = shakeAmplitude;
            duration = shakeDuration;
            elapsed = 0f;
        }

        private void LateUpdate()
        {
            if (elapsed >= duration)
            {
                CurrentAmplitude = 0f;
                transform.localPosition = restLocalPosition;
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            // 平方衰減：一開始最強，很快收掉
            float falloff = 1f - Mathf.Clamp01(elapsed / duration);
            CurrentAmplitude = amplitude * falloff * falloff;

            float time = Time.unscaledTime * frequency;
            Vector2 offset = new Vector2(
                Mathf.PerlinNoise(noiseSeed, time) * 2f - 1f,
                Mathf.PerlinNoise(noiseSeed + 50f, time) * 2f - 1f);
            transform.localPosition = restLocalPosition + (Vector3)(offset * CurrentAmplitude);
        }
    }
}
