namespace Cerberus.Combat
{
    public enum CombatAction
    {
        None,
        LightAttack,
        HeavyAttack,
        Block
    }

    /// <summary>
    /// 招式的三個階段：前搖 → 判定 → 硬直（收招）。
    /// 格檔沒有分段，整段都視為 Active。
    /// </summary>
    public enum ActionPhase
    {
        None,
        Startup,
        Active,
        Recovery
    }
}
