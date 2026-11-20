using UnityEngine;

namespace Cerberus.Feedback
{
    /// <summary>
    /// 戰鬥特效的產生器：火花、爆散、煙霧、閃光、布料碎片都在這裡用程式組出 Particle System，不需要另外做預製物。
    /// 場景裡放一個，其他程式透過 CombatVfx.Instance 呼叫；Instance 不存在時呼叫端直接略過。
    /// 數值都露出在 Inspector，調整外觀不用改程式。
    /// </summary>
    public class CombatVfx : MonoBehaviour
    {
        public static CombatVfx Instance { get; private set; }

        [Header("Materials")]
        [Tooltip("加亮（Cerberus/Additive + 圓形光暈貼圖）：火花、閃光")]
        [SerializeField] private Material glowMaterial;
        [Tooltip("一般混色（圓形光暈貼圖）：煙霧")]
        [SerializeField] private Material smokeMaterial;
        [Tooltip("一般混色（純色方塊）：碎片、布料")]
        [SerializeField] private Material solidMaterial;
        [SerializeField] private int sortingOrder = 40;

        [Header("Hit Spark")]
        [SerializeField] private Color hitSparkColor = new Color(1f, 0.85f, 0.55f);
        [SerializeField] private int hitSparkCount = 12;

        [Header("Enemy Death")]
        [SerializeField] private Color deathSparkColor = new Color(1f, 0.5f, 0.12f);
        [SerializeField] private Color debrisColorA = new Color(0.3f, 0.31f, 0.36f);
        [SerializeField] private Color debrisColorB = new Color(1f, 0.48f, 0.1f);
        [SerializeField] private Color deathSmokeColor = new Color(0.16f, 0.16f, 0.2f, 0.8f);

        [Header("Head Switch")]
        [SerializeField] private Color switchSmokeColor = new Color(0.55f, 0.5f, 0.66f, 0.75f);
        [SerializeField] private int switchSmokeCount = 18;
        [Tooltip("洛斯爆衣時飛出的布料顏色：西裝、襯衫、領帶")]
        [SerializeField] private Color[] clothColors =
        {
            new Color(0.24f, 0.24f, 0.29f),
            new Color(0.9f, 0.9f, 0.93f),
            new Color(0.2f, 0.3f, 0.58f)
        };
        [SerializeField] private int clothCount = 22;

        [Header("Muzzle Flash")]
        [SerializeField] private Color muzzleColor = new Color(1f, 0.85f, 0.45f);

        [Header("Weak Point")]
        [SerializeField] private Color weakPointColor = new Color(0.45f, 1f, 1f);

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── 效果 ──────────────────────────────────────────

        /// <summary>命中火花：往打擊方向噴出的亮線加一個小閃光</summary>
        public void HitSpark(Vector2 position, float directionX, Color tint)
        {
            Color color = tint.a > 0f ? Color.Lerp(hitSparkColor, tint, 0.45f) : hitSparkColor;
            Flash(position, color, 1.3f, 0.07f);

            ParticleSystem sparks = Create("HitSparks", position, glowMaterial, 0.3f);
            SetupMain(sparks, new Vector2(0.1f, 0.22f), new Vector2(7f, 14f), new Vector2(0.05f, 0.09f), color, Color.white, 0f);
            Burst(sparks, hitSparkCount);
            Cone(sparks, directionX >= 0f ? 0f : 180f, 55f);
            Stretch(sparks, 0.05f);
            FadeOut(sparks);
            sparks.Play();
        }

