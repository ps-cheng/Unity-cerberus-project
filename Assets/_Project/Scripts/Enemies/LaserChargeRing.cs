using UnityEngine;

namespace Cerberus.Enemies
{
    /// <summary>
    /// 雷射發射前的倒數圈：畫在槍口上，瞄準時出現，充能期間從大圈收縮到槍口，收到最小就是發射的瞬間。
    /// 只讀 LaserEmitter 的狀態，不影響雷射的時間和判定。掛在槍口位置的物件上（通常就是 LaserEmitter 的 firePoint）。
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LaserChargeRing : MonoBehaviour
    {
        [SerializeField] private LaserEmitter laser;

        [Header("外觀")]
        [Tooltip("瞄準階段（還沒開始充能）圈的半徑")]
        [SerializeField] private float startRadius = 0.6f;
        [Tooltip("充能結束（發射瞬間）圈的半徑")]
        [SerializeField] private float endRadius = 0.08f;
        [SerializeField] private float lineWidth = 0.04f;
        [SerializeField] private Color aimColor = new Color(1f, 0.25f, 0.3f, 0.6f);
        [SerializeField] private Color lockedColor = Color.white;
        [SerializeField, Range(12, 64)] private int segments = 32;

        private LineRenderer ring;

        private void Awake()
        {
            ring = GetComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = segments;
            ring.enabled = false;
        }

        private void LateUpdate()
        {
            bool isWindingUp = laser.CurrentPhase == LaserEmitter.Phase.Aim || laser.CurrentPhase == LaserEmitter.Phase.Charge;
            ring.enabled = isWindingUp;
            if (!isWindingUp)
                return;

            float radius = Mathf.Lerp(startRadius, endRadius, laser.ChargeProgress);
            Color color = laser.IsAimLocked ? lockedColor : aimColor;
            ring.startColor = ring.endColor = color;
            ring.startWidth = ring.endWidth = lineWidth;

            // 父物件翻面時圈不受影響（圓形左右對稱）
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }
    }
}
