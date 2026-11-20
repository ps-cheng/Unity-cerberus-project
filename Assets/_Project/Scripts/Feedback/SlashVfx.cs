using UnityEngine;

namespace Cerberus.Feedback
{
    /// <summary>
    /// 用 LineRenderer 畫的揮擊軌跡：
    /// - Arc：賽的拐杖揮過的弧線，前端畫出、尾端跟著消失
    /// - Claw：洛斯的三道平行爪痕，一道接一道劃下再淡出
    /// 每次出招產生一個，播完自己刪除。
    /// </summary>
    public class SlashVfx : MonoBehaviour
    {
        public enum Style { Arc, Claw }

        public struct Settings
        {
            public Style style;
            public Vector2 center;
            public int facing;
            public float radius;
            /// <summary>Arc：起始和結束角度（面向右時，0 = 正前方、90 = 正上方）</summary>
            public float startAngle;
            public float endAngle;
            public float width;
            /// <summary>畫出軌跡的時間</summary>
            public float drawDuration;
            /// <summary>畫完後淡出的時間</summary>
            public float fadeDuration;
            public Color color;
            public Material material;
            public int sortingOrder;
        }

        private const int ArcSegments = 20;
        private const int ClawCount = 3;

        private Settings settings;
        private LineRenderer[] lines;
        private float startedAt;

        public static void Spawn(Settings settings, Transform parent)
        {
            var go = new GameObject(settings.style == Style.Arc ? "SlashArc" : "ClawMarks");
            go.transform.SetParent(parent, false);
            go.AddComponent<SlashVfx>().Begin(settings);
        }

        private void Begin(Settings value)
        {
            settings = value;
            startedAt = Time.time;

            int count = settings.style == Style.Arc ? 2 : ClawCount;
            lines = new LineRenderer[count];
            for (int i = 0; i < count; i++)
                lines[i] = CreateLine(i);

            Destroy(gameObject, settings.drawDuration + settings.fadeDuration + 0.1f);
            Update();
        }

        private LineRenderer CreateLine(int index)
        {
            var go = new GameObject("Line" + index);
            go.transform.SetParent(transform, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = settings.material;
            line.sortingOrder = settings.sortingOrder + index;
            line.numCapVertices = 4;
            line.textureMode = LineTextureMode.Stretch;
            return line;
        }

        private void Update()
        {
            float age = Time.time - startedAt;
            float draw = settings.drawDuration > 0f ? Mathf.Clamp01(age / settings.drawDuration) : 1f;
            float fade = settings.fadeDuration > 0f ? Mathf.Clamp01((age - settings.drawDuration) / settings.fadeDuration) : 0f;

            if (settings.style == Style.Arc)
                UpdateArc(draw, fade);
            else
                UpdateClaws(age);
        }

        private void UpdateArc(float draw, float fade)
        {
            // 前端快速掃過（ease-out），尾端在淡出時追上前端，弧線從後面收掉
            float head = 1f - (1f - draw) * (1f - draw);
            float tail = Mathf.Clamp01(fade * 0.85f);

            // 外層粗而淡的光暈 + 內層細而亮的芯
            DrawArc(lines[0], tail, head, settings.width * 2.2f, settings.color * new Color(1f, 1f, 1f, 0.35f), fade);
            DrawArc(lines[1], tail, head, settings.width, Color.Lerp(settings.color, Color.white, 0.6f), fade);
        }

        private void DrawArc(LineRenderer line, float from, float to, float width, Color color, float fade)
        {
            int points = Mathf.Max(2, Mathf.CeilToInt(ArcSegments * (to - from)) + 1);
            line.positionCount = points;

            for (int i = 0; i < points; i++)
            {
                float t = Mathf.Lerp(from, to, i / (points - 1f));
                float angle = Mathf.Lerp(settings.startAngle, settings.endAngle, t) * Mathf.Deg2Rad;
                var offset = new Vector2(Mathf.Cos(angle) * settings.facing, Mathf.Sin(angle)) * settings.radius;
                line.SetPosition(i, settings.center + offset);
            }

            // 尾巴細、前端粗，像刀光
            line.widthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0.6f));
            line.widthMultiplier = width;

            Color tailColor = color;
            tailColor.a = 0f;
            Color headColor = color;
            headColor.a *= 1f - fade;
            line.colorGradient = Gradient(tailColor, headColor);
        }

        private void UpdateClaws(float age)
        {
            // 三道爪痕：從前上方往後下方斜劃，一道比一道晚一點
            const float stagger = 0.025f;
            var along = new Vector2(-0.45f * settings.facing, -1f).normalized;
            var across = new Vector2(1f * settings.facing, -0.45f).normalized;
            float length = settings.radius;

            for (int i = 0; i < lines.Length; i++)
            {
                float localAge = age - i * stagger;
                float draw = settings.drawDuration > 0f ? Mathf.Clamp01(localAge / settings.drawDuration) : 1f;
                float fade = settings.fadeDuration > 0f ? Mathf.Clamp01((localAge - settings.drawDuration) / settings.fadeDuration) : 0f;

                float offset = (i - (lines.Length - 1) * 0.5f) * settings.width * 4.5f;
                // 中間那道最長
                float scale = i == 1 ? 1f : 0.82f;
                Vector2 start = settings.center + across * offset - along * (length * 0.5f * scale);
                Vector2 end = start + along * (length * scale * (1f - (1f - draw) * (1f - draw)));

                LineRenderer line = lines[i];
                line.positionCount = 2;
                line.SetPosition(0, start);
                line.SetPosition(1, end);
                line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0f));
                line.widthMultiplier = settings.width * (1f + fade * 0.4f);

                Color color = Color.Lerp(Color.white, settings.color, Mathf.Clamp01(localAge / (settings.drawDuration + 0.05f)));
                color.a = draw > 0f ? 1f - fade : 0f;
                line.colorGradient = Gradient(color, color);
            }
        }

        private static Gradient Gradient(Color from, Color to)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
                new[] { new GradientAlphaKey(from.a, 0f), new GradientAlphaKey(to.a, 1f) });
            return gradient;
        }
    }
}
