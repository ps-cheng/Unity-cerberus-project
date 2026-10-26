using Cerberus.Combat;
using UnityEngine;
using UnityEngine.Serialization;

namespace Cerberus.DebugTools
{
    /// <summary>
    /// 試玩時在 Game 視窗畫出攻擊/特殊動作範圍，玩家與敵人共用。
    /// 用 SpriteRenderer 而不是 Gizmos，所以不需要開 Gizmos 也看得到。
    /// 顏色：黃 = 前搖（範圍預告）、紅 = 判定中、灰 = 硬直、藍 = 特殊動作。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CombatZoneVisualizer : MonoBehaviour
    {
        [Tooltip("需實作 ICombatZoneSource（例如 PlayerCombat、BasicEnemy）")]
        [FormerlySerializedAs("combat")]
        [SerializeField] private MonoBehaviour zoneSource;
        [SerializeField, Range(0f, 1f)] private float alpha = 0.35f;

        [SerializeField] private Color startupColor = Color.yellow;
        [SerializeField] private Color activeColor = Color.red;
        [SerializeField] private Color recoveryColor = Color.gray;
        [SerializeField] private Color specialColor = Color.cyan;

        private ICombatZoneSource source;
        private SpriteRenderer zoneRenderer;

        private void Awake()
        {
            source = (ICombatZoneSource)zoneSource;
            zoneRenderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            bool hasZone = source.TryGetCurrentZone(out Vector2 center, out Vector2 size);
            zoneRenderer.enabled = hasZone;

            if (!hasZone)
                return;

            // 使用 1x1 單位的方形 Sprite，所以 scale 直接等於範圍大小
            transform.position = center;
            transform.localScale = size;

            Color color = GetPhaseColor();
            color.a = alpha;
            zoneRenderer.color = color;
        }

        private Color GetPhaseColor()
        {
            if (source.IsSpecialActive)
                return specialColor;

            return source.CurrentPhase switch
            {
                ActionPhase.Startup => startupColor,
                ActionPhase.Active => activeColor,
                _ => recoveryColor
            };
        }
    }
}
