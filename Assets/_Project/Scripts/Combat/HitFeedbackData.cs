using System;
using UnityEngine;

namespace Cerberus.Combat
{
    /// <summary>
    /// 一招命中時的打擊感設定，跟著 AttackData 走，讓每招可以依力道分級。
    /// </summary>
    [Serializable]
    public class HitFeedbackData
    {
        [Tooltip("命中瞬間凍結畫面的時間（真實秒數），0.016 約等於 60fps 的 1 幀")]
        [Min(0f)] public float hitStopDuration = 0.04f;

        [Tooltip("鏡頭震動的最大偏移（世界單位）")]
        [Min(0f)] public float shakeAmplitude = 0.06f;

        [Min(0f)] public float shakeDuration = 0.12f;
    }
}
