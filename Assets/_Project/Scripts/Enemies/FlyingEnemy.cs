using Cerberus.Combat;
using UnityEngine;
using UnityEngine.Serialization;

namespace Cerberus.Enemies
{
    /// <summary>
    /// 空中雷射無人機：在出生點高度左右橫移，於兩個端點間來回，不受重力影響。
    /// 發現玩家後停下：紅色瞄準線追蹤 → 閃爍充能 → 轉白鎖定 → 瞬間射出雷射（hitscan，沒有飛行時間）。
    /// 雷射被完美格檔時會沿原路反射回無人機身上。
    /// 出生點高度要放在玩家跳躍攻擊打得到的範圍內。身體碰到玩家也會造成傷害。
    /// </summary>
    [RequireComponent(typeof(ContactDamageDealer))]
    public class FlyingEnemy : EnemyBase, IMovementRangeProvider
    {
        private enum State
        {
            Patrol,
            Aim,
            Charge,
            Fire,
            Recover
        }

        [Header("Hover Patrol")]
        [Tooltip("以出生點為中心，左右各飛多遠；在兩個端點間來回")]
        [SerializeField] private float patrolDistance = 2.5f;
        [SerializeField] private float patrolSpeed = 2f;

        [Header("Laser Attack")]
        [SerializeField] private Transform firePoint;
        [Tooltip("紅色瞄準線持續追蹤玩家的時間")]
        [SerializeField] private float aimDuration = 0.6f;
        [Tooltip("閃爍充能的時間，最後一段會轉白並鎖定方向")]
        [SerializeField] private float chargeDuration = 0.6f;
        [Tooltip("雷射持續的時間，期間內碰到玩家就命中一次")]
        [SerializeField] private float beamDuration = 0.1f;
        [Tooltip("射擊後停在原地的時間，是玩家反擊的機會")]
        [SerializeField] private float recoverDuration = 0.6f;
        [Tooltip("回到巡邏後，至少隔這麼久才會再次攻擊")]
        [SerializeField] private float fireCooldown = 1.2f;
        [SerializeField] private float laserRange = 12f;
        [SerializeField, Min(0)] private int laserDamage = 10;
        [SerializeField] private float laserKnockback = 6f;
        [Tooltip("雷射會被這些 Layer 擋住")]
        [SerializeField] private LayerMask obstacleLayers;

        [Header("Reflect")]
        [Tooltip("完美格檔的獎勵：雷射反射回來的傷害倍率")]
        [SerializeField] private float reflectedDamageMultiplier = 2f;
        [SerializeField] private Color reflectedColor = new Color(0.4f, 0.9f, 1f);

        [Header("Telegraph")]
        [SerializeField] private AttackTelegraph telegraph = new AttackTelegraph();
        [Tooltip("充能時身體膨脹的比例")]
        [SerializeField] private Vector2 chargeSwellScale = new Vector2(1.35f, 1.35f);
        [FormerlySerializedAs("aimLine")]
        [SerializeField] private LineRenderer laserLine;
        [SerializeField] private Color aimColor = new Color(1f, 0.15f, 0.2f);
        [SerializeField] private float aimWidth = 0.03f;
        [SerializeField] private float cueWidth = 0.06f;
        [SerializeField] private float beamWidth = 0.3f;
        [SerializeField] private Color beamColor = new Color(1f, 0.35f, 0.4f);

        public override string StateName
        {
            get
            {
                if (IsStunned)
                    return IsStaggered ? "Staggered" : "Hurt";

                return state == State.Charge ? $"Charge {stateTimer / chargeDuration:P0}" : state.ToString();
            }
        }

        private State state = State.Patrol;
        private float stateTimer;
        private float lastFireTime = float.NegativeInfinity;
        private Vector2 anchor;
        private float patrolTime;
        private Vector2 aimDirection = Vector2.down;
        private bool hasHitThisBeam;

        // 雷射畫面獨立於狀態計時，反射後狀態被打斷或死亡停留時，光束仍會顯示完
        private float beamVisibleUntil;
        private Vector2 beamStart;
        private Vector2 beamEnd;
        private Color beamDisplayColor;

        private bool IsAimLocked => state == State.Charge && telegraph.IsCueActive(stateTimer, chargeDuration);

        protected override void Awake()
        {
            base.Awake();
            anchor = transform.position;
            // 從中心點開始巡邏（PingPong 從左端點起算，所以先推進半程）
            patrolTime = patrolDistance / patrolSpeed;
            laserLine.enabled = false;
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
                case State.Aim:
                case State.Charge:
                    UpdateWindup();
                    break;
                case State.Fire:
                    UpdateFire();
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
                ChangeState(State.Aim);
        }

