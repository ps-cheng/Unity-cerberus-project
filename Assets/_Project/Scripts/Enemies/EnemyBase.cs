using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Enemies
{
    /// <summary>
    /// 所有敵人共用的部分：偵測玩家、受擊（扣血、閃白、短暫硬直）、死亡、面向翻轉。
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
        [SerializeField] private float hurtFlashDuration = 0.1f;

        [Header("Visual")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer bodyRenderer;

        public Transform Target { get; private set; }
        public bool HasTarget => Target != null;
        public float DetectionRadius => detectionRadius;
        public Health Health { get; private set; }
        public abstract string StateName { get; }

        protected Rigidbody2D Body { get; private set; }
        protected int FacingDirection { get; private set; } = 1;
        protected bool IsStunned => Time.time < stunnedUntil;
        protected Color BaseColor { get; private set; }

        private float stunnedUntil;
        private float flashUntil;

        protected virtual void Awake()
        {
            Health = GetComponent<Health>();
            Body = GetComponent<Rigidbody2D>();
            BaseColor = bodyRenderer.color;
            Health.Died += HandleDied;
        }

        protected virtual void Update()
        {
            UpdateTarget();
            bodyRenderer.color = Time.time < flashUntil ? Color.white : GetBodyColor();
        }

        public DamageResult TakeDamage(DamageInfo info)
        {
            if (Health.IsDead)
                return DamageResult.Ignored;

            Health.ApplyDamage(info.amount);
            flashUntil = Time.time + hurtFlashDuration;
            stunnedUntil = Time.time + hurtStunDuration;
            OnHurt(info);
            return DamageResult.Hit;
        }

        /// <summary>受擊後的個別反應（擊退、打斷招式）</summary>
        protected virtual void OnHurt(DamageInfo info) { }

        /// <summary>依目前狀態決定身體顏色，用來預告攻擊</summary>
        protected virtual Color GetBodyColor() => BaseColor;

        protected virtual void HandleDied()
        {
            Destroy(gameObject);
        }

        protected void SetFacing(float direction)
        {
            if (Mathf.Approximately(direction, 0f))
                return;

            FacingDirection = direction > 0f ? 1 : -1;
            visualRoot.localScale = new Vector3(FacingDirection, 1f, 1f);
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
