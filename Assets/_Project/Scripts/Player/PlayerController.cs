using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 角色的總指揮：讀取輸入，決定此刻允許哪些行為，再把指令分派給移動與戰鬥模組。
    /// 「攻擊中不能跳」「受傷時不能操作」這類規則都集中寫在這裡。
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler), typeof(PlayerMovement), typeof(PlayerCombat))]
    [RequireComponent(typeof(PlayerDamageReceiver))]
    public class PlayerController : MonoBehaviour
    {
        private PlayerInputHandler input;
        private PlayerMovement movement;
        private PlayerCombat combat;
        private PlayerDamageReceiver damageReceiver;

        private void Awake()
        {
            input = GetComponent<PlayerInputHandler>();
            movement = GetComponent<PlayerMovement>();
            combat = GetComponent<PlayerCombat>();
            damageReceiver = GetComponent<PlayerDamageReceiver>();
        }

        private void Update()
        {
            if (damageReceiver.IsStunned)
            {
                movement.SetMoveInput(0f, allowTurn: false);
                return;
            }

            HandleCombatInput();
            HandleMovementInput();
        }

        private void HandleCombatInput()
        {
            // 同一幀同時按下時，強攻擊優先
            if (input.HeavyAttackPressed)
                StartAttack(CombatAction.HeavyAttack);
            else if (input.LightAttackPressed)
                StartAttack(CombatAction.LightAttack);

            combat.UpdateSpecial(input.SpecialPressed, input.SpecialHeld, movement.FacingDirection);
        }

        private void StartAttack(CombatAction attackType)
        {
            if (combat.TryStartAttack(attackType, movement.FacingDirection))
                movement.CancelJumpRequest();
        }

        private void HandleMovementInput()
        {
            // 地面上出招或使用會定身的特殊動作時原地站定；空中則保留水平慣性與方向控制
            bool actionLocksMovement = combat.IsAttacking || (combat.IsSpecialActive && combat.SpecialLocksMovement);
            bool isRooted = actionLocksMovement && movement.IsGrounded;
            movement.SetMoveInput(isRooted ? 0f : input.MoveInput, allowTurn: !combat.IsBusy);

            if (input.JumpPressed && !combat.IsBusy)
                movement.RequestJump();

            movement.SetJumpHeld(input.JumpHeld);
        }
    }
}
