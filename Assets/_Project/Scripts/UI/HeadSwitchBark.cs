using System;
using System.Collections.Generic;
using Cerberus.Feedback;
using Cerberus.Player;
using TMPro;
using UnityEngine;

namespace Cerberus.UI
{
    /// <summary>
    /// 換頭台詞：切換頭的瞬間，角色頭上跳出小對話框（名字用識別色），約 1 秒後淡出。
    /// 對話框不放在角色底下，所以角色轉身時字不會跟著翻面；每幀跟著角色位置。
    /// </summary>
    public class HeadSwitchBark : MonoBehaviour
    {
        [Serializable]
        public class Line
        {
            public HeadConfig head;
            public string speakerName = "賽";
            public string text = "該前進了。";
        }

        [Header("References")]
        [SerializeField] private PlayerCombat combat;
        [Tooltip("識別色從這裡讀，跟換頭特效用同一組顏色")]
        [SerializeField] private PlayerPresentation presentation;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform bubbleRoot;
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private SpriteRenderer outline;
        [SerializeField] private SpriteRenderer tail;
        [SerializeField] private TextMeshPro label;

        [Header("Lines")]
        [SerializeField] private List<Line> lines = new List<Line>();

        [Header("Layout")]
        [Tooltip("對話框底部相對角色位置的偏移")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 2.1f);
        [SerializeField] private Vector2 padding = new Vector2(0.28f, 0.16f);
        [SerializeField] private float outlineThickness = 0.04f;

        [Header("Timing")]
        [SerializeField] private float showDuration = 1f;
        [SerializeField] private float popDuration = 0.12f;
        [SerializeField] private float fadeDuration = 0.2f;

        private float shownAt = float.NegativeInfinity;
        private Color backgroundColor;
        private Color tailColor;

        private void Awake()
        {
            backgroundColor = background.color;
            tailColor = tail.color;
            bubbleRoot.gameObject.SetActive(false);
        }

        private void OnEnable() => combat.HeadChanged += HandleHeadChanged;

        private void OnDisable() => combat.HeadChanged -= HandleHeadChanged;

        private void HandleHeadChanged(int newIndex, int step)
        {
            HeadConfig head = combat.Heads[newIndex];
            Line line = lines.Find(entry => entry.head == head);
            if (line == null)
                return;

            Color identity = presentation != null ? presentation.GetIdentityColor(head) : Color.white;
            // 先啟用再更新文字，停用中的 TMP 不會計算大小
            bubbleRoot.gameObject.SetActive(true);
            label.text = $"<color=#{ColorUtility.ToHtmlStringRGB(identity)}><b>{line.speakerName}</b></color>  {line.text}";
            outline.color = identity;

            // 依文字大小調整對話框
            Vector2 textSize = label.GetPreferredValues(label.text);
            Vector2 boxSize = textSize + padding * 2f;
            background.size = boxSize;
            outline.size = boxSize + Vector2.one * (outlineThickness * 2f);
            background.transform.localPosition = new Vector3(0f, boxSize.y * 0.5f, 0f);
            outline.transform.localPosition = background.transform.localPosition;
            label.transform.localPosition = background.transform.localPosition;

            shownAt = Time.time;
            bubbleRoot.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (!bubbleRoot.gameObject.activeSelf)
                return;

            float age = Time.time - shownAt;
            if (age > showDuration + fadeDuration)
            {
                bubbleRoot.gameObject.SetActive(false);
                return;
            }

            bubbleRoot.position = (Vector2)followTarget.position + offset;

            // 彈出：從 60% 放大到稍微超過原尺寸再回來
            float pop = Mathf.Clamp01(age / popDuration);
            float scale = pop < 1f ? Mathf.Lerp(0.6f, 1.08f, pop) : Mathf.Lerp(1.08f, 1f, Mathf.Clamp01((age - popDuration) / 0.08f));
            bubbleRoot.localScale = Vector3.one * scale;

            float alpha = 1f - Mathf.Clamp01((age - showDuration) / fadeDuration);
            background.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundColor.a * alpha);
            tail.color = new Color(tailColor.r, tailColor.g, tailColor.b, tailColor.a * alpha);
            Color outlineColor = outline.color;
            outlineColor.a = alpha;
            outline.color = outlineColor;
            label.alpha = alpha;
        }
    }
}
