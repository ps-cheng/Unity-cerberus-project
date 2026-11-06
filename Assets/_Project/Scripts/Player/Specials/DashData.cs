using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>衝刺的數值設定，行為在 DashSpecial。冷卻使用共用的 cooldownDuration，從衝刺結束起算。</summary>
    [CreateAssetMenu(fileName = "NewDash", menuName = "Cerberus/Specials/Dash")]
    public class DashData : SpecialAbilityData
    {
        [Min(0f)] public float distance = 3.5f;

        [Tooltip("衝完全程所需的時間，期間無敵")]
        [Min(0.01f)] public float duration = 0.15f;

        public float Speed => distance / duration;

        public override SpecialAbility CreateAbility(Transform owner) => new DashSpecial(this, owner);

        public override void DrawGizmos(Vector2 origin)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, origin + Vector2.right * distance);
            Gizmos.DrawWireSphere(origin + Vector2.right * distance, 0.15f);
        }
    }
}