        /// <summary>敵人死亡：閃光、火花、帶重力的碎片和一團煙。size 約等於敵人的寬度</summary>
        public void DeathBurst(Vector2 position, float size)
        {
            float s = Mathf.Max(0.5f, size);
            Flash(position, deathSparkColor, 2.6f * s, 0.12f);

            ParticleSystem sparks = Create("DeathSparks", position, glowMaterial, 0.6f);
            SetupMain(sparks, new Vector2(0.2f, 0.45f), new Vector2(5f, 13f) * Mathf.Sqrt(s), new Vector2(0.05f, 0.11f), deathSparkColor, Color.white, 0.8f);
            Burst(sparks, Mathf.RoundToInt(20 * s));
            Circle(sparks, 0.25f * s);
            Stretch(sparks, 0.06f);
            FadeOut(sparks);
            sparks.Play();

            ParticleSystem debris = Create("DeathDebris", position, solidMaterial, 1.4f);
            SetupMain(debris, new Vector2(0.6f, 1.1f), new Vector2(3f, 8f) * Mathf.Sqrt(s), new Vector2(0.07f, 0.2f) * Mathf.Sqrt(s), debrisColorA, debrisColorB, 2.4f);
            Burst(debris, Mathf.RoundToInt(12 * s));
            Cone(debris, 90f, 70f);
            Spin(debris, 720f);
            FadeOut(debris, 0.75f);
            debris.Play();

            Smoke(position, deathSmokeColor, Mathf.RoundToInt(9 * s), 0.5f * s, new Vector2(0.6f, 1.4f) * s, new Vector2(0.5f, 2f) * Mathf.Sqrt(s));
        }

        /// <summary>
        /// 換頭：白色閃光 + 識別色的光圈和閃點 + 一圈煙把身體包起來（遮住換裝的瞬間）。
        /// ripClothes = 切到洛斯，西裝布料炸開飛散。
        /// </summary>
        public void HeadSwitch(Vector2 position, Color identityColor, bool ripClothes)
        {
            Flash(position, Color.white, 4.2f, 0.12f);
            Flash(position, identityColor, 2.8f, 0.3f);

            ParticleSystem sparkles = Create("SwitchSparkles", position, glowMaterial, 0.7f);
            SetupMain(sparkles, new Vector2(0.25f, 0.5f), new Vector2(3f, 7f), new Vector2(0.06f, 0.12f), identityColor, Color.white, -0.3f);
            Burst(sparkles, 16);
            Circle(sparkles, 0.5f);
            Stretch(sparkles, 0.03f);
            FadeOut(sparkles);
            sparkles.Play();

            // 煙從身體四周往外擴散，稍微往上飄
            Smoke(position, switchSmokeColor, switchSmokeCount, 0.7f, new Vector2(0.7f, 1.3f), new Vector2(1.2f, 3.2f));

            if (ripClothes)
                ClothBurst(position);
        }

        /// <summary>伯特的槍口火光</summary>
        public void MuzzleFlash(Vector2 position, int facing)
        {
            Flash(position, muzzleColor, 1.1f, 0.06f);

            ParticleSystem sparks = Create("MuzzleSparks", position, glowMaterial, 0.25f);
            SetupMain(sparks, new Vector2(0.06f, 0.13f), new Vector2(9f, 16f), new Vector2(0.04f, 0.07f), muzzleColor, Color.white, 0f);
            Burst(sparks, 7);
            Cone(sparks, facing >= 0 ? 0f : 180f, 18f);
            Stretch(sparks, 0.04f);
            FadeOut(sparks);
            sparks.Play();
        }

        /// <summary>塔洛斯露出弱點的瞬間：一圈亮光和往外散的閃點</summary>
        public void WeakPointFlash(Vector2 position)
        {
            Flash(position, weakPointColor, 3.2f, 0.3f);
            Flash(position, Color.white, 1.6f, 0.1f);

            ParticleSystem sparkles = Create("WeakPointSparkles", position, glowMaterial, 0.8f);
            SetupMain(sparkles, new Vector2(0.3f, 0.6f), new Vector2(2f, 6f), new Vector2(0.06f, 0.12f), weakPointColor, Color.white, 0f);
            Burst(sparkles, 18);
            Circle(sparkles, 0.2f);
            FadeOut(sparkles);
            sparkles.Play();
        }

        // ── 共用的小零件 ──────────────────────────────────

