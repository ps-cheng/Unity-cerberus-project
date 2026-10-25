using System.Collections.Generic;
using UnityEngine;

namespace Cerberus.Combat
{
    /// <summary>
    /// 直線飛行的子彈。碰到目標、撞到地形、超過存活時間就消失。
    /// 本身也是 IDamageable，所以能被玩家的攻擊打掉。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(ContactDamageDealer))]
    public class Projectile : MonoBehaviour, IDamageable
    {
        [SerializeField] private float speed = 6f;
        [SerializeField] private float lifetime = 4f;
        [SerializeField] private LayerMask obstacleLayers;

        private Rigidbody2D body;
        private Collider2D ownCollider;
        private ContactFilter2D obstacleFilter;
        private readonly List<Collider2D> overlapResults = new List<Collider2D>();

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ownCollider = GetComponent<Collider2D>();
            obstacleFilter = new ContactFilter2D();
            obstacleFilter.SetLayerMask(obstacleLayers);

            // 打中玩家或被格檔都會消失；玩家無敵時則直接穿過
            GetComponent<ContactDamageDealer>().HitResolved += _ => Destroy(gameObject);
        }

        public void Launch(Vector2 direction)
        {
            direction.Normalize();
            body.linearVelocity = direction * speed;
            transform.right = direction;
            Destroy(gameObject, lifetime);
        }

        private void FixedUpdate()
        {
            if (ownCollider.Overlap(obstacleFilter, overlapResults) > 0)
                Destroy(gameObject);
        }

        public DamageResult TakeDamage(DamageInfo info)
        {
            Destroy(gameObject);
            return DamageResult.Hit;
        }
    }
}
