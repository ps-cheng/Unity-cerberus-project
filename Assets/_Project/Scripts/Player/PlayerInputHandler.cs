using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 集中管理玩家輸入。其他元件只讀取這裡公開的狀態，不直接碰 Input System。
    /// 鍵位設定在 Assets/_Project/Input/CerberusControls.inputactions（雙擊開啟編輯器），
    /// CerberusControls 類別是 Unity 依該資產自動產生的，不要手動修改。
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        private CerberusControls controls;
        private CerberusControls.GameplayActions gameplay;

        public float MoveInput => gameplay.Move.ReadValue<float>();
        public bool JumpPressed => gameplay.Jump.WasPressedThisFrame();
        public bool JumpHeld => gameplay.Jump.IsPressed();
        public bool LightAttackPressed => gameplay.LightAttack.WasPressedThisFrame();
        public bool HeavyAttackPressed => gameplay.HeavyAttack.WasPressedThisFrame();
        // 特殊動作依角色而不同（第一顆頭是格檔），所以只叫 Special
        public bool SpecialPressed => gameplay.Special.WasPressedThisFrame();
        public bool SpecialHeld => gameplay.Special.IsPressed();

        private void Awake()
        {
            controls = new CerberusControls();
            gameplay = controls.Gameplay;
        }

        private void OnEnable() => gameplay.Enable();

        private void OnDisable() => gameplay.Disable();

        private void OnDestroy() => controls.Dispose();
    }
}
