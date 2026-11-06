using System;
using UnityEngine;

namespace Cerberus.Combat
{
    /// <summary>
    /// 直線飛行的子彈。每幀用圓形掃描檢查這一段路徑，速度再快也不會穿過目標。
    /// 碰到目標造成傷害、碰到地形或飛超過射程就消失。不需要 Collider 和 Rigidbody。
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float hitRadius = 0.1f;
        [Tooltip("會擋下子彈的地形")]
        [SerializeField] private LayerMask obstacleLayers;

        private ProjectileAttackData attack;
        private int damage;
        private Vector2 direction;
        private float remainingDistance;
        private LayerMask targetLayers;
        private Action<Collider2D> onTargetHit;

        /// <param name="damageAmount">實際傷害，已套用發射者身上的加成（例如變身）</param>
        /// <param name="onTargetHit">一般命中目標（不含無敵、被格檔）時呼叫，用來觸發命中回饋</param>
        public void Launch(ProjectileAttackData attackData, Vector2 flyDirection, int damageAmount, LayerMask targets, Action<Collider2D> onTargetHit)
        {
            attack = attackData;
            damage = damageAmount;
            direction = flyDirection.normalized;
            remainingDistance = attackData.maxRange;
            targetLayers = targets;
            this.onTargetHit = onTargetHit;
            transform.right = direction;
        }

        private void Update()
        {
            // hit-stop 期間 deltaTime 為 0，子彈跟著停住
            float step = Mathf.Min(attack.projectileSpeed * Time.deltaTime, remainingDistance);
            if (step <= 0f && remainingDistance > 0f)
                return;

            RaycastHit2D hit = Physics2D.CircleCast(transform.position, hitRadius, direction, step, targetLayers | obstacleLayers);
            if (hit.collider != null)
            {
                HandleHit(hit.collider);
                return;
            }

            transform.position += (Vector3)(direction * step);
            remainingDistance -= step;

            if (remainingDistance <= 0f)
                Destroy(gameObject);
        }

        private void HandleHit(Collider2D target)
        {
            bool isTarget = (targetLayers.value & (1 << target.gameObject.layer)) != 0;
            IDamageable damageable = isTarget ? target.GetComponentInParent<IDamageable>() : null;

            if (damageable != null)
            {
                var info = new DamageInfo(damage, transform.position, attack.knockbackForce);
                if (damageable.TakeDamage(info) == DamageResult.Hit)
                    onTargetHit?.Invoke(target);
            }

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, hitRadius);
        }
    }
}
