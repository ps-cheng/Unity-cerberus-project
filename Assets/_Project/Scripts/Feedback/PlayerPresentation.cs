using System;
using System.Collections.Generic;
using Cerberus.Audio;
using Cerberus.Combat;
using Cerberus.Enemies;
using Cerberus.Player;
using UnityEngine;

namespace Cerberus.Feedback
{
    /// <summary>
    /// 把戰鬥事件接到特效和音效（只負責「看起來、聽起來」，不影響任何判定和數值）：
    /// - 出招：判定開始的那一刻播揮擊軌跡／槍口火光／爪痕和對應音效
    /// - 命中：火花 + 擊中音效
    /// - 換頭：閃光、煙霧（洛斯多布料碎片）、角色閃白、音效
    /// - 受傷、格擋、敵人死亡
    /// </summary>
    public class PlayerPresentation : MonoBehaviour
    {
        public enum AttackStyle { Arc, Claw, Muzzle }

        [Serializable]
        public class AttackVisual
        {
            public HeadConfig head;
            public CombatAction action = CombatAction.LightAttack;
            public AttackStyle style;
            public SfxId sound;
            [Tooltip("音高：重攻擊可以低一點")]
            public float soundPitch = 1f;
            public Color color = Color.white;
            [Tooltip("Arc：軌跡中心相對角色的位置。Muzzle：火光相對判定上槍口位置的偏移，只影響外觀（面向右）")]
            public Vector2 offset = new Vector2(0.2f, 0.3f);
            [Tooltip("Arc：半徑。Claw：爪痕長度")]
            public float size = 1.3f;
            [Tooltip("Arc：起始、結束角度（0 = 正前方，90 = 正上方）")]
            public Vector2 arcAngles = new Vector2(110f, -50f);
            public float width = 0.12f;
            public float drawDuration = 0.08f;
            public float fadeDuration = 0.14f;
        }

        [Serializable]
        public class HeadVisual
        {
            public HeadConfig head;
            [Tooltip("識別色：換頭特效、台詞名字都用這個顏色")]
            public Color identityColor = Color.white;
            [Tooltip("切到這顆頭時衣服炸開（洛斯）")]
            public bool ripClothesOnSwitch;
        }

        [Header("References")]
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerDamageReceiver damageReceiver;
        [Tooltip("換頭時閃白的角色圖")]
        [SerializeField] private SpriteFlash playerFlash;
        [Tooltip("揮擊軌跡用的材質（Cerberus/Additive）")]
        [SerializeField] private Material slashMaterial;
        [SerializeField] private int slashSortingOrder = 35;

        [Header("Attacks")]
        [SerializeField] private List<AttackVisual> attacks = new List<AttackVisual>();

        [Header("Head Switch")]
        [SerializeField] private List<HeadVisual> heads = new List<HeadVisual>();
        [Tooltip("特效出現在角色身體中間（角色位置 + 這個偏移）")]
        [SerializeField] private Vector2 bodyCenterOffset = new Vector2(0f, 0.4f);
        [SerializeField] private float switchFlashDuration = 0.18f;

        private ActionPhase previousPhase;

        public Color GetIdentityColor(HeadConfig head)
        {
            HeadVisual visual = heads.Find(entry => entry.head == head);
            return visual != null ? visual.identityColor : Color.white;
        }

        private void OnEnable()
        {
            combat.TargetHit += HandleTargetHit;
            combat.HeadChanged += HandleHeadChanged;
            damageReceiver.Hurt += HandleHurt;
            damageReceiver.Guarded += HandleGuarded;
            damageReceiver.Parried += HandleParried;
            EnemyBase.AnyDied += HandleEnemyDied;
        }

        private void OnDisable()
        {
            combat.TargetHit -= HandleTargetHit;
            combat.HeadChanged -= HandleHeadChanged;
            damageReceiver.Hurt -= HandleHurt;
            damageReceiver.Guarded -= HandleGuarded;
            damageReceiver.Parried -= HandleParried;
            EnemyBase.AnyDied -= HandleEnemyDied;
        }

        private void Update()
        {
            // 判定開始（進入 Active）的那一幀播出招效果，跟實際打到的時間一致
            ActionPhase phase = combat.CurrentPhase;
            if (phase == ActionPhase.Active && previousPhase != ActionPhase.Active && combat.IsAttacking)
                PlayAttack(combat.CurrentAttack);
            previousPhase = phase;
        }

