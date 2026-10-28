using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Enemies
{
    /// <summary>
    /// 空中射擊型敵人：在出生點高度左右橫移，於兩個端點間來回，不受重力影響。
    /// 發現玩家後停下蓄力（閃爍＋膨脹＋瞄準線），出招前轉白並鎖定方向，再朝玩家發射子彈。
    /// 出生點高度要放在玩家跳躍攻擊打得到的範圍內。
    /// 身體與子彈碰到玩家都會造成傷害。
    /// </summary>
    [RequireComponent(typeof(ContactDamageDealer))]
    public class FlyingEnemy : EnemyBase, IMovementRangeProvider
    {
        private enum State
        {
            Patrol,
            Windup,
            Recover
        }

        [Header("Hover Patrol")]
        [Tooltip("以出生點為中心，左右各飛多遠；在兩個端點間來回")]
        [SerializeField] private float patrolDistance = 2.5f;
        [SerializeField] private float patrolSpeed = 2f;

        [Header("Ranged Attack")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Transform firePoint;
        [Tooltip("生成的子彈會放在這個物件底下，保持 Hierarchy 整齊")]
        [SerializeField] private Transform projectileParent;
        [Tooltip("蓄力時間，最後一段會轉白並鎖定瞄準方向")]
        [SerializeField] private float windupDuration = 0.8f;
        [Tooltip("發射後停在原地的時間，也是玩家反擊或反彈子彈命中它的機會")]
        [SerializeField] private float recoverDuration = 0.6f;
        [Tooltip("回到巡邏後，至少隔這麼久才會再次發射")]
        [SerializeField] private float fireCooldown = 1.2f;

        [Header("Windup Telegraph")]
        [SerializeField] private AttackTelegraph telegraph = new AttackTelegraph();
        [Tooltip("蓄力時身體膨脹的比例")]
        [SerializeField] private Vector2 windupSwellScale = new Vector2(1.35f, 1.35f);
        [Tooltip("蓄力時顯示的瞄準線，讓玩家看得出子彈會往哪飛")]
        [SerializeField] private LineRenderer aimLine;
        [SerializeField] private float aimLineLength = 6f;

        public override string StateName
        {
            get
            {
                if (IsStunned)
                    return IsStaggered ? "Staggered" : "Hurt";

                return state == State.Windup ? $"Windup {stateTimer / windupDuration:P0}" : state.ToString();
            }
        }

        private State state = State.Patrol;
        private float stateTimer;
        private float lastFireTime = float.NegativeInfinity;
        private Vector2 anchor;
        private float patrolTime;
        private Vector2 aimDirection = Vector2.down;

        protected override void Awake()
        {
            base.Awake();
            anchor = transform.position;
            // 從中心點開始巡邏（PingPong 從左端點起算，所以先推進半程）
            patrolTime = patrolDistance / patrolSpeed;
            aimLine.enabled = false;
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
                case State.Windup:
                    UpdateWindup();
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
                ChangeState(State.Windup);
        }

        private void UpdateWindup()
        {
            if (!HasTarget)
            {
                ChangeState(State.Patrol);
                return;
            }

            // 轉白提示出現後鎖定方向，玩家看到提示還來得及閃避或格檔
            if (!telegraph.IsCueActive(stateTimer, windupDuration))
            {
                aimDirection = ((Vector2)Target.position - (Vector2)firePoint.position).normalized;
                SetFacing(GetDirectionToTarget());
            }

            if (stateTimer >= windupDuration)
                Fire();
        }

        private void Fire()
        {
            Projectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity, projectileParent);
            projectile.Launch(aimDirection, transform);
            lastFireTime = Time.time;
            ChangeState(State.Recover);
        }

        private void ChangeState(State newState)
        {
            state = newState;
            stateTimer = 0f;
        }

        protected override void OnHurt(DamageInfo info)
        {
            // 蓄力中被打會中斷射擊
            if (state == State.Windup)
                ChangeState(State.Recover);
        }

        protected override void Update()
        {
            bool isWindingUp = state == State.Windup && !IsStunned;
            float windupProgress = Mathf.Clamp01(stateTimer / windupDuration);
            VisualScale = isWindingUp ? Vector2.Lerp(Vector2.one, windupSwellScale, windupProgress) : Vector2.one;
            UpdateAimLine(isWindingUp);
            base.Update();
        }

        private void UpdateAimLine(bool isVisible)
        {
            aimLine.enabled = isVisible;
            if (!isVisible)
                return;

            Vector3 start = firePoint.position;
            aimLine.SetPosition(0, start);
            aimLine.SetPosition(1, start + (Vector3)(aimDirection * aimLineLength));

            Color color = telegraph.Evaluate(telegraph.warningColor, stateTimer, windupDuration);
            aimLine.startColor = color;
            aimLine.endColor = new Color(color.r, color.g, color.b, 0f);
        }

        protected override Color GetBodyColor()
        {
            if (state == State.Windup && !IsStunned)
                return telegraph.Evaluate(BaseColor, stateTimer, windupDuration);

            return BaseColor;
        }

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
