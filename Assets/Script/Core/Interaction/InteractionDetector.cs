using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Mendeteksi interactable terdekat dan memicu interaksi saat tombol ditekan.
    /// </summary>
    public class InteractionDetector : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private PlayerController playerController;

        [Header("Events (opsional)")]
        [SerializeField] private InteractionEventChannel eventChannel;

        [Header("Input")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private IInteractable currentFocus;
        private readonly Collider2D[] overlapBuffer = new Collider2D[16];

        private void Awake()
        {
            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            Debug.Log($"[InteractionDetector] Sensor aktif di {name}. Tombol interaksi: {interactKey}.", this);

            if (config == null)
                Debug.LogWarning($"[InteractionDetector] PlayerMovementConfig belum di-assign di {name}.", this);
        }

        private void Reset()
        {
            playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (playerController != null && playerController.CurrentState == PlayerMoveState.Frozen)
                return;

            DetectNearestInteractable();

            if (Input.GetKeyDown(interactKey))
            {
                if (currentFocus == null)
                {
                    LogInteractionDebug("Tombol E ditekan, tetapi tidak ada target interaksi.");
                    return;
                }

                if (!currentFocus.CanInteract())
                {
                    LogInteractionDebug($"Tombol E ditekan, tetapi target {GetTargetName(currentFocus)} tidak dapat berinteraksi.");
                    return;
                }

                LogInteractionDebug($"Player dapat berinteraksi dengan {GetTargetName(currentFocus)}.");
                currentFocus.OnInteract(gameObject);
                eventChannel?.RaiseInteracted(currentFocus);
            }
        }

        private void DetectNearestInteractable()
        {
            if (config == null)
            {
                SetFocus(null);
                return;
            }

            ContactFilter2D contactFilter = new ContactFilter2D();
            contactFilter.SetLayerMask(config.interactableLayer);
            int colliderCount = Physics2D.OverlapCircle(
                transform.position,
                config.interactionRadius,
                contactFilter,
                overlapBuffer
            );

            IInteractable nearest = null;
            float nearestDist = float.MaxValue;

            for (int i = 0; i < colliderCount; i++)
            {
                Collider2D collider = overlapBuffer[i];
                if (collider == null) continue;

                IInteractable interactable = collider.GetComponentInParent(typeof(IInteractable)) as IInteractable;
                if (interactable != null && interactable.CanInteract())
                {
                    Component interactableComponent = interactable as Component;
                    Vector3 targetPosition = interactableComponent != null
                        ? interactableComponent.transform.position
                        : collider.transform.position;
                    float distance = (targetPosition - transform.position).sqrMagnitude;
                    if (distance < nearestDist)
                    {
                        nearestDist = distance;
                        nearest = interactable;
                    }
                }
            }

            SetFocus(nearest);
        }

        private void SetFocus(IInteractable target)
        {
            if (target != currentFocus)
            {
                currentFocus = target;
                LogInteractionDebug(currentFocus == null
                    ? "Player tidak berada di dekat target interaksi."
                    : $"Player dapat berinteraksi dengan {GetTargetName(currentFocus)}. Tekan {interactKey}.");
                eventChannel?.RaiseFocusChanged(currentFocus);
            }
        }

        private void LogInteractionDebug(string message)
        {
            Debug.Log($"[InteractionDetector] {message}", this);
        }

        private static string GetTargetName(IInteractable target)
        {
            return target is Component component ? component.gameObject.name : target.GetType().Name;
        }

        public IInteractable GetCurrentFocus() => currentFocus;

        private void OnDrawGizmosSelected()
        {
            if (config == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, config.interactionRadius);
        }
    }
}