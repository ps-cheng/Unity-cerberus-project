using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>格檔的數值設定，行為在 ShieldBlockSpecial。</summary>
    [CreateAssetMenu(fileName = "NewShieldBlock", menuName = "Cerberus/Specials/Shield Block")]
    public class ShieldBlockData : SpecialAbilityData
    {
        [Header("Zone")]
        public Vector2 zoneOffset = new Vector2(0.6f, 0.2f);
        public Vector2 zoneSize = new Vector2(0.4f, 1.6f);

        [Header("Guard (按住)")]
        [Tooltip("按住格檔時擋下的傷害比例，剩下的仍會扣血")]
        [Range(0f, 1f)] public float guardDamageReduction = 0.7f;

        [Header("Parry (按下的瞬間)")]
        [Tooltip("按下後這段時間內被打中算完美格檔（0.133 秒 ≈ 60fps 的 8 幀）")]
        [Min(0f)] public float parryWindow = 0.133f;
        [Tooltip("判定時間結束仍沒擋到攻擊時，這段時間內按下不會再開啟完美格檔判定")]
        [Min(0f)] public float whiffLockoutDuration = 0.5f;

        public override SpecialAbility CreateAbility(Transform owner) => new ShieldBlockSpecial(this, owner);

        public override void DrawGizmos(Vector2 origin)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(origin + zoneOffset, zoneSize);
        }
    }
}
