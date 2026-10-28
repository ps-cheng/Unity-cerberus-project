using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Enemies
{
    /// <summary>
    /// 所有敵人共用的部分：偵測玩家、受擊（扣血、短暫硬直）、死亡（停留閃爍後刪除）、面向翻轉。
    /// 各種敵人只需要實作自己的行為（巡邏、追擊、射擊……）。
    /// </summary>
    [RequireComponent(typeof(Health), typeof(Rigidbody2D))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Detection")]
        [SerializeField] private float detectionRadius = 5f;
        [Tooltip("目標離開「偵測半徑 × 此倍數」才放棄追擊，避免在邊界反覆切換")]
        [SerializeField] private float loseTargetMultiplier = 1.4f;
        [SerializeField] private LayerMask targetLayers;

        [Header("Hit Reaction")]
        [SerializeField] private float hurtStunDuration = 0.25f;
        [Tooltip("攻擊被完美格檔後的硬直，也就是玩家反擊的機會")]
        [SerializeField] private float parriedStaggerDuration = 1.2f;
        [SerializeField] private Color staggerColor = new Color(0.55f, 0.75f, 1f);

        [Header("Death")]
        [Tooltip("死亡後留在場上閃爍的時間，讓玩家看清楚最後一擊，之後才刪除")]
        [SerializeField] private float deathLingerDuration = 0.45f;
        [SerializeField] private float deathBlinkFrequency = 12f;

        [Header("Visual")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer bodyRenderer;

        public Transform Target { get; private set; }
        public bool HasTarget => Target != null;
        public float DetectionRadius => detectionRadius;
        public Health Health { get; private set; }

        /// <summary>給除錯標籤顯示；死亡、硬直這些共通狀態優先於子類別自己的行為狀態</summary>
        public string StateName
        {
            get
            {
                if (IsDying)
                    return "Dead";

                if (IsStaggered)
                    return "Staggered";

                return IsStunned ? "Hurt" : BehaviorStateName;
            }
        }

        protected abstract string BehaviorStateName { get; }

        protected Rigidbody2D Body { get; private set; }
        protected int FacingDirection { get; private set; } = 1;
        // 死亡停留期間也視為硬直，子類別的行為邏輯會因此全部停下
        protected bool IsStunned => IsDying || Time.time < stunnedUntil;
        protected bool IsDying { get; private set; }
        protected bool IsStaggered => Time.time < staggeredUntil;
        protected Color BaseColor { get; private set; }
        protected LayerMask TargetLayers => targetLayers;
        /// <summary>外觀的縮放（不含面向翻轉），子類別可用來做蓄力壓縮等簡易動畫</summary>
        protected Vector2 VisualScale { get; set; } = Vector2.one;

        private float stunnedUntil;
        private float staggeredUntil;
        private float diedAt;

        protected virtual void Awake()
        {
            Health = GetComponent<Health>();
            Body = GetComponent<Rigidbody2D>();
            BaseColor = bodyRenderer.color;
            Health.Died += HandleDied;
        }

        protected virtual void Update()
        {
            if (IsDying)
            {
                UpdateDeathBlink();
                return;
            }

            UpdateTarget();
            bodyRenderer.color = GetCurrentColor();
            visualRoot.localScale = new Vector3(FacingDirection * VisualScale.x, VisualScale.y, 1f);
        }

        public DamageResult TakeDamage(DamageInfo info)
        {
            if (Health.IsDead)
                return DamageResult.Ignored;

            Health.ApplyDamage(info.amount);
            // 取較長者：被格檔硬直中挨打，不會因此縮短硬直
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + hurtStunDuration);
            OnHurt(info);
            return DamageResult.Hit;
        }

        /// <summary>受擊後的個別反應（擊退、打斷招式）</summary>
        protected virtual void OnHurt(DamageInfo info) { }

        /// <summary>自己的攻擊被玩家完美格檔時呼叫，進入較長的硬直</summary>
        protected void ApplyParriedStagger()
        {
            stunnedUntil = staggeredUntil = Time.time + parriedStaggerDuration;
        }

        private Color GetCurrentColor() => IsStaggered ? staggerColor : GetBodyColor();

        /// <summary>依目前狀態決定身體顏色，用來預告攻擊</summary>
        protected virtual Color GetBodyColor() => BaseColor;

        /// <summary>死亡：立刻失去判定與行為，原地閃爍一下再刪除</summary>
        private void HandleDied()
        {
            IsDying = true;
            diedAt = Time.time;
            Body.linearVelocity = Vector2.zero;
            Body.simulated = false;

            foreach (Collider2D ownCollider in GetComponentsInChildren<Collider2D>())
                ownCollider.enabled = false;

            foreach (ContactDamageDealer dealer in GetComponentsInChildren<ContactDamageDealer>())
                dealer.enabled = false;

            Destroy(gameObject, deathLingerDuration);
        }

        private void UpdateDeathBlink()
        {
            bool isVisible = Mathf.Repeat((Time.time - diedAt) * deathBlinkFrequency, 1f) < 0.5f;
            visualRoot.gameObject.SetActive(isVisible);
        }

        protected void SetFacing(float direction)
        {
            if (Mathf.Approximately(direction, 0f))
                return;

            FacingDirection = direction > 0f ? 1 : -1;
        }

        protected float GetDirectionToTarget() => Mathf.Sign(Target.position.x - transform.position.x);

        private void UpdateTarget()
        {
            if (HasTarget)
            {
                float distance = Vector2.Distance(transform.position, Target.position);
                if (distance > detectionRadius * loseTargetMultiplier)
                    Target = null;

                return;
            }

            Collider2D found = Physics2D.OverlapCircle(transform.position, detectionRadius, targetLayers);
            if (found != null)
                Target = found.transform;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
