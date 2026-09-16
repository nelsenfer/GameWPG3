using TMPro;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Menampilkan prompt untuk interactable yang sedang menjadi fokus.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private InteractionEventChannel eventChannel;
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private TMP_Text promptText;

        private void OnEnable()
        {
            if (eventChannel != null)
                eventChannel.OnFocusChanged += HandleFocusChanged;

            if (promptRoot != null)
                promptRoot.SetActive(false);
        }

        private void OnDisable()
        {
            if (eventChannel != null)
                eventChannel.OnFocusChanged -= HandleFocusChanged;
        }

        private void HandleFocusChanged(IInteractable target)
        {
            bool hasTarget = target != null;
            if (promptRoot != null) promptRoot.SetActive(hasTarget);
            if (hasTarget && promptText != null) promptText.text = target.GetInteractionPrompt();
        }
    }
}