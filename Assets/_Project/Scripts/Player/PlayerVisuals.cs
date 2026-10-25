using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 用幾何圖形的位移與旋轉代替動畫：翻轉面向、揮劍、舉盾、依招式階段變色。
    /// 之後換成正式美術時，整支替換成 Animator 驅動即可，不影響遊戲邏輯。
    /// </summary>
    public class PlayerVisuals : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCombat combat;
        [Tooltip("所有外觀物件的父節點，翻轉面向時只翻這層")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Transform swordPivot;
        [SerializeField] private Transform shieldPivot;

        [Header("Sword Angles (0 = 劍尖朝上, 負值往前揮)")]
        [SerializeField] private float swordRestAngle = -110f;
        [SerializeField] private float swordSwingEndAngle = -165f;

        [Header("Shield Pose")]
        [SerializeField] private Vector2 shieldRestPosition = new Vector2(-0.3f, -0.15f);
        [SerializeField] private Vector2 shieldBlockPosition = new Vector2(0.5f, 0.25f);
        [SerializeField] private float shieldMoveSpeed = 25f;

        [Header("Phase Colors")]
        [SerializeField] private Color idleColor = new Color(0.55f, 0.35f, 0.25f);
        [SerializeField] private Color startupColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color activeColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] private Color recoveryColor = new Color(0.45f, 0.45f, 0.5f);
        [SerializeField] private Color blockColor = new Color(0.3f, 0.75f, 1f);

        private void LateUpdate()
        {
            visualRoot.localScale = new Vector3(movement.FacingDirection, 1f, 1f);
            swordPivot.localRotation = Quaternion.Euler(0f, 0f, GetSwordAngle());
            UpdateShield();
            bodyRenderer.color = GetBodyColor();
        }

        private float GetSwordAngle()
        {
            if (!combat.IsAttacking)
                return swordRestAngle;

            float windupAngle = combat.CurrentAttack.windupAngle;
            float t = combat.PhaseProgress;

            // 前搖：舉劍到頭後方 → 判定：往前劈下 → 硬直：慢慢收回
            return combat.CurrentPhase switch
            {
                ActionPhase.Startup => Mathf.Lerp(swordRestAngle, windupAngle, t),
                ActionPhase.Active => Mathf.Lerp(windupAngle, swordSwingEndAngle, t),
                ActionPhase.Recovery => Mathf.Lerp(swordSwingEndAngle, swordRestAngle, t),
                _ => swordRestAngle
            };
        }

        private void UpdateShield()
        {
            Vector2 target = combat.IsBlocking ? shieldBlockPosition : shieldRestPosition;
            shieldPivot.localPosition = Vector2.Lerp(shieldPivot.localPosition, target, shieldMoveSpeed * Time.deltaTime);
        }

        private Color GetBodyColor()
        {
            if (combat.IsBlocking)
                return blockColor;

            return combat.CurrentPhase switch
            {
                ActionPhase.Startup => startupColor,
                ActionPhase.Active => activeColor,
                ActionPhase.Recovery => recoveryColor,
                _ => idleColor
            };
        }
    }
}
