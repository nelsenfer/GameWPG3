using UnityEngine;
using Ink.Runtime;
using Ink.UnityIntegration;
using Game.Core;
using Game.Dialogue.Data;
using Game.Dialogue.Events;

namespace Game.Dialogue.Core
{
    /// <summary>
    /// Menjalankan 1 percakapan Ink dari asset InkFile. Versi ini dilengkapi
    /// Debug.Log di titik-titik kunci untuk memudahkan diagnosis saat testing.
    /// Setelah sistem terbukti jalan, log-log ini boleh dikurangi/dihapus.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        [Header("Ink Source")]
        [SerializeField] private InkFile inkFile;

        [Header("Character Lookup")]
        [SerializeField] private CharacterDatabase characterDatabase;
        [SerializeField] private CharacterData fallbackSpeaker;

        [Header("References")]
        [SerializeField] private PlayerController playerController;

        [Header("Events")]
        [SerializeField] private ChoiceEventChannel choiceEventChannel;
        [SerializeField] private DialogueLineEventChannel lineEventChannel;
        [SerializeField] private DialogueChoicesEventChannel choicesEventChannel;
        [SerializeField] private VoidEventChannelSO dialogueEndedChannel;

        private Story story;
        private System.Action onFinished;
        public bool IsDialogueActive { get; private set; }

        private void Awake()
        {
            Debug.Log($"[DialogueManager] Awake. inkFile assigned: {inkFile != null}");

            if (inkFile != null)
            {
                story = new Story(inkFile.storyJson);
                Debug.Log("[DialogueManager] Story berhasil dibuat dari InkFile.");
            }
            else
            {
                Debug.LogError("[DialogueManager] inkFile masih NONE di Inspector — drag file .ink ke field Ink File.", this);
            }
        }

        [ContextMenu("TEST: Start Dialogue")]
        public void StartDialogue()
        {
            Debug.Log("[DialogueManager] StartDialogue() dipanggil.");

            if (inkFile != null) story = new Story(inkFile.storyJson);
            BeginDialogue();
        }

        private void BeginDialogue()
        {

            if (story == null)
            {
                Debug.LogError("[DialogueManager] story masih NULL. Cek apakah inkFile ter-assign dan file .ink tidak error compile.", this);
                return;
            }

            // Cek semua event channel ter-assign, biar ketauan kalau ada yang kosong.
            if (lineEventChannel == null) Debug.LogWarning("[DialogueManager] Line Event Channel belum di-assign!", this);
            if (choicesEventChannel == null) Debug.LogWarning("[DialogueManager] Choices Event Channel belum di-assign!", this);
            if (choiceEventChannel == null) Debug.LogWarning("[DialogueManager] Choice Event Channel belum di-assign!", this);
            if (dialogueEndedChannel == null) Debug.LogWarning("[DialogueManager] Dialogue Ended Channel belum di-assign!", this);
            if (characterDatabase == null) Debug.LogWarning("[DialogueManager] Character Database belum di-assign!", this);

            IsDialogueActive = true;
            playerController?.SetFrozen(true);
            story.ResetState();

            Debug.Log("[DialogueManager] Mulai ContinueStory() pertama kali.");
            ContinueStory();
        }

        /// <summary>
        /// Memulai dialog dari InkFile tertentu tanpa mengubah perilaku
        /// StartDialogue() yang memakai file default.
        /// </summary>
        public void StartDialogue(InkFile file, System.Action onFinishedCallback = null)
        {
            if (file == null)
            {
                Debug.LogError("[DialogueManager] InkFile dialog kosong.", this);
                return;
            }

            story = new Story(file.storyJson);
            onFinished = onFinishedCallback;
            Debug.Log("[DialogueManager] StartDialogue(file) dipanggil.");
            BeginDialogue();
        }

