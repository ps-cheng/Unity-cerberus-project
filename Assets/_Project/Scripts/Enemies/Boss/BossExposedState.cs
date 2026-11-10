using System;
using UnityEngine;

namespace Cerberus.Enemies.Boss
{
    /// <summary>
    /// 破綻：一輪模式結束後的喘息，Boss 停在原地、沒有盾也不攻擊，玩家可以放心輸出。
    /// 之後回到追擊。
    /// </summary>
    [Serializable]
    public class BossExposedState : BossState
    {
        public float exposedDuration = 3f;
        [Tooltip("破綻時身體在原色與此顏色之間脈動")]
        public Color exposedColor = new Color(0.45f, 0.45f, 0.55f);
        public float pulseFrequency = 1.5f;
        [Tooltip("破綻時身體垂下的比例")]
        public Vector2 slumpScale = new Vector2(1.06f, 0.85f);

        public override string DisplayName =>
            $"Exposed {Timer:0.0}/{exposedDuration:0.0}s" + (boss.IsRangedForced ? " → Ranged" : string.Empty);

        protected override void OnTick(float deltaTime)
        {
            boss.Move(0f);

            if (Timer >= exposedDuration)
                boss.FinishExposed();
        }

        public override Color GetBodyColor(Color baseColor)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f);
            return Color.Lerp(baseColor, exposedColor, Mathf.Lerp(0.5f, 0.9f, pulse));
        }

        public override void UpdateVisuals()
        {
            boss.SetPoseScale(slumpScale);
            boss.SetSwordAngle(boss.SwordDroopAngle);
        }
    }
}
