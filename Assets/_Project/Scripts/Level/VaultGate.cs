using UnityEngine;

namespace Cerberus.Level
{
    /// <summary>
    /// 保管庫的圓形大門：玩家靠近時往旁邊滾開（邊移動邊旋轉），可以被鎖上（Boss 戰期間關閉、打不開）。
    /// 門關著時碰撞體擋住通道，打開到一半以上才放行。
    /// </summary>
    public class VaultGate : PlayerTriggerZone
    {
        [Header("Door")]
        [Tooltip("會移動的門板（圓形門的圖）")]
        [SerializeField] private Transform door;
        [SerializeField] private Collider2D blocker;
        [Tooltip("門打開時往哪裡移動（相對關閉位置）")]
        [SerializeField] private Vector2 openOffset = new Vector2(0f, 3.2f);
        [Tooltip("滾動時轉的角度，正負決定方向")]
        [SerializeField] private float openRotation = 270f;
        [SerializeField] private float openDuration = 1.2f;
        [SerializeField] private float closeDuration = 0.5f;

        public bool IsLocked { get; private set; }
        public bool IsOpen => progress >= 0.999f;

        private Vector3 closedPosition;
        private float progress;
        private float target;

        private void Awake()
        {
            closedPosition = door.localPosition;
            ApplyProgress();
        }

        protected override void OnPlayerEnter(Collider2D player)
        {
            if (!IsLocked)
                target = 1f;
        }

        /// <summary>關上並鎖住，玩家靠近也不會開</summary>
        public void CloseAndLock()
        {
            IsLocked = true;
            target = 0f;
        }

        /// <summary>解鎖並關上，玩家下次靠近時重新打開</summary>
        public void ResetGate()
        {
            IsLocked = false;
            target = IsPlayerInside ? 1f : 0f;
        }

        private void Update()
        {
            float duration = target > progress ? openDuration : closeDuration;
            progress = Mathf.MoveTowards(progress, target, Time.deltaTime / Mathf.Max(duration, 0.01f));
            ApplyProgress();
        }

        private void ApplyProgress()
        {
            // 開門先慢後快再慢，像沉重的金庫門
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            door.localPosition = closedPosition + (Vector3)(openOffset * eased);
            door.localRotation = Quaternion.Euler(0f, 0f, openRotation * eased);
            blocker.enabled = progress < 0.5f;
        }
    }
}
