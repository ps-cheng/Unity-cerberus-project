using System;
using System.Collections;
using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 玩家的受擊規則：無敵時間、格檔判定、擊退與硬直、死亡後重生。
    /// 血量本身存在 Health，這裡只決定「這一下要不要扣血」。
    /// </summary>
    [RequireComponent(typeof(Health), typeof(PlayerMovement), typeof(PlayerCombat))]
    public class PlayerDamageReceiver : MonoBehaviour, IDamageable
    {
        [Header("Hit")]
        [Tooltip("受傷後的無敵時間，期間角色會閃爍")]
        [SerializeField] private float invincibilityDuration = 1f;
        [Tooltip("受傷後無法操作的時間")]
        [SerializeField] private float hitStunDuration = 0.3f;

        [Header("Block")]
        [SerializeField] private float blockPushbackSpeed = 3f;
        [Tooltip("格檔成功後的短暫無敵，避免持續接觸時每一幀都觸發格檔")]
        [SerializeField] private float blockGraceDuration = 0.25f;

        [Header("Respawn")]
        [SerializeField] private float respawnDelay = 1f;

        public bool IsInvincible => Time.time < invincibleUntil;
        public bool IsStunned => isDead || Time.time < stunnedUntil;
        public bool IsDead => isDead;

        public event Action<int> Hurt;
        public event Action Blocked;
        public event Action Died;
        public event Action Respawned;

        private Health health;
        private PlayerMovement movement;
        private PlayerCombat combat;
        private Vector2 spawnPosition;
        private float invincibleUntil;
        private float stunnedUntil;
        private bool isDead;

        private void Awake()
        {
            health = GetComponent<Health>();
            movement = GetComponent<PlayerMovement>();
            combat = GetComponent<PlayerCombat>();
            spawnPosition = transform.position;
            health.Died += HandleDied;
        }

        public DamageResult TakeDamage(DamageInfo info)
        {
            if (isDead || IsInvincible)
                return DamageResult.Ignored;

            // 往遠離攻擊來源的方向擊退
            float awayDirection = Mathf.Sign(transform.position.x - info.sourcePosition.x);

            if (combat.TryNegateHit(info))
            {
                movement.ApplyKnockback(new Vector2(awayDirection * blockPushbackSpeed, 0f), 0.1f);
                invincibleUntil = Time.time + blockGraceDuration;
                Blocked?.Invoke();
                return DamageResult.Blocked;
            }

            combat.CancelAction();
            movement.ApplyKnockback(new Vector2(awayDirection, 0.6f) * info.knockbackForce, hitStunDuration);
            invincibleUntil = Time.time + invincibilityDuration;
            stunnedUntil = Time.time + hitStunDuration;
            health.ApplyDamage(info.amount);
            Hurt?.Invoke(info.amount);
            return DamageResult.Hit;
        }

        private void HandleDied()
        {
            isDead = true;
            Died?.Invoke();
            StartCoroutine(RespawnAfterDelay());
        }

        // 原型階段的簡易重生：回到出生點、補滿血
        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);

            movement.Teleport(spawnPosition);
            health.ResetHealth();
            isDead = false;
            invincibleUntil = Time.time + invincibilityDuration;
            Respawned?.Invoke();
        }
    }
}
