using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 第一顆頭的特殊動作：舉起左手的盾，只對來自正面的攻擊有效。
    /// - 完美格檔（Parry）：按下後短時間內被打中，完全化解並讓敵人陷入硬直。點一下就能觸發，不需要按住。
    /// - 一般格檔（Guard）：按住期間被打中，傷害依比例減少。
    /// 連打時完美格檔的判定時間會縮短，避免靠狂按觸發（參考 Sekiro 的彈刀設計）。
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
        [Tooltip("按下後這段時間內被打中算完美格檔（0.2 秒 ≈ 60fps 的 12 幀）")]
        [SerializeField] private float parryWindow = 0.2f;
        [Tooltip("連打時的完美格檔時間（約 4 幀）")]
        [SerializeField] private float spamParryWindow = 0.067f;
        [Tooltip("放開後這段時間內再按下，會被視為連打")]
        [SerializeField] private float spamPenaltyDuration = 0.5f;

        public override string DisplayName => "Block";
        public override bool AllowsTurning => true;
        public bool IsParryWindowActive => Time.time - lastPressTime <= currentParryWindow;

        public override string StatusText
        {
            get
            {
                if (IsParryWindowActive)
                    return "Parry Window";

                return IsActive ? "Guard" : string.Empty;
            }
        }

        private int facing = 1;
        private float lastPressTime = float.NegativeInfinity;
        private float lastReleaseTime = float.NegativeInfinity;
        private float currentParryWindow;

        public override void Tick(bool pressed, bool held, int currentFacing)
        {
            facing = currentFacing;

            if (pressed)
            {
                bool isSpamming = Time.time - lastReleaseTime < spamPenaltyDuration;
                currentParryWindow = isSpamming ? spamParryWindow : parryWindow;
                lastPressTime = Time.time;
            }

            if (IsActive && !held)
                lastReleaseTime = Time.time;

            IsActive = held;
        }

        public override void Cancel()
        {
            if (IsActive)
                lastReleaseTime = Time.time;

            IsActive = false;
            lastPressTime = float.NegativeInfinity;
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
