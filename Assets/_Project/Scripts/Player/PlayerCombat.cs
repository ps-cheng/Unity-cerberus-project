using System;
using System.Collections.Generic;
using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 處理弱攻擊、強攻擊與特殊動作的狀態切換及命中判定。
    /// 近戰攻擊在判定期間檢查判定框；射擊攻擊在進入判定的瞬間發射子彈。
    /// 特殊動作的具體內容交給 SpecialAbility，這裡只管「現在是不是在做特殊動作」。
    /// 移動限制由 PlayerController 依這裡的狀態決定。
    /// </summary>
    public class PlayerCombat : MonoBehaviour, ICombatZoneSource
    {
        [Header("Heads")]
        [Tooltip("可切換的頭，依序為 1、2、3 號，方向鍵上 / 下循環切換。每顆頭是一組招式（弱攻擊、強攻擊、特殊動作）")]
        [SerializeField] private HeadConfig[] heads;
        [SerializeField, Min(0)] private int startingHeadIndex;

        [Header("Hit Detection")]
        [SerializeField] private LayerMask hittableLayers;

        [Header("Projectiles")]
        [Tooltip("發射的子彈放在這個物件底下，保持 Hierarchy 整齊")]
        [SerializeField] private Transform projectileParent;

        public IReadOnlyList<HeadConfig> Heads => heads;
        public int CurrentHeadIndex { get; private set; }
        public HeadConfig Head => heads[CurrentHeadIndex];
        public SpecialAbility Special => specialAbility;
        public CombatAction CurrentAction { get; private set; }
        public ActionPhase CurrentPhase { get; private set; }
        public AttackData CurrentAttack { get; private set; }
        public bool IsBusy => CurrentAction != CombatAction.None;
        public bool IsAttacking => CurrentAction == CombatAction.LightAttack || CurrentAction == CombatAction.HeavyAttack;
        public bool IsSpecialActive => CurrentAction == CombatAction.Special;
        public string SpecialName => specialAbility.DisplayName;
        public string CurrentActionName => CurrentAction switch
        {
            CombatAction.None => "-",
            CombatAction.Special => SpecialName,
            _ => CurrentAttack.displayName
        };
        public bool SpecialLocksMovement => specialAbility.LocksMovement;
        public bool SpecialAllowsTurning => specialAbility.AllowsTurning;
        public string SpecialStatusText => specialAbility.StatusText;
        public float SpecialCooldownRemaining => specialAbility.CooldownRemaining;
        public bool IsSpecialInvincible => specialAbility.IsInvincible;
        public float DamageTakenMultiplier => specialAbility.DamageTakenMultiplier;

        /// <summary>目前階段的進度 0~1，給視覺與 HUD 使用</summary>
        public float PhaseProgress { get; private set; }

        public event Action<CombatAction> ActionStarted;
        /// <summary>攻擊一般命中目標（不含被格檔、目標無敵）時觸發，附上是哪一招</summary>
        public event Action<Collider2D, AttackData> TargetHit;
        /// <summary>換頭時觸發，參數為新的頭的索引與切換方向（+1 = 下一顆，-1 = 上一顆）</summary>
        public event Action<int, int> HeadChanged;
        /// <summary>不佔用行動的特殊動作（變身）開啟或關閉時觸發，參數為是否開啟</summary>
        public event Action<bool> SpecialToggled;

        private SpecialAbility specialAbility;
        // 每顆頭的特殊動作只建立一次並保留，切換頭時執行期狀態（冷卻）不會重置，會在背景繼續倒數
        private readonly Dictionary<SpecialAbilityData, SpecialAbility> specialInstances = new Dictionary<SpecialAbilityData, SpecialAbility>();
        private int attackFacing = 1;
        private float phaseTimer;
        private ContactFilter2D hitFilter;
        private readonly List<Collider2D> overlapResults = new List<Collider2D>();
        // 同一次攻擊對同一目標只命中一次
        private readonly HashSet<Collider2D> targetsHitThisAttack = new HashSet<Collider2D>();

        private void Awake()
        {
            hitFilter = new ContactFilter2D();
            hitFilter.SetLayerMask(hittableLayers);
            hitFilter.useTriggers = true;

            CurrentHeadIndex = Mathf.Clamp(startingHeadIndex, 0, heads.Length - 1);
            specialAbility = GetOrCreateSpecial(Head.special);
        }

        /// <summary>
        /// 出招、特殊動作進行中不能換頭。換頭後原本那顆頭的特殊動作冷卻會在背景繼續倒數。
        /// 還開著的狀態型特殊動作（變身）會先關閉並進入冷卻，不能靠換頭甩掉代價或把效果帶到別顆頭。
        /// </summary>
        public bool TrySwitchHead(int step)
        {
            if (IsBusy || heads.Length < 2)
                return false;

            if (specialAbility.IsActive)
            {
                specialAbility.Cancel();
                SpecialToggled?.Invoke(false);
            }

            int count = heads.Length;
            CurrentHeadIndex = ((CurrentHeadIndex + step) % count + count) % count;
            specialAbility = GetOrCreateSpecial(Head.special);
            HeadChanged?.Invoke(CurrentHeadIndex, step);
            return true;
        }

        private SpecialAbility GetOrCreateSpecial(SpecialAbilityData data)
        {
            if (!specialInstances.TryGetValue(data, out SpecialAbility ability))
            {
                ability = data.CreateAbility(transform);
                specialInstances.Add(data, ability);
            }

            return ability;
        }

        public bool TryStartAttack(CombatAction attackType, int facing)
        {
            if (IsBusy)
                return false;

            CurrentAttack = attackType switch
            {
                CombatAction.LightAttack => Head.lightAttack,
                CombatAction.HeavyAttack => Head.heavyAttack,
                _ => null
            };

            if (CurrentAttack == null)
                return false;

            attackFacing = facing;
            targetsHitThisAttack.Clear();
            CurrentAction = attackType;
            ActionStarted?.Invoke(attackType);
            EnterPhase(ActionPhase.Startup);
            return true;
        }

        /// <summary>
        /// 每幀呼叫，把特殊鍵的狀態轉交給 SpecialAbility。
        /// 佔用行動的特殊動作在攻擊中不會被觸發；不佔用的（變身）攻擊中也能開關，也不會變成目前的動作。
        /// </summary>
        public void UpdateSpecial(bool pressed, bool held, int facing)
        {
            if (!specialAbility.OccupiesAction)
            {
                bool wasActive = specialAbility.IsActive;
                specialAbility.Tick(pressed, held, facing);

                if (specialAbility.IsActive != wasActive)
                    SpecialToggled?.Invoke(specialAbility.IsActive);

                return;
            }

            if (IsAttacking)
                return;

            specialAbility.Tick(pressed, held, facing);

            if (specialAbility.IsActive && !IsSpecialActive)
            {
                CurrentAction = CombatAction.Special;
                ActionStarted?.Invoke(CombatAction.Special);
                EnterPhase(ActionPhase.Active);
            }
            else if (!specialAbility.IsActive && IsSpecialActive)
            {
                EndAction();
            }
        }

        /// <summary>受擊時強制中斷目前的招式</summary>
        public void CancelAction()
        {
            if (IsSpecialActive)
                specialAbility.Cancel();

            if (IsBusy)
                EndAction();
        }

        // 不限於特殊動作進行中才詢問：像完美格檔這種「點一下」的判定，放開後仍可能有效
        public DamageResult ResolveIncomingHit(ref DamageInfo info) => specialAbility.ResolveIncomingHit(ref info);

        /// <summary>目前招式作用範圍的世界座標；沒有招式時回傳 false</summary>
        public bool TryGetCurrentZone(out Vector2 center, out Vector2 size)
        {
            // 射擊攻擊沒有判定框，命中範圍就是子彈本身
            if (IsAttacking && CurrentAttack is MeleeAttackData melee)
            {
                center = GetMeleeZoneCenter(melee);
                size = melee.hitboxSize;
                return true;
            }

            if (IsSpecialActive)
                return specialAbility.TryGetZone(out center, out size);

            center = default;
            size = default;
            return false;
        }

        private void Update()
        {
            if (!IsAttacking)
                return;

            phaseTimer += Time.deltaTime;
            float phaseDuration = GetPhaseDuration(CurrentPhase);
            PhaseProgress = phaseDuration > 0f ? Mathf.Clamp01(phaseTimer / phaseDuration) : 1f;

            if (CurrentPhase == ActionPhase.Active && CurrentAttack is MeleeAttackData melee)
                DetectMeleeHits(melee);

            if (phaseTimer >= phaseDuration)
                AdvancePhase();
        }

        private void AdvancePhase()
        {
            switch (CurrentPhase)
            {
                case ActionPhase.Startup:
                    EnterPhase(ActionPhase.Active);
                    break;
                case ActionPhase.Active:
                    EnterPhase(ActionPhase.Recovery);
                    break;
                case ActionPhase.Recovery:
                    EndAction();
                    break;
            }
        }

        private void EnterPhase(ActionPhase phase)
        {
            CurrentPhase = phase;
            phaseTimer = 0f;
            PhaseProgress = 0f;

            if (phase == ActionPhase.Active && CurrentAttack is ProjectileAttackData shot)
                FireProjectiles(shot);
        }

        private void EndAction()
        {
            CurrentAction = CombatAction.None;
            CurrentAttack = null;
            EnterPhase(ActionPhase.None);
        }

        private float GetPhaseDuration(ActionPhase phase) => phase switch
        {
            ActionPhase.Startup => CurrentAttack.startupDuration,
            ActionPhase.Active => CurrentAttack.activeDuration,
            ActionPhase.Recovery => CurrentAttack.recoveryDuration,
            _ => 0f
        };

        private Vector2 GetMeleeZoneCenter(MeleeAttackData melee)
        {
            Vector2 offset = melee.hitboxOffset;
            return (Vector2)transform.position + new Vector2(offset.x * attackFacing, offset.y);
        }

        private void DetectMeleeHits(MeleeAttackData melee)
        {
            Physics2D.OverlapBox(GetMeleeZoneCenter(melee), melee.hitboxSize, 0f, hitFilter, overlapResults);

            foreach (Collider2D target in overlapResults)
            {
                if (!targetsHitThisAttack.Add(target))
                    continue;

                IDamageable damageable = target.GetComponentInParent<IDamageable>();
                if (damageable == null)
                    continue;

                var info = new DamageInfo(ScaleDamage(melee.damage), transform.position, melee.knockbackForce);

                if (damageable.TakeDamage(info) == DamageResult.Hit)
                    TargetHit?.Invoke(target, melee);
            }
        }

        private void FireProjectiles(ProjectileAttackData shot)
        {
            Vector2 muzzle = shot.GetMuzzlePosition(transform.position, attackFacing);
            int damage = ScaleDamage(shot.damage);

            for (int i = 0; i < shot.projectileCount; i++)
            {
                Projectile projectile = Instantiate(shot.projectilePrefab, muzzle, Quaternion.identity, projectileParent);
                projectile.Launch(shot, shot.GetDirection(i, attackFacing), damage, hittableLayers, target => TargetHit?.Invoke(target, shot));
            }
        }

        // 特殊動作（例如變身）對造成傷害的加成
        private int ScaleDamage(int baseDamage) => Mathf.RoundToInt(baseDamage * specialAbility.DamageDealtMultiplier);

        private void OnDrawGizmosSelected()
        {
            // 未執行時在 Scene 視窗預覽起始那顆頭的招式範圍（以面向右為準）
            if (Application.isPlaying || heads == null || startingHeadIndex >= heads.Length)
                return;

            HeadConfig head = heads[startingHeadIndex];
            if (head == null)
                return;

            Vector2 origin = transform.position;
            DrawAttackGizmo(head.lightAttack, origin, new Color(1f, 0.6f, 0f));
            DrawAttackGizmo(head.heavyAttack, origin, Color.red);

            if (head.special != null)
                head.special.DrawGizmos(origin);
        }

        private static void DrawAttackGizmo(AttackData attack, Vector2 origin, Color color)
        {
            Gizmos.color = color;

            // 近戰畫判定框，射擊畫每一發子彈的彈道（長度 = 射程）
            if (attack is MeleeAttackData melee)
            {
                Gizmos.DrawWireCube(origin + melee.hitboxOffset, melee.hitboxSize);
            }
            else if (attack is ProjectileAttackData shot)
            {
                Vector2 muzzle = shot.GetMuzzlePosition(origin, 1);
                for (int i = 0; i < shot.projectileCount; i++)
                    Gizmos.DrawLine(muzzle, muzzle + shot.GetDirection(i, 1) * shot.maxRange);
            }
        }
    }
}
