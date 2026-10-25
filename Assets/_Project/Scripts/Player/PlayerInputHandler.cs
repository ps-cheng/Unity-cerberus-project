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
        private InputAction specialAction;

        public float MoveInput => moveAction.ReadValue<float>();
        public bool JumpPressed => jumpAction.WasPressedThisFrame();
        public bool JumpHeld => jumpAction.IsPressed();
        public bool LightAttackPressed => lightAttackAction.WasPressedThisFrame();
        public bool HeavyAttackPressed => heavyAttackAction.WasPressedThisFrame();
        public bool SpecialPressed => specialAction.WasPressedThisFrame();
        public bool SpecialHeld => specialAction.IsPressed();

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
            lightAttackAction.AddBinding("<Keyboard>/z");
            lightAttackAction.AddBinding("<Mouse>/leftButton");
            lightAttackAction.AddBinding("<Gamepad>/buttonWest");

            heavyAttackAction = new InputAction("HeavyAttack", InputActionType.Button);
            heavyAttackAction.AddBinding("<Keyboard>/k");
            heavyAttackAction.AddBinding("<Keyboard>/x");
            heavyAttackAction.AddBinding("<Mouse>/rightButton");
            heavyAttackAction.AddBinding("<Gamepad>/buttonNorth");

            // 特殊動作依角色而不同（第一顆頭是格檔），所以只叫 Special
            specialAction = new InputAction("Special", InputActionType.Button);
            specialAction.AddBinding("<Keyboard>/l");
            specialAction.AddBinding("<Keyboard>/c");
            specialAction.AddBinding("<Gamepad>/leftShoulder");
        }

        private void OnEnable()
        {
            moveAction.Enable();
            jumpAction.Enable();
            lightAttackAction.Enable();
            heavyAttackAction.Enable();
            specialAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
            jumpAction.Disable();
            lightAttackAction.Disable();
            heavyAttackAction.Disable();
            specialAction.Disable();
        }

        private void OnDestroy()
        {
            moveAction.Dispose();
            jumpAction.Dispose();
            lightAttackAction.Dispose();
            heavyAttackAction.Dispose();
            specialAction.Dispose();
        }
    }
}
