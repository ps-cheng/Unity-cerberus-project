using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 用幾何圖形的位移與旋轉代替動畫：翻轉面向、刺劍、舉盾、依招式階段變色、受傷閃爍。
    /// 之後換成正式美術時，整支替換成 Animator 驅動即可，不影響遊戲邏輯。
    /// </summary>
    public class PlayerVisuals : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerDamageReceiver damageReceiver;
        [Tooltip("所有外觀物件的父節點，翻轉面向時只翻這層")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Transform swordPivot;
        [SerializeField] private Transform shieldPivot;

        [Header("Sword Pose (角度 0 = 劍尖朝上, -90 = 朝前)")]
        [SerializeField] private float swordRestAngle = -130f;
        [SerializeField] private float swordThrustAngle = -90f;

        [Header("Shield Pose")]
        [SerializeField] private Vector2 shieldRestPosition = new Vector2(-0.3f, -0.15f);
        [SerializeField] private Vector2 shieldBlockPosition = new Vector2(0.5f, 0.25f);
        [SerializeField] private float shieldMoveSpeed = 25f;

        [Header("Phase Colors")]
        [SerializeField] private Color idleColor = new Color(0.55f, 0.35f, 0.25f);
        [SerializeField] private Color startupColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color activeColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] private Color recoveryColor = new Color(0.45f, 0.45f, 0.5f);
        [SerializeField] private Color specialColor = new Color(0.3f, 0.75f, 1f);

        [Header("Invincibility Blink")]
        [SerializeField] private float blinkFrequency = 15f;

        private Vector2 swordRestPosition;
        private SpriteRenderer[] visualRenderers;

        private void Awake()
        {
            swordRestPosition = swordPivot.localPosition;
            visualRenderers = visualRoot.GetComponentsInChildren<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            visualRoot.localScale = new Vector3(movement.FacingDirection, 1f, 1f);
            UpdateSword();
            UpdateShield();
            bodyRenderer.color = GetBodyColor();
            UpdateVisibility();
        }

        private void UpdateSword()
        {
            float angle = swordRestAngle;
            float reach = 0f;

            if (combat.IsAttacking)
            {
                AttackData attack = combat.CurrentAttack;
                float t = combat.PhaseProgress;

                // 前搖：劍轉平並往後收 → 判定：快速往前刺出 → 硬直：慢慢收回原位
                switch (combat.CurrentPhase)
                {
                    case ActionPhase.Startup:
                        angle = Mathf.Lerp(swordRestAngle, swordThrustAngle, t * 2f);
                        reach = Mathf.Lerp(0f, -attack.windupDistance, t);
                        break;
                    case ActionPhase.Active:
                        angle = swordThrustAngle;
                        // 開根號讓前段衝得快、後段減速，有「刺出去」的感覺
                        reach = Mathf.Lerp(-attack.windupDistance, attack.thrustDistance, Mathf.Sqrt(t));
                        break;
                    case ActionPhase.Recovery:
                        angle = Mathf.Lerp(swordThrustAngle, swordRestAngle, t);
                        reach = Mathf.Lerp(attack.thrustDistance, 0f, t);
                        break;
                }
            }

            swordPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
            swordPivot.localPosition = swordRestPosition + new Vector2(reach, 0f);
        }

        private void UpdateShield()
        {
            Vector2 target = combat.IsSpecialActive ? shieldBlockPosition : shieldRestPosition;
            shieldPivot.localPosition = Vector2.Lerp(shieldPivot.localPosition, target, shieldMoveSpeed * Time.deltaTime);
        }

        private Color GetBodyColor()
        {
            if (combat.IsSpecialActive)
                return specialColor;

            return combat.CurrentPhase switch
            {
                ActionPhase.Startup => startupColor,
                ActionPhase.Active => activeColor,
                ActionPhase.Recovery => recoveryColor,
                _ => idleColor
            };
        }

        private void UpdateVisibility()
        {
            bool isVisible = !damageReceiver.IsDead;

            // 無敵時間內閃爍
            if (isVisible && damageReceiver.IsInvincible)
                isVisible = Mathf.FloorToInt(Time.time * blinkFrequency) % 2 == 0;

            foreach (SpriteRenderer spriteRenderer in visualRenderers)
                spriteRenderer.enabled = isVisible;
        }
    }
}
