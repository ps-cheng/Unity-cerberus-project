using UnityEngine;

namespace Cerberus.Combat
{
    /// <summary>
    /// 能提供「目前招式範圍」的物件（玩家、敵人），讓同一個範圍視覺化元件可以共用。
    /// </summary>
    public interface ICombatZoneSource
    {
        ActionPhase CurrentPhase { get; }
        bool IsSpecialActive { get; }
        bool TryGetCurrentZone(out Vector2 center, out Vector2 size);
    }
}
