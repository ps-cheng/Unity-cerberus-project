using Cerberus.Combat;
using Cerberus.Player;
using UnityEngine;

namespace Cerberus.DebugTools
{
    /// <summary>
    /// 試玩時在 Game 視窗畫出攻擊/格檔範圍。
    /// 用 SpriteRenderer 而不是 Gizmos，所以不需要開 Gizmos 也看得到。
    /// 顏色：黃 = 前搖（範圍預告）、紅 = 判定中、灰 = 硬直、藍 = 格檔。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CombatZoneVisualizer : MonoBehaviour
    {
        [SerializeField] private PlayerCombat combat;
        [SerializeField, Range(0f, 1f)] private float alpha = 0.35f;

        [SerializeField] private Color startupColor = Color.yellow;
        [SerializeField] private Color activeColor = Color.red;
        [SerializeField] private Color recoveryColor = Color.gray;
        [SerializeField] private Color blockColor = Color.cyan;

        private SpriteRenderer zoneRenderer;

        private void Awake()
        {
            zoneRenderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            zoneRenderer.enabled = combat.IsBusy;

            if (!combat.IsBusy)
                return;

            // 使用 1x1 單位的方形 Sprite，所以 scale 直接等於範圍大小
            transform.position = combat.CurrentZoneCenter;
            transform.localScale = combat.CurrentZoneSize;

            Color color = GetPhaseColor();
            color.a = alpha;
            zoneRenderer.color = color;
        }

        private Color GetPhaseColor()
        {
            if (combat.IsBlocking)
                return blockColor;

            return combat.CurrentPhase switch
            {
                ActionPhase.Startup => startupColor,
                ActionPhase.Active => activeColor,
                _ => recoveryColor
            };
        }
    }
}
