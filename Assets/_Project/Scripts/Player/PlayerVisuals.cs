using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 角色外觀中不屬於動畫的部分：依面向翻轉、受傷後的無敵閃爍、死亡時隱藏。
    /// 動畫本身由 PlayerAnimator 驅動。
    /// </summary>
    public class PlayerVisuals : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerDamageReceiver damageReceiver;
        [Tooltip("所有外觀物件的父節點，翻轉面向時只翻這層")]
        [SerializeField] private Transform visualRoot;

        [Header("Invincibility Blink")]
        [SerializeField] private float blinkFrequency = 15f;

        private SpriteRenderer[] visualRenderers;

        private void Awake() => visualRenderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);

        private void LateUpdate()
        {
            visualRoot.localScale = new Vector3(movement.FacingDirection, 1f, 1f);
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            bool isVisible = !damageReceiver.IsDead;

            // 無敵時間內閃爍
            if (isVisible && damageReceiver.IsBlinking)
                isVisible = Mathf.FloorToInt(Time.time * blinkFrequency) % 2 == 0;

            foreach (SpriteRenderer spriteRenderer in visualRenderers)
                spriteRenderer.enabled = isVisible;
        }
    }
}
