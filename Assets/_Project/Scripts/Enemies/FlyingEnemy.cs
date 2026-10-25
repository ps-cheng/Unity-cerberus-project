using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Enemies
{
    /// <summary>
    /// 空中敵人：沿著固定軌跡飛行，不受重力影響；發現玩家後會邊飛邊瞄準射擊。
    /// 身體與子彈碰到玩家都會造成傷害。
    /// </summary>
    [RequireComponent(typeof(ContactDamageDealer))]
    public class FlyingEnemy : EnemyBase, IMovementRangeProvider
    {
        private enum FlightPattern
        {
            Horizontal,
            Circle,
            FigureEight
        }

        private enum State
        {
            Cruise,
            Aim,
            Cooldown
        }

        [Header("Flight Path")]
        [SerializeField] private FlightPattern flightPattern = FlightPattern.FigureEight;
        [Tooltip("軌跡在 X / Y 方向離出生點的最大距離")]
        [SerializeField] private Vector2 pathExtents = new Vector2(2.5f, 1f);
        [Tooltip("繞完一圈所需秒數")]
        [SerializeField] private float pathPeriod = 5f;

        [Header("Ranged Attack")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Transform firePoint;
        [Tooltip("生成的子彈會放在這個物件底下，保持 Hierarchy 整齊")]
        [SerializeField] private Transform projectileParent;
        [Tooltip("開火前的瞄準時間（變黃色預告）")]
        [SerializeField] private float aimDuration = 0.5f;
        [SerializeField] private float fireCooldown = 1.5f;
        [SerializeField] private Color aimColor = new Color(1f, 0.85f, 0.2f);

        public override string StateName => IsStunned ? "Hurt" : state.ToString();

        private const int PathPreviewSegments = 48;

        private State state = State.Cruise;
        private float stateTimer;
        private Vector2 pathAnchor;
        private float pathTime;

        protected override void Awake()
        {
            base.Awake();
            pathAnchor = transform.position;
        }

        private void FixedUpdate()
        {
            // 受擊硬直時停在原地
            if (IsStunned)
                return;

            pathTime += Time.fixedDeltaTime;
            Body.MovePosition(pathAnchor + EvaluatePath(pathTime));
        }

        protected override void Update()
        {
            base.Update();
            stateTimer += Time.deltaTime;

            if (HasTarget)
                SetFacing(GetDirectionToTarget());
            else
                SetFacing(EvaluatePath(pathTime + 0.05f).x - EvaluatePath(pathTime).x);

            switch (state)
            {
                case State.Cruise:
                    if (HasTarget)
                        ChangeState(State.Aim);
                    break;
                case State.Aim:
                    if (!HasTarget)
                        ChangeState(State.Cruise);
                    else if (stateTimer >= aimDuration)
                        Fire();
                    break;
                case State.Cooldown:
                    if (stateTimer >= fireCooldown)
                        ChangeState(HasTarget ? State.Aim : State.Cruise);
                    break;
            }
        }

        private void Fire()
        {
            Projectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity, projectileParent);
            projectile.Launch(Target.position - firePoint.position);
            ChangeState(State.Cooldown);
        }

        private void ChangeState(State newState)
        {
            state = newState;
            stateTimer = 0f;
        }

        /// <summary>依時間算出相對出生點的位置；軌跡都是週期函數，繞完會回到原點</summary>
        private Vector2 EvaluatePath(float time)
        {
            float angle = time / pathPeriod * Mathf.PI * 2f;

            return flightPattern switch
            {
                FlightPattern.Horizontal => new Vector2(Mathf.Sin(angle) * pathExtents.x, 0f),
                FlightPattern.Circle => new Vector2(Mathf.Sin(angle) * pathExtents.x, Mathf.Cos(angle) * pathExtents.y - pathExtents.y),
                // 8 字形：Y 的頻率是 X 的兩倍
                _ => new Vector2(Mathf.Sin(angle) * pathExtents.x, Mathf.Sin(angle * 2f) * pathExtents.y)
            };
        }

        protected override void OnHurt(DamageInfo info)
        {
            // 瞄準中被打會中斷射擊
            if (state == State.Aim)
                ChangeState(State.Cooldown);
        }

        protected override Color GetBodyColor() => state == State.Aim ? aimColor : BaseColor;

        public Vector3[] GetMovementRangePoints()
        {
            var points = new Vector3[PathPreviewSegments + 1];

            for (int i = 0; i <= PathPreviewSegments; i++)
            {
                float time = pathPeriod * i / PathPreviewSegments;
                points[i] = pathAnchor + EvaluatePath(time);
            }

            return points;
        }
    }
}
