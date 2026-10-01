using UnityEngine;
using Game.Core;
using Game.Dialogue.Core;
using Game.Dialogue.Events;

namespace Game.Dialogue.UI
{
    /// <summary>
    /// Menampilkan 1 kotak dialog AKTIF (gaya visual novel) dan menangani input
    /// "klik di mana aja / tekan tombol apa aja untuk lanjut" — KECUALI saat
    /// sedang menunjukkan pilihan (choices), di situ pemain wajib klik salah
    /// satu tombol pilihan, klik sembarangan tidak melanjutkan cerita.
    /// </summary>
    public class ActiveDialogueBoxController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("GameObject ChatBubbleUI yang dipasang TETAP di scene (bukan prefab).")]
        [SerializeField] private ChatBubbleUI bubble;
        [SerializeField] private GameObject bubbleRoot;
        [SerializeField] private DialogueManager dialogueManager;

        [Header("Events")]
        [SerializeField] private DialogueLineEventChannel lineEventChannel;
        [SerializeField] private DialogueChoicesEventChannel choicesEventChannel;
        [SerializeField] private VoidEventChannelSO dialogueEndedChannel;

        private bool isChoiceActive = false;
        private bool isDialogueVisible = false;

        private void OnEnable()
        {
            if (lineEventChannel != null) lineEventChannel.OnLineRaised += HandleLine;
            if (choicesEventChannel != null) choicesEventChannel.OnChoicesRaised += HandleChoices;
            if (dialogueEndedChannel != null) dialogueEndedChannel.OnRaised += HandleDialogueEnded;

            SetVisible(false);
        }

        private void OnDisable()
        {
            if (lineEventChannel != null) lineEventChannel.OnLineRaised -= HandleLine;
            if (choicesEventChannel != null) choicesEventChannel.OnChoicesRaised -= HandleChoices;
            if (dialogueEndedChannel != null) dialogueEndedChannel.OnRaised -= HandleDialogueEnded;
        }

        private void Update()
        {
            if (!isDialogueVisible || isChoiceActive) return;

            // Klik kiri mouse ATAU tekan tombol apa saja (keyboard/gamepad) → lanjut.
            bool advancePressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);

            if (advancePressed)
                dialogueManager?.ContinueStory();
        }

        private void HandleLine(DialogueLinePayload payload)
        {
            isChoiceActive = false; // baris baru datang → reset, belum ada choice untuk baris ini
            SetVisible(true);
            bubble?.SetData(payload.speaker, payload.text);
        }

        private void HandleChoices(System.Collections.Generic.List<Ink.Runtime.Choice> choices)
        {
            isChoiceActive = true; // ada pilihan tampil → klik sembarangan tidak lanjut cerita
        }

        private void HandleDialogueEnded()
        {
            isChoiceActive = false;
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            isDialogueVisible = visible;
            GameObject target = bubbleRoot != null ? bubbleRoot : (bubble != null ? bubble.gameObject : null);
            if (target != null) target.SetActive(visible);
        }
    }
}