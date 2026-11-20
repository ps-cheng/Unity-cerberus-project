using UnityEngine;

namespace Cerberus.Level
{
    /// <summary>撿取物的外觀：出現時從 0 彈出、之後上下漂浮，光暈跟著脈動。只管外觀，撿取判定另外處理。</summary>
    public class FloatingPickupVisual : MonoBehaviour
    {
        [SerializeField] private Transform art;
        [SerializeField] private SpriteRenderer glow;
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobPeriod = 1.6f;
        [SerializeField] private float popDuration = 0.5f;
        [SerializeField] private Vector2 glowAlphaRange = new Vector2(0.35f, 0.75f);

        private Vector3 artBasePosition;
        private Vector3 artBaseScale;
        private float enabledAt;

        private void Awake()
        {
            artBasePosition = art.localPosition;
            artBaseScale = art.localScale;
        }

        private void OnEnable() => enabledAt = Time.time;

        private void Update()
        {
            float age = Time.time - enabledAt;
            float wave = Mathf.Sin(age * Mathf.PI * 2f / bobPeriod);

            // 彈出（ease-out-back）：稍微超過原尺寸再回來
            float t = Mathf.Clamp01(age / popDuration);
            const float overshoot = 1.70158f;
            float u = t - 1f;
            float pop = 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
            art.localScale = artBaseScale * pop;
            art.localPosition = artBasePosition + Vector3.up * (wave * bobAmplitude);

            if (glow != null)
            {
                Color c = glow.color;
                c.a = Mathf.Lerp(glowAlphaRange.x, glowAlphaRange.y, 0.5f + 0.5f * wave) * t;
                glow.color = c;
                glow.transform.localPosition = art.localPosition;
            }
        }
    }
}
