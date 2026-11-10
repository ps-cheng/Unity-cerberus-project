using UnityEngine;

namespace Cerberus.Enemies.Boss
{
    /// <summary>
    /// 一隻觸手：負責伸出/收回的外觀，以及尖端雷射倒數時的顏色與膨脹提示。
    /// 雷射本身的倒數、發射、反射全部交給 LaserEmitter。
    /// </summary>
    public class BossTentacle : MonoBehaviour
    {
        [SerializeField] private LaserEmitter laser;
        [SerializeField] private SpriteRenderer tipRenderer;
        [Tooltip("充能時尖端膨脹的比例")]
        [SerializeField] private float chargeSwellScale = 1.6f;

        public LaserEmitter Laser => laser;

        private float extension;
        private float targetExtension;
        private float extendSpeed = float.PositiveInfinity;
        private Color tipBaseColor;
        private Vector3 tipBaseScale;

        private void Awake()
        {
            tipBaseColor = tipRenderer.color;
            tipBaseScale = tipRenderer.transform.localScale;
            ApplyExtension();
        }

        public void SetExtended(bool isExtended, float duration)
        {
            targetExtension = isExtended ? 1f : 0f;
            extendSpeed = duration > 0f ? 1f / duration : float.PositiveInfinity;
        }

        private void Update()
        {
            extension = Mathf.MoveTowards(extension, targetExtension, extendSpeed * Time.deltaTime);
            ApplyExtension();

            tipRenderer.color = laser.EvaluateTelegraph(tipBaseColor);
            tipRenderer.transform.localScale = tipBaseScale * Mathf.Lerp(1f, chargeSwellScale, laser.ChargeProgress);
        }

        // 沿觸手自己的方向縮放，尖端（雷射發射點）會跟著伸出去
        private void ApplyExtension()
        {
            transform.localScale = new Vector3(1f, Mathf.Max(extension, 0.001f), 1f);
            tipRenderer.enabled = extension > 0.05f;
        }
    }
}
