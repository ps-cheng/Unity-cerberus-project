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
    }
}
