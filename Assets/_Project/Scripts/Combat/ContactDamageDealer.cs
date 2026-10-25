using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cerberus.Combat
{
    /// <summary>
    /// 碰到目標就造成傷害（敵人身體、子彈）。
    /// 用主動的 Overlap 查詢而不是碰撞事件，所以雙方不需要產生物理碰撞，玩家可以穿過敵人。
    /// 持續接觸時每個物理幀都會嘗試造成傷害，靠受擊方的無敵時間避免連續扣血。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ContactDamageDealer : MonoBehaviour
    {
        [SerializeField, Min(0)] private int damage = 10;
        [SerializeField] private float knockbackForce = 8f;
        [SerializeField] private LayerMask targetLayers;

        /// <summary>造成傷害或被格檔時觸發；目標無敵（Ignored）時不觸發</summary>
        public event Action<DamageResult> HitResolved;

        private Collider2D ownCollider;
        private ContactFilter2D targetFilter;
        private readonly List<Collider2D> overlapResults = new List<Collider2D>();

        private void Awake()
        {
            ownCollider = GetComponent<Collider2D>();
            targetFilter = new ContactFilter2D();
            targetFilter.SetLayerMask(targetLayers);
            targetFilter.useTriggers = true;
        }

        private void FixedUpdate()
        {
            ownCollider.Overlap(targetFilter, overlapResults);

            foreach (Collider2D target in overlapResults)
            {
                IDamageable damageable = target.GetComponentInParent<IDamageable>();
                if (damageable == null)
                    continue;

                DamageResult result = damageable.TakeDamage(new DamageInfo(damage, transform.position, knockbackForce));

                if (result != DamageResult.Ignored)
                    HitResolved?.Invoke(result);
            }
        }
    }
}
