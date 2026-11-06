using UnityEngine;

namespace Cerberus.Combat
{
    /// <summary>近戰攻擊：判定期間在角色前方的判定框內命中目標。</summary>
    [CreateAssetMenu(fileName = "NewMeleeAttack", menuName = "Cerberus/Attacks/Melee Attack")]
    public class MeleeAttackData : AttackData
    {
        [Header("Hitbox")]
        [Tooltip("判定框中心相對角色的位置，x 會依面向自動翻轉")]
        public Vector2 hitboxOffset = new Vector2(1f, 0f);

        public Vector2 hitboxSize = Vector2.one;

        [Header("Sword Visual")]
        [Tooltip("前搖時劍往後收的距離（純視覺）")]
        public float windupDistance = 0.2f;

        [Tooltip("判定時劍往前刺出的距離（純視覺）")]
        public float thrustDistance = 0.6f;
    }
}
