using UnityEngine;

namespace Cerberus.Level
{
    /// <summary>
    /// 攝影機在指定的水平範圍內才顯示這些 Renderer，邊界附近淡入淡出。
    /// 用於只屬於某一區的圖層，例如街道的近景剪影進了保管庫就不該出現。
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class CameraRangeVisibility : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] targets;
        [Tooltip("攝影機 x 在這個範圍內時完全顯示")]
        [SerializeField] private Vector2 visibleRangeX = new Vector2(-1000f, 112f);
        [Tooltip("範圍外多遠淡出完畢")]
        [SerializeField] private float fadeDistance = 4f;

        private Camera viewCamera;
        private float[] baseAlpha;

        private void Awake()
        {
            viewCamera = Camera.main;
            baseAlpha = new float[targets.Length];
            for (int i = 0; i < targets.Length; i++)
                baseAlpha[i] = targets[i].color.a;
        }

        private void LateUpdate()
        {
            float x = viewCamera.transform.position.x;
            float outside = Mathf.Max(visibleRangeX.x - x, x - visibleRangeX.y, 0f);
            float visibility = fadeDistance > 0f ? 1f - Mathf.Clamp01(outside / fadeDistance) : (outside > 0f ? 0f : 1f);

            for (int i = 0; i < targets.Length; i++)
            {
                Color c = targets[i].color;
                c.a = baseAlpha[i] * visibility;
                targets[i].color = c;
                targets[i].enabled = visibility > 0f;
            }
        }
    }
}
