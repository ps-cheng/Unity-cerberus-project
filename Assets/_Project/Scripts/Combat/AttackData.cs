using UnityEngine;

namespace Cerberus.Combat
{
    /// <summary>
    /// 一招攻擊的數值設定，存成獨立的 .asset 檔（Assets/_Project/Data/Attacks）。
    /// 所有參照同一個檔案的角色共用這份數值；Play Mode 中調整的數值停止後會保留。
    /// 執行時只讀不寫，每個角色各自變動的狀態（血量、目前階段）不要放在這裡。
    /// </summary>
    [CreateAssetMenu(fileName = "NewAttack", menuName = "Cerberus/Attack Data")]
    public class AttackData : ScriptableObject
    {
        public string displayName = "Attack";

        [Tooltip("前搖：按下到判定出現的時間")]
        [Min(0f)] public float startupDuration = 0.1f;

        [Tooltip("判定持續時間")]
        [Min(0f)] public float activeDuration = 0.1f;

        [Tooltip("硬直：判定結束到可以再行動的時間")]
        [Min(0f)] public float recoveryDuration = 0.2f;

        [Tooltip("判定框中心相對角色的位置，x 會依面向自動翻轉")]
        public Vector2 hitboxOffset = new Vector2(1f, 0f);

        public Vector2 hitboxSize = Vector2.one;

        [Min(0)] public int damage = 10;

        public float knockbackForce = 3f;

        [Tooltip("前搖時劍往後收的距離（純視覺）")]
        public float windupDistance = 0.2f;

        [Tooltip("判定時劍往前刺出的距離（純視覺）")]
        public float thrustDistance = 0.6f;

        [Tooltip("命中時的 hit-stop 與鏡頭震動")]
        public HitFeedbackData hitFeedback = new HitFeedbackData();
    }
}
