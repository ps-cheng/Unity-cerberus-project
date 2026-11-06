using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 特殊動作的數值資產基底。每種特殊動作各自繼承一個資料類型（格檔 = ShieldBlockData），
    /// 並負責產生對應的 SpecialAbility。資產本身執行時只讀不寫。
    /// </summary>
    public abstract class SpecialAbilityData : ScriptableObject
    {
        public string displayName = "Special";

        [Tooltip("HUD 上的操作說明，例如「tap = dash」")]
        public string controlHint;

        [Tooltip("特殊動作結束後要等多久才能再用，冷卻中完全不能啟動")]
        [Min(0f)] public float cooldownDuration;

        public abstract SpecialAbility CreateAbility(Transform owner);

        /// <summary>未執行時在 Scene 視窗預覽範圍（以面向右為準）</summary>
        public virtual void DrawGizmos(Vector2 origin) { }
    }
}
