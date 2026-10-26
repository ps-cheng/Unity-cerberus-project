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

        [Header("Guard Feedback")]
        [Tooltip("完美格檔判定時間內盾牌的顏色，讓玩家看得出按下的時機")]
        [SerializeField] private Color parryWindowShieldColor = new Color(0.85f, 0.95f, 1f);
        [SerializeField] private Color guardFlashColor = Color.white;
        [SerializeField] private Color parryFlashColor = new Color(1f, 0.9f, 0.2f);
        [SerializeField] private float guardFlashDuration = 0.12f;
        [SerializeField] private float parryFlashDuration = 0.3f;
        [Tooltip("完美格檔時盾牌放大的倍率")]
        [SerializeField] private float parryShieldPopScale = 1.6f;

        [Header("Invincibility Blink")]
        [SerializeField] private float blinkFrequency = 15f;

        private Vector2 swordRestPosition;
        private SpriteRenderer[] visualRenderers;
        private SpriteRenderer shieldRenderer;
        private ShieldBlockSpecial shieldBlock;
        private Color shieldBaseColor;
        private Color shieldFlashColor;
        private float shieldFlashStartTime = float.NegativeInfinity;
        private float shieldFlashDuration;
        private float shieldPopScale = 1f;

        private void Awake()
        {
            swordRestPosition = swordPivot.localPosition;
            visualRenderers = visualRoot.GetComponentsInChildren<SpriteRenderer>();
            shieldRenderer = shieldPivot.GetComponentInChildren<SpriteRenderer>();
            shieldBaseColor = shieldRenderer.color;
            shieldBlock = GetComponent<ShieldBlockSpecial>();
        }

        private void OnEnable()
        {
            damageReceiver.Guarded += HandleGuarded;
            damageReceiver.Parried += HandleParried;
        }

        private void OnDisable()
        {
            damageReceiver.Guarded -= HandleGuarded;
            damageReceiver.Parried -= HandleParried;
        }

        private void HandleGuarded(int damageTaken) => StartShieldFlash(guardFlashColor, guardFlashDuration, 1f);
        private void HandleParried() => StartShieldFlash(parryFlashColor, parryFlashDuration, parryShieldPopScale);

        private void StartShieldFlash(Color color, float duration, float popScale)
        {
            shieldFlashColor = color;
            shieldFlashDuration = duration;
            shieldPopScale = popScale;
            shieldFlashStartTime = Time.time;
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

            // 格檔成功時閃色並放大，隨時間淡回原狀
            float flashProgress = (Time.time - shieldFlashStartTime) / shieldFlashDuration;
            if (flashProgress < 1f)
            {
                shieldRenderer.color = Color.Lerp(shieldFlashColor, shieldBaseColor, flashProgress);
                shieldPivot.localScale = Vector3.one * Mathf.Lerp(shieldPopScale, 1f, flashProgress);
                return;
            }

            bool isParryWindow = shieldBlock != null && shieldBlock.IsParryWindowActive;
            shieldRenderer.color = isParryWindow ? parryWindowShieldColor : shieldBaseColor;
            shieldPivot.localScale = Vector3.one;
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
            if (isVisible && damageReceiver.IsBlinking)
                isVisible = Mathf.FloorToInt(Time.time * blinkFrequency) % 2 == 0;

            foreach (SpriteRenderer spriteRenderer in visualRenderers)
                spriteRenderer.enabled = isVisible;
        }
    }
}
