using System;
using UnityEngine;

namespace Cerberus.Enemies.Boss
{
    /// <summary>
    /// 觸手雷射：後退 → 從背後伸出觸手 → 每隻觸手依序開始倒數並發射（彼此錯開）→ 全部打完收回 → 破綻。
    /// 每隻觸手的倒數與發射都是同一套 LaserEmitter（跟飛行敵人相同的提示與格檔反射）。
    /// </summary>
    [Serializable]
    public class BossRangedLaserState : BossState
    {
        private enum Step
        {
            Retreat,
            Extend,
            Fire,
            Finish
        }

        [Tooltip("觸手數量（Boss 生成時依此建立，執行中修改不會增減）")]
        [Min(1)] public int tentacleCount = 4;
        [Tooltip("觸手從背後展開的扇形角度")]
        public float tentacleSpreadAngle = 70f;

        [Header("Timing")]
        public float retreatSpeed = 5f;
        public float retreatDuration = 0.8f;
        [Tooltip("強制輪替時改用快速往後衝刺拉開距離")]
        public float dashBackSpeed = 14f;
        public float dashBackDuration = 0.35f;
        public float extendDuration = 0.4f;
        [Tooltip("每隻觸手開始倒數的間隔秒數，讓雷射一發一發錯開")]
        public float staggerInterval = 0.6f;
        [Tooltip("最後一發打完後，觸手收回前停留的時間")]
        public float finishDelay = 0.4f;

        public override string DisplayName => step switch
        {
            Step.Fire => $"RangedLaser Fire {firedCount}/{boss.Tentacles.Count}",
            Step.Retreat when isForced => "RangedLaser DashBack",
            _ => $"RangedLaser {step}"
        };

        private Step step;
        private float stepTimer;
        private int firedCount;
        private bool isForced;

        /// <summary>進入此狀態前設定：是否為強制輪替（改用往後衝刺）</summary>
        public void Prepare(bool forced) => isForced = forced;

        protected override void OnEnter()
        {
            SetStep(Step.Retreat);
        }

        protected override void OnTick(float deltaTime)
        {
            stepTimer += deltaTime;
            boss.FaceTarget();

            switch (step)
            {
                case Step.Retreat:
                    UpdateRetreat();
                    break;
                case Step.Extend:
                    boss.Move(0f);
                    if (stepTimer >= extendDuration)
                        SetStep(Step.Fire);
                    break;
                case Step.Fire:
                    boss.Move(0f);
                    UpdateFire(deltaTime);
                    break;
                case Step.Finish:
                    boss.Move(0f);
                    if (stepTimer >= finishDelay)
                        boss.ChangeState(boss.exposed);
                    break;
            }
        }

        private void UpdateRetreat()
        {
            float speed = isForced ? dashBackSpeed : retreatSpeed;
            float duration = isForced ? dashBackDuration : retreatDuration;
            boss.Move(boss.HasTarget ? -boss.DirectionToTarget * speed : 0f);

            if (stepTimer < duration)
                return;

            boss.SetTentaclesExtended(true, extendDuration);
            SetStep(Step.Extend);
        }

        private void UpdateFire(float deltaTime)
        {
            var tentacles = boss.Tentacles;

            // 依間隔讓下一隻觸手開始倒數
            while (firedCount < tentacles.Count && stepTimer >= firedCount * staggerInterval)
            {
                tentacles[firedCount].Laser.Begin(boss.Target);
                firedCount++;
            }

            bool anyBusy = false;
            foreach (BossTentacle tentacle in tentacles)
            {
                tentacle.Laser.Tick(deltaTime);
                anyBusy |= tentacle.Laser.IsBusy;
            }

            if (firedCount >= tentacles.Count && !anyBusy)
                SetStep(Step.Finish);
        }

        private void SetStep(Step newStep)
        {
            step = newStep;
            stepTimer = 0f;

            if (newStep == Step.Fire)
                firedCount = 0;
        }

        public override void Exit()
        {
            boss.CancelTentacleLasers();
            boss.SetTentaclesExtended(false, extendDuration);
        }
    }
}
