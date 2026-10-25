namespace Cerberus.Combat
{
    /// <summary>
    /// 任何能被攻擊命中的物件（玩家、敵人、子彈）都實作這個介面。
    /// 是否扣血、是否格檔、無敵時間等規則由受擊方自己決定。
    /// </summary>
    public interface IDamageable
    {
        DamageResult TakeDamage(DamageInfo info);
    }
}
