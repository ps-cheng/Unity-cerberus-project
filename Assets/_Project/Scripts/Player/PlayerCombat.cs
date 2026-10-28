using System;
using System.Collections.Generic;
using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 處理弱攻擊、強攻擊與特殊動作的狀態切換及命中判定。
    /// 特殊動作的具體內容交給 SpecialAbility，這裡只管「現在是不是在做特殊動作」。
    /// 移動限制由 PlayerController 依這裡的狀態決定。
    /// </summary>
    public class PlayerCombat : MonoBehaviour, ICombatZoneSource
    {
        [Header("Attacks")]
        [SerializeField] private AttackData lightAttack = new AttackData
        {
            displayName = "Light Thrust",
            startupDuration = 0.06f,
            activeDuration = 0.1f,
            recoveryDuration = 0.14f,
            hitboxOffset = new Vector2(1.15f, 0.35f),
            hitboxSize = new Vector2(1.5f, 0.4f),
            damage = 10,
            knockbackForce = 3f,
            windupDistance = 0.15f,
            thrustDistance = 0.6f,
            hitFeedback = new HitFeedbackData { hitStopDuration = 0.04f, shakeAmplitude = 0.06f, shakeDuration = 0.1f }
        };

        [SerializeField] private AttackData heavyAttack = new AttackData
        {
            displayName = "Heavy Thrust",
            startupDuration = 0.28f,
            activeDuration = 0.16f,
            recoveryDuration = 0.5f,
            hitboxOffset = new Vector2(1.65f, 0.35f),
            hitboxSize = new Vector2(2.5f, 0.8f),
            damage = 25,
            knockbackForce = 8f,
            windupDistance = 0.45f,
            thrustDistance = 1.3f,
            hitFeedback = new HitFeedbackData { hitStopDuration = 0.09f, shakeAmplitude = 0.22f, shakeDuration = 0.22f }
        };

        [Header("Special")]
        [Tooltip("這顆頭的特殊動作，切換角色時會換成不同的實作")]
        [SerializeField] private SpecialAbility specialAbility;

        [Header("Hit Detection")]
        [SerializeField] private LayerMask hittableLayers;

        public CombatAction CurrentAction { get; private set; }
        public ActionPhase CurrentPhase { get; private set; }
        public AttackData CurrentAttack { get; private set; }
        public bool IsBusy => CurrentAction != CombatAction.None;
        public bool IsAttacking => CurrentAction == CombatAction.LightAttack || CurrentAction == CombatAction.HeavyAttack;
        public bool IsSpecialActive => CurrentAction == CombatAction.Special;
        public string SpecialName => specialAbility.DisplayName;
        public string CurrentActionName => CurrentAction switch
        {
            CombatAction.None => "-",
            CombatAction.Special => SpecialName,
            _ => CurrentAttack.displayName
        };
        public bool SpecialLocksMovement => specialAbility.LocksMovement;
        public bool SpecialAllowsTurning => specialAbility.AllowsTurning;
        public string SpecialStatusText => specialAbility.StatusText;

        /// <summary>目前階段的進度 0~1，給視覺與 HUD 使用</summary>
        public float PhaseProgress { get; private set; }

        public event Action<CombatAction> ActionStarted;
        /// <summary>攻擊一般命中目標（不含被格檔、目標無敵）時觸發，附上是哪一招</summary>
        public event Action<Collider2D, AttackData> TargetHit;

        private int attackFacing = 1;
        private float phaseTimer;
        private ContactFilter2D hitFilter;
        private readonly List<Collider2D> overlapResults = new List<Collider2D>();
        // 同一次攻擊對同一目標只命中一次
        private readonly HashSet<Collider2D> targetsHitThisAttack = new HashSet<Collider2D>();

        private void Awake()
        {
            hitFilter = new ContactFilter2D();
            hitFilter.SetLayerMask(hittableLayers);
            hitFilter.useTriggers = true;
        }

        public bool TryStartAttack(CombatAction attackType, int facing)
        {
            if (IsBusy)
                return false;

            CurrentAttack = attackType switch
            {
                CombatAction.LightAttack => lightAttack,
                CombatAction.HeavyAttack => heavyAttack,
                _ => null
            };

            if (CurrentAttack == null)
                return false;

            attackFacing = facing;
            targetsHitThisAttack.Clear();
            CurrentAction = attackType;
            ActionStarted?.Invoke(attackType);
            EnterPhase(ActionPhase.Startup);
            return true;
        }

        /// <summary>每幀呼叫，把特殊鍵的狀態轉交給 SpecialAbility。攻擊中不會被打斷。</summary>
        public void UpdateSpecial(bool pressed, bool held, int facing)
        {
            if (IsAttacking)
                return;

            specialAbility.Tick(pressed, held, facing);

            if (specialAbility.IsActive && !IsSpecialActive)
            {
                CurrentAction = CombatAction.Special;
                ActionStarted?.Invoke(CombatAction.Special);
                EnterPhase(ActionPhase.Active);
            }
            else if (!specialAbility.IsActive && IsSpecialActive)
            {
                EndAction();
            }
        }

        /// <summary>受擊時強制中斷目前的招式</summary>
        public void CancelAction()
        {
            if (IsSpecialActive)
                specialAbility.Cancel();

            if (IsBusy)
                EndAction();
        }

        // 不限於特殊動作進行中才詢問：像完美格檔這種「點一下」的判定，放開後仍可能有效
        public DamageResult ResolveIncomingHit(ref DamageInfo info) => specialAbility.ResolveIncomingHit(ref info);

        /// <summary>目前招式作用範圍的世界座標；沒有招式時回傳 false</summary>
        public bool TryGetCurrentZone(out Vector2 center, out Vector2 size)
        {
            if (IsAttacking)
            {
                center = GetAttackZoneCenter();
                size = CurrentAttack.hitboxSize;
                return true;
            }

            if (IsSpecialActive)
                return specialAbility.TryGetZone(out center, out size);

            center = default;
            size = default;
            return false;
        }

        private void Update()
        {
            if (!IsAttacking)
                return;

            phaseTimer += Time.deltaTime;
            float phaseDuration = GetPhaseDuration(CurrentPhase);
            PhaseProgress = phaseDuration > 0f ? Mathf.Clamp01(phaseTimer / phaseDuration) : 1f;

            if (CurrentPhase == ActionPhase.Active)
                DetectHits();

            if (phaseTimer >= phaseDuration)
                AdvancePhase();
        }

        private void AdvancePhase()
        {
            switch (CurrentPhase)
            {
                case ActionPhase.Startup:
                    EnterPhase(ActionPhase.Active);
                    break;
                case ActionPhase.Active:
                    EnterPhase(ActionPhase.Recovery);
                    break;
                case ActionPhase.Recovery:
                    EndAction();
                    break;
            }
        }

        private void EnterPhase(ActionPhase phase)
        {
            CurrentPhase = phase;
            phaseTimer = 0f;
            PhaseProgress = 0f;
        }

        private void EndAction()
        {
            CurrentAction = CombatAction.None;
            CurrentAttack = null;
            EnterPhase(ActionPhase.None);
        }

        private float GetPhaseDuration(ActionPhase phase) => phase switch
        {
            ActionPhase.Startup => CurrentAttack.startupDuration,
            ActionPhase.Active => CurrentAttack.activeDuration,
            ActionPhase.Recovery => CurrentAttack.recoveryDuration,
            _ => 0f
        };

        private Vector2 GetAttackZoneCenter()
        {
            Vector2 offset = CurrentAttack.hitboxOffset;
            return (Vector2)transform.position + new Vector2(offset.x * attackFacing, offset.y);
        }

        private void DetectHits()
        {
            Physics2D.OverlapBox(GetAttackZoneCenter(), CurrentAttack.hitboxSize, 0f, hitFilter, overlapResults);

            foreach (Collider2D target in overlapResults)
            {
                if (!targetsHitThisAttack.Add(target))
                    continue;

                IDamageable damageable = target.GetComponentInParent<IDamageable>();
                if (damageable == null)
                    continue;

                var info = new DamageInfo(CurrentAttack.damage, transform.position, CurrentAttack.knockbackForce);

                if (damageable.TakeDamage(info) == DamageResult.Hit)
                    TargetHit?.Invoke(target, CurrentAttack);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 未執行時在 Scene 視窗預覽兩種攻擊的範圍（以面向右為準）
            if (Application.isPlaying)
                return;

            Vector2 origin = transform.position;
            Gizmos.color = new Color(1f, 0.6f, 0f);
            Gizmos.DrawWireCube(origin + lightAttack.hitboxOffset, lightAttack.hitboxSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(origin + heavyAttack.hitboxOffset, heavyAttack.hitboxSize);
        }
    }
}
