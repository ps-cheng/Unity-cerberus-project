using System;
using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Enemies
{
    /// <summary>
    /// 敵人共用的雷射發射器（飛行敵人、Boss 觸手）：
    /// 紅色瞄準線追蹤 → 閃爍充能（越來越快）→ 轉白鎖定方向 → 瞬間射出雷射（hitscan）。
    /// 被完美格檔時沿原路反射，命中發射者本身（父物件上的 IDamageable）。
    /// 由擁有者在 FixedUpdate 呼叫 Tick 推進，擁有者硬直時不呼叫就會暫停。
    /// </summary>
    public class LaserEmitter : MonoBehaviour
    {
        public enum Phase
        {
            Idle,
            Aim,
            Charge,
            Fire
        }

        [SerializeField] private Transform firePoint;
        [SerializeField] private LineRenderer laserLine;

        [Header("Timing")]
        [Tooltip("紅色瞄準線持續追蹤玩家的時間")]
        [SerializeField] private float aimDuration = 0.6f;
        [Tooltip("閃爍充能的時間，最後一段會轉白並鎖定方向")]
        [SerializeField] private float chargeDuration = 0.6f;
        [Tooltip("雷射持續的時間，期間內碰到玩家就命中一次")]
        [SerializeField] private float beamDuration = 0.1f;

        [Header("Damage")]
        [SerializeField] private float laserRange = 12f;
        [SerializeField, Min(0)] private int laserDamage = 10;
        [SerializeField] private float laserKnockback = 6f;
        [SerializeField] private LayerMask targetLayers;
        [Tooltip("雷射會被這些 Layer 擋住")]
        [SerializeField] private LayerMask obstacleLayers;

        [Header("Reflect")]
        [Tooltip("完美格檔的獎勵：雷射反射回來的傷害倍率")]
        [SerializeField] private float reflectedDamageMultiplier = 2f;
        [SerializeField] private Color reflectedColor = new Color(0.4f, 0.9f, 1f);

        [Header("Telegraph")]
        [SerializeField] private AttackTelegraph telegraph = new AttackTelegraph();
        [SerializeField] private Color aimColor = new Color(1f, 0.15f, 0.2f);
        [SerializeField] private float aimWidth = 0.03f;
        [SerializeField] private float cueWidth = 0.06f;
        [SerializeField] private float beamWidth = 0.3f;
        [SerializeField] private Color beamColor = new Color(1f, 0.35f, 0.4f);

        public Phase CurrentPhase { get; private set; } = Phase.Idle;
        public bool IsBusy => CurrentPhase != Phase.Idle;
        public bool IsAimLocked => CurrentPhase == Phase.Charge && telegraph.IsCueActive(phaseTimer, chargeDuration);
        public float ChargeProgress => CurrentPhase == Phase.Charge ? Mathf.Clamp01(phaseTimer / chargeDuration) : 0f;
        public float WindupDuration => aimDuration + chargeDuration;

        public string PhaseText => CurrentPhase == Phase.Charge ? $"Charge {ChargeProgress:P0}" : CurrentPhase.ToString();

        /// <summary>雷射射完（包含被格檔反射）時觸發</summary>
        public event Action Finished;

        private Transform target;
        private IDamageable owner;
        private float phaseTimer;
        private Vector2 aimDirection = Vector2.down;
        private bool hasHitThisBeam;

        // 雷射畫面獨立於階段計時，反射後被中斷或發射者死亡停留時，光束仍會顯示完
        private float beamVisibleUntil;
        private Vector2 beamStart;
        private Vector2 beamEnd;
        private Color beamDisplayColor;

        private void Awake()
        {
            owner = GetComponentInParent<IDamageable>();
            laserLine.enabled = false;
        }

        public void Begin(Transform newTarget)
        {
            if (newTarget == null)
                return;

            target = newTarget;
            SetPhase(Phase.Aim);
            TrackTarget();
        }

        public void Cancel()
        {
            target = null;
            SetPhase(Phase.Idle);
        }

        public void Tick(float deltaTime)
        {
            if (!IsBusy)
                return;

            if (target == null)
            {
                Cancel();
                return;
            }

            phaseTimer += deltaTime;

            switch (CurrentPhase)
            {
                case Phase.Aim:
                    TrackTarget();
                    if (phaseTimer >= aimDuration)
                        SetPhase(Phase.Charge);
                    break;

                case Phase.Charge:
                    // 轉白提示出現後鎖定方向，玩家看到提示還來得及閃避或格檔
                    if (!IsAimLocked)
                        TrackTarget();
                    if (phaseTimer >= chargeDuration)
                        StartFire();
                    break;

                case Phase.Fire:
                    UpdateFire();
                    break;
            }
        }

        /// <summary>充能期間的身體顏色提示，跟所有敵人出招前的閃爍規則一致</summary>
        public Color EvaluateTelegraph(Color baseColor)
        {
            return CurrentPhase == Phase.Charge ? telegraph.Evaluate(baseColor, phaseTimer, chargeDuration) : baseColor;
        }

        private void TrackTarget()
        {
            aimDirection = ((Vector2)target.position - (Vector2)firePoint.position).normalized;
        }

        private void StartFire()
        {
            hasHitThisBeam = false;
            SetPhase(Phase.Fire);
            UpdateFire();
        }

        private void UpdateFire()
        {
            RaycastHit2D hit = CastLaser();
            ShowBeam(firePoint.position, hit ? hit.point : (Vector2)firePoint.position + aimDirection * laserRange, beamColor);

            if (!hasHitThisBeam && hit && IsInLayerMask(hit.collider.gameObject.layer, targetLayers))
                TryDamageTarget(hit);

            // 反射時擁有者受擊，可能已經呼叫 Cancel
            if (CurrentPhase == Phase.Fire && phaseTimer >= beamDuration)
                Complete();
        }

        private void Complete()
        {
            target = null;
            SetPhase(Phase.Idle);
            Finished?.Invoke();
        }

        private void TryDamageTarget(RaycastHit2D hit)
        {
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable == null)
                return;

            DamageResult result = damageable.TakeDamage(new DamageInfo(laserDamage, firePoint.position, laserKnockback));

            // 目標無敵時（Ignored）不算命中，雷射持續期間仍可再嘗試
            if (result == DamageResult.Ignored)
                return;

            hasHitThisBeam = true;

            if (result == DamageResult.Parried)
                Reflect(hit.point);
        }

        /// <summary>完美格檔：雷射從格檔點沿原路射回，直接命中發射者本身</summary>
        private void Reflect(Vector2 parryPoint)
        {
            ShowBeam(parryPoint, firePoint.position, reflectedColor);
            Complete();

            if (owner == null)
                return;

            int reflectedDamage = Mathf.RoundToInt(laserDamage * reflectedDamageMultiplier);
            if (owner.TakeDamage(new DamageInfo(reflectedDamage, parryPoint, laserKnockback)) == DamageResult.Hit)
                CombatEvents.RaiseReflectedHit();
        }

        private RaycastHit2D CastLaser()
        {
            return Physics2D.Raycast(firePoint.position, aimDirection, laserRange, obstacleLayers | targetLayers);
        }

        private static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

        private void SetPhase(Phase newPhase)
        {
            CurrentPhase = newPhase;
            phaseTimer = 0f;
        }

        private void ShowBeam(Vector2 start, Vector2 end, Color color)
        {
            beamStart = start;
            beamEnd = end;
            beamDisplayColor = color;
            beamVisibleUntil = Time.time + beamDuration;
        }

        private void Update()
        {
            if (Time.time < beamVisibleUntil)
            {
                DrawLine(beamStart, beamEnd, beamDisplayColor, beamWidth);
                return;
            }

            bool isAiming = CurrentPhase == Phase.Aim || CurrentPhase == Phase.Charge;
            laserLine.enabled = isAiming;
            if (!isAiming)
                return;

            RaycastHit2D hit = CastLaser();
            Vector2 start = firePoint.position;
            Vector2 end = hit ? hit.point : start + aimDirection * laserRange;

            if (IsAimLocked)
                DrawLine(start, end, telegraph.cueColor, cueWidth);
            else if (CurrentPhase == Phase.Aim || telegraph.IsBlinkHighlighted(phaseTimer, chargeDuration))
                DrawLine(start, end, aimColor, aimWidth);
            else
                DrawLine(start, end, new Color(aimColor.r, aimColor.g, aimColor.b, 0.2f), aimWidth);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, float width)
        {
            laserLine.enabled = true;
            laserLine.SetPosition(0, start);
            laserLine.SetPosition(1, end);
            laserLine.startColor = color;
            laserLine.endColor = color;
            laserLine.startWidth = width;
            laserLine.endWidth = width;
        }

        private void OnDisable()
        {
            laserLine.enabled = false;
        }
    }
}
