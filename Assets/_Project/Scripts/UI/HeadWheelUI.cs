using Cerberus.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Cerberus.UI
{
    /// <summary>
    /// 換頭轉盤：中間是目前的頭號碼，左右各一個較小的相鄰號碼，換頭時整圈像轉盤一樣轉過去。
    /// 號碼排在一段圓弧上，離中間越遠越小、越淡，並稍微跟著圓弧傾斜。
    /// 轉盤位置是連續的數值，每個格子依位置換算成頭號碼，所以只有兩顆頭時左右兩邊都會顯示另一顆，
    /// 而且按 Q / E 會往不同方向轉。
    /// </summary>
    public class HeadWheelUI : MonoBehaviour
    {
        // 中間左右各算兩格：一格完整顯示，外面一格只在轉動途中淡入淡出
        private const int SlotRange = 2;

        [SerializeField] private PlayerCombat combat;
        [SerializeField] private Font font;

        [Header("Layout")]
        [Tooltip("號碼排列的圓弧半徑")]
        [SerializeField] private float radius = 70f;
        [Tooltip("相鄰兩個號碼在圓弧上相隔的角度")]
        [SerializeField] private float slotAngle = 50f;
        [SerializeField] private int fontSize = 44;
        [Tooltip("左右號碼相對中間號碼的大小")]
        [SerializeField, Range(0f, 1f)] private float sideScale = 0.6f;
        [Tooltip("號碼跟著圓弧傾斜的比例，1 = 完全沿著圓弧轉，0 = 永遠保持直立")]
        [SerializeField, Range(0f, 1f)] private float labelTilt = 0.35f;

        [Header("Colors")]
        [SerializeField] private Color centerColor = Color.white;
        [SerializeField] private Color sideColor = new Color(1f, 1f, 1f, 0.65f);

        [Header("Animation")]
        [Tooltip("數值越大轉得越快；用不受 hit-stop 影響的真實時間")]
        [SerializeField] private float rotateSpeed = 14f;

        private Text[] labels;
        private string[] numberTexts;
        private float displayPosition;
        private int targetPosition;

        private void Awake()
        {
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            labels = new Text[SlotRange * 2 + 1];
            for (int i = 0; i < labels.Length; i++)
                labels[i] = CreateLabel();
        }

        private void Start()
        {
            numberTexts = new string[combat.Heads.Count];
            for (int i = 0; i < numberTexts.Length; i++)
                numberTexts[i] = (i + 1).ToString();

            targetPosition = combat.CurrentHeadIndex;
            displayPosition = targetPosition;
        }

        private void OnEnable() => combat.HeadChanged += HandleHeadChanged;

        private void OnDisable() => combat.HeadChanged -= HandleHeadChanged;

        private void HandleHeadChanged(int index, int step) => targetPosition += step;

        private void Update()
        {
            displayPosition = Mathf.Lerp(displayPosition, targetPosition, 1f - Mathf.Exp(-rotateSpeed * Time.unscaledDeltaTime));
            if (Mathf.Abs(displayPosition - targetPosition) < 0.001f)
                displayPosition = targetPosition;

            int baseSlot = Mathf.RoundToInt(displayPosition);
            for (int i = 0; i < labels.Length; i++)
            {
                int slot = baseSlot + i - SlotRange;
                LayoutLabel(labels[i], slot - displayPosition, Wrap(slot, numberTexts.Length));
            }
        }

        /// <param name="offset">與轉盤中心相差幾格，正數在右邊</param>
        private void LayoutLabel(Text label, float offset, int headIndex)
        {
            float angle = offset * slotAngle;
            float radians = angle * Mathf.Deg2Rad;
            float distance = Mathf.Abs(offset);
            float closeness = 1f - Mathf.Clamp01(distance);

            RectTransform rect = label.rectTransform;
            rect.anchoredPosition = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians) - 1f) * radius;
            rect.localRotation = Quaternion.Euler(0f, 0f, -angle * labelTilt);
            rect.localScale = Vector3.one * Mathf.Lerp(sideScale, 1f, closeness);

            // 超過一格的位置逐漸淡出，轉動時外側號碼不會突然出現或消失
            Color color = Color.Lerp(sideColor, centerColor, closeness);
            color.a *= 1f - Mathf.Clamp01(distance - 1f);
            label.color = color;
            label.text = numberTexts[headIndex];
        }

        private Text CreateLabel()
        {
            var labelObject = new GameObject("Number", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(transform, false);

            var label = labelObject.GetComponent<Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.rectTransform.sizeDelta = new Vector2(fontSize * 1.5f, fontSize * 1.5f);
            return label;
        }

        private static int Wrap(int value, int count) => (value % count + count) % count;
    }
}
