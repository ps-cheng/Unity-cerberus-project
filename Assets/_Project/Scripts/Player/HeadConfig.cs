using Cerberus.Combat;
using UnityEngine;
using UnityEngine.Serialization;

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

        [Header("Animation")]
        [Tooltip("這顆頭的動畫（共用 Player Animator Controller 的 Override）")]
        public AnimatorOverrideController animatorOverride;
        [Tooltip("idle 播完一個循環的秒數，例如 1 = 一秒一個循環。0 = 用圖集 manifest 的 fps")]
        [Min(0f)] public float idleCycleDuration = 1f;
        [Tooltip("run 播放速度 = 水平速度 ÷ 最高移動速度 × 這個倍率。腳步在滑就調大，踩太快就調小")]
        [FormerlySerializedAs("walkAnimationSpeedMultiplier")]
        [Min(0f)] public float runAnimationSpeedMultiplier = 1f;
        [Tooltip("按住特殊鍵期間停在特殊動作的第幾格（從 0 起算），例如格檔舉盾那一格。-1 = 不停")]
        public int specialHoldFrame = -1;
        [Tooltip("變身期間替換用的 sprite，名稱要和一般版同序，只差開頭的角色名（例如 Rus_idle_0 ↔ RusBerserk_idle_0）")]
        public Sprite[] transformedSprites;
    }
}
