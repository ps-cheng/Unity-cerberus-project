using UnityEngine;

namespace Cerberus.Enemies.Boss
{
    /// <summary>
    /// 塔洛斯的零件動畫，全部用程式驅動，不需要逐格動畫：
    /// - 待機呼吸：軀幹上下起伏，持盾手臂跟著微微擺動
    /// - 走路：依水平速度擺動兩條腿，軀幹隨步伐上下
    /// - 腳踝弱點：平時暗著，進入破綻（Exposed）時脈動發光
    /// 揮刀角度、舉盾、整體縮放仍由 BossController 和各狀態控制，這裡只加在它們之上，不改任何戰鬥邏輯。
    /// </summary>
    public class TalosRig : MonoBehaviour
    {
        [SerializeField] private BossController boss;
        [SerializeField] private Rigidbody2D body;

        [Header("零件")]
        [Tooltip("軀幹（含頭和兩隻手臂）的父節點，呼吸和走路起伏都加在這層")]
        [SerializeField] private Transform torso;
        [SerializeField] private Transform shieldArm;
        [SerializeField] private Transform frontLeg;
        [SerializeField] private Transform backLeg;

        [Header("待機呼吸")]
        [SerializeField] private float breathAmplitude = 0.05f;
        [Tooltip("呼吸一次的秒數")]
        [SerializeField] private float breathPeriod = 2.2f;
        [SerializeField] private float shieldArmSwayAngle = 2.5f;

        [Header("走路")]
        [Tooltip("全速時腿前後擺動的角度")]
        [SerializeField] private float legSwingAngle = 18f;
        [Tooltip("每移動 1 單位，步伐循環前進多少（弧度）")]
        [SerializeField] private float strideRate = 2.2f;
        [SerializeField] private float stepBobAmplitude = 0.08f;
        [Tooltip("速度達到這個值時腿擺到最大")]
        [SerializeField] private float fullSpeed = 3f;

        [Header("腳踝弱點")]
        [SerializeField] private SpriteRenderer weakPointRenderer;
        [Tooltip("弱點外圈的光暈（可留空）")]
        [SerializeField] private SpriteRenderer weakPointGlow;
        [SerializeField] private Color weakPointDimColor = new Color(0.15f, 0.3f, 0.32f);
        [SerializeField] private Color weakPointLitColor = new Color(0.45f, 1f, 1f);
        [Tooltip("破綻時每秒閃幾次")]
        [SerializeField] private float weakPointPulseFrequency = 2.5f;
        [SerializeField] private float glowMaxScale = 1.6f;

        private Vector3 torsoBasePosition;
        private Quaternion shieldArmBaseRotation;
        private Quaternion frontLegBaseRotation;
        private Quaternion backLegBaseRotation;
        private Vector3 glowBaseScale;
        private float stridePhase;

        private void Awake()
        {
            torsoBasePosition = torso.localPosition;
            shieldArmBaseRotation = shieldArm.localRotation;
            frontLegBaseRotation = frontLeg.localRotation;
            backLegBaseRotation = backLeg.localRotation;
            if (weakPointGlow != null)
                glowBaseScale = weakPointGlow.transform.localScale;
        }

        private void LateUpdate()
        {
            float speed = body.simulated ? Mathf.Abs(body.linearVelocity.x) : 0f;
            float speed01 = Mathf.Clamp01(speed / fullSpeed);

            // 步伐依實際移動距離推進，速度變化時腳步不會打滑
            stridePhase += speed * strideRate * Time.deltaTime;
            float stride = Mathf.Sin(stridePhase) * speed01;

            frontLeg.localRotation = frontLegBaseRotation * Quaternion.Euler(0f, 0f, stride * legSwingAngle);
            backLeg.localRotation = backLegBaseRotation * Quaternion.Euler(0f, 0f, -stride * legSwingAngle);

            float breath = Mathf.Sin(Time.time * Mathf.PI * 2f / breathPeriod);
            float stepBob = Mathf.Abs(Mathf.Sin(stridePhase)) * stepBobAmplitude * speed01;
            torso.localPosition = torsoBasePosition + Vector3.up * (breath * breathAmplitude * (1f - speed01) - stepBob);
            shieldArm.localRotation = shieldArmBaseRotation * Quaternion.Euler(0f, 0f, breath * shieldArmSwayAngle);

            UpdateWeakPoint();
        }

        private void UpdateWeakPoint()
        {
            bool isExposed = boss.CurrentState == boss.exposed;
            float pulse = isExposed ? 0.5f + 0.5f * Mathf.Sin(Time.time * weakPointPulseFrequency * Mathf.PI * 2f) : 0f;

            weakPointRenderer.color = isExposed ? Color.Lerp(weakPointLitColor * 0.8f, weakPointLitColor, pulse) : weakPointDimColor;
            weakPointRenderer.color = new Color(weakPointRenderer.color.r, weakPointRenderer.color.g, weakPointRenderer.color.b, 1f);

            if (weakPointGlow == null)
                return;

            weakPointGlow.enabled = isExposed;
            weakPointGlow.transform.localScale = glowBaseScale * Mathf.Lerp(1f, glowMaxScale, pulse);
            Color glow = weakPointLitColor;
            glow.a = Mathf.Lerp(0.35f, 0.8f, pulse);
            weakPointGlow.color = glow;
        }
    }
}
