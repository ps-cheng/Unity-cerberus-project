using System;

namespace Cerberus.Combat
{
    /// <summary>
    /// 跨物件的戰鬥事件。用在「發生的地方」跟「玩家」沒有直接關聯的情況，
    /// 例如反射的雷射或子彈是由敵人、子彈自己結算，但命中回饋要算在玩家身上。
    /// </summary>
    public static class CombatEvents
    {
        /// <summary>玩家完美格檔反射回去的攻擊命中了敵人</summary>
        public static event Action ReflectedHitLanded;

        public static void RaiseReflectedHit() => ReflectedHitLanded?.Invoke();
    }
}
