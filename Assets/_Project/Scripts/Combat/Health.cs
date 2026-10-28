using System;
using UnityEngine;

namespace Cerberus.Combat
{
    /// <summary>
    /// 純粹的血量資料。受擊規則（無敵、格檔）不寫在這裡，讓玩家與敵人可以共用。
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 100;

        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        public event Action Died;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void ApplyDamage(int amount)
        {
            if (IsDead || amount <= 0)
                return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

            if (IsDead)
                Died?.Invoke();
        }

        public void ResetHealth() => CurrentHealth = maxHealth;
    }
}
