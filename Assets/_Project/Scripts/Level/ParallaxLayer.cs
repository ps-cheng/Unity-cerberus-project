using UnityEngine;

namespace Cerberus.Level
{
    /// <summary>
    /// 視差背景的一層：攝影機移動時，這層跟著移動一部分，越遠的層跟得越多、看起來越慢。
    /// 圖要左右接續的話，SpriteRenderer 的 Draw Mode 用 Tiled，寬度拉到蓋過整個關卡。
    /// 排在 CameraFollow 之後執行，用的是攝影機這一幀的最終位置（包含畫面震動）。
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("留空就用 Main Camera")]
        [SerializeField] private Camera viewCamera;

        [Tooltip("這層跟著攝影機移動的比例。0 = 固定在世界上（跟平台一樣），1 = 黏在畫面上不動。遠景接近 1，近景用負值會比平台移動得更快")]
        [SerializeField] private Vector2 followFactor = new Vector2(0.5f, 0.5f);

        private Vector3 startPosition;
        private Vector3 cameraStartPosition;

        private void Start()
        {
            if (viewCamera == null)
                viewCamera = Camera.main;

            startPosition = transform.position;
            cameraStartPosition = viewCamera.transform.position;
        }

        private void LateUpdate()
        {
            Vector3 cameraDelta = viewCamera.transform.position - cameraStartPosition;
            transform.position = new Vector3(
                startPosition.x + cameraDelta.x * followFactor.x,
                startPosition.y + cameraDelta.y * followFactor.y,
                startPosition.z);
        }
    }
}
