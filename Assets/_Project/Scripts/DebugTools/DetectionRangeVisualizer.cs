using Cerberus.Enemies;
using UnityEngine;

namespace Cerberus.DebugTools
{
    /// <summary>
    /// 用 LineRenderer 畫出敵人的偵測圓圈。平常是綠色，發現玩家後變紅色。
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class DetectionRangeVisualizer : MonoBehaviour
    {
        [SerializeField] private EnemyBase enemy;
        [SerializeField] private int segments = 48;
        [SerializeField] private Color idleColor = new Color(0.3f, 1f, 0.3f, 0.5f);
        [SerializeField] private Color alertColor = new Color(1f, 0.3f, 0.3f, 0.8f);

        private LineRenderer lineRenderer;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = segments;

            // 用本地座標畫一次即可，圓圈會跟著敵人移動
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                lineRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * enemy.DetectionRadius);
            }
        }

        private void LateUpdate()
        {
            Color color = enemy.HasTarget ? alertColor : idleColor;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }
}
