using System;
using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 用幾何圖形的位移與旋轉代替動畫：翻轉面向、刺劍、舉盾、開槍後座力、依招式階段變色、受傷閃爍。
    /// 換頭時切換手上的裝備（劍盾 / 槍）與身體顏色。
    /// 之後換成正式美術時，整支替換成 Animator 驅動即可，不影響遊戲邏輯。
    /// </summary>
    public class PlayerVisuals : MonoBehaviour
    {
        [Serializable]
        private class HeadGear
        {
            public HeadConfig head;
            [Tooltip("這顆頭手上的裝備，換到這顆頭時才顯示")]
            public GameObject gearRoot;
        }

        [Header("References")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerDamageReceiver damageReceiver;
        [Tooltip("所有外觀物件的父節點，翻轉面向時只翻這層")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Transform swordPivot;
        [SerializeField] private Transform shieldPivot;
        [SerializeField] private Transform gunPivot;

        [Header("Head Gear")]
        [SerializeField] private HeadGear[] headGears;

        [Header("Sword Pose (角度 0 = 劍尖朝上, -90 = 朝前)")]
        [SerializeField] private float swordRestAngle = -130f;
        [SerializeField] private float swordThrustAngle = -90f;

        [Header("Shield Pose")]
        [SerializeField] private Vector2 shieldRestPosition = new Vector2(-0.3f, -0.15f);
        [SerializeField] private Vector2 shieldBlockPosition = new Vector2(0.5f, 0.25f);
        [SerializeField] private float shieldMoveSpeed = 25f;

        [Header("Gun Pose")]
        [Tooltip("開槍瞬間槍往後退的距離")]
        [SerializeField] private float gunRecoilDistance = 0.15f;
        [Tooltip("開槍瞬間槍口上揚的角度")]
        [SerializeField] private float gunRecoilAngle = 12f;

        [Header("Phase Colors (待機顏色在各顆頭的 HeadConfig)")]
        [SerializeField] private Color startupColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color activeColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] private Color recoveryColor = new Color(0.45f, 0.45f, 0.5f);
        [SerializeField] private Color specialColor = new Color(0.3f, 0.75f, 1f);

        [Header("Guard Feedback")]
        [Tooltip("完美格檔判定時間內盾牌的顏色，讓玩家看得出按下的時機")]
        [SerializeField] private Color parryWindowShieldColor = new Color(0.85f, 0.95f, 1f);
        [Tooltip("冷卻期間盾牌變暗，提示現在按下不能格檔")]
        [SerializeField] private Color parryLockedShieldColor = new Color(0.25f, 0.3f, 0.4f);
        [SerializeField] private Color guardFlashColor = Color.white;
        [SerializeField] private Color parryFlashColor = new Color(1f, 0.9f, 0.2f);
        [SerializeField] private float guardFlashDuration = 0.12f;
        [SerializeField] private float parryFlashDuration = 0.3f;
        [Tooltip("完美格檔時盾牌放大的倍率")]
        [SerializeField] private float parryShieldPopScale = 1.6f;

        [Header("Invincibility Blink")]
        [SerializeField] private float blinkFrequency = 15f;

        private Vector2 swordRestPosition;
        private Vector2 gunRestPosition;
        private SpriteRenderer[] visualRenderers;
        private SpriteRenderer shieldRenderer;
        private Color shieldBaseColor;
        private Color shieldFlashColor;
        private float shieldFlashStartTime = float.NegativeInfinity;
        private float shieldFlashDuration;
        private float shieldPopScale = 1f;

        private void Awake()
        {
            swordRestPosition = swordPivot.localPosition;
            gunRestPosition = gunPivot.localPosition;
            // 包含目前隱藏的裝備，換頭後受傷閃爍才會套用到新裝備上
            visualRenderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
            shieldRenderer = shieldPivot.GetComponentInChildren<SpriteRenderer>(true);
            shieldBaseColor = shieldRenderer.color;
        }

        private void Start() => ShowGearFor(combat.Head);

        private void OnEnable()
        {
            damageReceiver.Guarded += HandleGuarded;
            damageReceiver.Parried += HandleParried;
            combat.HeadChanged += HandleHeadChanged;
        }

        private void OnDisable()
        {
            damageReceiver.Guarded -= HandleGuarded;
            damageReceiver.Parried -= HandleParried;
            combat.HeadChanged -= HandleHeadChanged;
        }

        private void HandleHeadChanged(int index, int step) => ShowGearFor(combat.Head);

        private void ShowGearFor(HeadConfig head)
        {
            foreach (HeadGear gear in headGears)
                gear.gearRoot.SetActive(gear.head == head);
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
            UpdateGun();
            bodyRenderer.color = GetBodyColor();
            UpdateVisibility();
        }

        private void UpdateSword()
        {
            float angle = swordRestAngle;
            float reach = 0f;

            // 只有近戰攻擊有刺劍動作，射擊攻擊靠身體變色表示階段
            if (combat.IsAttacking && combat.CurrentAttack is MeleeAttackData attack)
            {
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
            bool isBlocking = combat.IsSpecialActive && combat.Special is ShieldBlockSpecial;
            Vector2 target = isBlocking ? shieldBlockPosition : shieldRestPosition;
            shieldPivot.localPosition = Vector2.Lerp(shieldPivot.localPosition, target, shieldMoveSpeed * Time.deltaTime);

            // 格檔成功時閃色並放大，隨時間淡回原狀
            float flashProgress = (Time.time - shieldFlashStartTime) / shieldFlashDuration;
            if (flashProgress < 1f)
            {
                shieldRenderer.color = Color.Lerp(shieldFlashColor, shieldBaseColor, flashProgress);
                shieldPivot.localScale = Vector3.one * Mathf.Lerp(shieldPopScale, 1f, flashProgress);
                return;
            }

            shieldRenderer.color = GetShieldColor();
            shieldPivot.localScale = Vector3.one;
        }

        private void UpdateGun()
        {
            float angle = 0f;
            float reach = 0f;

            // 前搖：槍微微往後收 → 開槍：後座力往後退並上揚 → 硬直：慢慢回到原位
            if (combat.IsAttacking && combat.CurrentAttack is ProjectileAttackData)
            {
                float t = combat.PhaseProgress;

                switch (combat.CurrentPhase)
                {
                    case ActionPhase.Startup:
                        reach = Mathf.Lerp(0f, -gunRecoilDistance * 0.3f, t);
                        break;
                    case ActionPhase.Active:
                        reach = -gunRecoilDistance;
                        angle = gunRecoilAngle;
                        break;
                    case ActionPhase.Recovery:
                        reach = Mathf.Lerp(-gunRecoilDistance, 0f, t);
                        angle = Mathf.Lerp(gunRecoilAngle, 0f, t);
                        break;
                }
            }

            gunPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
            gunPivot.localPosition = gunRestPosition + new Vector2(reach, 0f);
        }

        private Color GetShieldColor()
        {
            // 只有格檔才有完美格檔的盾牌提示，其他頭的特殊動作維持原色
            if (!(combat.Special is ShieldBlockSpecial shieldBlock))
                return shieldBaseColor;

            if (shieldBlock.IsParryWindowActive)
                return parryWindowShieldColor;

            return shieldBlock.IsOnCooldown ? parryLockedShieldColor : shieldBaseColor;
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
                _ => combat.Head.bodyColor
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
