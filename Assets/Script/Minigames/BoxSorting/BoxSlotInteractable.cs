using UnityEngine;
using Game.Core;

namespace Game.Progression
{
    /// <summary>
    /// Komponen interaktif pada tiap Tempat/Slot (A - D).
    /// Posisinya di Scene View bisa digeser sesuka hati di Unity Editor.
    /// Memiliki indikator kotak kecil di atasnya yang menyala jika kotak yang diletakkan benar.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class BoxSlotInteractable : MonoBehaviour, IInteractable
    {
        [Header("Identitas Slot")]
        [Tooltip("Kotak yang harus diletakkan di sini agar indikator menyala (A, B, C, atau D).")]
        [SerializeField] private string requiredId = "A";

        [Header("References")]
        [SerializeField] private BoxSortingMinigame minigame;
        [SerializeField] private SpriteRenderer indicatorRenderer;

        [Header("Warna Indikator")]
        [SerializeField] private Color offColor = new Color(0.18f, 0.24f, 0.30f, 0.55f);
        [SerializeField] private Color onColor = new Color(0.40f, 1.00f, 0.88f, 1f);

        private SortableBoxInteractable placedBox;

        public string RequiredId => requiredId;
        public SortableBoxInteractable PlacedBox => placedBox;
        public bool IsCorrect => placedBox != null && placedBox.BoxId == requiredId;

        private void Awake()
        {
            if (indicatorRenderer == null)
            {
                Transform indTr = transform.Find("Indikator");
                if (indTr != null) indicatorRenderer = indTr.GetComponent<SpriteRenderer>();
            }
            RefreshIndicator();
        }

        public void Initialize(
            BoxSortingMinigame owner,
            string id,
            SpriteRenderer indicator,
            Color off,
            Color on)
        {
            minigame = owner;
            if (!string.IsNullOrEmpty(id)) requiredId = id;
            if (indicator != null) indicatorRenderer = indicator;
            offColor = off;
            onColor = on;
            RefreshIndicator();
        }

        public void PlaceBox(SortableBoxInteractable box)
        {
            placedBox = box;
            if (box != null)
                box.SetPlacedInSlot(true, transform.position + new Vector3(0f, 0.04f, 0f));
            RefreshIndicator();
        }

        public SortableBoxInteractable TakeBox()
        {
            SortableBoxInteractable box = placedBox;
            placedBox = null;
            RefreshIndicator();
            return box;
        }

        public void ResetSlot()
        {
            placedBox = null;
            RefreshIndicator();
        }

        public void RefreshIndicator()
        {
            if (indicatorRenderer == null) return;
            indicatorRenderer.color = IsCorrect ? onColor : offColor;
        }

        public void UpdatePulse(float pulse)
        {
            if (indicatorRenderer == null) return;
            if (IsCorrect)
            {
                Color c = onColor;
                c.a = Mathf.Clamp01(onColor.a * pulse);
                indicatorRenderer.color = c;
                indicatorRenderer.transform.localScale = new Vector3(0.42f * pulse, 0.42f * pulse, 1f);
            }
            else
            {
                indicatorRenderer.color = offColor;
                indicatorRenderer.transform.localScale = new Vector3(0.34f, 0.34f, 1f);
            }
        }

        public bool CanInteract()
        {
            if (minigame == null) minigame = GetComponentInParent<BoxSortingMinigame>();
            if (minigame == null || !minigame.IsActiveMinigame) return false;

            return minigame.CarriedBox != null || placedBox != null;
        }

        public string GetInteractionPrompt()
        {
            if (minigame == null) minigame = GetComponentInParent<BoxSortingMinigame>();
            if (minigame == null) return "";

            if (minigame.CarriedBox != null && placedBox == null)
                return "[E] Letakkan Kotak " + minigame.CarriedBox.BoxId;

            if (minigame.CarriedBox == null && placedBox != null)
                return "[E] Ambil Kotak " + placedBox.BoxId;

            if (minigame.CarriedBox != null && placedBox != null)
                return "[E] Tukar Kotak " + minigame.CarriedBox.BoxId + " & " + placedBox.BoxId;

            return "";
        }

        public void OnInteract(GameObject interactor)
        {
            if (CanInteract())
            {
                if (minigame == null) minigame = GetComponentInParent<BoxSortingMinigame>();
                minigame?.InteractWithSlot(this);
            }
        }
    }
}
