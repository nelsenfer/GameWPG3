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
        private readonly Collider2D[] overlapBuffer = new Collider2D[8];

        private void Reset()
        {
            playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (playerController != null && playerController.CurrentState == PlayerMoveState.Frozen)
                return;

            DetectNearestInteractable();

            if (currentFocus != null && Input.GetKeyDown(interactKey) && currentFocus.CanInteract())
            {
                currentFocus.OnInteract(gameObject);
                eventChannel?.RaiseInteracted(currentFocus);
            }
        }

        private void DetectNearestInteractable()
        {
            if (config == null) return;

            int count = Physics2D.OverlapCircleNonAlloc(
                transform.position,
                config.interactionRadius,
                overlapBuffer,
                config.interactableLayer
            );

            IInteractable nearest = null;
            float nearestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider2D collider = overlapBuffer[i];
                if (collider == null) continue;

                IInteractable interactable = collider.GetComponent(typeof(IInteractable)) as IInteractable;
                if (interactable != null && interactable.CanInteract())
                {
                    float distance = (collider.transform.position - transform.position).sqrMagnitude;
                    if (distance < nearestDist)
                    {
                        nearestDist = distance;
                        nearest = interactable;
                    }
                }
            }

            if (nearest != currentFocus)
            {
                currentFocus = nearest;
                eventChannel?.RaiseFocusChanged(currentFocus);
            }
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