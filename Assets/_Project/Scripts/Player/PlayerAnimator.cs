using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 用 Animator 播放角色的像素動畫。三顆頭共用一個 Animator Controller，各自用 Override Controller 換 clip。
    /// 攻擊時間以 PlayerCombat 為準：攻擊狀態的播放速度會縮放成「動畫總長 = 前搖 + 判定 + 硬直」。
    /// 換頭時接續目前狀態與進度，不讓動畫從頭開始。
    /// 頭沒有設定 Override 時，改顯示原本的方塊外觀。
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int IdleState = Animator.StringToHash("Base Layer.idle");
        private static readonly int WalkState = Animator.StringToHash("Base Layer.walk");
        private static readonly int LightAttackState = Animator.StringToHash("Base Layer.attack_light");
        private static readonly int HeavyAttackState = Animator.StringToHash("Base Layer.attack_heavy");
        private static readonly int IsMovingParam = Animator.StringToHash("IsMoving");
        private static readonly int IdleSpeedParam = Animator.StringToHash("IdleSpeed");
        private static readonly int WalkSpeedParam = Animator.StringToHash("WalkSpeed");
        private static readonly int AttackSpeedParam = Animator.StringToHash("AttackSpeed");

        [Header("References")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCombat combat;
        [Tooltip("掛著 SpriteRenderer 的像素圖物件，頭沒有動畫時會整個隱藏")]
        [SerializeField] private Animator animator;
        [Tooltip("頭有像素圖時隱藏的方塊外觀（身體、頭、被像素圖取代的裝備）")]
        [SerializeField] private GameObject[] placeholderParts;

        [Header("Base Clips (共用 Controller 裡各狀態原本的 clip，用來查 Override 換上的 clip 長度)")]
        [SerializeField] private AnimationClip baseIdleClip;
        [SerializeField] private AnimationClip baseLightAttackClip;
        [SerializeField] private AnimationClip baseHeavyAttackClip;

        [Header("Walk (各顆頭的 idle / walk 播放速度在 HeadConfig)")]
        [Tooltip("水平速度超過這個值才播 walk")]
        [SerializeField, Min(0f)] private float walkSpeedThreshold = 0.1f;

        private void OnEnable()
        {
            combat.ActionStarted += HandleActionStarted;
            combat.HeadChanged += HandleHeadChanged;
        }

        private void OnDisable()
        {
            combat.ActionStarted -= HandleActionStarted;
            combat.HeadChanged -= HandleHeadChanged;
        }

        private void Start() => ApplyHead(combat.Head, false);

        private void HandleHeadChanged(int index, int step) => ApplyHead(combat.Head, true);

        private void ApplyHead(HeadConfig head, bool keepProgress)
        {
            bool hasArt = head.animatorOverride != null;
            foreach (GameObject part in placeholderParts)
                part.SetActive(!hasArt);

            // 換 Controller 會把 Animator 重置回預設狀態，所以先記下目前的狀態與進度
            bool canResume = keepProgress && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null;
            AnimatorStateInfo previous = canResume ? animator.GetCurrentAnimatorStateInfo(0) : default;

            animator.gameObject.SetActive(hasArt);
            if (!hasArt)
                return;

            animator.runtimeAnimatorController = head.animatorOverride;
            UpdateLocomotionParameters();

            if (canResume)
            {
                float time = previous.loop ? Mathf.Repeat(previous.normalizedTime, 1f) : previous.normalizedTime;
                animator.SetFloat(AttackSpeedParam, previous.speedMultiplier);
                animator.Play(previous.fullPathHash, 0, time);
                animator.Update(0f);
            }
        }

        private void HandleActionStarted(CombatAction action)
        {
            if (!animator.isActiveAndEnabled)
                return;

            int state;
            AnimationClip baseClip;
            switch (action)
            {
                case CombatAction.LightAttack:
                    state = LightAttackState;
                    baseClip = baseLightAttackClip;
                    break;
                case CombatAction.HeavyAttack:
                    state = HeavyAttackState;
                    baseClip = baseHeavyAttackClip;
                    break;
                default:
                    return;
            }

            AttackData attack = combat.CurrentAttack;
            float totalDuration = attack.startupDuration + attack.activeDuration + attack.recoveryDuration;
            float clipLength = GetClipLength(baseClip);
            animator.SetFloat(AttackSpeedParam, totalDuration > 0f && clipLength > 0f ? clipLength / totalDuration : 1f);

            // 直接從頭播，跟 PlayerCombat 開始計時的同一幀對齊
            animator.Play(state, 0, 0f);
        }

        private float GetClipLength(AnimationClip baseClip)
        {
            if (animator.runtimeAnimatorController is AnimatorOverrideController overrides && overrides[baseClip] != null)
                return overrides[baseClip].length;

            return baseClip.length;
        }

        private void Update()
        {
            if (!animator.isActiveAndEnabled)
                return;

            UpdateLocomotionParameters();

            // 攻擊被受傷打斷時，程式已經結束攻擊，動畫也跟著收掉
            int current = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            bool inAttackState = current == LightAttackState || current == HeavyAttackState;
            if (inAttackState && !combat.IsAttacking && !animator.IsInTransition(0))
                animator.Play(IsMoving() ? WalkState : IdleState, 0, 0f);
        }

        private void UpdateLocomotionParameters()
        {
            float speed = Mathf.Abs(movement.Velocity.x);
            animator.SetBool(IsMovingParam, IsMoving());
            HeadConfig head = combat.Head;
            animator.SetFloat(WalkSpeedParam, movement.MoveSpeed > 0f ? speed / movement.MoveSpeed * head.walkAnimationSpeedMultiplier : 1f);

            // 每幀重算，Play Mode 中改 HeadConfig 的數值可以馬上看到效果
            float idleLength = GetClipLength(baseIdleClip);
            animator.SetFloat(IdleSpeedParam, head.idleCycleDuration > 0f && idleLength > 0f ? idleLength / head.idleCycleDuration : 1f);
        }

        private bool IsMoving() => Mathf.Abs(movement.Velocity.x) > walkSpeedThreshold;
    }
}
