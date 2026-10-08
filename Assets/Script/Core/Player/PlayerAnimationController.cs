using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Hanya mengatur ANIMASI player (tidak ikut urus gerakan).
    /// - Jalan kanan/kiri : parameter IsWalking, sprite di-flip kalau ke kiri
    /// - Diam / tekan S   : idle menghadap kamera
    /// - Tekan W sekali   : membelakangi kamera dan TETAP begitu (latch)
    ///                      sampai tekan S atau mulai jalan kanan/kiri
    ///
    /// Status jalan dibaca dari PlayerController (stabil, tidak kedip antar frame).
    /// </summary>
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Rigidbody2D rb;

        [Header("Input")]
        [SerializeField] private KeyCode backKey = KeyCode.W;
        [SerializeField] private KeyCode frontKey = KeyCode.S;

        [Header("Arah sprite jalan asli")]
        [Tooltip("Centang kalau gambar walk aslinya menghadap KANAN.")]
        [SerializeField] private bool artFacesRight = true;

        private bool isBackLatched;

        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private static readonly int IsBackHash = Animator.StringToHash("IsBack");

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            playerController = GetComponent<PlayerController>();
            rb = GetComponent<Rigidbody2D>();
        }

        private void Awake()
        {
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (playerController == null)
                Debug.LogWarning("[PlayerAnimationController] PlayerController tidak ditemukan di objek ini.", this);
        }

        private void Update()
        {
            bool walking = playerController != null
                && playerController.CurrentState == PlayerMoveState.Walking;

            // Arah flip: pakai input langsung supaya responsif, bukan kecepatan.
            float inputX = Input.GetAxisRaw("Horizontal");
            if (walking && Mathf.Abs(inputX) > 0.01f && spriteRenderer != null)
            {
                bool goingLeft = inputX < 0f;
                spriteRenderer.flipX = artFacesRight ? goingLeft : !goingLeft;
            }

            bool frozen = playerController != null
                && playerController.CurrentState == PlayerMoveState.Frozen;

            // W sekali = membelakangi dan bertahan. S atau mulai jalan = reset.
            if (!frozen && Input.GetKeyDown(backKey)) isBackLatched = true;
            if (Input.GetKeyDown(frontKey)) isBackLatched = false;
            if (walking) isBackLatched = false;

            bool back = isBackLatched && !walking;

            if (animator != null)
            {
                animator.SetBool(IsWalkingHash, walking);
                animator.SetBool(IsBackHash, back);
            }
        }
    }
}