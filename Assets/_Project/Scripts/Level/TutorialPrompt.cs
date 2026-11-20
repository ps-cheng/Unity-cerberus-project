using Cerberus.Player;
using TMPro;
using UnityEngine;

namespace Cerberus.Level
{
    /// <summary>
    /// 操作教學：玩家走進範圍時，空白霓虹招牌亮起並顯示提示文字。
    /// 玩家做過一次指定的操作後招牌熄滅，之後不再出現；沒做就離開的話，下次走回來還會再亮。
    /// 文字寫在 Inspector 的 message 欄位，按鍵請照 CerberusControls.inputactions 的實際綁定填寫。
    /// </summary>
    public class TutorialPrompt : PlayerTriggerZone
    {
        public enum RequiredAction
        {
            [Tooltip("離開範圍就算看過")]
            None,
            Move,
            Jump,
            Attack,
            Special,
            SwitchHead
        }

        [Header("Prompt")]
        [TextArea(2, 4)]
        [SerializeField] private string message = "移動";
        [Tooltip("做過這個操作，提示就永久消失")]
        [SerializeField] private RequiredAction requiredAction = RequiredAction.None;
        [Tooltip("提示出現後至少顯示幾秒才會因為完成操作而消失，免得一閃而過")]
        [SerializeField] private float minimumShowTime = 1.2f;

        [Header("Visual")]
        [Tooltip("招牌和文字的父物件，淡入淡出時縮放這層")]
        [SerializeField] private Transform sign;
        [SerializeField] private SpriteRenderer frame;
        [SerializeField] private TextMeshPro label;
        [SerializeField] private float fadeSpeed = 6f;
        [Tooltip("招牌沒亮時的亮度（0 = 完全隱藏）")]
        [SerializeField, Range(0f, 1f)] private float idleVisibility = 0f;

        private PlayerInputHandler input;
        private bool isCompleted;
        private bool isShowing;
        private float shownAt;
        private float visibility;
        private Vector3 signBaseScale;

        private void Awake()
        {
            signBaseScale = sign.localScale;
            label.text = message;
            ApplyVisibility(idleVisibility);
            visibility = idleVisibility;
        }

        private void OnValidate()
        {
            if (label != null)
                label.text = message;
        }

        protected override void OnPlayerEnter(Collider2D player)
        {
            if (isCompleted)
                return;

            input = player.GetComponentInParent<PlayerInputHandler>();
            isShowing = true;
            shownAt = Time.time;
        }

        protected override void OnPlayerExit()
        {
            if (isShowing && requiredAction == RequiredAction.None)
                isCompleted = true;

            isShowing = false;
        }

        private void Update()
        {
            if (isShowing && !isCompleted && Time.time - shownAt >= minimumShowTime && WasActionPerformed())
            {
                isCompleted = true;
                isShowing = false;
            }

            float target = isShowing ? 1f : (isCompleted ? 0f : idleVisibility);
            visibility = Mathf.MoveTowards(visibility, target, fadeSpeed * Time.deltaTime);
            ApplyVisibility(visibility);
        }

        // 輸入狀態只在玩家站在範圍內時讀取
        private bool WasActionPerformed()
        {
            if (input == null)
                return false;

            switch (requiredAction)
            {
                case RequiredAction.Move:
                    return Mathf.Abs(input.MoveInput) > 0.1f;
                case RequiredAction.Jump:
                    return input.JumpPressed;
                case RequiredAction.Attack:
                    return input.LightAttackPressed || input.HeavyAttackPressed;
                case RequiredAction.Special:
                    return input.SpecialPressed;
                case RequiredAction.SwitchHead:
                    return input.PreviousHeadPressed || input.NextHeadPressed;
                default:
                    return false;
            }
        }

        private void ApplyVisibility(float value)
        {
            // 彈出感：從 80% 放大到原尺寸
            float eased = 1f - (1f - value) * (1f - value);
            sign.localScale = signBaseScale * Mathf.Lerp(0.8f, 1f, eased);
            sign.gameObject.SetActive(value > 0.001f);

            Color frameColor = frame.color;
            frameColor.a = value;
            frame.color = frameColor;
            label.alpha = value;
        }
    }
}
