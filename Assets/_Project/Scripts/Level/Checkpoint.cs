using Cerberus.Player;
using UnityEngine;

namespace Cerberus.Level
{
    /// <summary>
    /// 重生點：玩家經過時把重生位置設在這裡，指示燈從暗轉亮。
    /// 之後死亡會回到最後經過的重生點。
    /// </summary>
    public class Checkpoint : PlayerTriggerZone
    {
        [Tooltip("重生時玩家出現的位置（相對這個物件）")]
        [SerializeField] private Vector2 respawnOffset = new Vector2(0f, 0.8f);

        [Header("Indicator")]
        [SerializeField] private SpriteRenderer indicator;
        [SerializeField] private Color inactiveColor = new Color(0.25f, 0.25f, 0.3f);
        [SerializeField] private Color activeColor = new Color(0.43f, 0.89f, 0.78f);

        public bool IsActivated { get; private set; }

        private void Awake()
        {
            if (indicator != null)
                indicator.color = inactiveColor;
        }

        protected override void OnPlayerEnter(Collider2D player)
        {
            if (IsActivated)
                return;

            PlayerDamageReceiver receiver = player.GetComponentInParent<PlayerDamageReceiver>();
            if (receiver == null)
                return;

            receiver.SetRespawnPoint((Vector2)transform.position + respawnOffset);
            IsActivated = true;

            if (indicator != null)
                indicator.color = activeColor;
        }

        protected override Color GizmoColor => new Color(0.43f, 0.89f, 0.78f, 0.7f);
    }
}
