using UnityEngine;
using UnityEngine.InputSystem;

namespace Cerberus.Player
{
    /// <summary>
    /// 集中管理玩家輸入。其他元件只讀取這裡公開的狀態，不直接碰 Input System，
    /// 之後要改鍵位或加入三頭犬切換鍵時只需要改這支。
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction lightAttackAction;
        private InputAction heavyAttackAction;
        private InputAction blockAction;

        public float MoveInput => moveAction.ReadValue<float>();
        public bool JumpPressed => jumpAction.WasPressedThisFrame();
        public bool JumpHeld => jumpAction.IsPressed();
        public bool LightAttackPressed => lightAttackAction.WasPressedThisFrame();
        public bool HeavyAttackPressed => heavyAttackAction.WasPressedThisFrame();
        public bool BlockHeld => blockAction.IsPressed();

        private void Awake()
        {
            // 用程式碼建立 Action，鍵位一目了然，也不依賴 .inputactions 資產
            moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Axis");
            moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick/x");
            moveAction.AddBinding("<Gamepad>/dpad/x");

            jumpAction = new InputAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");

            lightAttackAction = new InputAction("LightAttack", InputActionType.Button);
            lightAttackAction.AddBinding("<Keyboard>/j");
            lightAttackAction.AddBinding("<Mouse>/leftButton");
            lightAttackAction.AddBinding("<Gamepad>/buttonWest");

            heavyAttackAction = new InputAction("HeavyAttack", InputActionType.Button);
            heavyAttackAction.AddBinding("<Keyboard>/k");
            heavyAttackAction.AddBinding("<Mouse>/rightButton");
            heavyAttackAction.AddBinding("<Gamepad>/buttonNorth");

            blockAction = new InputAction("Block", InputActionType.Button);
            blockAction.AddBinding("<Keyboard>/l");
            blockAction.AddBinding("<Gamepad>/leftShoulder");
        }

        private void OnEnable()
        {
            moveAction.Enable();
            jumpAction.Enable();
            lightAttackAction.Enable();
            heavyAttackAction.Enable();
            blockAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
            jumpAction.Disable();
            lightAttackAction.Disable();
            heavyAttackAction.Disable();
            blockAction.Disable();
        }

        private void OnDestroy()
        {
            moveAction.Dispose();
            jumpAction.Dispose();
            lightAttackAction.Dispose();
            heavyAttackAction.Dispose();
            blockAction.Dispose();
        }
    }
}
