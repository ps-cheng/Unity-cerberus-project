using UnityEngine;

namespace Cerberus.Level
{
    /// <summary>
    /// 關卡觸發區的共同部分：每個 FixedUpdate 用 OverlapBox 檢查玩家在不在範圍內，進出時通知子類別。
    /// 跟 LevelGoal 一樣不依賴碰撞事件，範圍在 Scene 視窗用綠框顯示。
    /// </summary>
    public abstract class PlayerTriggerZone : MonoBehaviour
    {
        [Header("Trigger")]
        [SerializeField] private Vector2 triggerOffset = new Vector2(0f, 1f);
        [SerializeField] private Vector2 triggerSize = new Vector2(2f, 3f);
        [SerializeField] private LayerMask playerLayers;

        protected bool IsPlayerInside { get; private set; }
        protected Collider2D PlayerCollider { get; private set; }

        protected virtual void FixedUpdate()
        {
            Collider2D found = Physics2D.OverlapBox((Vector2)transform.position + triggerOffset, triggerSize, 0f, playerLayers);
            bool inside = found != null;

            if (inside && !IsPlayerInside)
            {
                PlayerCollider = found;
                IsPlayerInside = true;
                OnPlayerEnter(found);
            }
            else if (!inside && IsPlayerInside)
            {
                IsPlayerInside = false;
                OnPlayerExit();
            }
        }

        protected virtual void OnPlayerEnter(Collider2D player) { }

        protected virtual void OnPlayerExit() { }

        protected virtual Color GizmoColor => new Color(0.4f, 1f, 0.4f, 0.6f);

        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor;
            Gizmos.DrawWireCube((Vector2)transform.position + triggerOffset, triggerSize);
        }
    }
}