        private void UpdateWindup()
        {
            if (!HasTarget)
            {
                ChangeState(State.Patrol);
                return;
            }

            // 轉白提示出現後鎖定方向，玩家看到提示還來得及閃避或格檔
            if (!IsAimLocked)
            {
                aimDirection = ((Vector2)Target.position - (Vector2)firePoint.position).normalized;
                SetFacing(GetDirectionToTarget());
            }

            if (state == State.Aim && stateTimer >= aimDuration)
                ChangeState(State.Charge);
            else if (state == State.Charge && stateTimer >= chargeDuration)
                Fire();
        }

        private void Fire()
        {
            hasHitThisBeam = false;
            lastFireTime = Time.time;
            ChangeState(State.Fire);
            UpdateFire();
        }

        private void UpdateFire()
        {
            RaycastHit2D hit = CastLaser();
            ShowBeam(firePoint.position, hit ? hit.point : (Vector2)firePoint.position + aimDirection * laserRange, beamColor);

            if (!hasHitThisBeam && hit && IsInLayerMask(hit.collider.gameObject.layer, TargetLayers))
                TryDamageTarget(hit);

            if (stateTimer >= beamDuration)
                ChangeState(State.Recover);
        }

        private void TryDamageTarget(RaycastHit2D hit)
        {
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable == null)
                return;

            DamageResult result = damageable.TakeDamage(new DamageInfo(laserDamage, firePoint.position, laserKnockback));

            // 目標無敵時（Ignored）不算命中，雷射持續期間仍可再嘗試
            if (result == DamageResult.Ignored)
                return;

            hasHitThisBeam = true;

            if (result == DamageResult.Parried)
                Reflect(hit.point);
        }

        /// <summary>完美格檔：雷射從格檔點沿原路射回無人機，直接命中發射者本身</summary>
        private void Reflect(Vector2 parryPoint)
        {
            ShowBeam(parryPoint, firePoint.position, reflectedColor);
            int reflectedDamage = Mathf.RoundToInt(laserDamage * reflectedDamageMultiplier);
            if (TakeDamage(new DamageInfo(reflectedDamage, parryPoint, laserKnockback)) == DamageResult.Hit)
                CombatEvents.RaiseReflectedHit();
        }

        private RaycastHit2D CastLaser()
        {
            return Physics2D.Raycast(firePoint.position, aimDirection, laserRange, obstacleLayers | TargetLayers);
        }

        private static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

        private void ShowBeam(Vector2 start, Vector2 end, Color color)
        {
            beamStart = start;
            beamEnd = end;
            beamDisplayColor = color;
            beamVisibleUntil = Time.time + beamDuration;
        }

        private void ChangeState(State newState)
        {
            state = newState;
            stateTimer = 0f;
        }

        protected override void OnHurt(DamageInfo info)
        {
            // 瞄準或充能中被打會中斷射擊
            if (state == State.Aim || state == State.Charge || state == State.Fire)
                ChangeState(State.Recover);
        }

        protected override void Update()
        {
            bool isCharging = state == State.Charge && !IsStunned;
            VisualScale = isCharging ? Vector2.Lerp(Vector2.one, chargeSwellScale, Mathf.Clamp01(stateTimer / chargeDuration)) : Vector2.one;
            UpdateLaserLine();
            base.Update();
        }

        private void UpdateLaserLine()
        {
            if (Time.time < beamVisibleUntil)
            {
                DrawLine(beamStart, beamEnd, beamDisplayColor, beamWidth);
                return;
            }

            bool isAiming = (state == State.Aim || state == State.Charge) && !IsStunned;
            laserLine.enabled = isAiming;
            if (!isAiming)
                return;

            RaycastHit2D hit = CastLaser();
            Vector2 start = firePoint.position;
            Vector2 end = hit ? hit.point : start + aimDirection * laserRange;

            if (IsAimLocked)
                DrawLine(start, end, telegraph.cueColor, cueWidth);
            else if (state == State.Aim || telegraph.IsBlinkHighlighted(stateTimer, chargeDuration))
                DrawLine(start, end, aimColor, aimWidth);
            else
                DrawLine(start, end, new Color(aimColor.r, aimColor.g, aimColor.b, 0.2f), aimWidth);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, float width)
        {
            laserLine.enabled = true;
            laserLine.SetPosition(0, start);
            laserLine.SetPosition(1, end);
            laserLine.startColor = color;
            laserLine.endColor = color;
            laserLine.startWidth = width;
            laserLine.endWidth = width;
        }

        protected override Color GetBodyColor()
        {
            if (state == State.Charge && !IsStunned)
                return telegraph.Evaluate(BaseColor, stateTimer, chargeDuration);

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
