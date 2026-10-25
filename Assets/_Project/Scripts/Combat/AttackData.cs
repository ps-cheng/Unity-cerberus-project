using System;
using UnityEngine;

namespace Cerberus.Combat
{
    /// <summary>
    /// 一招攻擊的數值設定。可在 Inspector 直接調整手感。
    /// </summary>
    [Serializable]
    public class AttackData
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

        [Tooltip("揮劍起手時劍舉起的角度（純視覺）")]
        public float windupAngle = 30f;

        public float TotalDuration => startupDuration + activeDuration + recoveryDuration;
    }
}
