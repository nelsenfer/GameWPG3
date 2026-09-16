using UnityEngine;
using Game.Core;

namespace Game.Fulfillment
{
    /// <summary>
    /// Contoh interactable: menyalakan lilin pada kue ulang tahun.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CandleInteractable : MonoBehaviour, IInteractable
    {
        [Header("State")]
        [SerializeField] private bool isLit;

        [Header("Visual/FX")]
        [SerializeField] private ParticleSystem flameParticle;
        [SerializeField] private Sprite litSprite;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Event (opsional)")]
        [Tooltip("Diraise saat lilin berhasil dinyalakan.")]
        [SerializeField] private VoidEventChannelSO onTaskCompleted;

        public bool CanInteract() => !isLit;

        public string GetInteractionPrompt() => "[E] Nyalakan Lilin";

        public void OnInteract(GameObject interactor)
        {
            if (isLit) return;

            isLit = true;

            if (flameParticle != null) flameParticle.Play();
            if (spriteRenderer != null && litSprite != null) spriteRenderer.sprite = litSprite;

            onTaskCompleted?.Raise();
        }
    }
}