using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
        [Tooltip("Opsional: selama panel log terbuka, klik/Space tidak melanjutkan dialog.")]
        [SerializeField] private LogPanelController logPanel;

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
            if (logPanel != null && logPanel.IsOpen) return;

            // Klik kiri mouse (bukan di atas tombol UI, misal tombol Log) ATAU Space/Enter → lanjut.
            bool clicked = Input.GetMouseButtonDown(0) && !IsPointerOverButton();
            bool advancePressed = clicked || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);

            if (advancePressed)
                dialogueManager?.ContinueStory();
        }

        private static readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        private static bool IsPointerOverButton()
        {
            if (EventSystem.current == null) return false;

            PointerEventData data = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            raycastResults.Clear();
            EventSystem.current.RaycastAll(data, raycastResults);

            foreach (RaycastResult r in raycastResults)
                if (r.gameObject.GetComponentInParent<Button>() != null) return true;
            return false;
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