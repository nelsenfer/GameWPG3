using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Ink.UnityIntegration;
using Game.Core;
using Game.Dialogue.Core;

namespace Game.Progression
{
    /// <summary>
    /// Satu "tahap" interaksi sebuah objek. Tahap pertama yang syaratnya cocok yang dipakai.
    /// Urutan aksi: tampilkan panel -> main dialog -> tutup panel -> (minigame) -> selesai.
    /// </summary>
    [Serializable]
    public class InteractionStep
    {
        public string stepName = "Tahap";

        [Header("Syarat (kosong = tidak ada syarat)")]
        [Tooltip("Semua flag ini harus sudah aktif.")]
        public string[] requiresFlags;
        [Tooltip("Tahap ini TIDAK berlaku kalau salah satu flag ini sudah aktif.")]
        public string[] blockedByFlags;

        [Header("Tampilan")]
        public string prompt = "[E] Periksa";

        [Header("Aksi (semua opsional)")]
        [Tooltip("Panel yang tampil selama dialog, lalu ditutup otomatis.")]
        public GameObject showPanel;
        public InkFile dialogue;
        [Tooltip("Minigame yang dibuka setelah dialog (kosong = tanpa minigame). Selesai = lulus.")]
        public MinigameLauncher minigame;

        [Header("Setelah selesai")]
        public string[] setFlags;
        [Tooltip("Teks misi baru. Kosong = tidak diubah.")]
        public string newObjective;
        public bool clearObjective;
        [Tooltip("Sembunyikan objek ini (misal kopi yang sudah diambil).")]
        public bool hideObjectAfter;
        [Tooltip("Aksi tambahan bebas (suara, nyalakan objek, dst) saat tahap selesai.")]
        public UnityEvent onFinished;
    }

    /// <summary>
    /// Interactable berbasis progres cerita. Satu komponen ini bisa jadi komputer
    /// (beberapa tahap) atau item sekali ambil (satu tahap), tanpa kode baru per objek.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FlagInteractable : MonoBehaviour, IInteractable
    {
        [Header("References")]
        [SerializeField] private StoryProgress progress;
        [SerializeField] private DialogueManager dialogueManager;

        [Header("Tahap (dicek dari atas ke bawah)")]
        [SerializeField] private List<InteractionStep> steps = new List<InteractionStep>();

        private bool isBusy;
        private MinigameLauncher activeMinigame;

        public bool CanInteract() => !isBusy && FindStep() != null;

        public string GetInteractionPrompt()
        {
            InteractionStep step = FindStep();
            return step != null ? step.prompt : "";
        }

        public void OnInteract(GameObject interactor)
        {
            if (isBusy) return;

            InteractionStep step = FindStep();
            if (step == null) return;

            isBusy = true;
            if (step.showPanel != null) step.showPanel.SetActive(true);

            if (step.dialogue != null)
            {
                if (dialogueManager == null)
                {
                    Debug.LogError("[FlagInteractable] Dialogue Manager belum di-assign.", this);
                    Abort(step);
                    return;
                }
                dialogueManager.StartDialogue(step.dialogue, () => AfterDialogue(step));
            }
            else
            {
                AfterDialogue(step);
            }
        }

        private void AfterDialogue(InteractionStep step)
        {
            if (step.showPanel != null) step.showPanel.SetActive(false);

            if (step.minigame != null)
            {
                activeMinigame = step.minigame;
                activeMinigame.Launch(() => Finish(step));
            }
            else
            {
                Finish(step);
            }
        }

        private void Update()
        {
            // Minigame ditutup tanpa lulus -> objek bisa dipakai lagi, tahap tidak maju.
            if (activeMinigame != null && !activeMinigame.IsOpen)
            {
                activeMinigame = null;
                isBusy = false;
            }
        }

        private void Finish(InteractionStep step)
        {
            activeMinigame = null;

            if (progress != null)
            {
                if (step.setFlags != null)
                    foreach (string f in step.setFlags) progress.SetFlag(f);

                if (step.clearObjective) progress.ClearObjective();
                else if (!string.IsNullOrWhiteSpace(step.newObjective)) progress.SetObjective(step.newObjective);
            }

            isBusy = false;
            step.onFinished?.Invoke();
            if (step.hideObjectAfter) gameObject.SetActive(false);
        }

        private void Abort(InteractionStep step)
        {
            if (step.showPanel != null) step.showPanel.SetActive(false);
            isBusy = false;
        }

        private InteractionStep FindStep()
        {
            foreach (InteractionStep step in steps)
                if (Matches(step)) return step;
            return null;
        }

        private bool Matches(InteractionStep step)
        {
            if (progress == null) return false;

            if (step.requiresFlags != null)
                foreach (string f in step.requiresFlags)
                    if (!progress.HasFlag(f)) return false;

            if (step.blockedByFlags != null)
                foreach (string f in step.blockedByFlags)
                    if (!string.IsNullOrWhiteSpace(f) && progress.HasFlag(f)) return false;

            return true;
        }
    }
}