        private void PlayAttack(AttackData attack)
        {
            AttackVisual visual = attacks.Find(entry => entry.head == combat.Head && entry.action == combat.CurrentAction);
            if (visual == null)
                return;

            int facing = movement.FacingDirection;
            Vector2 origin = transform.position;
            SfxPlayer.Play(visual.sound, visual.soundPitch);

            switch (visual.style)
            {
                case AttackStyle.Muzzle:
                    if (attack is ProjectileAttackData shot && CombatVfx.Instance != null)
                        CombatVfx.Instance.MuzzleFlash(shot.GetMuzzlePosition(origin, facing) + new Vector2(visual.offset.x * facing, visual.offset.y), facing);
                    break;

                case AttackStyle.Arc:
                case AttackStyle.Claw:
                    SlashVfx.Spawn(new SlashVfx.Settings
                    {
                        style = visual.style == AttackStyle.Arc ? SlashVfx.Style.Arc : SlashVfx.Style.Claw,
                        center = origin + new Vector2(visual.offset.x * facing, visual.offset.y),
                        facing = facing,
                        radius = visual.size,
                        startAngle = visual.arcAngles.x,
                        endAngle = visual.arcAngles.y,
                        width = visual.width,
                        drawDuration = visual.drawDuration,
                        fadeDuration = visual.fadeDuration,
                        color = visual.color,
                        material = slashMaterial,
                        sortingOrder = slashSortingOrder
                    }, CombatVfx.Instance != null ? CombatVfx.Instance.transform : null);
                    break;
            }
        }

        private void HandleTargetHit(Collider2D target, AttackData attack)
        {
            SfxPlayer.Play(SfxId.Hit, attack == combat.Head.heavyAttack ? 0.85f : 1f);

            if (CombatVfx.Instance == null || target == null)
                return;

            // 火花放在目標身上最靠近角色的那一點
            Vector2 point = target.ClosestPoint((Vector2)transform.position + bodyCenterOffset);
            float direction = Mathf.Sign(target.bounds.center.x - transform.position.x);
            CombatVfx.Instance.HitSpark(point, direction, GetIdentityColor(combat.Head));
        }

        private void HandleHeadChanged(int newIndex, int step)
        {
            HeadConfig head = combat.Heads[newIndex];
            HeadVisual visual = heads.Find(entry => entry.head == head);
            Color color = visual != null ? visual.identityColor : Color.white;

            SfxPlayer.Play(SfxId.HeadSwitch);
            if (playerFlash != null)
                playerFlash.Flash(Color.white, switchFlashDuration);
            if (CombatVfx.Instance != null)
                CombatVfx.Instance.HeadSwitch((Vector2)transform.position + bodyCenterOffset, color, visual != null && visual.ripClothesOnSwitch);
        }

        private void HandleHurt(int amount) => SfxPlayer.Play(SfxId.Hurt);

        private void HandleGuarded(int amount) => SfxPlayer.Play(SfxId.Parry, 0.8f, 0.8f);

        private void HandleParried()
        {
            SfxPlayer.Play(SfxId.Parry, 1.1f);
            if (CombatVfx.Instance != null)
                CombatVfx.Instance.HitSpark((Vector2)transform.position + new Vector2(0.6f * movement.FacingDirection, 0.4f), movement.FacingDirection, Color.white);
        }

        private void HandleEnemyDied(EnemyBase enemy)
        {
            SfxPlayer.Play(SfxId.EnemyExplode, enemy is Enemies.Boss.BossController ? 0.7f : 1f);
            if (CombatVfx.Instance == null)
                return;

            // 依敵人圖的大小決定爆炸規模，Boss 自然比較大
            Bounds bounds = new Bounds(enemy.transform.position, Vector3.zero);
            foreach (SpriteRenderer part in enemy.GetComponentsInChildren<SpriteRenderer>())
            {
                if (part.enabled && part.sprite != null)
                    bounds.Encapsulate(part.bounds);
            }

            CombatVfx.Instance.DeathBurst(bounds.center, Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.y), 0.6f, 4f));
        }
    }
}
