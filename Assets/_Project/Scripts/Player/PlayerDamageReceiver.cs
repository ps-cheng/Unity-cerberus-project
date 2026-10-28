using System;
using System.Collections;
using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 玩家的受擊規則：無敵時間、格檔判定、擊退與硬直、死亡後重生。
    /// 血量本身存在 Health，這裡只決定「這一下要扣多少血」。
    /// </summary>
    [RequireComponent(typeof(Health), typeof(PlayerMovement), typeof(PlayerCombat))]
    public class PlayerDamageReceiver : MonoBehaviour, IDamageable
    {
        [Header("Hit")]
        [Tooltip("受傷後的無敵時間，期間角色會閃爍")]
        [SerializeField] private float invincibilityDuration = 1f;
        [Tooltip("受傷後無法操作的時間")]
        [SerializeField] private float hitStunDuration = 0.3f;
        [Tooltip("受傷擊退的水平速度 = 攻擊的 knockbackForce × 此倍率")]
        [SerializeField] private float hurtKnockbackHorizontalScale = 0.54f;
        [Tooltip("受傷擊退往上彈的速度 = 攻擊的 knockbackForce × 此倍率")]
        [SerializeField] private float hurtKnockbackLiftScale = 0.42f;

        [Header("Guard / Parry")]
        [SerializeField] private float guardPushbackSpeed = 3f;
        [Tooltip("格檔成功後的短暫無敵，避免持續接觸時每一幀都觸發格檔")]
        [SerializeField] private float guardGraceDuration = 0.25f;

        [Header("Respawn")]
        [SerializeField] private float respawnDelay = 1f;

        public bool IsInvincible => Time.time < invincibleUntil;
        public bool IsStunned => isDead || Time.time < stunnedUntil;
        public bool IsDead => isDead;
        /// <summary>只有真的受傷或重生後才閃爍，格檔後的短暫無敵不閃</summary>
        public bool IsBlinking => Time.time < blinkUntil;

        public event Action<int> Hurt;
        /// <summary>一般格檔成功，參數是減傷後仍承受的傷害</summary>
        public event Action<int> Guarded;
        public event Action Parried;
        public event Action Died;
        public event Action Respawned;

        private Health health;
        private PlayerMovement movement;
        private PlayerCombat combat;
        private Vector2 spawnPosition;
        private float invincibleUntil;
        private float stunnedUntil;
        private float blinkUntil;
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

            DamageResult result = combat.ResolveIncomingHit(ref info);

            switch (result)
            {
                case DamageResult.Parried:
                    invincibleUntil = Time.time + guardGraceDuration;
                    Parried?.Invoke();
                    break;

                case DamageResult.Blocked:
                    movement.ApplyKnockback(new Vector2(GetAwayDirection(info) * guardPushbackSpeed, 0f), 0.1f);
                    invincibleUntil = Time.time + guardGraceDuration;
                    health.ApplyDamage(info.amount);
                    Guarded?.Invoke(info.amount);
                    break;

                default:
                    ApplyHit(info);
                    break;
            }

            return result;
        }

        private void ApplyHit(DamageInfo info)
        {
            combat.CancelAction();
            Vector2 knockback = new Vector2(GetAwayDirection(info) * hurtKnockbackHorizontalScale, hurtKnockbackLiftScale) * info.knockbackForce;
            movement.ApplyKnockback(knockback, hitStunDuration);
            invincibleUntil = blinkUntil = Time.time + invincibilityDuration;
            stunnedUntil = Time.time + hitStunDuration;
            health.ApplyDamage(info.amount);
            Hurt?.Invoke(info.amount);
        }

        private float GetAwayDirection(DamageInfo info) => Mathf.Sign(transform.position.x - info.sourcePosition.x);

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
            invincibleUntil = blinkUntil = Time.time + invincibilityDuration;
            Respawned?.Invoke();
        }
    }
}
