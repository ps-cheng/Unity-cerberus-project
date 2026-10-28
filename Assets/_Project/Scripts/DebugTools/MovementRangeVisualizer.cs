using Cerberus.Enemies;
using UnityEngine;

namespace Cerberus.DebugTools
{
    /// <summary>
    /// 畫出敵人的活動範圍（例如飛行敵人來回橫移的兩個端點）。
    /// 範圍以世界座標固定，不會跟著敵人移動；物件仍放在敵人底下，敵人死亡時一起消失。
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class MovementRangeVisualizer : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour rangeProvider;

        private void Start()
        {
            // 放在 Start：等敵人在 Awake 記錄好出生點後再取範圍
            Vector3[] points = ((IMovementRangeProvider)rangeProvider).GetMovementRangePoints();

            LineRenderer lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);
        }
    }
}
