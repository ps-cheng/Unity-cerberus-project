namespace Cerberus.Combat
{
    public enum CombatAction
    {
        None,
        LightAttack,
        HeavyAttack,
        Special
    }

    /// <summary>
    /// 招式的三個階段：前搖 → 判定 → 硬直（收招）。
    /// 特殊動作沒有分段，整段都視為 Active。
    /// </summary>
    public enum ActionPhase
    {
        None,
        Startup,
        Active,
        Recovery
    }

    /// <summary>
    /// 一次攻擊的結果，攻擊方可以據此做出反應（例如被格檔時彈開）。
    /// </summary>
    public enum DamageResult
    {
        Ignored,
        Blocked,
        Hit
    }
}
