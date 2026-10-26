using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 第一顆頭的特殊動作：舉起左手的盾，只對來自正面的攻擊有效。
    /// - 完美格檔（Parry）：按下後極短時間內被打中，完全化解並讓敵人陷入硬直。
    /// - 一般格檔（Guard）：按住期間被打中，傷害依比例減少。
    /// - 揮空懲罰：完美格檔的判定時間結束卻沒擋到攻擊，一段時間內再按也不會開啟判定（仍可一般格檔）。
    /// 最佳打法是抓準時機按下後繼續按住：抓準了無傷，抓不準至少還有一般格檔。
    /// </summary>
    public class ShieldBlockSpecial : SpecialAbility
    {
        [Header("Zone")]
        [SerializeField] private Vector2 zoneOffset = new Vector2(0.6f, 0.2f);
        [SerializeField] private Vector2 zoneSize = new Vector2(0.4f, 1.6f);

        [Header("Guard (按住)")]
        [Tooltip("按住格檔時擋下的傷害比例，剩下的仍會扣血")]
        [SerializeField, Range(0f, 1f)] private float guardDamageReduction = 0.7f;

        [Header("Parry (按下的瞬間)")]
        [Tooltip("按下後這段時間內被打中算完美格檔（0.133 秒 ≈ 60fps 的 8 幀）")]
        [SerializeField] private float parryWindow = 0.133f;
        [Tooltip("判定時間結束仍沒擋到攻擊時，這段時間內按下不會再開啟完美格檔判定")]
        [SerializeField] private float whiffLockoutDuration = 0.5f;

        public override string DisplayName => "Block";
        public override bool AllowsTurning => true;
        public bool IsParryWindowActive => Time.time - parryWindowStartTime <= parryWindow;
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

        private int facing = 1;
        private float parryWindowStartTime = float.NegativeInfinity;
        private float parryLockedUntil = float.NegativeInfinity;

        public override void Tick(bool pressed, bool held, int currentFacing)
        {
            facing = currentFacing;

            if (pressed && !IsParryLocked)
            {
                parryWindowStartTime = Time.time;
                // 先假設會揮空，從判定結束起算鎖定；成功格檔時再解除
                parryLockedUntil = Time.time + parryWindow + whiffLockoutDuration;
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
            center = (Vector2)transform.position + new Vector2(zoneOffset.x * facing, zoneOffset.y);
            size = zoneSize;
            return true;
        }

        public override DamageResult ResolveIncomingHit(ref DamageInfo info)
        {
            // 攻擊來源在面向那一側才擋得住，背後被打一樣會受傷
            bool isFromFront = Mathf.Sign(info.sourcePosition.x - transform.position.x) == facing;
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
                info.amount = Mathf.CeilToInt(info.amount * (1f - guardDamageReduction));
                return DamageResult.Blocked;
            }

            return DamageResult.Hit;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube((Vector2)transform.position + zoneOffset, zoneSize);
        }
    }
}
