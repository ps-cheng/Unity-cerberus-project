using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Enemies
{
    /// <summary>
    /// 空中雷射無人機：在出生點高度左右橫移，於兩個端點間來回，不受重力影響。
    /// 發現玩家後停下，用 LaserEmitter 瞄準 → 充能 → 發射；被完美格檔時雷射反射回無人機身上。
    /// 出生點高度要放在玩家跳躍攻擊打得到的範圍內。身體碰到玩家也會造成傷害。
    /// </summary>
    [RequireComponent(typeof(ContactDamageDealer))]
    public class FlyingEnemy : EnemyBase, IMovementRangeProvider
    {
        private enum State
        {
            Patrol,
            Attack,
            Recover
        }

        [Header("Hover Patrol")]
        [Tooltip("以出生點為中心，左右各飛多遠；在兩個端點間來回")]
        [SerializeField] private float patrolDistance = 2.5f;
        [SerializeField] private float patrolSpeed = 2f;

        [Header("Laser Attack")]
        [SerializeField] private LaserEmitter laser;
        [Tooltip("射擊後停在原地的時間，是玩家反擊的機會")]
        [SerializeField] private float recoverDuration = 0.6f;
        [Tooltip("回到巡邏後，至少隔這麼久才會再次攻擊")]
        [SerializeField] private float fireCooldown = 1.2f;
        [Tooltip("充能時身體膨脹的比例")]
        [SerializeField] private Vector2 chargeSwellScale = new Vector2(1.35f, 1.35f);

        protected override string BehaviorStateName => state == State.Attack ? laser.PhaseText : state.ToString();

        private State state = State.Patrol;
        private float stateTimer;
        private float lastFireTime = float.NegativeInfinity;
        private Vector2 anchor;
        private float patrolTime;

        protected override void Awake()
        {
            base.Awake();
            anchor = transform.position;
            // 從中心點開始巡邏（PingPong 從左端點起算，所以先推進半程）
            patrolTime = patrolDistance / patrolSpeed;
            // 射出雷射才開始算冷卻（含被格檔反射），瞄準中被打斷不算
            laser.Finished += () => lastFireTime = Time.time;
        }

        private void FixedUpdate()
        {
            // 受擊硬直時停在原地
            if (IsStunned)
                return;

            stateTimer += Time.fixedDeltaTime;

            switch (state)
            {
                case State.Patrol:
                    UpdatePatrol();
                    break;
                case State.Attack:
                    UpdateAttack();
                    break;
                case State.Recover:
                    if (stateTimer >= recoverDuration)
                        ChangeState(State.Patrol);
                    break;
            }
        }

        private void UpdatePatrol()
        {
            float previousX = Body.position.x;
            patrolTime += Time.fixedDeltaTime;
            float x = anchor.x - patrolDistance + Mathf.PingPong(patrolTime * patrolSpeed, patrolDistance * 2f);
            Body.MovePosition(new Vector2(x, anchor.y));

            if (HasTarget)
                SetFacing(GetDirectionToTarget());
            else
                SetFacing(x - previousX);

            if (HasTarget && Time.time - lastFireTime >= fireCooldown)
            {
                laser.Begin(Target);
                ChangeState(State.Attack);
            }
        }

        private void UpdateAttack()
        {
            if (!HasTarget)
            {
                laser.Cancel();
                ChangeState(State.Patrol);
                return;
            }

            if (!laser.IsAimLocked)
                SetFacing(GetDirectionToTarget());

            laser.Tick(Time.fixedDeltaTime);

            // 反射命中自己時 OnHurt 已經切到 Recover
            if (state == State.Attack && !laser.IsBusy)
                ChangeState(State.Recover);
        }

        private void ChangeState(State newState)
        {
            state = newState;
            stateTimer = 0f;
        }

        protected override void OnHurt(DamageInfo info)
        {
            // 瞄準或充能中被打會中斷射擊
            if (state == State.Attack)
            {
                laser.Cancel();
                ChangeState(State.Recover);
            }
        }

        protected override void Update()
        {
            bool isCharging = laser.CurrentPhase == LaserEmitter.Phase.Charge && !IsStunned;
            VisualScale = isCharging ? Vector2.Lerp(Vector2.one, chargeSwellScale, laser.ChargeProgress) : Vector2.one;
            base.Update();
        }

        protected override Color GetBodyColor() => IsStunned ? BaseColor : laser.EvaluateTelegraph(BaseColor);

        public Vector3[] GetMovementRangePoints()
        {
            return new[]
            {
                new Vector3(anchor.x - patrolDistance, anchor.y),
                new Vector3(anchor.x + patrolDistance, anchor.y)
            };
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            Gizmos.color = Color.green;
            Vector3 center = Application.isPlaying ? (Vector3)anchor : transform.position;
            Gizmos.DrawLine(center + Vector3.left * patrolDistance, center + Vector3.right * patrolDistance);
        }
    }
}
