using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Kamera side-scrolling dengan dead zone: kamera diam selama player masih
    /// di dalam kotak tengah layar, dan baru ikut bergerak saat player menyentuh
    /// tepi kotak itu. Opsional: dibatasi oleh batas kiri/kanan level.
    ///
    /// Taruh di Main Camera. Kamera harus Orthographic.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class SideScrollCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Dead Zone (satuan world, dari tengah kamera)")]
        [Tooltip("Setengah lebar zona di mana player boleh bergerak tanpa menggeser kamera.")]
        [SerializeField] private float deadZoneHalfWidth = 2.5f;

        [Header("Gerak Kamera")]
        [Tooltip("Makin kecil makin cepat/kaku. 0 = langsung nempel.")]
        [SerializeField] private float smoothTime = 0.15f;
        [Tooltip("Ikut sumbu Y juga? Biasanya mati untuk side-scroller datar.")]
        [SerializeField] private bool followY = false;
        [SerializeField] private float yOffset = 0f;

        [Header("Batas Level (opsional)")]
        [SerializeField] private bool useBounds = false;
        [SerializeField] private float minX = -10f;
        [SerializeField] private float maxX = 10f;

        private Camera cam;
        private float velocityX;
        private float velocityY;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            if (target == null)
            {
                var player = FindFirstObjectByType<PlayerController>();
                if (player != null) target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 pos = transform.position;

            // Hitung posisi X yang diinginkan: hanya geser kalau player keluar dead zone.
            float desiredX = pos.x;
            float leftEdge = pos.x - deadZoneHalfWidth;
            float rightEdge = pos.x + deadZoneHalfWidth;

            if (target.position.x > rightEdge) desiredX = target.position.x - deadZoneHalfWidth;
            else if (target.position.x < leftEdge) desiredX = target.position.x + deadZoneHalfWidth;

            if (useBounds)
            {
                float halfWidth = cam.orthographicSize * cam.aspect;
                float lo = minX + halfWidth;
                float hi = maxX - halfWidth;
                desiredX = lo > hi ? (minX + maxX) * 0.5f : Mathf.Clamp(desiredX, lo, hi);
            }

            pos.x = smoothTime > 0f
                ? Mathf.SmoothDamp(pos.x, desiredX, ref velocityX, smoothTime)
                : desiredX;

            if (followY)
            {
                float desiredY = target.position.y + yOffset;
                pos.y = smoothTime > 0f
                    ? Mathf.SmoothDamp(pos.y, desiredY, ref velocityY, smoothTime)
                    : desiredY;
            }

            transform.position = pos;
        }

        /// <summary>Langsung pindah ke posisi player tanpa smoothing (misal setelah teleport/lift).</summary>
        public void SnapToTarget()
        {
            if (target == null) return;
            Vector3 pos = transform.position;
            pos.x = target.position.x;
            if (followY) pos.y = target.position.y + yOffset;
            transform.position = pos;
            velocityX = velocityY = 0f;
        }

        private void OnDrawGizmosSelected()
        {
            // Kotak kuning = dead zone, garis merah = batas level.
            Gizmos.color = Color.yellow;
            Camera c = cam != null ? cam : GetComponent<Camera>();
            float h = c != null ? c.orthographicSize * 2f : 10f;
            Vector3 center = transform.position;
            Gizmos.DrawWireCube(new Vector3(center.x, center.y, 0f), new Vector3(deadZoneHalfWidth * 2f, h, 0f));

            if (useBounds)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(new Vector3(minX, center.y - h, 0f), new Vector3(minX, center.y + h, 0f));
                Gizmos.DrawLine(new Vector3(maxX, center.y - h, 0f), new Vector3(maxX, center.y + h, 0f));
            }
        }
    }
}
