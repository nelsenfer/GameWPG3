using UnityEngine;
using UnityEngine.UI;
using Game.Dialogue.Events;

namespace Game.Dialogue.UI
{
    public class ChatLogController : MonoBehaviour
    {
        [SerializeField] private ChatBubbleUI bubblePrefab;
        [SerializeField] private Transform contentParent;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private DialogueLineEventChannel lineEventChannel;
        [SerializeField] private Game.Core.VoidEventChannelSO dialogueEndedChannel;
        [SerializeField] private bool clearLogOnDialogueEnd = true;

        private void OnEnable()
        {
            if (lineEventChannel != null) lineEventChannel.OnLineRaised += HandleLine;
            if (dialogueEndedChannel != null) dialogueEndedChannel.OnRaised += HandleDialogueEnded;
        }

        private void OnDisable()
        {
            if (lineEventChannel != null) lineEventChannel.OnLineRaised -= HandleLine;
            if (dialogueEndedChannel != null) dialogueEndedChannel.OnRaised -= HandleDialogueEnded;
        }

        private void HandleLine(DialogueLinePayload payload)
        {
            if (bubblePrefab == null || contentParent == null) return;

            ChatBubbleUI bubble = Instantiate(bubblePrefab, contentParent);
            bubble.SetData(payload.speaker, payload.text);
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
        }

        private void HandleDialogueEnded()
        {
            if (!clearLogOnDialogueEnd || contentParent == null) return;

            for (int i = contentParent.childCount - 1; i >= 0; i--)
                Destroy(contentParent.GetChild(i).gameObject);
        }
    }
}