using UnityEngine;

namespace Cerberus.Level
{
    /// <summary>
    /// 平台外觀：依碰撞體的寬度鋪上平台圖（Tiled，左右端點不拉伸、中段重複），圖的頂面對齊碰撞體的頂面。
    /// 碰撞體是唯一的尺寸來源，改平台大小時只要改 Transform 或 BoxCollider2D，外觀會自動跟上（編輯器裡也會）。
    /// 外觀放在子物件上並抵銷父物件的縮放，舊平台用 Transform 縮放做尺寸的寫法可以原封不動保留。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlatformSkin : MonoBehaviour
    {
        [Tooltip("平台圖的 SpriteRenderer，放在子物件上")]
        [SerializeField] private SpriteRenderer skin;
        [Tooltip("圖的頂端比碰撞體頂面高多少（單位）。平台圖上緣通常有一點描邊或欄杆，站立面在它下面一點")]
        [SerializeField] private float surfaceInset = 0.04f;

        [Header("平台本體（碰撞體範圍內、平台圖下方的部分）")]
        [Tooltip("本體的 SpriteRenderer（通常是父物件自己原本的色塊）。浮空平台不需要就關掉")]
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private bool showBody = true;
        [SerializeField] private Color bodyColor = new Color(0.1f, 0.1f, 0.15f);

        private BoxCollider2D box;

        private void OnEnable() => Apply();

#if UNITY_EDITOR
        // 編輯器裡拖動大小、改 Inspector 時即時更新（不用 OnValidate：Unity 不允許在那裡改 SpriteRenderer）。
        // 執行時平台不會變形，只在啟用時套用一次
        private void Update()
        {
            if (!Application.isPlaying)
                Apply();
        }
#endif

        private void Apply()
        {
            if (skin == null || skin.sprite == null)
                return;

            if (box == null)
                box = GetComponent<BoxCollider2D>();

            if (body != null)
            {
                body.enabled = showBody;
                body.color = bodyColor;
            }

            Vector3 scale = transform.lossyScale;
            if (Mathf.Approximately(scale.x, 0f) || Mathf.Approximately(scale.y, 0f))
                return;

            // 碰撞體在世界中的寬度和頂面
            float width = Mathf.Abs(box.size.x * scale.x);
            float top = box.offset.y * scale.y + Mathf.Abs(box.size.y * scale.y) * 0.5f;
            float height = skin.sprite.bounds.size.y;

            skin.drawMode = SpriteDrawMode.Tiled;
            skin.tileMode = SpriteTileMode.Continuous;
            skin.size = new Vector2(width, height);

            // 抵銷父物件縮放，外觀用世界單位計算
            Transform t = skin.transform;
            t.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f);
            float centerY = top + surfaceInset - height * 0.5f;
            t.localPosition = new Vector3(box.offset.x, centerY / scale.y, t.localPosition.z);
            t.localRotation = Quaternion.identity;
        }
    }
}
