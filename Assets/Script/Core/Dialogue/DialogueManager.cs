using Ink.Runtime;
using UnityEngine;
using Game.Core;
using Game.Dialogue.Data;
using Game.Dialogue.Events;

namespace Game.Dialogue.Core
{
    public class DialogueManager : MonoBehaviour
    {
        [Header("Ink Source")]
        [SerializeField] private TextAsset inkJSONAsset;

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
        public bool IsDialogueActive { get; private set; }

        private void Awake()
        {
            if (inkJSONAsset != null)
                story = new Story(inkJSONAsset.text);
        }

        public void StartDialogue()
        {
            if (story == null)
            {
                Debug.LogError("[DialogueManager] Ink JSON Asset belum di-assign.", this);
                return;
            }

            IsDialogueActive = true;
            playerController?.SetFrozen(true);
            story.ResetState();
            ContinueStory();
        }

        public void ContinueStory()
        {
            if (story == null || !IsDialogueActive) return;

            if (story.canContinue)
            {
                DialogueLinePayload payload = ParseLine(story.Continue().Trim());
                lineEventChannel?.Raise(payload);

                if (story.currentChoices.Count > 0)
                    choicesEventChannel?.Raise(story.currentChoices);
            }
            else if (story.currentChoices.Count > 0)
            {
                choicesEventChannel?.Raise(story.currentChoices);
            }
            else
            {
                EndDialogue();
            }
        }

        public void ChooseOption(int choiceIndex)
        {
            if (story == null || !IsDialogueActive || choiceIndex < 0 || choiceIndex >= story.currentChoices.Count)
                return;

            Choice selectedChoice = story.currentChoices[choiceIndex];
            ChoiceAlignment alignment = ParseAlignmentFromTags(selectedChoice.tags);
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
        }
    }
}