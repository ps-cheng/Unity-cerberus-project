using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Enemies
{
    /// <summary>
    /// 基本敵人：發現玩家後靠近 → 進入攻擊距離就停下蓄力（閃爍＋壓縮預告）→ 出招 → 收招後重新靠近。
    /// 「靠近」與「攻擊」明確分成兩段，攻擊期間不會移動，方便玩家判讀。
    /// 身體沒有接觸傷害，只有出招會造成傷害。
    /// </summary>
    public class BasicEnemy : EnemyBase, ICombatZoneSource
    {
        private enum State
        {
            Idle,
            Approach,
            Windup,
            Attack,
            Recover
        }

        [Header("Movement")]
        [Tooltip("比玩家移動速度（7）稍慢：玩家拉得開距離，但停下來就會被追上")]
        [SerializeField] private float moveSpeed = 5.5f;
        [SerializeField] private float acceleration = 40f;

        [Header("Attack")]
        [Tooltip("與玩家水平距離小於此值就停下準備攻擊")]
        [SerializeField] private float attackTriggerDistance = 1.6f;
        [Tooltip("高度差超過此值不出招，例如玩家站在頭頂的平台上")]
        [SerializeField] private float attackMaxHeightDifference = 1.5f;
        [SerializeField] private AttackData attack = new AttackData
        {
            displayName = "Swipe",
            startupDuration = 0.65f,
            activeDuration = 0.15f,
            recoveryDuration = 0.6f,
            hitboxOffset = new Vector2(1f, 0f),
            hitboxSize = new Vector2(1.4f, 1.2f),
            damage = 15,
            knockbackForce = 8f
        };

        [Header("Windup Telegraph")]
        [SerializeField] private Color telegraphColor = new Color(1f, 0.85f, 0.2f);
        [Tooltip("閃爍頻率從 Start 漸增到 End，越接近出招閃得越快")]
        [SerializeField] private float telegraphBlinkStartFrequency = 4f;
        [SerializeField] private float telegraphBlinkEndFrequency = 14f;
        [Tooltip("蓄力時身體壓扁的比例，出招時再往前伸展")]
        [SerializeField] private Vector2 windupSquashScale = new Vector2(1.15f, 0.8f);
        [SerializeField] private Vector2 attackStretchScale = new Vector2(1.3f, 0.9f);
        [SerializeField] private Color attackColor = new Color(1f, 0.3f, 0.2f);

        public ActionPhase CurrentPhase => state switch
        {
            State.Windup => ActionPhase.Startup,
            State.Attack => ActionPhase.Active,
            State.Recover => ActionPhase.Recovery,
            _ => ActionPhase.None
        };

        public bool IsSpecialActive => false;

        public override string StateName
        {
            get
            {
                if (IsStaggered)
                    return "Staggered";

                if (IsStunned)
                    return "Hurt";

                return state == State.Windup ? $"Windup {PhaseProgress:P0}" : state.ToString();
            }
        }

        private State state = State.Idle;
        private float stateTimer;
        private bool hasHitThisAttack;

        private float PhaseProgress
        {
            get
            {
                float duration = state switch
                {
                    State.Windup => attack.startupDuration,
                    State.Attack => attack.activeDuration,
                    State.Recover => attack.recoveryDuration,
                    _ => 0f
                };

                return duration > 0f ? Mathf.Clamp01(stateTimer / duration) : 0f;
            }
        }

        private void FixedUpdate()
        {
            // 受擊硬直中不做任何決策，讓擊退效果完整呈現
            if (IsStunned)
                return;

            stateTimer += Time.fixedDeltaTime;

            switch (state)
            {
                case State.Idle:
                    UpdateIdle();
                    break;
                case State.Approach:
                    UpdateApproach();
                    break;
                case State.Windup:
                    UpdateWindup();
                    break;
                case State.Attack:
                    UpdateAttack();
                    break;
                case State.Recover:
                    UpdateRecover();
                    break;
            }
        }

        private void UpdateIdle()
        {
            MoveTowardsSpeed(0f);

            if (HasTarget)
                ChangeState(State.Approach);
        }

        private void UpdateApproach()
        {
            if (!HasTarget)
            {
                ChangeState(State.Idle);
                return;
            }

            float direction = GetDirectionToTarget();
            SetFacing(direction);

            if (IsTargetInAttackRange())
            {
                // 進入距離就立刻停下，不邊走邊打
                SetHorizontalVelocity(0f);
                ChangeState(State.Windup);
                return;
            }

            MoveTowardsSpeed(direction * moveSpeed);
        }

        private void UpdateWindup()
        {
            SetHorizontalVelocity(0f);

            if (stateTimer < attack.startupDuration)
                return;

            hasHitThisAttack = false;
            ChangeState(State.Attack);
        }

        private void UpdateAttack()
        {
            TryHitTarget();

            if (stateTimer >= attack.activeDuration)
                ChangeState(State.Recover);
        }

        private void UpdateRecover()
        {
            if (stateTimer >= attack.recoveryDuration)
                ChangeState(HasTarget ? State.Approach : State.Idle);
        }

        private bool IsTargetInAttackRange()
        {
            Vector2 offset = Target.position - transform.position;
            return Mathf.Abs(offset.x) <= attackTriggerDistance && Mathf.Abs(offset.y) <= attackMaxHeightDifference;
        }

        private void TryHitTarget()
        {
            if (hasHitThisAttack)
                return;

            Collider2D hit = Physics2D.OverlapBox(GetAttackZoneCenter(), attack.hitboxSize, 0f, TargetLayers);
            if (hit == null)
                return;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null)
                return;

            DamageResult result = damageable.TakeDamage(new DamageInfo(attack.damage, transform.position, attack.knockbackForce));

            // 目標無敵時（Ignored）不算命中，判定期間內仍可再嘗試
            if (result != DamageResult.Ignored)
                hasHitThisAttack = true;

            // 被完美格檔：中斷出招，硬直結束後重新靠近
            if (result == DamageResult.Parried)
            {
                ApplyParriedStagger();
                ChangeState(State.Approach);
            }
        }

        private Vector2 GetAttackZoneCenter()
        {
            return (Vector2)transform.position + new Vector2(attack.hitboxOffset.x * FacingDirection, attack.hitboxOffset.y);
        }

        public bool TryGetCurrentZone(out Vector2 center, out Vector2 size)
        {
            center = GetAttackZoneCenter();
            size = attack.hitboxSize;
            return CurrentPhase != ActionPhase.None && !IsStunned;
        }

        private void ChangeState(State newState)
        {
            state = newState;
            stateTimer = 0f;
        }

        private void MoveTowardsSpeed(float targetSpeed)
        {
            SetHorizontalVelocity(Mathf.MoveTowards(Body.linearVelocity.x, targetSpeed, acceleration * Time.fixedDeltaTime));
        }

        private void SetHorizontalVelocity(float x) => Body.linearVelocity = new Vector2(x, Body.linearVelocity.y);

        protected override void OnHurt(DamageInfo info)
        {
            float awayDirection = Mathf.Sign(transform.position.x - info.sourcePosition.x);
            Body.linearVelocity = new Vector2(awayDirection * info.knockbackForce, 2f);

            // 蓄力或出招中被打會中斷，硬直結束後要重新靠近、重新蓄力，玩家有喘息空間
            if (state == State.Windup || state == State.Attack)
                ChangeState(State.Approach);
        }

        protected override void Update()
        {
            VisualScale = GetPoseScale();
            base.Update();
        }

        private Vector2 GetPoseScale()
        {
            if (IsStunned)
                return Vector2.one;

            return state switch
            {
                State.Windup => Vector2.Lerp(Vector2.one, windupSquashScale, PhaseProgress),
                State.Attack => attackStretchScale,
                State.Recover => Vector2.Lerp(attackStretchScale, Vector2.one, PhaseProgress),
                _ => Vector2.one
            };
        }

        protected override Color GetBodyColor()
        {
            if (IsStunned)
                return BaseColor;

            switch (state)
            {
                case State.Windup:
                    float frequency = Mathf.Lerp(telegraphBlinkStartFrequency, telegraphBlinkEndFrequency, PhaseProgress);
                    bool isHighlighted = Mathf.Repeat(stateTimer * frequency, 1f) < 0.5f;
                    return isHighlighted ? telegraphColor : BaseColor;
                case State.Attack:
                    return attackColor;
                default:
                    return BaseColor;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (Application.isPlaying)
                return;

            // 未執行時預覽攻擊距離與攻擊範圍（以面向右為準）
            Gizmos.color = new Color(1f, 0.6f, 0f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * attackTriggerDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube((Vector2)transform.position + attack.hitboxOffset, attack.hitboxSize);
        }
    }
}
