using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ink.Runtime;
using Game.Dialogue.Core;
using Game.Dialogue.Events;

namespace Game.Dialogue.UI
{
    public class DialogueChoiceUIController : MonoBehaviour
    {
        [SerializeField] private DialogueManager dialogueManager;
        [SerializeField] private Transform choiceButtonContainer;
        [SerializeField] private Button choiceButtonPrefab;
        [SerializeField] private Button continueButton;
        [SerializeField] private DialogueLineEventChannel lineEventChannel;
        [SerializeField] private DialogueChoicesEventChannel choicesEventChannel;
        [SerializeField] private Game.Core.VoidEventChannelSO dialogueEndedChannel;

        private readonly List<GameObject> spawnedButtons = new();

        private void OnEnable()
        {
            if (lineEventChannel != null) lineEventChannel.OnLineRaised += HandleLine;
            if (choicesEventChannel != null) choicesEventChannel.OnChoicesRaised += HandleChoices;
            if (dialogueEndedChannel != null) dialogueEndedChannel.OnRaised += HandleDialogueEnded;
            if (continueButton != null) continueButton.onClick.AddListener(ContinueDialogue);
        }

        private void OnDisable()
        {
            if (lineEventChannel != null) lineEventChannel.OnLineRaised -= HandleLine;
            if (choicesEventChannel != null) choicesEventChannel.OnChoicesRaised -= HandleChoices;
            if (dialogueEndedChannel != null) dialogueEndedChannel.OnRaised -= HandleDialogueEnded;
            if (continueButton != null) continueButton.onClick.RemoveListener(ContinueDialogue);
        }

        private void ContinueDialogue()
        {
            dialogueManager?.ContinueStory();
        }

        private void HandleLine(DialogueLinePayload payload)
        {
            ClearChoiceButtons();
            if (continueButton != null) continueButton.gameObject.SetActive(true);
        }

        private void HandleChoices(List<Choice> choices)
        {
            ClearChoiceButtons();
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (choiceButtonPrefab == null || choiceButtonContainer == null || dialogueManager == null) return;

            for (int i = 0; i < choices.Count; i++)
            {
                int index = i;
                Button button = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = choices[i].text;
                button.onClick.AddListener(() => dialogueManager.ChooseOption(index));
                spawnedButtons.Add(button.gameObject);
            }
        }

        private void HandleDialogueEnded()
        {
            ClearChoiceButtons();
            if (continueButton != null) continueButton.gameObject.SetActive(false);
        }

        private void ClearChoiceButtons()
        {
            foreach (GameObject button in spawnedButtons)
                Destroy(button);
            spawnedButtons.Clear();
        }
    }
}