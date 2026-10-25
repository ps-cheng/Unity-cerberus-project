using System;
using UnityEngine;

namespace Cerberus.Player
{
    /// <summary>
    /// 只負責物理層面的移動與跳躍。要不要移動、能不能跳由 PlayerController 決定，
    /// 這裡只執行收到的指令。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Run")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float groundAcceleration = 80f;
        [SerializeField] private float airAcceleration = 45f;

        [Header("Jump")]
        [SerializeField] private float jumpVelocity = 14f;
        [Tooltip("提早放開跳躍鍵時，上升速度乘上此值，做出長按跳高、短按跳低")]
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.5f;
        [Tooltip("離開平台後仍可起跳的寬限時間")]
        [SerializeField] private float coyoteTime = 0.1f;
        [Tooltip("落地前提早按跳躍仍會生效的緩衝時間")]
        [SerializeField] private float jumpBufferTime = 0.1f;
        [SerializeField] private float fallGravityMultiplier = 1.8f;
        [SerializeField] private float maxFallSpeed = 20f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.7f, 0.1f);
        [SerializeField] private LayerMask groundLayer;

        public bool IsGrounded { get; private set; }
        public int FacingDirection { get; private set; } = 1;
        public Vector2 Velocity => body.linearVelocity;

        public event Action Jumped;

        private Rigidbody2D body;
        private float baseGravityScale;
        private float moveInput;
        private bool isJumpHeld;
        private float lastGroundedTime = float.NegativeInfinity;
        private float lastJumpRequestTime = float.NegativeInfinity;
        private float knockbackEndTime = float.NegativeInfinity;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            baseGravityScale = body.gravityScale;
        }

        /// <param name="allowTurn">攻擊或格檔中鎖定面向，避免招式中途轉身</param>
        public void SetMoveInput(float input, bool allowTurn)
        {
            moveInput = Mathf.Clamp(input, -1f, 1f);

            if (allowTurn && Mathf.Abs(moveInput) > 0.01f)
                FacingDirection = moveInput > 0f ? 1 : -1;
        }

        public void RequestJump() => lastJumpRequestTime = Time.time;

        public void CancelJumpRequest() => lastJumpRequestTime = float.NegativeInfinity;

        public void SetJumpHeld(bool held)
        {
            // 放開跳躍鍵的瞬間若仍在上升，就削減上升速度
            if (isJumpHeld && !held && body.linearVelocity.y > 0f)
                body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * jumpCutMultiplier);

            isJumpHeld = held;
        }

        /// <param name="controlLockDuration">這段時間內不套用水平加速，讓擊退速度不會馬上被移動輸入抵銷</param>
        public void ApplyKnockback(Vector2 velocity, float controlLockDuration)
        {
            body.linearVelocity = velocity;
            knockbackEndTime = Time.time + controlLockDuration;
        }

        public void Teleport(Vector2 position)
        {
            body.position = position;
            transform.position = position;
            body.linearVelocity = Vector2.zero;
        }

        private void FixedUpdate()
        {
            UpdateGrounded();
            ApplyHorizontalMovement();
            TryConsumeJump();
            ApplyGravity();
        }

        private void UpdateGrounded()
        {
            IsGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer) != null;

            if (IsGrounded)
                lastGroundedTime = Time.time;
        }

        private void ApplyHorizontalMovement()
        {
            if (Time.time < knockbackEndTime)
                return;

            float acceleration = IsGrounded ? groundAcceleration : airAcceleration;
            float targetSpeed = moveInput * moveSpeed;
            float newSpeed = Mathf.MoveTowards(body.linearVelocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
            body.linearVelocity = new Vector2(newSpeed, body.linearVelocity.y);
        }

        private void TryConsumeJump()
        {
            bool hasBufferedJump = Time.time - lastJumpRequestTime <= jumpBufferTime;
            bool canJump = Time.time - lastGroundedTime <= coyoteTime;

            if (!hasBufferedJump || !canJump)
                return;

            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpVelocity);
            lastJumpRequestTime = float.NegativeInfinity;
            lastGroundedTime = float.NegativeInfinity;
            Jumped?.Invoke();
        }

        private void ApplyGravity()
        {
            // 下落時加重重力，讓跳躍弧線更俐落
            body.gravityScale = body.linearVelocity.y < 0f ? baseGravityScale * fallGravityMultiplier : baseGravityScale;

            if (body.linearVelocity.y < -maxFallSpeed)
                body.linearVelocity = new Vector2(body.linearVelocity.x, -maxFallSpeed);
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null)
                return;

            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}
