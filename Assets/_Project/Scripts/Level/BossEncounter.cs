using System.Collections;
using Cerberus.Cameras;
using Cerberus.Combat;
using Cerberus.Feedback;
using Cerberus.Player;
using Cerberus.UI;
using UnityEngine;

namespace Cerberus.Level
{
    /// <summary>
    /// 保管庫的 Boss 戰流程：
    /// 1. 玩家走進保管庫（這個觸發區）→ 大門關上鎖住、鏡頭鎖在房間內
    /// 2. 系統廣播 → 塔洛斯從上方落下登場
    /// 3. 玩家死亡 → 重生後 Boss 移除、大門解鎖，下次走進來 Boss 戰從頭開始
    /// 4. 塔洛斯被擊敗 → 冥河硬幣出現在展示櫃（撿取由硬幣上的 LevelGoal 處理）
    /// Boss 本身的行為完全不動，這裡只負責生成和移除。
    /// </summary>
    public class BossEncounter : PlayerTriggerZone
    {
        private enum State { Waiting, Intro, Fighting, Cleared }

        [Header("References")]
        [SerializeField] private VaultGate entranceGate;
        [Tooltip("場景裡停用的 Boss，每次開戰複製一份，原本這份不會被改動")]
        [SerializeField] private GameObject bossTemplate;
        [SerializeField] private Transform bossSpawnPoint;
        [SerializeField] private PlayerDamageReceiver player;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private CameraShake cameraShake;
        [SerializeField] private BroadcastBanner broadcast;
        [Tooltip("擊敗 Boss 後出現的獎勵（冥河硬幣），一開始停用")]
        [SerializeField] private GameObject reward;

        [Header("Arena")]
        [Tooltip("Boss 戰期間鏡頭的範圍（世界座標）")]
        [SerializeField] private Rect arenaCameraBounds = new Rect(110f, -1.5f, 32f, 14f);
        [Tooltip("Boss 戰期間鏡頭拉遠到這個大小（orthographic size），讓塔洛斯和雷射都在畫面內。0 = 不拉遠")]
        [SerializeField] private float arenaOrthographicSize = 8f;
        [Tooltip("鏡頭拉遠／拉回需要的秒數")]
        [SerializeField] private float zoomDuration = 1f;

        [Header("Intro")]
        [SerializeField] private string broadcastMessage = "赫托斯重工 保全單位 塔洛斯，啟動。";
        [SerializeField] private float broadcastHoldDuration = 1.5f;
        [Tooltip("Boss 從生成點上方多高的地方落下")]
        [SerializeField] private float dropHeight = 7f;
        [SerializeField] private float landingShakeAmplitude = 0.35f;
        [SerializeField] private float landingShakeDuration = 0.35f;

        [Header("Victory")]
        [Tooltip("Boss 倒下後多久出現硬幣")]
        [SerializeField] private float rewardDelay = 1.2f;

        private State state = State.Waiting;
        private GameObject bossInstance;
        private PlayerController playerController;
        private Camera viewCamera;
        private float defaultOrthographicSize;
        private float targetOrthographicSize;

        private void Awake()
        {
            bossTemplate.SetActive(false);
            reward.SetActive(false);
            playerController = player.GetComponent<PlayerController>();
            viewCamera = Camera.main;
            defaultOrthographicSize = targetOrthographicSize = viewCamera.orthographicSize;
        }

        private void Update()
        {
            // 鏡頭大小慢慢靠近目標；CameraFollow 每幀依目前大小重新計算邊界
            float speed = Mathf.Abs(arenaOrthographicSize - defaultOrthographicSize) / Mathf.Max(zoomDuration, 0.01f);
            viewCamera.orthographicSize = Mathf.MoveTowards(viewCamera.orthographicSize, targetOrthographicSize, speed * Time.deltaTime);
        }

        private void OnEnable()
        {
            player.Died += HandlePlayerDied;
            player.Respawned += HandlePlayerRespawned;
        }

        private void OnDisable()
        {
            player.Died -= HandlePlayerDied;
            player.Respawned -= HandlePlayerRespawned;
        }

        protected override void OnPlayerEnter(Collider2D playerCollider)
        {
            if (state == State.Waiting)
                StartCoroutine(PlayIntro());
        }

        private IEnumerator PlayIntro()
        {
            state = State.Intro;
            entranceGate.CloseAndLock();
            cameraFollow.SetBounds(arenaCameraBounds);
            if (arenaOrthographicSize > 0f)
                targetOrthographicSize = arenaOrthographicSize;

            // 廣播期間玩家不能動，看完再開打
            playerController.SetInputEnabled(false);
            broadcast.Show(broadcastMessage, broadcastHoldDuration);
            yield return new WaitForSeconds(broadcast.GetTypingDuration(broadcastMessage) + 0.4f);

            SpawnBoss();
            yield return WaitForBossLanding();

            cameraShake.Shake(landingShakeAmplitude, landingShakeDuration);
            playerController.SetInputEnabled(true);
            if (state == State.Intro)
                state = State.Fighting;
        }

        private void SpawnBoss()
        {
            Vector3 spawn = bossSpawnPoint.position + Vector3.up * dropHeight;
            bossInstance = Instantiate(bossTemplate, spawn, bossTemplate.transform.rotation, bossTemplate.transform.parent);
            bossInstance.name = bossTemplate.name;
            bossInstance.SetActive(true);
            bossInstance.GetComponent<Health>().Died += HandleBossDied;
        }

        private IEnumerator WaitForBossLanding()
        {
            Rigidbody2D body = bossInstance.GetComponent<Rigidbody2D>();
            float timeout = Time.time + 3f;
            // 先等它開始往下掉，再等它停下來
            yield return new WaitForSeconds(0.1f);
            while (bossInstance != null && Time.time < timeout && Mathf.Abs(body.linearVelocity.y) > 0.05f)
                yield return null;
        }

        private void HandleBossDied()
        {
            state = State.Cleared;
            StartCoroutine(RevealReward());
        }

        private IEnumerator RevealReward()
        {
            yield return new WaitForSeconds(rewardDelay);
            reward.SetActive(true);
        }

        private void HandlePlayerDied()
        {
            if (state != State.Intro && state != State.Fighting)
                return;

            StopAllCoroutines();
            playerController.SetInputEnabled(true);
        }

        // 重生時才移除 Boss，死亡那一秒還看得到 Boss 站在場上
        private void HandlePlayerRespawned()
        {
            if (state != State.Intro && state != State.Fighting)
                return;

            if (bossInstance != null)
                Destroy(bossInstance);

            bossInstance = null;
            state = State.Waiting;
            entranceGate.ResetGate();
            cameraFollow.ResetBounds();
            targetOrthographicSize = defaultOrthographicSize;
        }

        protected override Color GizmoColor => new Color(1f, 0.48f, 0.1f, 0.7f);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.48f, 0.1f, 0.4f);
            Gizmos.DrawWireCube(arenaCameraBounds.center, arenaCameraBounds.size);
        }
    }
}
