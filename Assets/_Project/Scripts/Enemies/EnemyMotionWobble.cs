using UnityEngine;

namespace Cerberus.Enemies
{
    /// <summary>
    /// 用程式做的簡易動態，取代逐格動畫：掛在敵人外觀的圖片物件上（不是根物件，免得影響碰撞）。
    /// - 上下浮動：無人機懸停（靜止時也有）、履帶機器人行進時的顛簸（移動越快越明顯）
    /// - 前後傾斜：依水平速度往行進方向傾
    /// 只改這個物件自己的 localPosition / localRotation，面向翻轉和縮放仍由 EnemyBase 處理。
    /// </summary>
    public class EnemyMotionWobble : MonoBehaviour
    {
        [Tooltip("讀取速度用，留空會往父物件找")]
        [SerializeField] private Rigidbody2D body;

        [Header("上下浮動")]
        [Tooltip("靜止時的浮動幅度（單位）。無人機懸停用，地面機器人設 0")]
        [SerializeField] private float idleBobAmplitude = 0f;
        [Tooltip("全速移動時額外增加的浮動幅度")]
        [SerializeField] private float moveBobAmplitude = 0.04f;
        [Tooltip("每秒上下幾次")]
        [SerializeField] private float bobFrequency = 8f;
        [Tooltip("速度達到這個值時視為全速")]
        [SerializeField] private float fullSpeed = 4f;

        [Header("傾斜")]
        [Tooltip("全速時往行進方向傾斜的角度")]
        [SerializeField] private float maxTiltAngle = 0f;
        [Tooltip("傾斜跟上速度變化的快慢")]
        [SerializeField] private float tiltResponsiveness = 8f;

        private Vector3 baseLocalPosition;
        private float phase;
        private float tilt;

        private void Awake()
        {
            if (body == null)
                body = GetComponentInParent<Rigidbody2D>();

            baseLocalPosition = transform.localPosition;
            // 同一種敵人放好幾隻時，起始相位錯開，不會整齊劃一地一起晃
            phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            Vector2 velocity = body != null && body.simulated ? body.linearVelocity : Vector2.zero;
            float speed01 = fullSpeed > 0f ? Mathf.Clamp01(Mathf.Abs(velocity.x) / fullSpeed) : 0f;

            phase += Time.deltaTime * bobFrequency * Mathf.PI * 2f;
            float amplitude = idleBobAmplitude + moveBobAmplitude * speed01;
            transform.localPosition = baseLocalPosition + Vector3.up * (Mathf.Sin(phase) * amplitude);

            // 父物件翻面時 localScale.x 是 -1，傾斜方向用世界速度換算回本地
            float facing = Mathf.Sign(transform.lossyScale.x);
            float targetTilt = -Mathf.Sign(velocity.x) * facing * maxTiltAngle * speed01;
            tilt = Mathf.Lerp(tilt, targetTilt, 1f - Mathf.Exp(-tiltResponsiveness * Time.deltaTime));
            transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
        }
    }
}
