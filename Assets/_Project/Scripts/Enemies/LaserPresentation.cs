using Cerberus.Audio;
using UnityEngine;

namespace Cerberus.Enemies
{
    /// <summary>
    /// 雷射的蓄力光芒和音效：蓄力時發射口的光暈從小長大、越閃越快，鎖定後變白；
    /// 開始蓄力、發射時各播一次音效。只讀 LaserEmitter 的狀態，不影響判定。
    /// </summary>
    public class LaserPresentation : MonoBehaviour
    {
        [SerializeField] private LaserEmitter laser;
        [Tooltip("發射口的光暈（加亮材質）")]
        [SerializeField] private SpriteRenderer glow;
        [SerializeField] private float aimGlowScale = 0.4f;
        [SerializeField] private float chargedGlowScale = 1.6f;
        [SerializeField] private Color chargeColor = new Color(1f, 0.3f, 0.35f, 0.9f);
        [SerializeField] private Color lockedColor = new Color(1f, 0.95f, 0.95f, 1f);
        [Tooltip("蓄力末段的閃爍頻率（次/秒）")]
        [SerializeField] private float maxFlickerFrequency = 18f;
        [Tooltip("發射期間光暈維持的大小比例")]
        [SerializeField] private float fireGlowScale = 1.1f;
        [SerializeField, Range(0f, 1f)] private float soundVolume = 1f;

        private LaserEmitter.Phase previousPhase;
        private float flickerPhase;

        private void Awake()
        {
            if (glow != null)
                glow.enabled = false;
        }

        private void LateUpdate()
        {
            LaserEmitter.Phase phase = laser.CurrentPhase;
            if (phase != previousPhase)
            {
                if (phase == LaserEmitter.Phase.Charge)
                    SfxPlayer.Play(SfxId.LaserCharge, 1f, soundVolume);
                else if (phase == LaserEmitter.Phase.Fire)
                    SfxPlayer.Play(SfxId.LaserFire, 1f, soundVolume);
                previousPhase = phase;
            }

            if (glow == null)
                return;

            float scale;
            Color color;
            switch (phase)
            {
                case LaserEmitter.Phase.Aim:
                    scale = aimGlowScale;
                    color = chargeColor * new Color(1f, 1f, 1f, 0.5f);
                    break;
                case LaserEmitter.Phase.Charge:
                    float progress = laser.ChargeProgress;
                    flickerPhase += Time.deltaTime * Mathf.Lerp(3f, maxFlickerFrequency, progress) * Mathf.PI * 2f;
                    float flicker = 0.85f + 0.15f * Mathf.Sin(flickerPhase);
                    scale = Mathf.Lerp(aimGlowScale, chargedGlowScale, progress * progress) * flicker;
                    color = laser.IsAimLocked ? lockedColor : chargeColor;
                    break;
                case LaserEmitter.Phase.Fire:
                    scale = chargedGlowScale * fireGlowScale;
                    color = lockedColor;
                    break;
                default:
                    glow.enabled = false;
                    return;
            }

            glow.enabled = true;
            glow.color = color;
            // 父物件可能左右翻面，光暈是圓的，只取絕對值
            Vector3 parentScale = glow.transform.parent != null ? glow.transform.parent.lossyScale : Vector3.one;
            glow.transform.localScale = new Vector3(scale / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)), scale / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)), 1f);
        }
    }
}