        private void ClothBurst(Vector2 position)
        {
            ParticleSystem cloth = Create("ClothBurst", position, solidMaterial, 1.6f);
            var main = cloth.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(0.24f, 0.5f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(0.12f, 0.26f);
            main.startSizeZ = 1f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 1.6f;
            main.startColor = RandomColor(clothColors);
            Burst(cloth, clothCount);
            Cone(cloth, 90f, 75f);
            Spin(cloth, 900f);

            // 布料比碎片輕：空氣阻力讓它飄一下再落下
            var limit = cloth.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 4f;
            limit.dampen = 0.08f;

            FadeOut(cloth, 0.7f);
            cloth.Play();
        }

        private void Smoke(Vector2 position, Color color, int count, float radius, Vector2 sizeRange, Vector2 speedRange)
        {
            ParticleSystem smoke = Create("Smoke", position, smokeMaterial, 1.2f, -1);
            SetupMain(smoke, new Vector2(0.45f, 0.85f), speedRange, sizeRange, color, color * 0.8f, -0.15f);
            Burst(smoke, count);
            Circle(smoke, radius);
            Spin(smoke, 90f);

            var size = smoke.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(1f, 1.4f)));

            // 煙一開始衝得快，很快慢下來
            var limit = smoke.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0.6f;
            limit.dampen = 0.2f;

            FadeOut(smoke);
            smoke.Play();
        }

        /// <summary>單一個瞬間放大、淡出的光點</summary>
        private void Flash(Vector2 position, Color color, float size, float duration)
        {
            ParticleSystem flash = Create("Flash", position, glowMaterial, duration + 0.05f, 2);
            var main = flash.main;
            main.startLifetime = duration;
            main.startSpeed = 0f;
            main.startSize = size;
            main.startColor = color;
            Burst(flash, 1);

            var sizeOverLife = flash.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(1f, 1.15f)));

            FadeOut(flash);
            flash.Play();
        }

        private ParticleSystem Create(string effectName, Vector2 position, Material material, float lifetime, int sortingOffset = 0)
        {
            var go = new GameObject(effectName);
            go.transform.SetParent(transform, false);
            go.transform.position = position;

            ParticleSystem system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = system.emission;
            emission.rateOverTime = 0f;

            var shape = system.shape;
            shape.enabled = false;

            var particleRenderer = go.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = material;
            particleRenderer.sortingOrder = sortingOrder + sortingOffset;

            Destroy(go, lifetime + 1.5f);
            return system;
        }

        private static void SetupMain(ParticleSystem system, Vector2 lifetime, Vector2 speed, Vector2 size, Color colorA, Color colorB, float gravity)
        {
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.gravityModifier = gravity;
        }

        private static void Burst(ParticleSystem system, int count)
        {
            system.emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Max(1, count)) });
        }

        private static void Cone(ParticleSystem system, float directionDegrees, float spreadDegrees)
        {
            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.05f;
            shape.arc = spreadDegrees * 2f;
            shape.rotation = new Vector3(0f, 0f, directionDegrees - spreadDegrees);
        }

        private static void Circle(ParticleSystem system, float radius)
        {
            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = 1f;
            shape.arc = 360f;
        }

        private static void Stretch(ParticleSystem system, float velocityScale)
        {
            var particleRenderer = system.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            particleRenderer.velocityScale = velocityScale;
            particleRenderer.lengthScale = 1f;
        }

        private static void Spin(ParticleSystem system, float degreesPerSecond)
        {
            var main = system.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            float radians = degreesPerSecond * Mathf.Deg2Rad;
            rotation.z = new ParticleSystem.MinMaxCurve(-radians, radians);
        }

        /// <param name="holdUntil">生命週期的這個比例之前保持不透明</param>
        private static void FadeOut(ParticleSystem system, float holdUntil = 0f)
        {
            var color = system.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, holdUntil), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
        }

        private static ParticleSystem.MinMaxGradient RandomColor(Color[] colors)
        {
            if (colors == null || colors.Length == 0)
                return Color.white;

            // 用固定色票隨機挑色：做一個分段的漸層，startColor 設成 RandomColor 模式
            var gradient = new Gradient { mode = GradientMode.Fixed };
            var keys = new GradientColorKey[Mathf.Min(colors.Length, 8)];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = new GradientColorKey(colors[i], (i + 1f) / keys.Length);
            gradient.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };
        }
    }
}
