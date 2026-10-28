using Cerberus.Combat;
using Cerberus.Player;
using UnityEngine;

namespace Cerberus.Feedback
{
    /// <summary>
    /// 把玩家的戰鬥事件轉成畫面回饋，各效果元件彼此不需要知道對方：
    /// - 命中敵人：依招式的 HitFeedbackData 做 hit-stop + 鏡頭震動
    /// - 完美格檔反射的攻擊命中敵人：同上，使用反射專用的強度
    /// - 玩家受傷：短 hit-stop + 紅色暈影，不震動鏡頭（無敵閃爍由 PlayerVisuals 處理）
    /// </summary>
    public class CombatFeedback : MonoBehaviour
    {
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private PlayerDamageReceiver playerDamageReceiver;
        [SerializeField] private HitStop hitStop;
        [SerializeField] private CameraShake cameraShake;
        [SerializeField] private DamageVignette damageVignette;

        [Header("Reflected Hit")]
        [Tooltip("反射傷害是 2 倍，回饋強度比照強攻擊")]
        [SerializeField] private HitFeedbackData reflectedHitFeedback = new HitFeedbackData
        {
            hitStopDuration = 0.09f,
            shakeAmplitude = 0.22f,
            shakeDuration = 0.22f
        };

        [Header("Player Hurt")]
        [Tooltip("玩家受傷的 hit-stop，約 60fps 的 2 幀")]
        [SerializeField] private float hurtHitStopDuration = 0.035f;

        private void OnEnable()
        {
            playerCombat.TargetHit += HandleTargetHit;
            playerDamageReceiver.Hurt += HandlePlayerHurt;
            CombatEvents.ReflectedHitLanded += HandleReflectedHit;
        }

        private void OnDisable()
        {
            playerCombat.TargetHit -= HandleTargetHit;
            playerDamageReceiver.Hurt -= HandlePlayerHurt;
            CombatEvents.ReflectedHitLanded -= HandleReflectedHit;
        }

        private void HandleTargetHit(Collider2D target, AttackData attack) => PlayHitFeedback(attack.hitFeedback);

        private void HandleReflectedHit() => PlayHitFeedback(reflectedHitFeedback);

        private void PlayHitFeedback(HitFeedbackData feedback)
        {
            hitStop.Trigger(feedback.hitStopDuration);
            cameraShake.Shake(feedback.shakeAmplitude, feedback.shakeDuration);
        }

        private void HandlePlayerHurt(int amount)
        {
            hitStop.Trigger(hurtHitStopDuration);
            damageVignette.Flash();
        }
    }
}
