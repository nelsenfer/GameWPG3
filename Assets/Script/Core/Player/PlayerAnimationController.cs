using UnityEngine;

namespace Game.Core
{
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

        [Header("Config Animasi")]
        [Tooltip("Centang kalau gambar walk aslinya menghadap KANAN.")]
        [SerializeField] private bool artFacesRight = true;
        [SerializeField] private float crossFadeDuration = 0.1f;

        private bool isBackLatched;
        private int currentStateHash; // Melacak animasi aktif agar tidak dipanggil berulang (Poin 2 & 5)

        // Optimasi: Mengubah nama State di Animator menjadi Hash Integer (Poin 5)
        // Nama-nama ini HARUS sama persis dengan nama kotak/node state di Animator Window
        private static readonly int IdleHash = Animator.StringToHash("player");
        private static readonly int WalkHash = Animator.StringToHash("Player_Walk");
        private static readonly int BackHash = Animator.StringToHash("Player_Back");

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
        }

        private void Update()
        {
            CheckAnimation(); // Logika Transisi (Poin 3)
        }

        private void CheckAnimation()
        {
            bool walking = playerController != null
                && playerController.CurrentState == PlayerMoveState.Walking;

            // Arah flip sprite
            float inputX = Input.GetAxisRaw("Horizontal");
            if (walking && Mathf.Abs(inputX) > 0.01f && spriteRenderer != null)
            {
                bool goingLeft = inputX < 0f;
                spriteRenderer.flipX = artFacesRight ? goingLeft : !goingLeft;
            }

            bool frozen = playerController != null
                && playerController.CurrentState == PlayerMoveState.Frozen;

            // Logika latch input menghadap belakang/depan
            if (!frozen && Input.GetKeyDown(backKey)) isBackLatched = true;
            if (Input.GetKeyDown(frontKey)) isBackLatched = false;
            if (walking) isBackLatched = false;

            bool back = isBackLatched && !walking;

            // Menentukan state animasi apa yang seharusnya diputar
            if (walking)
            {
                ChangeAnimationState(WalkHash);
            }
            else if (back)
            {
                ChangeAnimationState(BackHash);
            }
            else
            {
                ChangeAnimationState(IdleHash);
            }
        }

        /// <summary>
        /// Mengganti animasi menggunakan CrossFade (Poin 2).
        /// Hanya dieksekusi jika state baru berbeda dari state saat ini.
        /// </summary>
        private void ChangeAnimationState(int newStateHash)
        {
            if (currentStateHash == newStateHash) return;

            if (animator != null)
            {
                animator.CrossFade(newStateHash, crossFadeDuration, 0);
            }
            
            currentStateHash = newStateHash;
        }
    }
}