using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 第一顆頭的特殊動作：舉起左手的盾，只對來自正面的攻擊有效。數值在 ShieldBlockData。
    /// - 完美格檔（Parry）：按下後極短時間內被打中，完全化解並讓敵人陷入硬直。
    /// - 一般格檔（Guard）：按住期間被打中，傷害依比例減少。
    /// - 冷卻（共用結構）：放開盾牌後進入冷卻，冷卻中完全不能格檔；完美格檔成功則這次不進冷卻。
    /// 最佳打法是抓準時機按下後繼續按住：抓準了無傷，抓不準至少還有一般格檔。不能靠連打碰運氣。
    /// </summary>
    public class ShieldBlockSpecial : SpecialAbility
    {
        private readonly ShieldBlockData data;

        private int facing = 1;
        private float parryWindowStartTime = float.NegativeInfinity;

        public ShieldBlockSpecial(ShieldBlockData data, Transform owner) : base(data, owner)
        {
            this.data = data;
        }

        public override bool AllowsTurning => true;
        public bool IsParryWindowActive => Time.time - parryWindowStartTime <= data.parryWindow;

        public override string StatusText
        {
            get
            {
                if (IsParryWindowActive)
                    return "Parry Window";

                return IsActive ? "Guard" : string.Empty;
            }
        }

        protected override void OnTick(bool pressed, bool held, int currentFacing)
        {
            facing = currentFacing;

            if (pressed)
                parryWindowStartTime = Time.time;

            // 只有按下的那一刻能舉盾；冷卻中一直按住，冷卻結束後也不會自己舉起來
            IsActive = held && (IsActive || pressed);
        }

        protected override void OnCancel()
        {
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
                // 完美格檔成功的獎勵：這次不進冷卻，可以馬上接下一次
                parryWindowStartTime = float.NegativeInfinity;
                WaiveCooldown();
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
