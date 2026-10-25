using System;
using System.Collections.Generic;
using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 處理弱攻擊、強攻擊與格檔的計時、階段切換與命中判定。
    /// 只管「戰鬥狀態」，移動限制由 PlayerController 依 IsBusy 決定。
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Attacks")]
        [SerializeField] private AttackData lightAttack = new AttackData
        {
            displayName = "Light Attack",
            startupDuration = 0.06f,
            activeDuration = 0.1f,
            recoveryDuration = 0.14f,
            hitboxOffset = new Vector2(0.95f, 0.15f),
            hitboxSize = new Vector2(1.2f, 0.9f),
            damage = 10,
            windupAngle = 20f
        };

        [SerializeField] private AttackData heavyAttack = new AttackData
        {
            displayName = "Heavy Attack",
            startupDuration = 0.28f,
            activeDuration = 0.16f,
            recoveryDuration = 0.5f,
            hitboxOffset = new Vector2(1.35f, 0.3f),
            hitboxSize = new Vector2(2.2f, 1.8f),
            damage = 25,
            windupAngle = 70f
        };

        [Header("Block")]
        [SerializeField] private Vector2 blockZoneOffset = new Vector2(0.6f, 0.2f);
        [SerializeField] private Vector2 blockZoneSize = new Vector2(0.4f, 1.6f);

        [Header("Hit Detection")]
        [SerializeField] private LayerMask hittableLayers;

        public CombatAction CurrentAction { get; private set; }
        public ActionPhase CurrentPhase { get; private set; }
        public AttackData CurrentAttack { get; private set; }
        public bool IsBusy => CurrentAction != CombatAction.None;
        public bool IsAttacking => CurrentAction == CombatAction.LightAttack || CurrentAction == CombatAction.HeavyAttack;
        public bool IsBlocking => CurrentAction == CombatAction.Block;

        /// <summary>目前階段的進度 0~1，給視覺與 HUD 使用</summary>
        public float PhaseProgress { get; private set; }

        /// <summary>目前招式（攻擊或格檔）作用範圍的世界座標中心</summary>
        public Vector2 CurrentZoneCenter => GetZoneCenter(CurrentAction == CombatAction.Block ? blockZoneOffset : CurrentAttack.hitboxOffset);
        public Vector2 CurrentZoneSize => CurrentAction == CombatAction.Block ? blockZoneSize : CurrentAttack.hitboxSize;

        public event Action<CombatAction> ActionStarted;
        public event Action<ActionPhase> PhaseChanged;
        public event Action<Collider2D> TargetHit;

        private int actionFacing = 1;
        private float phaseTimer;
        private ContactFilter2D hitFilter;
        private readonly List<Collider2D> overlapResults = new List<Collider2D>();
        // 同一次揮擊對同一目標只命中一次
        private readonly HashSet<Collider2D> targetsHitThisSwing = new HashSet<Collider2D>();

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

            actionFacing = facing;
            targetsHitThisSwing.Clear();
            CurrentAction = attackType;
            ActionStarted?.Invoke(attackType);
            EnterPhase(ActionPhase.Startup);
            return true;
        }

        /// <summary>每幀呼叫：按住就進入格檔，放開就解除。攻擊中不會被打斷。</summary>
        public void UpdateBlock(bool blockHeld, int facing)
        {
            if (blockHeld && !IsBusy)
            {
                actionFacing = facing;
                CurrentAction = CombatAction.Block;
                ActionStarted?.Invoke(CombatAction.Block);
                EnterPhase(ActionPhase.Active);
            }
            else if (!blockHeld && IsBlocking)
            {
                EndAction();
            }
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
            PhaseChanged?.Invoke(phase);
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

        private Vector2 GetZoneCenter(Vector2 offset)
        {
            return (Vector2)transform.position + new Vector2(offset.x * actionFacing, offset.y);
        }

        private void DetectHits()
        {
            Physics2D.OverlapBox(CurrentZoneCenter, CurrentZoneSize, 0f, hitFilter, overlapResults);

            foreach (Collider2D target in overlapResults)
            {
                if (!targetsHitThisSwing.Add(target))
                    continue;

                if (target.TryGetComponent(out IDamageable damageable))
                    damageable.TakeDamage(CurrentAttack.damage, new Vector2(actionFacing, 0f));

                TargetHit?.Invoke(target);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 未執行時在 Scene 視窗預覽各招範圍（以面向右為準）
            if (Application.isPlaying)
                return;

            Vector2 origin = transform.position;
            Gizmos.color = new Color(1f, 0.6f, 0f);
            Gizmos.DrawWireCube(origin + lightAttack.hitboxOffset, lightAttack.hitboxSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(origin + heavyAttack.hitboxOffset, heavyAttack.hitboxSize);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(origin + blockZoneOffset, blockZoneSize);
        }
    }
}
