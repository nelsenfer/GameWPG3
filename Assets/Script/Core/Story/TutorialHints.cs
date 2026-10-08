using UnityEngine;
using Game.Core;
using Game.Dialogue.Data;
using Game.Dialogue.Events;
using Game.Dialogue.UI;

namespace Game.Progression
{
    /// <summary>
    /// Petunjuk tutorial 2 langkah di awal game, ditampilkan di KOTAK DIALOG yang sama
    /// dengan dialog biasa (tidak perlu panel baru). Maju dengan klik kiri / Space / Enter:
    /// 1) petunjuk bergerak -> klik -> 2) petunjuk interaksi -> klik -> kotak ditutup.
    /// Kalau dialog sungguhan mulai, dialog itu yang menguasai kotaknya dan petunjuk dilepas.
    /// Taruh di ==Manager==.
    /// </summary>
    public class TutorialHints : MonoBehaviour
    {
        private enum Step { Move, Interact, Done }

        [Header("Kotak Dialog (pakai yang sama dengan ActiveDialogueBoxController)")]
        [SerializeField] private ChatBubbleUI bubble;
        [Tooltip("Objek yang di-show/hide. Isi sama dengan Bubble Root di ActiveDialogueBoxController.")]
        [SerializeField] private GameObject bubbleRoot;
        [Tooltip("Opsional: karakter untuk nama/gambar pembicara, misal 'Petunjuk'. Kosong = tanpa nama.")]
        [SerializeField] private CharacterData hintSpeaker;

        [Header("Events")]
        [Tooltip("Dipakai untuk tahu kapan dialog sungguhan mengambil alih kotak dialog.")]
        [SerializeField] private DialogueLineEventChannel lineEventChannel;

        [Header("Teks")]
        [SerializeField, TextArea] private string moveHint = "Tekan A / D untuk berjalan. Tekan W / S untuk menghadap belakang / depan.";
        [SerializeField, TextArea] private string interactHint = "Dekati objek, lalu tekan E untuk berinteraksi.";

        private Step step = Step.Move;
        private bool hintOwnsBubble;

        private void Start()
        {
            ShowStep(Step.Move);
        }

        private void OnEnable()
        {
            if (lineEventChannel != null) lineEventChannel.OnLineRaised += HandleDialogueLine;
        }

        private void OnDisable()
        {
            if (lineEventChannel != null) lineEventChannel.OnLineRaised -= HandleDialogueLine;
        }

        private void Update()
        {
            // Hanya bereaksi kalau kotak dialog sedang berisi petunjuk (bukan dialog sungguhan).
            if (!hintOwnsBubble || step == Step.Done) return;

            bool advance = Input.GetMouseButtonDown(0)
                        || Input.GetKeyDown(KeyCode.Space)
                        || Input.GetKeyDown(KeyCode.Return);
            if (!advance) return;

            ShowStep(step == Step.Move ? Step.Interact : Step.Done);
        }

        // Dialog sungguhan mulai -> kotak dialog jadi milik dialog, bukan petunjuk.
        private void HandleDialogueLine(DialogueLinePayload payload)
        {
            hintOwnsBubble = false;
        }

        private void ShowStep(Step next)
        {
            step = next;

            if (step == Step.Done)
            {
                if (hintOwnsBubble && bubbleRoot != null) bubbleRoot.SetActive(false);
                hintOwnsBubble = false;
                return;
            }

            string text = step == Step.Move ? moveHint : interactHint;
            if (bubble != null) bubble.SetData(hintSpeaker, text);
            if (bubbleRoot != null) bubbleRoot.SetActive(true);
            hintOwnsBubble = true;
        }
    }
}