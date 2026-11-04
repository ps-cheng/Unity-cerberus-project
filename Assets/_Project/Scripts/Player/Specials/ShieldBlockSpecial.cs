using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 第一顆頭的特殊動作：舉起左手的盾，只對來自正面的攻擊有效。數值在 ShieldBlockData。
    /// - 完美格檔（Parry）：按下後極短時間內被打中，完全化解並讓敵人陷入硬直。
    /// - 一般格檔（Guard）：按住期間被打中，傷害依比例減少。
    /// - 揮空懲罰：完美格檔的判定時間結束卻沒擋到攻擊，一段時間內再按也不會開啟判定（仍可一般格檔）。
    /// 最佳打法是抓準時機按下後繼續按住：抓準了無傷，抓不準至少還有一般格檔。
    /// </summary>
    public class ShieldBlockSpecial : SpecialAbility
    {
        private readonly ShieldBlockData data;

        private int facing = 1;
        private float parryWindowStartTime = float.NegativeInfinity;
        private float parryLockedUntil = float.NegativeInfinity;

        public ShieldBlockSpecial(ShieldBlockData data, Transform owner) : base(owner)
        {
            this.data = data;
        }

        public override string DisplayName => data.displayName;
        public override bool AllowsTurning => true;
        public bool IsParryWindowActive => Time.time - parryWindowStartTime <= data.parryWindow;
        public bool IsParryLocked => Time.time < parryLockedUntil;

        public override string StatusText
        {
            get
            {
                if (IsParryWindowActive)
                    return "Parry Window";

                if (!IsActive)
                    return string.Empty;

                return IsParryLocked ? "Guard, Parry Locked" : "Guard";
            }
        }

        public override void Tick(bool pressed, bool held, int currentFacing)
        {
            facing = currentFacing;

            if (pressed && !IsParryLocked)
            {
                parryWindowStartTime = Time.time;
                // 先假設會揮空，從判定結束起算鎖定；成功格檔時再解除
                parryLockedUntil = Time.time + data.parryWindow + data.whiffLockoutDuration;
            }

            IsActive = held;
        }

        public override void Cancel()
        {
            IsActive = false;
            parryWindowStartTime = float.NegativeInfinity;
        }

        public override bool TryGetZone(out Vector2 center, out Vector2 size)
        {
            center = (Vector2)owner.position + new Vector2(data.zoneOffset.x * facing, data.zoneOffset.y);
            size = data.zoneSize;
            return true;
        }

        public override DamageResult ResolveIncomingHit(ref DamageInfo info)
        {
            // 攻擊來源在面向那一側才擋得住，背後被打一樣會受傷
            bool isFromFront = Mathf.Sign(info.sourcePosition.x - owner.position.x) == facing;
            if (!isFromFront)
                return DamageResult.Hit;

            if (IsParryWindowActive)
            {
                // 成功就不算揮空，可以馬上接下一次完美格檔
                parryWindowStartTime = float.NegativeInfinity;
                parryLockedUntil = float.NegativeInfinity;
                info.amount = 0;
                return DamageResult.Parried;
            }

            if (IsActive)
            {
                info.amount = Mathf.CeilToInt(info.amount * (1f - data.guardDamageReduction));
                return DamageResult.Blocked;
            }

            return DamageResult.Hit;
        }
    }
}
