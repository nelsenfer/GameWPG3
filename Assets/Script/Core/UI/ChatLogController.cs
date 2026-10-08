using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Core;
using Game.Dialogue.Events;

namespace Game.Dialogue.UI
{
    /// <summary>
    /// Mencatat dialog yang sudah lewat.
    /// MODE TEKS (disarankan, paling simpel): isi Log Text, tiap baris ditambahkan sebagai "Nama: teks".
    /// MODE BUBBLE (lama): kosongkan Log Text, isi Bubble Prefab + Content Parent.
    /// </summary>
    public class ChatLogController : MonoBehaviour
    {
        [Header("Mode Teks (simpel)")]
        [Tooltip("1 komponen TMP_Text di dalam Content. Kalau diisi, mode bubble tidak dipakai.")]
        [SerializeField] private TMP_Text logText;

        [Header("Mode Bubble (opsional)")]
        [SerializeField] private ChatBubbleUI bubblePrefab;
        [SerializeField] private Transform contentParent;

        [Header("References")]
        [SerializeField] private ScrollRect scrollRect;

        [Header("Events")]
        [SerializeField] private DialogueLineEventChannel lineEventChannel;
        [SerializeField] private VoidEventChannelSO dialogueEndedChannel;

        [Header("Behaviour")]
        [Tooltip("Kalau dicentang, log dikosongkan tiap dialog selesai (percakapan terpisah per hantu).")]
        [SerializeField] private bool clearLogOnDialogueEnd = true;

        private readonly StringBuilder builder = new StringBuilder();

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
            if (logText != null)
            {
                string name = payload.speaker != null ? payload.speaker.displayName : "";
                if (builder.Length > 0) builder.Append("\n\n");
                builder.Append(string.IsNullOrEmpty(name) ? payload.text : "<b>" + name + "</b>\n" + payload.text);
                logText.text = builder.ToString();
            }
            else if (bubblePrefab != null && contentParent != null)
            {
                ChatBubbleUI bubble = Instantiate(bubblePrefab, contentParent);
                bubble.SetData(payload.speaker, payload.text);
            }

            ScrollToBottom();
        }

        private void HandleDialogueEnded()
        {
            if (!clearLogOnDialogueEnd) return;

            builder.Clear();
            if (logText != null) logText.text = "";

            if (contentParent != null)
                for (int i = contentParent.childCount - 1; i >= 0; i--)
                    Destroy(contentParent.GetChild(i).gameObject);
        }

        private void ScrollToBottom()
        {
            if (scrollRect == null || !scrollRect.gameObject.activeInHierarchy) return;
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}