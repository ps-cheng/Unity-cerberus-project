using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 第三顆頭的特殊動作：按一下開啟變身、再按一下關閉，不會自己結束。
    /// 變身中造成與受到的傷害都提高；不佔用行動，開著時照常移動、出招，受傷也不會被打斷。
    /// 開和關共用同一個冷卻；換到別顆頭時會自動關閉（同樣進入冷卻）。數值在 TransformationData。
    /// </summary>
    public class TransformationSpecial : SpecialAbility
    {
        private readonly TransformationData data;

        public TransformationSpecial(TransformationData data, Transform owner) : base(data, owner)
        {
            this.data = data;
        }

        public override bool OccupiesAction => false;
        public override bool AllowsTurning => true;
        public override float DamageDealtMultiplier => IsActive ? data.damageDealtMultiplier : 1f;
        public override float DamageTakenMultiplier => IsActive ? data.damageTakenMultiplier : 1f;
        public override string StatusText =>
            IsActive ? $"Transformed (DMG x{data.damageDealtMultiplier:0.##}, Taken x{data.damageTakenMultiplier:0.##})" : string.Empty;

        protected override bool StartsCooldownOnActivate => true;

        protected override void OnTick(bool pressed, bool held, int facing)
        {
            if (pressed)
                IsActive = !IsActive;
        }
    }
}
