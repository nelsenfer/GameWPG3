using UnityEngine;

namespace Game.Core
{
    public class InteractionDetector : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private PlayerController playerController;

        [Header("Events (opsional, untuk UI prompt dsb)")]
        [SerializeField] private InteractionEventChannel eventChannel;

        [Header("Input")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private IInteractable currentFocus;
        private readonly Collider2D[] overlapBuffer = new Collider2D[8];
        private ContactFilter2D contactFilter;

        private void Reset()
        {
            playerController = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (playerController != null && playerController.CurrentState == PlayerMoveState.Frozen)
            {
                // Saat dialog/minigame: hapus fokus supaya prompt "E" hilang.
                if (currentFocus != null)
                {
                    currentFocus = null;
                    eventChannel?.RaiseFocusChanged(null);
                }
                return;
            }

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

            // Filter eksplisit: selalu ikut deteksi collider "Is Trigger",
            // tidak tergantung Project Settings > Physics 2D > Queries Hit Triggers.
            contactFilter.useTriggers = true;
            contactFilter.SetLayerMask(config.interactableLayer);

            int count = Physics2D.OverlapCircle(
                transform.position,
                config.interactionRadius,
                contactFilter,
                overlapBuffer
            );

            IInteractable nearest = null;
            float nearestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var col = overlapBuffer[i];
                if (col == null) continue;

                if (col.TryGetComponent<IInteractable>(out var interactable) && interactable.CanInteract())
                {
                    float dist = (col.transform.position - transform.position).sqrMagnitude;
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
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