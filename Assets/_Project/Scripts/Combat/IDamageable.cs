using UnityEngine;

namespace Cerberus.Combat
{
    /// <summary>
    /// 任何能被攻擊命中的物件（敵人、可破壞物）都實作這個介面。
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(int amount, Vector2 hitDirection);
    }
}
