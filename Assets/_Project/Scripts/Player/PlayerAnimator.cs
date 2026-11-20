using System.Collections.Generic;
using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 用 Animator 播放角色動畫。三顆頭共用一個 Animator Controller，各自用 Override Controller 換 clip。
    /// 要播哪個狀態由這裡依遊戲狀態決定（Controller 裡沒有轉場），優先順序：攻擊 → 特殊動作 → 空中 → 落地 → 跑 / 待機。
    /// - 攻擊、衝刺的時間以程式為準：播放速度縮放成「動畫總長 = 程式裡的動作時間」。
    /// - 跳躍依垂直速度選格：起跳、上升、下降、落地各一格，高度交給物理。
    /// - 按住型特殊動作（格檔）按住期間停在指定的那一格。
    /// - 開關型特殊動作（變身）開啟時正著播、關閉時倒著播；移動或出招會打斷，只有站著不動才看得到完整動畫。
    ///   變身期間把 sprite 換成 HeadConfig 裡的變身版（同名同序）。
    /// - 換頭時接續目前狀態與進度，不讓動畫從頭開始。
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        private enum Mode { Locomotion, Attack, Special }

        private static readonly int IdleState = Animator.StringToHash("Base Layer.idle");
        private static readonly int WalkState = Animator.StringToHash("Base Layer.walk");
        private static readonly int JumpState = Animator.StringToHash("Base Layer.jump");
        private static readonly int LightAttackState = Animator.StringToHash("Base Layer.attack_light");
        private static readonly int HeavyAttackState = Animator.StringToHash("Base Layer.attack_heavy");
        private static readonly int SpecialState = Animator.StringToHash("Base Layer.special");
        private static readonly int IdleSpeedParam = Animator.StringToHash("IdleSpeed");
        private static readonly int WalkSpeedParam = Animator.StringToHash("WalkSpeed");
        private static readonly int AttackSpeedParam = Animator.StringToHash("AttackSpeed");
        private static readonly int SpecialSpeedParam = Animator.StringToHash("SpecialSpeed");

        // 跳躍 clip 的四格
        private const int TakeoffFrame = 0;
        private const int RisingFrame = 1;
        private const int FallingFrame = 2;
        private const int LandingFrame = 3;

        [Header("References")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerCombat combat;
        [Tooltip("掛著 SpriteRenderer 的角色圖物件")]
        [SerializeField] private Animator animator;

        [Header("Base Clips (共用 Controller 裡各狀態原本的 clip，用來查 Override 換上的 clip)")]
        [SerializeField] private AnimationClip baseIdleClip;
        [SerializeField] private AnimationClip baseLightAttackClip;
        [SerializeField] private AnimationClip baseHeavyAttackClip;
        [SerializeField] private AnimationClip baseJumpClip;
        [SerializeField] private AnimationClip baseSpecialClip;

        [Header("Locomotion (各顆頭的 idle / 跑步播放速度在 HeadConfig)")]
        [Tooltip("水平速度超過這個值才播跑步")]
        [SerializeField, Min(0f)] private float walkSpeedThreshold = 0.1f;

        [Header("Jump")]
        [Tooltip("起跳後顯示蹲姿那一格的秒數")]
        [SerializeField, Min(0f)] private float takeoffDuration = 0.06f;
        [Tooltip("落地後顯示落地那一格的秒數（落地就移動的話直接接跑步）")]
        [SerializeField, Min(0f)] private float landingDuration = 0.1f;

        private SpriteRenderer spriteRenderer;
        private Mode mode = Mode.Locomotion;
        private int locomotionState;
        private int jumpFrame = -1;
        private float jumpStartTime = float.NegativeInfinity;
        private float landTime = float.NegativeInfinity;
        private bool wasGrounded = true;
        private bool specialReversed;
        private bool specialHolding;
        // 一般 sprite → 變身版 sprite，第一次用到時才查表
        private readonly Dictionary<Sprite, Sprite> transformedLookup = new Dictionary<Sprite, Sprite>();

        private void Awake() => spriteRenderer = animator.GetComponent<SpriteRenderer>();

        private void OnEnable()
        {
            combat.ActionStarted += HandleActionStarted;
            combat.HeadChanged += HandleHeadChanged;
            combat.SpecialToggled += HandleSpecialToggled;
            movement.Jumped += HandleJumped;
        }

        private void OnDisable()
        {
            combat.ActionStarted -= HandleActionStarted;
            combat.HeadChanged -= HandleHeadChanged;
            combat.SpecialToggled -= HandleSpecialToggled;
            movement.Jumped -= HandleJumped;
        }

        private void Start() => ApplyHead(combat.Head, false);

        private void HandleHeadChanged(int index, int step) => ApplyHead(combat.Head, true);

        private void HandleJumped() => jumpStartTime = Time.time;

        private void ApplyHead(HeadConfig head, bool keepProgress)
        {
            // 換 Controller 會把 Animator 重置回預設狀態，所以先記下目前的狀態與進度
            bool canResume = keepProgress && mode == Mode.Locomotion && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null;
            AnimatorStateInfo previous = canResume ? animator.GetCurrentAnimatorStateInfo(0) : default;

            transformedLookup.Clear();
            if (head.animatorOverride == null)
            {
                Debug.LogWarning($"{head.name} 沒有設定 Animator Override，沿用目前的動畫", head);
                return;
            }

            animator.runtimeAnimatorController = head.animatorOverride;
            UpdateSpeedParameters();

            // 換頭時還在播的特殊動作（例如變身被換頭關掉）屬於上一顆頭，直接回到移動狀態
            mode = Mode.Locomotion;
            specialHolding = false;

            if (canResume)
            {
                float time = previous.loop ? Mathf.Repeat(previous.normalizedTime, 1f) : previous.normalizedTime;
                animator.Play(previous.fullPathHash, 0, time);
                animator.Update(0f);
            }
            else
            {
                locomotionState = 0;
                jumpFrame = -1;
                UpdateLocomotion();
            }
        }

        private void HandleActionStarted(CombatAction action)
        {
            if (!animator.isActiveAndEnabled)
                return;

            switch (action)
            {
                case CombatAction.LightAttack:
                    PlayAttack(LightAttackState, baseLightAttackClip);
                    break;
                case CombatAction.HeavyAttack:
                    PlayAttack(HeavyAttackState, baseHeavyAttackClip);
                    break;
                case CombatAction.Special:
                    PlaySpecial(false, GetSpecialPlaybackSpeed());
                    break;
            }
        }

        // 開關型特殊動作（變身）：開啟正著播，關閉倒著播
        private void HandleSpecialToggled(bool active)
        {
            if (animator.isActiveAndEnabled)
                PlaySpecial(!active, 1f);
        }

        private void PlayAttack(int state, AnimationClip baseClip)
        {
            AttackData attack = combat.CurrentAttack;
            float totalDuration = attack.startupDuration + attack.activeDuration + attack.recoveryDuration;
            float clipLength = GetClip(baseClip).length;
            animator.SetFloat(AttackSpeedParam, totalDuration > 0f && clipLength > 0f ? clipLength / totalDuration : 1f);

            // 直接從頭播，跟 PlayerCombat 開始計時的同一幀對齊
            animator.Play(state, 0, 0f);
            mode = Mode.Attack;
        }

        private void PlaySpecial(bool reversed, float speed)
        {
            specialReversed = reversed;
            specialHolding = false;
            animator.SetFloat(SpecialSpeedParam, reversed ? -speed : speed);
            animator.Play(SpecialState, 0, reversed ? 1f : 0f);
            mode = Mode.Special;
        }

        // 衝刺這類有固定時間的特殊動作，動畫壓在程式的動作時間內播完
        private float GetSpecialPlaybackSpeed()
        {
            float clipLength = GetClip(baseSpecialClip).length;
            if (combat.Head.special is DashData dash && dash.duration > 0f && clipLength > 0f)
                return clipLength / dash.duration;

            return 1f;
        }

        private AnimationClip GetClip(AnimationClip baseClip)
        {
            if (animator.runtimeAnimatorController is AnimatorOverrideController overrides && overrides[baseClip] != null)
                return overrides[baseClip];

            return baseClip;
        }

        private void Update()
        {
            if (!animator.isActiveAndEnabled)
                return;

            TrackGrounding();
            UpdateSpeedParameters();

            switch (mode)
            {
                case Mode.Attack:
                    // 受傷打斷或正常結束，都以程式為準
                    if (!combat.IsAttacking)
                        ReturnToLocomotion();
                    break;
                case Mode.Special:
                    UpdateSpecial();
                    break;
                default:
                    UpdateLocomotion();
                    break;
            }
        }

        private void TrackGrounding()
        {
            bool grounded = movement.IsGrounded;
            if (grounded && !wasGrounded)
                landTime = Time.time;
            wasGrounded = grounded;
        }

        private void UpdateSpecial()
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash != SpecialState)
                return; // Play 要到下一次 Animator 更新才生效

            // 開關型（變身）不佔用行動：移動、跳起來就打斷，只有站著不動才播完整
            if (!combat.Special.OccupiesAction && (IsMoving() || !movement.IsGrounded))
            {
                ReturnToLocomotion();
                return;
            }

            UpdateSpecialHold(state);

            bool finished = specialReversed ? state.normalizedTime <= 0f : state.normalizedTime >= 1f;
            if (finished && !specialHolding)
                ReturnToLocomotion();
        }

        // 按住型（格檔）：按住期間停在指定格，放開後從那一格繼續播完
        private void UpdateSpecialHold(AnimatorStateInfo state)
        {
            int holdFrame = combat.Head.specialHoldFrame;
            if (holdFrame < 0 || specialReversed)
                return;

            AnimationClip clip = GetClip(baseSpecialClip);
            int frameCount = Mathf.Max(1, Mathf.RoundToInt(clip.length * clip.frameRate));
            float holdTime = (holdFrame + 0.5f) / frameCount;

            // 一播到那一格就停住，不必等到格子中間
            if (!specialHolding && combat.IsSpecialActive && state.normalizedTime >= (float)holdFrame / frameCount)
            {
                specialHolding = true;
                animator.SetFloat(SpecialSpeedParam, 0f);
                animator.Play(SpecialState, 0, holdTime);
            }
            else if (specialHolding && !combat.IsSpecialActive)
            {
                specialHolding = false;
                animator.SetFloat(SpecialSpeedParam, 1f);
            }
        }

        private void ReturnToLocomotion()
        {
            mode = Mode.Locomotion;
            specialHolding = false;
            locomotionState = 0;
            jumpFrame = -1;
            UpdateLocomotion();
        }

        private void UpdateLocomotion()
        {
            if (!movement.IsGrounded)
            {
                bool takingOff = Time.time - jumpStartTime < takeoffDuration;
                ShowJumpFrame(takingOff ? TakeoffFrame : movement.Velocity.y > 0f ? RisingFrame : FallingFrame);
                return;
            }

            bool moving = IsMoving();
            if (!moving && Time.time - landTime < landingDuration)
            {
                ShowJumpFrame(LandingFrame);
                return;
            }

            int target = moving ? WalkState : IdleState;
            if (locomotionState == target)
                return;

            locomotionState = target;
            jumpFrame = -1;
            animator.Play(target, 0, 0f);
        }

        // 跳躍狀態的播放速度是 0，直接指定要停在哪一格
        private void ShowJumpFrame(int frame)
        {
            if (locomotionState == JumpState && jumpFrame == frame)
                return;

            AnimationClip clip = GetClip(baseJumpClip);
            int frameCount = Mathf.Max(1, Mathf.RoundToInt(clip.length * clip.frameRate));
            locomotionState = JumpState;
            jumpFrame = frame;
            animator.Play(JumpState, 0, (Mathf.Min(frame, frameCount - 1) + 0.5f) / frameCount);
        }

        private void UpdateSpeedParameters()
        {
            HeadConfig head = combat.Head;
            float speed = Mathf.Abs(movement.Velocity.x);
            animator.SetFloat(WalkSpeedParam, movement.MoveSpeed > 0f ? speed / movement.MoveSpeed * head.walkAnimationSpeedMultiplier : 1f);

            // 每幀重算，Play Mode 中改 HeadConfig 的數值可以馬上看到效果
            float idleLength = GetClip(baseIdleClip).length;
            animator.SetFloat(IdleSpeedParam, head.idleCycleDuration > 0f && idleLength > 0f ? idleLength / head.idleCycleDuration : 1f);
        }

        private bool IsMoving() => Mathf.Abs(movement.Velocity.x) > walkSpeedThreshold;

        // Animator 每次更新都會寫回一般版的 sprite，所以每幀在它之後換掉
        private void LateUpdate()
        {
            if (!animator.isActiveAndEnabled || mode == Mode.Special)
                return;

            Sprite[] transformed = combat.Head.transformedSprites;
            if (transformed == null || transformed.Length == 0 || !combat.Special.IsActive)
                return;

            Sprite current = spriteRenderer.sprite;
            if (current != null)
                spriteRenderer.sprite = GetTransformedSprite(current, transformed);
        }

        // 一般版和變身版同名同序，只差開頭的角色名（例如 Rus_idle_0 ↔ RusBerserk_idle_0）
        private Sprite GetTransformedSprite(Sprite normal, Sprite[] transformed)
        {
            if (transformedLookup.TryGetValue(normal, out Sprite result))
                return result;

            string suffix = GetFrameSuffix(normal.name);
            result = normal;
            foreach (Sprite candidate in transformed)
            {
                if (candidate != null && GetFrameSuffix(candidate.name) == suffix)
                {
                    result = candidate;
                    break;
                }
            }

            transformedLookup[normal] = result;
            return result;
        }

        private static string GetFrameSuffix(string spriteName)
        {
            int separator = spriteName.IndexOf('_');
            return separator >= 0 ? spriteName.Substring(separator + 1) : spriteName;
        }
    }
}
