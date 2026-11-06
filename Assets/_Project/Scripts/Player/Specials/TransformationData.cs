using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 變身的數值設定，行為在 TransformationSpecial。
    /// 共用的 cooldownDuration 在開啟和關閉時都會觸發，避免開開關關閃避風險。
    /// </summary>
    [CreateAssetMenu(fileName = "NewTransformation", menuName = "Cerberus/Specials/Transformation")]
    public class TransformationData : SpecialAbilityData
    {
        [Tooltip("變身中造成的傷害倍率")]
        [Min(0f)] public float damageDealtMultiplier = 1.5f;

        [Tooltip("變身中受到的傷害倍率，跟造成傷害的倍率分開調整")]
        [Min(0f)] public float damageTakenMultiplier = 1.5f;

        public override SpecialAbility CreateAbility(Transform owner) => new TransformationSpecial(this, owner);
    }
}
