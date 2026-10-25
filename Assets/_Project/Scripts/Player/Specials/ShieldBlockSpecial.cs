using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 第一顆頭的特殊動作：按住舉起左手的盾，擋下來自正面的攻擊。
    /// </summary>
    public class ShieldBlockSpecial : SpecialAbility
    {
        [SerializeField] private Vector2 zoneOffset = new Vector2(0.6f, 0.2f);
        [SerializeField] private Vector2 zoneSize = new Vector2(0.4f, 1.6f);

        private int facing = 1;

        public override string DisplayName => "Block";

        public override void Tick(bool pressed, bool held, int currentFacing)
        {
            // 舉盾的瞬間鎖定方向，舉盾期間不轉身
            if (held && !IsActive)
                facing = currentFacing;

            IsActive = held;
        }

        public override void Cancel() => IsActive = false;

        public override bool TryGetZone(out Vector2 center, out Vector2 size)
        {
            center = (Vector2)transform.position + new Vector2(zoneOffset.x * facing, zoneOffset.y);
            size = zoneSize;
            return true;
        }

        public override bool TryNegateHit(DamageInfo info)
        {
            if (!IsActive)
                return false;

            // 攻擊來源在面向那一側才算擋住，背後被打一樣會受傷
            bool isFromFront = Mathf.Sign(info.sourcePosition.x - transform.position.x) == facing;
            return isFromFront;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube((Vector2)transform.position + zoneOffset, zoneSize);
        }
    }
}
