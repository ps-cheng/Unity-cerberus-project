using System;
using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Enemies.Boss
{
    /// <summary>
    /// 近身揮刀：前搖（跟一般敵人相同的黃色閃爍 → 轉白提示）→ 判定 → 收招。
    /// 判定時直接交給玩家的受擊流程，被玩家的格檔頭完美格檔就會回傳 Parried，Boss 進入擊暈。
    /// </summary>
    [Serializable]
    public class BossMeleeSwordState : BossState
    {
        [Tooltip("揮刀的時間、範圍、傷害")]
        public MeleeAttackData attack;
        [Tooltip("一輪近身模式最多揮幾刀，揮滿就進入破綻")]
        [Min(1)] public int maxSwingsPerRound = 3;

        [Header("Pose（角度 0 = 刀朝前，正值 = 往上舉）")]
        public float raiseAngle = 135f;
        public float swingAngle = -70f;
        [Tooltip("前搖時身體壓低蓄力的比例")]
        public Vector2 windupSquashScale = new Vector2(1.08f, 0.92f);
        public Color attackColor = new Color(1f, 0.3f, 0.2f);

        public override string DisplayName =>
            $"MeleeSword {phase} {PhaseProgress:P0} ({boss.MeleeSwingCount + 1}/{maxSwingsPerRound})";

        private ActionPhase phase;
        private float phaseTimer;
        private bool hasHit;

        private float PhaseDuration => phase switch
        {
            ActionPhase.Startup => attack.startupDuration,
            ActionPhase.Active => attack.activeDuration,
            _ => attack.recoveryDuration
        };

        private float PhaseProgress => PhaseDuration > 0f ? Mathf.Clamp01(phaseTimer / PhaseDuration) : 1f;

        protected override void OnEnter()
        {
            hasHit = false;
            boss.FaceTarget();
            SetPhase(ActionPhase.Startup);
        }

        protected override void OnTick(float deltaTime)
        {
            boss.Move(0f);
            phaseTimer += deltaTime;

            switch (phase)
            {
                case ActionPhase.Startup:
                    if (phaseTimer >= attack.startupDuration)
                        SetPhase(ActionPhase.Active);
                    break;

                case ActionPhase.Active:
                    if (TryHitTarget())
                        return;
                    if (phaseTimer >= attack.activeDuration)
                        SetPhase(ActionPhase.Recovery);
                    break;

                case ActionPhase.Recovery:
                    if (phaseTimer >= attack.recoveryDuration)
                        Finish();
                    break;
            }
        }

        /// <returns>被格檔而提前結束這一刀時回傳 true</returns>
        private bool TryHitTarget()
        {
            if (hasHit)
                return false;

            var info = new DamageInfo(attack.damage, boss.transform.position, attack.knockbackForce);
            DamageResult result = boss.HitInBox(GetZoneCenter(), attack.hitboxSize, info);

            // 目標無敵時（Ignored）不算命中，判定期間內仍可再嘗試
            if (result != DamageResult.Ignored)
                hasHit = true;

            if (result != DamageResult.Parried)
                return false;

            boss.StaggerFromParry();
            Finish();
            return true;
        }

        private void Finish()
        {
            boss.MeleeSwingCount++;
            boss.ContinueMeleeRound(this);
        }

        private void SetPhase(ActionPhase newPhase)
        {
            phase = newPhase;
            phaseTimer = 0f;
        }

        private Vector2 GetZoneCenter()
        {
            return (Vector2)boss.transform.position + new Vector2(attack.hitboxOffset.x * boss.Facing, attack.hitboxOffset.y);
        }

        public override Color GetBodyColor(Color baseColor)
        {
            return phase switch
            {
                ActionPhase.Startup => boss.telegraph.Evaluate(baseColor, phaseTimer, attack.startupDuration),
                ActionPhase.Active => attackColor,
                _ => baseColor
            };
        }

        public override void UpdateVisuals()
        {
            float t = PhaseProgress;

            switch (phase)
            {
                case ActionPhase.Startup:
                    boss.SetSwordAngle(Mathf.Lerp(boss.SwordRestAngle, raiseAngle, t));
                    boss.SetPoseScale(Vector2.Lerp(Vector2.one, windupSquashScale, t));
                    break;
                case ActionPhase.Active:
                    // 開根號讓刀前段揮得快
                    boss.SetSwordAngle(Mathf.Lerp(raiseAngle, swingAngle, Mathf.Sqrt(t)));
                    break;
                case ActionPhase.Recovery:
                    boss.SetSwordAngle(Mathf.Lerp(swingAngle, boss.SwordRestAngle, t));
                    break;
            }
        }

        public override bool TryGetZone(out Vector2 center, out Vector2 size, out ActionPhase zonePhase)
        {
            center = GetZoneCenter();
            size = attack.hitboxSize;
            zonePhase = phase;
            return true;
        }
    }
}
