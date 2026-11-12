using UnityEngine;
using Game.Core;

namespace Game.Progression
{
    /// <summary>
    /// Komponen interaktif pada tiap Kotak (A - D).
    /// Posisinya di Scene View bisa digeser sesuka hati di Unity Editor.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class SortableBoxInteractable : MonoBehaviour, IInteractable
    {
        [Header("Identitas Kotak")]
        [Tooltip("ID kotak (A, B, C, atau D).")]
        [SerializeField] private string boxId = "A";

        [Header("References")]
        [SerializeField] private BoxSortingMinigame minigame;
        [SerializeField] private BoxCollider2D boxCollider;

        private bool isInSlot;
        private bool isCarried;
        private Vector3 initialPosition;

        public string BoxId => boxId;
        public bool IsCarried => isCarried;
        public bool IsInSlot => isInSlot;

        private void Awake()
        {
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
            initialPosition = transform.position;
        }

        public void Initialize(BoxSortingMinigame owner, string id, BoxCollider2D col)
        {
            minigame = owner;
            if (!string.IsNullOrEmpty(id)) boxId = id;
            if (col != null) boxCollider = col;
            isInSlot = false;
            isCarried = false;
            initialPosition = transform.position;
        }

        public void SetCarried(bool carried)
        {
            isCarried = carried;
            if (carried) isInSlot = false;
            if (boxCollider != null) boxCollider.enabled = !carried && !isInSlot;
        }

        public void SetPlacedInSlot(bool inSlot, Vector3 worldPos)
        {
            isInSlot = inSlot;
            isCarried = false;
            transform.position = worldPos;
            if (boxCollider != null) boxCollider.enabled = !inSlot;
        }

        public void ResetToInitialPosition()
        {
            isInSlot = false;
            isCarried = false;
            transform.position = initialPosition;
            if (boxCollider != null) boxCollider.enabled = true;
        }

        public bool CanInteract()
        {
            if (minigame == null) minigame = GetComponentInParent<BoxSortingMinigame>();
            return minigame != null
                && minigame.IsActiveMinigame
                && !isCarried
                && !isInSlot
                && minigame.CarriedBox == null;
        }

        public string GetInteractionPrompt()
        {
            return "[E] Ambil Kotak " + boxId;
        }

        public void OnInteract(GameObject interactor)
        {
            if (CanInteract())
            {
                if (minigame == null) minigame = GetComponentInParent<BoxSortingMinigame>();
                minigame?.PickUpLooseBox(this);
            }
        }
    }
}
