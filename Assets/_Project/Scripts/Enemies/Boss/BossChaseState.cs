using System;
using UnityEngine;

namespace Cerberus.Enemies.Boss
{
    /// <summary>
    /// 追擊：朝玩家走過去，並依距離決定下一個模式。
    /// 進入近戰距離 → 近身模式；玩家一直保持在遠距離 → 遠程模式。
    /// </summary>
    [Serializable]
    public class BossChaseState : BossState
    {
        [Tooltip("比玩家移動速度（7）慢很多，玩家可以拉開距離")]
        public float moveSpeed = 3f;

        [Header("Distance Thresholds（Boss 與玩家中心的水平距離）")]
        [Tooltip("玩家進入此距離 → 開始近身模式")]
        public float meleeEnterDistance = 3f;
        [Tooltip("近身模式中，每招結束時玩家超過此距離 → 中斷近身模式回到追擊")]
        public float meleeExitDistance = 4.5f;
        [Tooltip("玩家在此距離以外待滿下面的秒數 → 開始遠程模式")]
        public float rangedEnterDistance = 7f;
        public float rangedTriggerDelay = 1.5f;

        public override string DisplayName =>
            (farTimer > 0f ? $"Chase (far {farTimer:0.0}/{rangedTriggerDelay:0.0}s)" : "Chase") +
            $" [melee {boss.MeleeRoundsCompleted}/{boss.meleeRoundsBeforeRanged}]";

        private float farTimer;

        protected override void OnEnter()
        {
            farTimer = 0f;
        }

        protected override void OnTick(float deltaTime)
        {
            if (!boss.HasTarget)
            {
                boss.Move(0f);
                return;
            }

            boss.FaceTarget();
            float distance = boss.HorizontalDistanceToTarget;

            if (distance <= meleeEnterDistance)
            {
                boss.StartMeleeRound();
                return;
            }

            farTimer = distance >= rangedEnterDistance ? farTimer + deltaTime : 0f;
            if (farTimer >= rangedTriggerDelay)
            {
                boss.StartRangedRound();
                return;
            }

            boss.Move(boss.DirectionToTarget * moveSpeed);
        }
    }
}
