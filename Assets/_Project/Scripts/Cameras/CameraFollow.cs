using UnityEngine;

namespace Cerberus.Cameras
{
    /// <summary>
    /// 鏡頭跟隨：掛在攝影機的父物件上，平滑追著目標，並限制畫面不超出關卡範圍。
    /// 攝影機本身的 localPosition 留給 CameraShake 做震動偏移，兩者互不干擾。
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Camera viewCamera;

        [Tooltip("畫面中心相對目標的位置，y 為正代表角色在畫面偏下方，看得到更多上方空間")]
        [SerializeField] private Vector2 targetOffset = new Vector2(0f, 1.5f);
        [Tooltip("追上目標所需的大約時間，越小跟得越緊")]
        [SerializeField] private float smoothTime = 0.15f;

        [Tooltip("關卡範圍（世界座標），畫面邊緣不會超出這個範圍")]
        [SerializeField] private Rect levelBounds = new Rect(-1f, -4f, 112f, 24f);

        private Vector2 velocity;

        private void Start()
        {
            // 一開始直接對準，不要從場景原點滑過去
            transform.position = GetRigPosition(GetClampedViewCenter());
        }

        private void LateUpdate()
        {
            Vector2 current = transform.position;
            Vector2 desired = GetRigPosition(GetClampedViewCenter());
            Vector2 next = Vector2.SmoothDamp(current, desired, ref velocity, smoothTime);
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }

        private Vector2 GetClampedViewCenter()
        {
            Vector2 center = (Vector2)target.position + targetOffset;
            float halfHeight = viewCamera.orthographicSize;
            float halfWidth = halfHeight * viewCamera.aspect;

            center.x = ClampAxis(center.x, levelBounds.xMin + halfWidth, levelBounds.xMax - halfWidth);
            center.y = ClampAxis(center.y, levelBounds.yMin + halfHeight, levelBounds.yMax - halfHeight);
            return center;
        }

        // 關卡比畫面還小時置中
        private static float ClampAxis(float value, float min, float max) =>
            min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);

        // 攝影機在父物件下可能有偏移，換算成父物件該在的位置
        private Vector2 GetRigPosition(Vector2 viewCenter) =>
            viewCenter - (Vector2)viewCamera.transform.localPosition;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(levelBounds.center, levelBounds.size);
        }
    }
}
