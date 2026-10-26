using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Enemies
{
    /// <summary>
    /// 地面敵人：在出生點左右固定範圍內巡邏；發現玩家後追上去，
    /// 進入距離後停頓蓄力（變黃色預告），再跳起撲向玩家。身體碰到玩家就造成傷害。
    /// </summary>
    [RequireComponent(typeof(ContactDamageDealer))]
    public class GroundPatrolEnemy : EnemyBase, IMovementRangeProvider
    {
        private enum State
        {
            Patrol,
            Chase,
            Windup,
            Pounce,
            Recover
        }

        [Header("Patrol")]
        [Tooltip("以出生點為中心，左右各可走多遠")]
        [SerializeField] private float patrolDistance = 2.5f;
        [SerializeField] private float patrolSpeed = 1.5f;

        [Header("Chase")]
        [SerializeField] private float chaseSpeed = 3.5f;

        [Header("Pounce")]
        [Tooltip("與玩家水平距離小於此值時開始蓄力撲擊")]
        [SerializeField] private float pounceTriggerDistance = 3f;
        [SerializeField] private float pounceWindupDuration = 0.45f;
        [SerializeField] private float pounceHorizontalSpeed = 7f;
        [SerializeField] private float pounceVerticalSpeed = 8f;
        [Tooltip("撲擊落地後的硬直，玩家反擊的機會")]
        [SerializeField] private float pounceRecoverDuration = 0.8f;
        [SerializeField] private Color windupColor = new Color(1f, 0.85f, 0.2f);

        [Header("Hit Reaction")]
        [Tooltip("撞上玩家的盾（格檔或完美格檔）時被彈開的速度")]
        [SerializeField] private float blockedBounceSpeed = 5f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);
        [SerializeField] private LayerMask groundLayer;

        public override string StateName => IsStunned ? "Hurt" : state.ToString();

        private State state = State.Patrol;
        private float stateTimer;
        private float patrolLeftX;
        private float patrolRightX;
        private float patrolDirection = 1f;
        private bool isGrounded;
        private bool hasLeftGround;

        protected override void Awake()
        {
            base.Awake();
            patrolLeftX = transform.position.x - patrolDistance;
            patrolRightX = transform.position.x + patrolDistance;
            GetComponent<ContactDamageDealer>().HitResolved += HandleContactResolved;
        }

        private void FixedUpdate()
        {
            isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer) != null;

            // 受擊硬直中不做任何決策，讓擊退效果完整呈現
            if (IsStunned)
                return;

            stateTimer += Time.fixedDeltaTime;

            switch (state)
            {
                case State.Patrol:
                    UpdatePatrol();
                    break;
                case State.Chase:
                    UpdateChase();
                    break;
                case State.Windup:
                    UpdateWindup();
                    break;
                case State.Pounce:
                    UpdatePounce();
                    break;
                case State.Recover:
                    UpdateRecover();
                    break;
            }
        }

        private void UpdatePatrol()
        {
            if (HasTarget)
            {
                ChangeState(State.Chase);
                return;
            }

            // 超出範圍就掉頭；追擊結束後若在範圍外，也會自然走回範圍內
            if (transform.position.x <= patrolLeftX)
                patrolDirection = 1f;
            else if (transform.position.x >= patrolRightX)
                patrolDirection = -1f;

            SetFacing(patrolDirection);
            SetHorizontalVelocity(patrolDirection * patrolSpeed);
        }

        private void UpdateChase()
        {
            if (!HasTarget)
            {
                ChangeState(State.Patrol);
                return;
            }

            float direction = GetDirectionToTarget();
            SetFacing(direction);

            float horizontalDistance = Mathf.Abs(Target.position.x - transform.position.x);
            if (horizontalDistance <= pounceTriggerDistance && isGrounded)
            {
                ChangeState(State.Windup);
                return;
            }

            SetHorizontalVelocity(direction * chaseSpeed);
        }

        private void UpdateWindup()
        {
            SetHorizontalVelocity(0f);

            if (stateTimer < pounceWindupDuration)
                return;

            Body.linearVelocity = new Vector2(FacingDirection * pounceHorizontalSpeed, pounceVerticalSpeed);
            hasLeftGround = false;
            ChangeState(State.Pounce);
        }

        private void UpdatePounce()
        {
            // 起跳的第一幀可能還判定在地上，所以要先離地過才算落地
            if (!isGrounded)
                hasLeftGround = true;
            else if (hasLeftGround)
                ChangeState(State.Recover);
        }

        private void UpdateRecover()
        {
            SetHorizontalVelocity(Mathf.MoveTowards(Body.linearVelocity.x, 0f, 30f * Time.fixedDeltaTime));

            if (stateTimer >= pounceRecoverDuration)
                ChangeState(HasTarget ? State.Chase : State.Patrol);
        }

        private void ChangeState(State newState)
        {
            state = newState;
            stateTimer = 0f;
        }

        private void SetHorizontalVelocity(float x) => Body.linearVelocity = new Vector2(x, Body.linearVelocity.y);

        protected override void OnHurt(DamageInfo info)
        {
            float awayDirection = Mathf.Sign(transform.position.x - info.sourcePosition.x);
            Body.linearVelocity = new Vector2(awayDirection * info.knockbackForce, 2f);

            // 蓄力中被打會中斷撲擊
            if (state == State.Windup)
                ChangeState(State.Chase);
        }

        private void HandleContactResolved(DamageResult result)
        {
            if (result == DamageResult.Hit)
                return;

            if (result == DamageResult.Parried)
                ApplyParriedStagger();

            Body.linearVelocity = new Vector2(-FacingDirection * blockedBounceSpeed, 3f);
            ChangeState(State.Recover);
        }

        protected override Color GetBodyColor() => state == State.Windup ? windupColor : BaseColor;

        public Vector3[] GetMovementRangePoints()
        {
            float y = groundCheck.position.y;
            return new[] { new Vector3(patrolLeftX, y), new Vector3(patrolRightX, y) };
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            Gizmos.color = Color.green;
            Vector3 center = Application.isPlaying ? new Vector3((patrolLeftX + patrolRightX) * 0.5f, transform.position.y) : transform.position;
            Gizmos.DrawLine(center + Vector3.left * patrolDistance, center + Vector3.right * patrolDistance);
        }
    }
}
