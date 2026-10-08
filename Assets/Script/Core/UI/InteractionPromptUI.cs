using UnityEngine;
using TMPro;

namespace Game.Core
{
    /// <summary>
    /// Menampilkan prompt interaksi (misal "[E] Bicara"). Kalau Follow Target menyala,
    /// panel prompt menempel di atas objek interaktif yang sedang difokuskan; kalau
    /// dimatikan, panel diam di posisi yang kamu atur manual di Canvas.
    ///
    /// Script ini ditaruh di GameObject terpisah (misal di ==UI Logic==), BUKAN di dalam
    /// panel promptRoot, karena panel itu disembunyikan oleh script ini sendiri.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InteractionEventChannel eventChannel;
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private TMP_Text promptText;

        [Header("Mengikuti Objek")]
        [SerializeField] private bool followTarget = true;
        [Tooltip("Canvas tempat panel prompt berada (dipakai untuk konversi posisi).")]
        [SerializeField] private Canvas canvas;
        [Tooltip("Geser dari titik atas objek, dalam pixel layar. Y positif = lebih ke atas.")]
        [SerializeField] private Vector2 offsetPixel = new Vector2(0f, 20f);

        private RectTransform promptRect;
        private RectTransform canvasRect;
        private Camera worldCamera;
        private Component currentTarget;

        private void Awake()
        {
            if (promptRoot != null) promptRect = promptRoot.GetComponent<RectTransform>();
            if (canvas != null) canvasRect = canvas.transform as RectTransform;
        }

        private void OnEnable()
        {
            if (eventChannel != null)
                eventChannel.OnFocusChanged += HandleFocusChanged;

            if (promptRoot != null)
                promptRoot.SetActive(false);
        }

        private void OnDisable()
        {
            if (eventChannel != null)
                eventChannel.OnFocusChanged -= HandleFocusChanged;
        }

        private void HandleFocusChanged(IInteractable target)
        {
            bool hasTarget = target != null;
            currentTarget = target as Component;

            if (promptRoot != null) promptRoot.SetActive(hasTarget);
            if (hasTarget && promptText != null) promptText.text = target.GetInteractionPrompt();
            if (hasTarget) UpdatePosition();
        }

        private void LateUpdate()
        {
            if (!followTarget || currentTarget == null || promptRoot == null || !promptRoot.activeSelf) return;

            if (currentTarget is IInteractable interactable && promptText != null)
            {
                string latestPrompt = interactable.GetInteractionPrompt();
                if (promptText.text != latestPrompt) promptText.text = latestPrompt;
            }

            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (!followTarget || currentTarget == null || promptRect == null || canvas == null || canvasRect == null) return;

            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera == null) return;

            // Titik tempel: tengah-atas collider objek (atau posisi objek kalau tidak punya collider).
            Vector3 anchorPoint = currentTarget.transform.position;
            if (currentTarget.TryGetComponent<Collider2D>(out var col))
                anchorPoint = new Vector3(col.bounds.center.x, col.bounds.max.y, anchorPoint.z);

            Vector2 screenPoint = (Vector2)worldCamera.WorldToScreenPoint(anchorPoint) + offsetPixel;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector3 worldPoint))
                promptRect.position = worldPoint;
        }
    }
}