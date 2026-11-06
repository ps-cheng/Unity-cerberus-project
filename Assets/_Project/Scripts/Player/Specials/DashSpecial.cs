using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 第二顆頭的特殊動作：按一下朝面向方向快速位移固定距離，途中無敵，可以穿過敵人。
    /// 按住沒有額外效果。冷卻由 SpecialAbility 的共用結構處理。數值在 DashData。
    /// </summary>
    public class DashSpecial : SpecialAbility
    {
        private readonly DashData data;
        private readonly PlayerMovement movement;

        public DashSpecial(DashData data, Transform owner) : base(data, owner)
        {
            this.data = data;
            movement = owner.GetComponent<PlayerMovement>();
        }

        public override bool IsInvincible => IsActive;
        public override string StatusText => IsActive ? "Dashing, Invincible" : string.Empty;

        protected override void OnTick(bool pressed, bool held, int facing)
        {
            if (pressed && !IsActive)
                movement.StartDash(new Vector2(facing * data.Speed, 0f), data.duration);

            IsActive = movement.IsDashing;
        }

        protected override void OnCancel() => movement.StopDash();
    }
}
