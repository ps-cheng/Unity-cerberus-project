using UnityEngine;

namespace Cerberus.Combat
{
    /// <summary>
    /// 一招攻擊的共通數值（階段時間、傷害、擊退、命中回饋），存成獨立的 .asset 檔（Assets/_Project/Data/Attacks）。
    /// 怎麼命中由子類別決定：近戰判定框 = MeleeAttackData，發射子彈 = ProjectileAttackData。
    /// 所有參照同一個檔案的角色共用這份數值；執行時只讀不寫。
    /// </summary>
    public abstract class AttackData : ScriptableObject
    {
        public string displayName = "Attack";

        [Tooltip("前搖：按下到判定出現的時間")]
        [Min(0f)] public float startupDuration = 0.1f;

        [Tooltip("判定持續時間")]
        [Min(0f)] public float activeDuration = 0.1f;

        [Tooltip("硬直：判定結束到可以再行動的時間")]
        [Min(0f)] public float recoveryDuration = 0.2f;

        [Tooltip("每次命中的傷害（射擊類是每一發子彈的傷害）")]
        [Min(0)] public int damage = 10;

        public float knockbackForce = 3f;

        [Tooltip("命中時的 hit-stop 與鏡頭震動")]
        public HitFeedbackData hitFeedback = new HitFeedbackData();
    }
}
