using System;
using Cerberus.Player;
using UnityEngine;

namespace Cerberus.Level
{
    /// <summary>
    /// 關卡終點的門：玩家進入範圍就算通關，停止玩家操作並觸發 Reached（通關訊息等由訂閱者處理）。
    /// 用 OverlapBox 偵測，跟其他判定一樣不依賴碰撞事件。
    /// </summary>
    public class LevelGoal : MonoBehaviour
    {
        [SerializeField] private Vector2 triggerOffset = new Vector2(0f, 1.1f);
        [SerializeField] private Vector2 triggerSize = new Vector2(1.2f, 2.2f);
        [SerializeField] private LayerMask playerLayers;

        public bool IsReached { get; private set; }

        public event Action Reached;

        private void FixedUpdate()
        {
            if (IsReached)
                return;

            Collider2D player = Physics2D.OverlapBox((Vector2)transform.position + triggerOffset, triggerSize, 0f, playerLayers);
            if (player == null)
                return;

            IsReached = true;

            PlayerController controller = player.GetComponentInParent<PlayerController>();
            if (controller != null)
                controller.SetInputEnabled(false);

            Reached?.Invoke();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = IsReached ? Color.green : new Color(0.4f, 1f, 0.4f, 0.6f);
            Gizmos.DrawWireCube((Vector2)transform.position + triggerOffset, triggerSize);
        }
    }
}
