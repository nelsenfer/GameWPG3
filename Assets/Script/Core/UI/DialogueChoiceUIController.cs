using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ink.Runtime;
using Game.Dialogue.Core;
using Game.Dialogue.Events;

namespace Game.Dialogue.UI
{
    /// <summary>
    /// Satu-satunya tempat tombol pilihan dibuat (dialog biasa dan balasan HP).
    /// Baris bergaya HP dan daftar kontak diabaikan di sini; PhoneChatPresenter yang menentukan
    /// kapan pilihan balasan HP boleh tampil, lalu memanggil ShowChoices().
    /// </summary>
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
        private bool lastLineWasPhone;

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

        // ---------- API publik (dipakai PhoneChatPresenter) ----------

        /// <summary>Tampilkan tombol pilihan. onPicked null = langsung ChooseOption ke DialogueManager.</summary>
        public void ShowChoices(List<Choice> choices, System.Action<int> onPicked = null)
        {
            ClearChoiceButtons();
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (choiceButtonPrefab == null || choiceButtonContainer == null) return;

            for (int i = 0; i < choices.Count; i++)
            {
                int index = i;
                Button button = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = choices[i].text;
                button.onClick.AddListener(() => Pick(index, onPicked));
                spawnedButtons.Add(button.gameObject);
            }
        }

        public void ClearChoices() => ClearChoiceButtons();

        // ---------- Event ----------

        private void HandleLine(DialogueLinePayload payload)
        {
            lastLineWasPhone = payload.style == DialogueStyle.Phone;
            ClearChoiceButtons();

            // Chat HP maju otomatis, jadi tombol lanjut tidak dipakai.
            if (continueButton != null) continueButton.gameObject.SetActive(!lastLineWasPhone);
        }

        private void HandleChoices(List<Choice> choices)
        {
            if (continueButton != null) continueButton.gameObject.SetActive(false);

            // Daftar kontak dan balasan HP ditangani PhoneChatPresenter.
            if (lastLineWasPhone || PhoneChoiceTags.IsContactList(choices)) return;

            ShowChoices(choices);
        }

        private void HandleDialogueEnded()
        {
            lastLineWasPhone = false;
            ClearChoiceButtons();
            if (continueButton != null) continueButton.gameObject.SetActive(false);
        }

        // ---------- Util ----------

        private void Pick(int index, System.Action<int> onPicked)
        {
            ClearChoiceButtons();
            if (onPicked != null) onPicked(index);
            else if (dialogueManager != null) dialogueManager.ChooseOption(index);
        }

        private void ContinueDialogue()
        {
            if (dialogueManager != null) dialogueManager.ContinueStory();
        }

        private void ClearChoiceButtons()
        {
            foreach (GameObject button in spawnedButtons)
                if (button != null) Destroy(button);
            spawnedButtons.Clear();
        }
    }
}