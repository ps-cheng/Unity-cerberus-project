using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 特殊動作的共同基底。三頭犬的每顆頭各有一種特殊動作（格檔、衝刺……），
    /// PlayerCombat 只透過這個類別操作，不需要知道具體是哪一種。
    /// </summary>
    public abstract class SpecialAbility : MonoBehaviour
    {
        public abstract string DisplayName { get; }
        public bool IsActive { get; protected set; }
        public virtual bool LocksMovement => true;
        public virtual bool AllowsTurning => false;

        /// <summary>給 HUD 顯示的附加狀態，例如「Parry Window」</summary>
        public virtual string StatusText => string.Empty;

        /// <summary>
        /// 每幀由 PlayerCombat 呼叫。同時傳入「剛按下」與「按住中」，
        /// 按一下觸發型與按住持續型的特殊動作都能用同一個介面實作。
        /// </summary>
        public abstract void Tick(bool pressed, bool held, int facing);

        public abstract void Cancel();

        /// <summary>作用範圍（世界座標）。沒有範圍的特殊動作回傳 false。</summary>
        public virtual bool TryGetZone(out Vector2 center, out Vector2 size)
        {
            center = default;
            size = default;
            return false;
        }

        /// <summary>
        /// 受到攻擊時詢問特殊動作如何處理這一下。可以修改 info.amount 來減傷。
        /// 回傳 Hit 代表照常受傷；Blocked 代表減傷後的傷害；Parried 代表完全化解。
        /// </summary>
        public virtual DamageResult ResolveIncomingHit(ref DamageInfo info) => DamageResult.Hit;
    }
}
