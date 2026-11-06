using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 特殊動作的共同基底。三頭犬的每顆頭各有一種特殊動作（格檔、瞬移……），
    /// PlayerCombat 只透過這個類別操作，不需要知道具體是哪一種。
    /// 不是元件：由對應的 SpecialAbilityData 資產產生，數值讀資產，計時等狀態存在這個物件上。
    ///
    /// 冷卻由這裡統一處理，子類別不用自己寫：
    /// - 特殊動作結束（IsActive 由 true 變 false，或被 Cancel）時開始冷卻。
    /// - 冷卻中按鍵完全不會傳給子類別，不能啟動。
    /// </summary>
    public abstract class SpecialAbility
    {
        protected readonly Transform owner;
        private readonly SpecialAbilityData baseData;

        // 記錄冷卻結束的時間點而不是每幀遞減，不管這顆頭有沒有被選中都會照常倒數
        private float cooldownEndTime = float.NegativeInfinity;
        private bool isCooldownWaived;

        protected SpecialAbility(SpecialAbilityData data, Transform owner)
        {
            baseData = data;
            this.owner = owner;
        }

        public string DisplayName => baseData.displayName;
        public bool IsActive { get; protected set; }
        public virtual bool LocksMovement => true;
        public virtual bool AllowsTurning => false;

        public bool IsOnCooldown => Time.time < cooldownEndTime;
        public float CooldownRemaining => Mathf.Max(0f, cooldownEndTime - Time.time);

        /// <summary>給 HUD 顯示的附加狀態，例如「Parry Window」</summary>
        public virtual string StatusText => string.Empty;

        /// <summary>
        /// 每幀由 PlayerCombat 呼叫。同時傳入「剛按下」與「按住中」，
        /// 按一下觸發型與按住持續型的特殊動作都能用同一個介面實作。
        /// </summary>
        public void Tick(bool pressed, bool held, int facing)
        {
            if (!IsActive && IsOnCooldown)
            {
                pressed = false;
                held = false;
            }

            bool wasActive = IsActive;
            OnTick(pressed, held, facing);

            if (!wasActive && IsActive)
                isCooldownWaived = false;
            else if (wasActive && !IsActive)
                BeginCooldown();
        }

        public void Cancel()
        {
            bool wasActive = IsActive;
            OnCancel();
            IsActive = false;

            if (wasActive)
                BeginCooldown();
        }

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

        /// <summary>冷卻已確認可以使用時才會被呼叫。依按鍵狀態設定 IsActive。</summary>
        protected abstract void OnTick(bool pressed, bool held, int facing);

        protected virtual void OnCancel() { }

        /// <summary>免除這次使用的冷卻（例如完美格檔成功的獎勵）。已經開始的冷卻會清掉，還沒開始的不會開始。</summary>
        protected void WaiveCooldown()
        {
            isCooldownWaived = true;
            cooldownEndTime = float.NegativeInfinity;
        }

        private void BeginCooldown()
        {
            if (!isCooldownWaived)
                cooldownEndTime = Time.time + baseData.cooldownDuration;

            isCooldownWaived = false;
        }
    }
}
