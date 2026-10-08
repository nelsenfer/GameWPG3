using UnityEngine;

namespace Game.Core
{
    public enum PlayerMoveState { Idle, Walking, Frozen }

    /// <summary>
    /// Kontrol gerak horizontal pemain dengan dukungan state Frozen.
    /// Animasi & flip sprite diurus PlayerAnimationController.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private PlayerMovementConfig config;

        private Rigidbody2D rb;
        private float currentVelocityX;
        private float inputX;

        public PlayerMoveState CurrentState { get; private set; } = PlayerMoveState.Idle;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            if (config == null)
                Debug.LogWarning($"[PlayerController] PlayerMovementConfig belum di-assign di {name}.", this);
        }

        private void Update()
        {
            if (CurrentState == PlayerMoveState.Frozen)
            {
                inputX = 0f;
                return;
            }

            inputX = Input.GetAxisRaw("Horizontal");
        }

        private void FixedUpdate()
        {
            if (config == null) return;

            float targetVelocityX = inputX * config.moveSpeed;
            float rate = Mathf.Abs(targetVelocityX) > 0.01f ? config.acceleration : config.deceleration;

            currentVelocityX = Mathf.MoveTowards(currentVelocityX, targetVelocityX, rate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(currentVelocityX, rb.linearVelocity.y);

            CurrentState = CurrentState != PlayerMoveState.Frozen
                ? (Mathf.Abs(currentVelocityX) > 0.05f ? PlayerMoveState.Walking : PlayerMoveState.Idle)
                : PlayerMoveState.Frozen;
        }

        public void SetFrozen(bool frozen)
        {
            CurrentState = frozen ? PlayerMoveState.Frozen : PlayerMoveState.Idle;
            currentVelocityX = 0f;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        public PlayerMovementConfig GetConfig() => config;
    }
}