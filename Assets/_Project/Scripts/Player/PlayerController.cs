using Cerberus.Combat;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 角色的總指揮：讀取輸入，決定此刻允許哪些行為，再把指令分派給移動與戰鬥模組。
    /// 「攻擊中不能跳」這類規則都集中寫在這裡。
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler), typeof(PlayerMovement), typeof(PlayerCombat))]
    public class PlayerController : MonoBehaviour
    {
        private PlayerInputHandler input;
        private PlayerMovement movement;
        private PlayerCombat combat;

        private void Awake()
        {
            input = GetComponent<PlayerInputHandler>();
            movement = GetComponent<PlayerMovement>();
            combat = GetComponent<PlayerCombat>();
        }

        private void Update()
        {
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

            combat.UpdateBlock(input.BlockHeld, movement.FacingDirection);
        }

        private void StartAttack(CombatAction attackType)
        {
            if (combat.TryStartAttack(attackType, movement.FacingDirection))
                movement.CancelJumpRequest();
        }

        private void HandleMovementInput()
        {
            // 地面上出招或格檔時原地站定；空中出招則保留水平慣性與方向控制
            bool isRooted = combat.IsBusy && movement.IsGrounded;
            movement.SetMoveInput(isRooted ? 0f : input.MoveInput, allowTurn: !combat.IsBusy);

            if (input.JumpPressed && !combat.IsBusy)
                movement.RequestJump();

            movement.SetJumpHeld(input.JumpHeld);
        }
    }
}
