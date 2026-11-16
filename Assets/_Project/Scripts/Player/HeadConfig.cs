using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 一顆頭的完整招式組合：弱攻擊、強攻擊、特殊動作。
    /// 存在 Assets/_Project/Data/Heads，切換頭時 PlayerCombat 換成另一個 HeadConfig 即可。
    /// </summary>
    [CreateAssetMenu(fileName = "NewHead", menuName = "Cerberus/Head Config")]
    public class HeadConfig : ScriptableObject
    {
        public string displayName = "Head";
        public AttackData lightAttack;
        public AttackData heavyAttack;
        public SpecialAbilityData special;

        [Header("Visual")]
        [Tooltip("待機時的身體顏色，出招時仍會依階段變色")]
        public Color bodyColor = new Color(0.55f, 0.35f, 0.25f);
        [Tooltip("這顆頭的像素動畫（共用 Player Animator Controller 的 Override）。留空則顯示方塊外觀")]
        public AnimatorOverrideController animatorOverride;
        [Tooltip("idle 播完一個循環的秒數，例如 1 = 一秒一個循環。0 = 用 Aseprite 裡設定的原速")]
        [Min(0f)] public float idleCycleDuration = 1f;
        [Tooltip("walk 播放速度 = 水平速度 ÷ 最高移動速度 × 這個倍率。腳步在滑就調大，踩太快就調小")]
        [Min(0f)] public float walkAnimationSpeedMultiplier = 1f;
    }
}
