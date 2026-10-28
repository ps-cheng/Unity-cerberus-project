using System.Collections.Generic;
using UnityEngine;

namespace Cerberus.Combat
{
    /// <summary>
    /// 直線飛行的子彈。碰到目標、撞到地形、超過存活時間就消失。
    /// 本身也是 IDamageable，所以能被玩家的攻擊打掉。
    /// 被完美格檔（Parried）時會反彈：改打敵人並追向原本的發射者。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(ContactDamageDealer))]
    public class Projectile : MonoBehaviour, IDamageable
    {
        [SerializeField] private float speed = 6f;
        [SerializeField] private float lifetime = 4f;
        [SerializeField] private LayerMask obstacleLayers;

        [Header("Reflect")]
        [Tooltip("被反彈後改為攻擊這些 Layer")]
        [SerializeField] private LayerMask reflectedTargetLayers;
        [Tooltip("被反彈後切換到這個 Layer，避免又被玩家的攻擊打掉")]
        [SerializeField] private string reflectedLayerName = "PlayerProjectile";
        [SerializeField] private float reflectedSpeedMultiplier = 1.5f;
        [Tooltip("完美格檔的獎勵：反彈後的傷害倍率")]
        [SerializeField] private float reflectedDamageMultiplier = 2f;
        [Tooltip("反彈後每秒最多轉向幾度來追蹤發射者，避免對方移動後打不中")]
        [SerializeField] private float reflectedHomingTurnRate = 360f;
        [SerializeField] private Color reflectedColor = new Color(0.4f, 0.9f, 1f);

        public bool IsReflected { get; private set; }

        private Rigidbody2D body;
        private Collider2D ownCollider;
        private ContactDamageDealer damageDealer;
        private SpriteRenderer coreRenderer;
        private Transform owner;
        private float despawnTime;
        private ContactFilter2D obstacleFilter;
        private readonly List<Collider2D> overlapResults = new List<Collider2D>();

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ownCollider = GetComponent<Collider2D>();
            damageDealer = GetComponent<ContactDamageDealer>();
            coreRenderer = GetComponentInChildren<SpriteRenderer>();
            obstacleFilter = new ContactFilter2D();
            obstacleFilter.SetLayerMask(obstacleLayers);

            damageDealer.HitResolved += HandleHitResolved;
        }

        /// <param name="shooter">發射者，被反彈時會追向它</param>
        public void Launch(Vector2 direction, Transform shooter)
        {
            owner = shooter;
            SetVelocity(direction.normalized * speed);
            despawnTime = Time.time + lifetime;
        }

        private void HandleHitResolved(DamageResult result)
        {
            // 完美格檔就反彈；一般格檔、命中、或反彈後打中敵人都直接消失。玩家無敵時（Ignored）則穿過
            if (result == DamageResult.Parried && !IsReflected)
            {
                Reflect();
                return;
            }

            if (IsReflected && result == DamageResult.Hit)
                CombatEvents.RaiseReflectedHit();

            Destroy(gameObject);
        }

        private void Reflect()
        {
            IsReflected = true;
            gameObject.layer = LayerMask.NameToLayer(reflectedLayerName);
            damageDealer.Retarget(reflectedTargetLayers, reflectedDamageMultiplier);
            coreRenderer.color = reflectedColor;

            // 從玩家位置朝發射者射回去；發射者已死亡就原路反轉
            Vector2 direction = owner != null ? (Vector2)(owner.position - transform.position) : -body.linearVelocity;
            SetVelocity(direction.normalized * speed * reflectedSpeedMultiplier);
            despawnTime = Time.time + lifetime;
        }

        private void FixedUpdate()
        {
            if (Time.time >= despawnTime || ownCollider.Overlap(obstacleFilter, overlapResults) > 0)
            {
                Destroy(gameObject);
                return;
            }

            if (IsReflected && owner != null)
                SteerTowards(owner.position);
        }

        private void SteerTowards(Vector2 targetPosition)
        {
            Vector2 velocity = body.linearVelocity;
            Vector2 desired = (targetPosition - body.position).normalized * velocity.magnitude;
            float maxRadians = reflectedHomingTurnRate * Mathf.Deg2Rad * Time.fixedDeltaTime;
            SetVelocity(Vector3.RotateTowards(velocity, desired, maxRadians, 0f));
        }

        private void SetVelocity(Vector2 velocity)
        {
            body.linearVelocity = velocity;
            transform.right = velocity;
        }

        public DamageResult TakeDamage(DamageInfo info)
        {
            // 反彈後屬於玩家方，不會被打掉
            if (IsReflected)
                return DamageResult.Ignored;

            Destroy(gameObject);
            return DamageResult.Hit;
        }
    }
}
