using UnityEngine;
using Game.Core;

namespace Game.Typing
{
    /// <summary>
    /// Taruh di objek di scene (komputer, meja kerja, dll) yang punya Collider2D
    /// Is Trigger + layer Interactable. Saat pemain tekan E, buka minigame mengetik
    /// untuk 1 TypingTaskData. Setelah lulus, objek ini nggak bisa dipakai lagi dan
    /// (opsional) raise event supaya bisa disambung ke cerita/dialog.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TypingTaskInteractable : MonoBehaviour, IInteractable
    {
        [Header("References")]
        [SerializeField] private TypingMinigameController controller;
        [SerializeField] private TypingTaskData taskData;

        [Header("Prompt")]
        [SerializeField] private string interactionPrompt = "[E] Ketik laporan";

        [Header("Event (opsional)")]
        [Tooltip("Diraise saat task berhasil diselesaikan.")]
        [SerializeField] private VoidEventChannelSO onTaskCompleted;

        [Header("Pengulangan")]
        [Tooltip("Nyala: setelah lulus, objek tidak bisa dipakai lagi. Matikan untuk testing / task yang boleh diulang.")]
        [SerializeField] private bool oneTimeOnly = true;

        private bool completed;

        public bool CanInteract() => !(oneTimeOnly && completed) && (controller == null || !controller.IsOpen);
        public string GetInteractionPrompt() => interactionPrompt;

        public void OnInteract(GameObject interactor)
        {
            if (!CanInteract()) return;

            if (controller == null || taskData == null)
            {
                Debug.LogError("[TypingTaskInteractable] Controller atau Task Data belum di-assign.", this);
                return;
            }

            controller.Open(taskData, HandlePassed);
        }

        private void HandlePassed()
        {
            completed = true;
            onTaskCompleted?.Raise();
        }
    }
}