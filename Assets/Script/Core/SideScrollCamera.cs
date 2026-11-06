using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Kamera side-scrolling dengan dead zone: kamera diam selama player masih
    /// di dalam kotak tengah layar, dan baru ikut bergerak saat player menyentuh
    /// tepi kotak itu. Opsional: dibatasi oleh batas kiri/kanan level atau SpriteRenderer background.
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

        [Header("Batas Level (Map / Background)")]
        [SerializeField] private bool useBounds = true;
        [Tooltip("Jika diisi, batas kiri & kanan kamera otomatis mengikuti lebar SpriteRenderer background ini.")]
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private float minX = -33.89f;
        [SerializeField] private float maxX = 8.89f;

        private Camera cam;
        private float velocityX;
        private float velocityY;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            AutoFindBackground();
        }

        private void Start()
        {
            if (target == null)
            {
                var player = FindFirstObjectByType<PlayerController>();
                if (player != null) target = player.transform;
            }

            AutoFindBackground();
            SnapToTarget();
        }

        private void AutoFindBackground()
        {
            if (backgroundRenderer != null) return;
            GameObject bgObj = GameObject.Find("background");
            if (bgObj != null)
                backgroundRenderer = bgObj.GetComponent<SpriteRenderer>();
        }

        private void GetXBounds(out float boundMinX, out float boundMaxX)
        {
            if (backgroundRenderer != null && backgroundRenderer.sprite != null)
            {
                Bounds b = backgroundRenderer.bounds;
                boundMinX = b.min.x;
                boundMaxX = b.max.x;
                return;
            }

            boundMinX = minX;
            boundMaxX = maxX;
        }

        private float ClampXToBounds(float x, out bool clamped)
        {
            clamped = false;
            if (!useBounds || cam == null) return x;

            GetXBounds(out float boundMinX, out float boundMaxX);
            float halfWidth = cam.orthographicSize * cam.aspect;
            float lo = boundMinX + halfWidth;
            float hi = boundMaxX - halfWidth;

            if (lo > hi)
            {
                clamped = true;
                return (boundMinX + boundMaxX) * 0.5f;
            }

            if (x < lo)
            {
                clamped = true;
                return lo;
            }
            if (x > hi)
            {
                clamped = true;
                return hi;
            }
            return x;
        }

        private float ClampYToBounds(float y)
        {
            if (!useBounds || cam == null || backgroundRenderer == null || backgroundRenderer.sprite == null)
                return y;

            Bounds b = backgroundRenderer.bounds;
            float halfHeight = cam.orthographicSize;
            float lo = b.min.y + halfHeight;
            float hi = b.max.y - halfHeight;
            return lo > hi ? b.center.y : Mathf.Clamp(y, lo, hi);
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
                desiredX = ClampXToBounds(desiredX, out _);
            }

            pos.x = smoothTime > 0f
                ? Mathf.SmoothDamp(pos.x, desiredX, ref velocityX, smoothTime)
                : desiredX;

            if (useBounds)
            {
                pos.x = ClampXToBounds(pos.x, out bool hitBound);
                if (hitBound) velocityX = 0f;
            }

            if (followY)
            {
                float desiredY = target.position.y + yOffset;
                pos.y = smoothTime > 0f
                    ? Mathf.SmoothDamp(pos.y, desiredY, ref velocityY, smoothTime)
                    : desiredY;
            }

            if (useBounds)
            {
                pos.y = ClampYToBounds(pos.y);
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

            if (useBounds)
            {
                pos.x = ClampXToBounds(pos.x, out _);
                pos.y = ClampYToBounds(pos.y);
            }

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
                GetXBounds(out float boundMinX, out float boundMaxX);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(new Vector3(boundMinX, center.y - h, 0f), new Vector3(boundMinX, center.y + h, 0f));
                Gizmos.DrawLine(new Vector3(boundMaxX, center.y - h, 0f), new Vector3(boundMaxX, center.y + h, 0f));
            }
        }
    }
}
