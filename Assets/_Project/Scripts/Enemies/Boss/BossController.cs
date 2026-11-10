using System.Collections.Generic;
using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Enemies.Boss
{
    /// <summary>
    /// Boss 本體：持有各狀態並負責切換，提供狀態共用的移動、面向、外觀操作。
    /// 流程：Chase →（玩家靠近）近身模式：揮刀/舉盾交替，揮滿刀數 → Exposed → Chase
    ///            →（玩家一直在遠處）遠程模式：後退、觸手依序雷射 → Exposed → Chase
    /// 強制輪替：近身模式累積跑完指定輪數後，下一次破綻結束不回 Chase，而是往後衝刺強制進入遠程模式，
    /// 避免玩家一直貼身時永遠只看到近戰。
    /// 受擊（扣血、閃白、hit-stop、震動）沿用 EnemyBase 與玩家端的命中回饋；
    /// Boss 不會因為一般受擊而硬直，只有被完美格檔才會擊暈。總血量在 Health 元件上調整。
    /// </summary>
    public class BossController : EnemyBase, ICombatZoneSource
    {
        [Header("States")]
        public BossChaseState chase = new BossChaseState();
        public BossMeleeSwordState sword = new BossMeleeSwordState();
        public BossMeleeShieldState shield = new BossMeleeShieldState();
        public BossRangedLaserState ranged = new BossRangedLaserState();
        public BossExposedState exposed = new BossExposedState();

        [Header("Rotation")]
        [Tooltip("近身模式完整跑完幾輪（揮刀/舉盾交替到揮滿刀數）後，不管玩家距離多近都強制後退進入觸手雷射")]
        [Min(1)] public int meleeRoundsBeforeRanged = 2;

        [Header("Shared")]
        [Tooltip("揮刀前搖的提示，規則與一般敵人相同")]
        public AttackTelegraph telegraph = new AttackTelegraph();
        [Tooltip("受擊擊退 = 攻擊的擊退力道 × 此比例，Boss 體型大只會被推動一點")]
        public float knockbackScale = 0.15f;
        [Tooltip("擊退速度每秒衰減多少")]
        public float knockbackDecay = 20f;

        [Header("Parts")]
        [SerializeField] private Transform swordPivot;
        [Tooltip("待機時刀的角度（0 = 朝前）")]
        [SerializeField] private float swordRestAngle = 50f;
        [SerializeField] private float swordDroopAngle = -60f;
        [SerializeField] private SpriteRenderer shieldRenderer;
        [SerializeField] private Color shieldBlockFlashColor = Color.white;
        [SerializeField] private float shieldBlockFlashDuration = 0.12f;
        [Tooltip("舉盾時顯示正面擋傷害的扇形範圍（除錯用）")]
        [SerializeField] private LineRenderer shieldConeLine;
        [Tooltip("觸手的樣板（關閉狀態），生成時依觸手數量複製")]
        [SerializeField] private BossTentacle tentacleTemplate;

        public IReadOnlyList<BossTentacle> Tentacles => tentacles;
        public int Facing => FacingDirection;
        public float SwordRestAngle => swordRestAngle;
        public float SwordDroopAngle => swordDroopAngle;
        public BossState CurrentState { get; private set; }
        /// <summary>這一輪近身模式已經揮了幾刀</summary>
        public int MeleeSwingCount { get; set; }
        /// <summary>上次遠程模式之後，近身模式完整跑完的輪數</summary>
        public int MeleeRoundsCompleted { get; private set; }
        public bool IsRangedForced => MeleeRoundsCompleted >= meleeRoundsBeforeRanged;

        public float HorizontalDistanceToTarget =>
            HasTarget ? Mathf.Abs(Target.position.x - transform.position.x) : float.PositiveInfinity;

        public float DirectionToTarget => HasTarget ? GetDirectionToTarget() : FacingDirection;

        protected override string BehaviorStateName => CurrentState.DisplayName;

        private readonly List<BossTentacle> tentacles = new List<BossTentacle>();
        private Color shieldBaseColor;
        private float shieldFlashUntil;
        private float knockbackVelocity;

        protected override void Awake()
        {
            base.Awake();

            foreach (BossState state in new BossState[] { chase, sword, shield, ranged, exposed })
                state.Initialize(this);

            shieldBaseColor = shieldRenderer.color;
            BuildTentacles();
            ChangeState(chase);
        }

        private void BuildTentacles()
        {
            tentacleTemplate.gameObject.SetActive(false);
            int count = ranged.tentacleCount;

            for (int i = 0; i < count; i++)
            {
                BossTentacle tentacle = Instantiate(tentacleTemplate, tentacleTemplate.transform.parent);
                tentacle.name = $"Tentacle_{i + 1}";

                // 以樣板的角度為中心，在背後展開成扇形
                float t = count > 1 ? (float)i / (count - 1) - 0.5f : 0f;
                tentacle.transform.localRotation = tentacleTemplate.transform.localRotation *
                                                   Quaternion.Euler(0f, 0f, t * ranged.tentacleSpreadAngle);
                tentacle.gameObject.SetActive(true);
                tentacles.Add(tentacle);
            }
        }

        public void ChangeState(BossState next)
        {
            CurrentState?.Exit();
            CurrentState = next;
            CurrentState.Enter();
        }

        private void FixedUpdate()
        {
            knockbackVelocity = Mathf.MoveTowards(knockbackVelocity, 0f, knockbackDecay * Time.fixedDeltaTime);

            // 擊暈中不做任何決策，只保留擊退
            if (IsStunned)
            {
                Move(0f);
                return;
            }

            CurrentState.Tick(Time.fixedDeltaTime);
        }

        #region Mode Flow

        public void StartMeleeRound()
        {
            MeleeSwingCount = 0;
            ChangeState(sword);
        }

        /// <param name="isForced">近身輪數達上限的強制輪替，用往後衝刺取代一般後退</param>
        public void StartRangedRound(bool isForced = false)
        {
            MeleeRoundsCompleted = 0;
            ranged.Prepare(isForced);
            ChangeState(ranged);
        }

        /// <summary>近身模式中每個動作結束時決定下一步：揮刀與舉盾交替，揮滿刀數就進入破綻</summary>
        public void ContinueMeleeRound(BossState finished)
        {
            if (MeleeSwingCount >= sword.maxSwingsPerRound)
            {
                MeleeRoundsCompleted++;
                ChangeState(exposed);
            }
            else if (HorizontalDistanceToTarget > chase.meleeExitDistance)
                ChangeState(chase);
            else
                ChangeState(finished == sword ? (BossState)shield : sword);
        }

        /// <summary>破綻結束：近身輪數達上限就強制進入遠程模式，否則回到追擊</summary>
        public void FinishExposed()
        {
            if (IsRangedForced)
                StartRangedRound(isForced: true);
            else
                ChangeState(chase);
        }

        #endregion

        #region Shared Actions

        public void Move(float speed) => Body.linearVelocity = new Vector2(speed + knockbackVelocity, Body.linearVelocity.y);

        public void FaceTarget()
        {
            if (HasTarget)
                SetFacing(GetDirectionToTarget());
        }

        public DamageResult HitInBox(Vector2 center, Vector2 size, DamageInfo info) => HitTargetInBox(center, size, info);

        public void StaggerFromParry() => ApplyParriedStagger();

        public void SetTentaclesExtended(bool isExtended, float duration)
        {
            foreach (BossTentacle tentacle in tentacles)
                tentacle.SetExtended(isExtended, duration);
        }

        public void CancelTentacleLasers()
        {
            foreach (BossTentacle tentacle in tentacles)
                tentacle.Laser.Cancel();
        }

        #endregion

        #region Hit Handling

        protected override DamageResult ResolveIncomingHit(DamageInfo info)
        {
            // 被格檔擊暈時是完全的破綻，盾不會生效
            return IsStaggered ? DamageResult.Hit : CurrentState.ResolveIncomingHit(info);
        }

        protected override void OnHurt(DamageInfo info)
        {
            float awayDirection = Mathf.Sign(transform.position.x - info.sourcePosition.x);
            knockbackVelocity = awayDirection * info.knockbackForce * knockbackScale;
        }

        protected override void OnDied()
        {
            CurrentState.Exit();
            CancelTentacleLasers();
        }

        #endregion

        #region Visuals

        protected override void Update()
        {
            ResetPose();

            if (!IsStunned)
                CurrentState.UpdateVisuals();

            base.Update();
        }

        private void ResetPose()
        {
            VisualScale = Vector2.one;
            SetSwordAngle(swordRestAngle);
            shieldRenderer.enabled = false;
            shieldConeLine.enabled = false;
        }

        public void SetSwordAngle(float angle) => swordPivot.localRotation = Quaternion.Euler(0f, 0f, angle);

        public void SetPoseScale(Vector2 scale) => VisualScale = scale;

        public void ShowShield(float blockAngle, float coneRadius)
        {
            shieldRenderer.enabled = true;
            shieldRenderer.color = Time.time < shieldFlashUntil ? shieldBlockFlashColor : shieldBaseColor;

            // 扇形外框：上緣 → 中心 → 下緣
            Vector2 center = transform.position;
            Vector2 forward = new Vector2(Facing, 0f);
            shieldConeLine.enabled = true;
            shieldConeLine.SetPosition(0, center + Rotate(forward, blockAngle * 0.5f) * coneRadius);
            shieldConeLine.SetPosition(1, center);
            shieldConeLine.SetPosition(2, center + Rotate(forward, -blockAngle * 0.5f) * coneRadius);
        }

        public void FlashShield() => shieldFlashUntil = Time.time + shieldBlockFlashDuration;

        private static Vector2 Rotate(Vector2 v, float degrees) => Quaternion.Euler(0f, 0f, degrees) * v;

        protected override Color GetBodyColor() => CurrentState.GetBodyColor(BaseColor);

        #endregion

        #region ICombatZoneSource

        public ActionPhase CurrentPhase =>
            CurrentState != null && CurrentState.TryGetZone(out _, out _, out ActionPhase phase) ? phase : ActionPhase.None;

        public bool IsSpecialActive => false;

        public bool TryGetCurrentZone(out Vector2 center, out Vector2 size)
        {
            center = Vector2.zero;
            size = Vector2.zero;
            return !IsStunned && CurrentState != null && CurrentState.TryGetZone(out center, out size, out _);
        }

        #endregion
    }
}
