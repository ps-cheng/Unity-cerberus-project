using Cerberus.Enemies;
using UnityEngine;

namespace Cerberus.Feedback
{
    /// <summary>
    /// 讓一組精靈整張變成純色（閃白）。換上 Cerberus/SpriteFlash 材質，用 MaterialPropertyBlock 個別控制，
    /// 所以白色為底的圖也看得出閃白（原本只改 SpriteRenderer.color，乘上白色看不出變化）。
    /// 掛在敵人身上時會自動跟著 EnemyBase 的受擊閃白；其他情況（例如換頭）呼叫 Flash。
    /// </summary>
    public class SpriteFlash : MonoBehaviour
    {
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

        [SerializeField] private Material flashMaterial;
        [Tooltip("空白 = 這個物件底下所有的 SpriteRenderer")]
        [SerializeField] private SpriteRenderer[] renderers;
        [Tooltip("跟著這個敵人的受擊閃白，空白 = 自動往上層找")]
        [SerializeField] private EnemyBase enemy;
        [SerializeField] private Color hitFlashColor = Color.white;

        private MaterialPropertyBlock block;
        private float flashStart;
        private float flashUntil = float.NegativeInfinity;
        private Color flashColor = Color.white;
        private float appliedAmount = -1f;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (enemy == null)
                enemy = GetComponentInParent<EnemyBase>();

            block = new MaterialPropertyBlock();
            foreach (SpriteRenderer target in renderers)
                target.sharedMaterial = flashMaterial;
        }

        /// <summary>閃成指定顏色，在 duration 秒內淡回原圖</summary>
        public void Flash(Color color, float duration)
        {
            flashColor = color;
            flashStart = Time.unscaledTime;
            flashUntil = flashStart + Mathf.Max(duration, 0.01f);
        }

        private void LateUpdate()
        {
            float amount = 0f;
            Color color = flashColor;

            if (Time.unscaledTime < flashUntil)
                amount = 1f - (Time.unscaledTime - flashStart) / (flashUntil - flashStart);

            if (enemy != null && enemy.IsHitFlashing)
            {
                amount = 1f;
                color = hitFlashColor;
            }

            if (Mathf.Approximately(amount, appliedAmount) && amount == 0f)
                return;

            appliedAmount = amount;
            foreach (SpriteRenderer target in renderers)
            {
                if (target == null)
                    continue;

                target.GetPropertyBlock(block);
                block.SetFloat(FlashAmountId, amount);
                block.SetColor(FlashColorId, color);
                target.SetPropertyBlock(block);
            }
        }
    }
}
