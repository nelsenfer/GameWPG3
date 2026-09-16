using UnityEngine;
using UnityEngine.Events;

namespace Game.Core
{
    /// <summary>
    /// Interactable generik untuk task sederhana yang dapat dikonfigurasi lewat Inspector.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GenericInteractable : MonoBehaviour, IInteractable
    {
        [Header("Prompt")]
        [Tooltip("Teks yang muncul di UI prompt, misal '[E] Ambil Buku'.")]
        [SerializeField] private string interactionPrompt = "[E] Interaksi";

        [Header("Perilaku")]
        [Tooltip("Jika aktif, objek hanya dapat diinteraksi sekali.")]
        [SerializeField] private bool oneTimeUse = true;

        [Tooltip("Jumlah interaksi sebelum task dianggap selesai.")]
        [SerializeField] private int requiredInteractCount = 1;

        [Header("Events - drag GameObject dan pilih fungsi di sini")]
        [Tooltip("Dipanggil setiap kali objek diinteraksi.")]
        [SerializeField] private UnityEvent onInteractStep;

        [Tooltip("Dipanggil sekali saat jumlah interaksi yang dibutuhkan tercapai.")]
        [SerializeField] private UnityEvent onTaskCompleted;

        private int currentInteractCount;
        private bool isCompleted;

        public bool CanInteract() => !isCompleted;

        public string GetInteractionPrompt() => interactionPrompt;

        public void OnInteract(GameObject interactor)
        {
            if (isCompleted) return;

            currentInteractCount++;
            onInteractStep?.Invoke();

            if (currentInteractCount >= GetRequiredInteractCount())
            {
                isCompleted = true;
                onTaskCompleted?.Invoke();
            }
            else if (!oneTimeUse && GetRequiredInteractCount() <= 1)
            {
                currentInteractCount = 0;
            }
        }

        /// <summary>
        /// Mereset state agar objek dapat digunakan kembali.
        /// </summary>
        public void ResetInteraction()
        {
            currentInteractCount = 0;
            isCompleted = false;
        }

        public bool IsCompleted => isCompleted;
        public int CurrentInteractCount => currentInteractCount;

        private int GetRequiredInteractCount() => Mathf.Max(1, requiredInteractCount);
    }
}