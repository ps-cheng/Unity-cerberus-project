using System;
using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Enemies.Boss
{
    /// <summary>
    /// 舉盾：純防禦姿態，不攻擊。開始時鎖定面向，只擋「正面扇形角度內」來的攻擊，
    /// 背後、正上方（跳起來從頭頂打）完全不擋。讓玩家學會繞到背後或從上方打。
    /// </summary>
    [Serializable]
    public class BossMeleeShieldState : BossState
    {
        public float shieldDuration = 1.5f;
        [Tooltip("正面擋傷害的扇形總角度（以面向為中心，左右各一半）")]
        [Range(0f, 360f)] public float blockAngle = 120f;
        [Tooltip("除錯用的扇形外框長度，不影響判定")]
        public float debugConeRadius = 3.5f;

        public override string DisplayName => $"MeleeShield {Timer:0.0}/{shieldDuration:0.0}s";

        protected override void OnEnter()
        {
            // 舉盾期間不轉身，玩家才有機會繞到背後
            boss.FaceTarget();
        }

        protected override void OnTick(float deltaTime)
        {
            boss.Move(0f);

            if (Timer >= shieldDuration)
                boss.ContinueMeleeRound(this);
        }

        public override DamageResult ResolveIncomingHit(DamageInfo info)
        {
            if (!IsInFrontCone(info.sourcePosition))
                return DamageResult.Hit;

            boss.FlashShield();
            return DamageResult.Blocked;
        }

        private bool IsInFrontCone(Vector2 sourcePosition)
        {
            Vector2 toSource = sourcePosition - (Vector2)boss.transform.position;
            return Vector2.Angle(new Vector2(boss.Facing, 0f), toSource) <= blockAngle * 0.5f;
        }

        public override void UpdateVisuals()
        {
            boss.ShowShield(blockAngle, debugConeRadius);
        }
    }
}
