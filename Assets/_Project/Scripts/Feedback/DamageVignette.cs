using UnityEngine;
using UnityEngine.UI;

namespace Cerberus.Feedback
{
    /// <summary>
    /// 玩家受傷時畫面邊緣閃紅（暈影），中央只帶一點淡紅，不擋住視線。
    /// 暈影貼圖在執行時用程式產生，不需要美術資源。
    /// 用真實時間淡出，hit-stop 凍結期間也看得到。
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class DamageVignette : MonoBehaviour
    {
        [SerializeField] private Color color = new Color(1f, 0.05f, 0.05f);
        [Tooltip("畫面中央的透明度")]
        [SerializeField, Range(0f, 1f)] private float centerAlpha = 0f;
        [Tooltip("畫面邊緣（最濃處）的透明度")]
        [SerializeField, Range(0f, 1f)] private float edgeAlpha = 0.08f;
        [Tooltip("從中心算起，這個半徑以內維持中央透明度（0~1，1 為畫面角落）")]
        [SerializeField, Range(0f, 1f)] private float innerRadius = 0.55f;
        [SerializeField] private float fadeDuration = 0.4f;
        [SerializeField] private int textureSize = 128;

        public float CurrentIntensity { get; private set; }

        private RawImage image;
        private float elapsed = float.PositiveInfinity;

        private void Awake()
        {
            image = GetComponent<RawImage>();
            image.texture = CreateVignetteTexture();
            image.raycastTarget = false;
            ApplyIntensity(0f);
        }

        public void Flash()
        {
            elapsed = 0f;
        }

        private void Update()
        {
            if (elapsed > fadeDuration)
                return;

            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            ApplyIntensity(1f - progress * progress);
        }

        private void ApplyIntensity(float intensity)
        {
            CurrentIntensity = intensity;
            image.color = new Color(color.r, color.g, color.b, intensity);
            image.enabled = intensity > 0f;
        }

        private Texture2D CreateVignetteTexture()
        {
            var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                name = "DamageVignette"
            };

            float half = (textureSize - 1) * 0.5f;
            var pixels = new Color[textureSize * textureSize];

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    // 正規化到中心 0、角落 1 的距離
                    float distance = new Vector2((x - half) / half, (y - half) / half).magnitude / Mathf.Sqrt(2f);
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(innerRadius, 1f, distance));
                    pixels[y * textureSize + x] = new Color(1f, 1f, 1f, Mathf.Lerp(centerAlpha, edgeAlpha, t));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