        public void ContinueStory()
        {
            if (story == null || !IsDialogueActive)
            {
                Debug.LogWarning($"[DialogueManager] ContinueStory dibatalkan. story null: {story == null}, IsDialogueActive: {IsDialogueActive}");
                return;
            }

            if (story.canContinue)
            {
                string rawLine = story.Continue().Trim();
                Debug.Log($"[DialogueManager] Baris Ink mentah: \"{rawLine}\"");

                DialogueLinePayload payload = ParseLine(rawLine);
                Debug.Log($"[DialogueManager] Payload hasil parse -> speaker: {(payload.speaker != null ? payload.speaker.displayName : "NULL/fallback")}, text: \"{payload.text}\"");

                if (lineEventChannel != null)
                {
                    lineEventChannel.Raise(payload);
                    Debug.Log("[DialogueManager] lineEventChannel.Raise() dipanggil.");
                }
                else
                {
                    Debug.LogError("[DialogueManager] lineEventChannel NULL — baris dialog tidak akan sampai ke UI manapun!", this);
                }

                if (story.currentChoices.Count > 0)
                {
                    Debug.Log($"[DialogueManager] Ada {story.currentChoices.Count} choice tersedia, raise choicesEventChannel.");
                    choicesEventChannel?.Raise(story.currentChoices);
                }
            }
            else if (story.currentChoices.Count > 0)
            {
                Debug.Log($"[DialogueManager] Tidak ada baris baru, tapi ada {story.currentChoices.Count} choice.");
                choicesEventChannel?.Raise(story.currentChoices);
            }
            else
            {
                Debug.Log("[DialogueManager] Cerita selesai (canContinue=false, tidak ada choice). EndDialogue().");
                EndDialogue();
            }
        }

        public void ChooseOption(int choiceIndex)
        {
            Debug.Log($"[DialogueManager] ChooseOption({choiceIndex}) dipanggil.");

            if (story == null || !IsDialogueActive || choiceIndex < 0 || choiceIndex >= story.currentChoices.Count)
            {
                Debug.LogWarning("[DialogueManager] ChooseOption dibatalkan — index di luar jangkauan atau dialog tidak aktif.");
                return;
            }

            Choice selectedChoice = story.currentChoices[choiceIndex];
            ChoiceAlignment alignment = ParseAlignmentFromTags(selectedChoice.tags);
            Debug.Log($"[DialogueManager] Choice dipilih: \"{selectedChoice.text}\", alignment: {alignment}");

            if (alignment != ChoiceAlignment.Neutral)
                choiceEventChannel?.Raise(alignment);

            story.ChooseChoiceIndex(choiceIndex);
            ContinueStory();
        }

        private DialogueLinePayload ParseLine(string rawLine)
        {
            int separatorIndex = rawLine.IndexOf(':');
            if (separatorIndex > 0 && separatorIndex < 30)
            {
                string speakerID = rawLine.Substring(0, separatorIndex).Trim();
                string text = rawLine.Substring(separatorIndex + 1).Trim();
                CharacterData speaker = characterDatabase != null ? characterDatabase.GetByID(speakerID) : null;

                if (speaker != null)
                    return new DialogueLinePayload(speaker, text);

                Debug.LogWarning($"[DialogueManager] SpeakerID \"{speakerID}\" tidak ketemu di CharacterDatabase. Cek characterID di CharacterData sama persis (case-sensitive)?");
            }

            return new DialogueLinePayload(fallbackSpeaker, rawLine);
        }

        private ChoiceAlignment ParseAlignmentFromTags(System.Collections.Generic.List<string> tags)
        {
            if (tags == null) return ChoiceAlignment.Neutral;

            foreach (string tag in tags)
            {
                string normalizedTag = tag.Trim().ToLowerInvariant();
                if (normalizedTag == "reflective") return ChoiceAlignment.Reflective;
                if (normalizedTag == "avoidant") return ChoiceAlignment.Avoidant;
            }

            return ChoiceAlignment.Neutral;
        }

        private void EndDialogue()
        {
            IsDialogueActive = false;
            playerController?.SetFrozen(false);
            dialogueEndedChannel?.Raise();

            System.Action callback = onFinished;
            onFinished = null;
            callback?.Invoke();
        }
    }
}