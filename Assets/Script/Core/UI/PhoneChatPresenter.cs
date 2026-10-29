using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Core;
using Game.Dialogue.Core;
using Game.Dialogue.Events;

namespace Game.Dialogue.UI
{
    /// <summary>
    /// Menampilkan baris Ink bertag #hp sebagai chat HP.
    /// Pesan masuk dapat dilanjutkan dengan klik/Space/Enter. Balasan pemain
    /// memerlukan tahan tombol sebentar sebelum teks ditampilkan bertahap.
    /// </summary>
    public class PhoneChatPresenter : MonoBehaviour
    {
        private enum State { Idle, WaitingHold, Typing }

        [Header("Tampilan HP")]
        [Tooltip("Root panel HP. Dimatikan saat tidak ada chat.")]
        [SerializeField] private GameObject phoneRoot;
        [Tooltip("Content di dalam Scroll View HP.")]
        [SerializeField] private RectTransform content;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private ChatBubbleUI incomingBubblePrefab;
        [SerializeField] private ChatBubbleUI outgoingBubblePrefab;

        [Header("Petunjuk balas (opsional)")]
        [Tooltip("Objek petunjuk 'tahan tombol apa saja'.")]
        [SerializeField] private GameObject holdHint;
        [Tooltip("Image bertipe Filled untuk menunjukkan progres tahan tombol.")]
        [SerializeField] private Image holdFill;

        [Header("Mekanik balas")]
        [SerializeField] private float holdSeconds = 0.35f;
        [SerializeField] private float charsPerSecond = 35f;

        [Header("References")]
        [SerializeField] private DialogueManager dialogueManager;
        [Tooltip("Selama panel log terbuka, input chat HP diabaikan.")]
        [SerializeField] private LogPanelController logPanel;

        [Header("Events")]
        [SerializeField] private DialogueLineEventChannel lineEventChannel;
        [SerializeField] private DialogueChoicesEventChannel choicesEventChannel;
        [SerializeField] private VoidEventChannelSO dialogueEndedChannel;

        private State state = State.Idle;
        private bool phoneVisible;
        private bool isChoiceActive;
        private bool released;
        private float holdTimer;
        private ChatBubbleUI pendingBubble;
        private Coroutine typingRoutine;

        private void OnEnable()
        {
            if (lineEventChannel != null) lineEventChannel.OnLineRaised += HandleLine;
            if (choicesEventChannel != null) choicesEventChannel.OnChoicesRaised += HandleChoices;
            if (dialogueEndedChannel != null) dialogueEndedChannel.OnRaised += HandleDialogueEnded;

            SetPhoneVisible(false);
            SetHoldUI(false);
        }

        private void OnDisable()
        {
            if (lineEventChannel != null) lineEventChannel.OnLineRaised -= HandleLine;
            if (choicesEventChannel != null) choicesEventChannel.OnChoicesRaised -= HandleChoices;
            if (dialogueEndedChannel != null) dialogueEndedChannel.OnRaised -= HandleDialogueEnded;

            StopTyping();
            SetHoldUI(false);
        }

        private void Update()
        {
            if (!phoneVisible || isChoiceActive) return;
            if (logPanel != null && logPanel.IsOpen) return;

            switch (state)
            {
                case State.Idle:
                    if (AdvancePressed()) dialogueManager?.ContinueStory();
                    break;
                case State.WaitingHold:
                    UpdateHold();
                    break;
            }
        }

        private void HandleLine(DialogueLinePayload payload)
        {
            isChoiceActive = false;

            if (payload.style != DialogueStyle.Phone)
            {
                StopTyping();
                SetHoldUI(false);
                state = State.Idle;
                SetPhoneVisible(false);
                return;
            }

            SetPhoneVisible(true);

            bool outgoing = payload.speaker != null && payload.speaker.isPlayerSide;
            ChatBubbleUI prefab = outgoing ? outgoingBubblePrefab : incomingBubblePrefab;
            if (prefab == null || content == null)
            {
                Debug.LogError("[PhoneChatPresenter] Prefab bubble atau Content belum di-assign.", this);
                state = State.Idle;
                return;
            }

            ChatBubbleUI bubble = Instantiate(prefab, content);
            bubble.SetData(payload.speaker, payload.text);
            RefreshLayout();

            if (outgoing) BeginReply(bubble);
            else state = State.Idle;
        }

        private void HandleChoices(System.Collections.Generic.List<Ink.Runtime.Choice> choices)
        {
            isChoiceActive = true;
            SetHoldUI(false);
        }

        private void HandleDialogueEnded()
        {
            StopTyping();
            SetHoldUI(false);
            state = State.Idle;
            isChoiceActive = false;
            SetPhoneVisible(false);

            if (content != null)
                for (int i = content.childCount - 1; i >= 0; i--)
                    Destroy(content.GetChild(i).gameObject);
        }

        private void BeginReply(ChatBubbleUI bubble)
        {
            pendingBubble = bubble;
            holdTimer = 0f;
            released = !Input.anyKey;
            state = State.WaitingHold;

            if (bubble.DialogueText != null)
                bubble.DialogueText.maxVisibleCharacters = 0;

            SetHoldUI(true);
            UpdateHoldFill(0f);
        }

        private void UpdateHold()
        {
            if (!released)
            {
                if (!Input.anyKey) released = true;
                return;
            }

            if (Input.anyKey) holdTimer += Time.unscaledDeltaTime;
            else holdTimer = 0f;

            float progress = holdSeconds > 0f ? holdTimer / holdSeconds : 1f;
            UpdateHoldFill(progress);

            if (holdTimer >= holdSeconds) StartTyping();
        }

        private void StartTyping()
        {
            state = State.Typing;
            SetHoldUI(false);
            typingRoutine = StartCoroutine(TypeRoutine(pendingBubble));
        }

        private IEnumerator TypeRoutine(ChatBubbleUI bubble)
        {
            TMP_Text text = bubble != null ? bubble.DialogueText : null;

            if (text != null)
            {
                text.ForceMeshUpdate();
                int total = text.textInfo.characterCount;
                float shown = 0f;

                while (text != null && shown < total)
                {
                    shown += charsPerSecond * Time.unscaledDeltaTime;
                    text.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
                    yield return null;
                }

                if (text != null) text.maxVisibleCharacters = int.MaxValue;
            }

            typingRoutine = null;
            pendingBubble = null;
            state = State.Idle;
            ScrollToBottom();
        }

        private void StopTyping()
        {
            if (typingRoutine != null) StopCoroutine(typingRoutine);
            typingRoutine = null;

            if (pendingBubble != null && pendingBubble.DialogueText != null)
                pendingBubble.DialogueText.maxVisibleCharacters = int.MaxValue;

            pendingBubble = null;
            if (state == State.Typing) state = State.Idle;
        }

        private static bool AdvancePressed()
        {
            bool clicked = Input.GetMouseButtonDown(0) && !ActiveDialogueBoxController.IsPointerOverButton();
            return clicked || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
        }

        private void SetPhoneVisible(bool visible)
        {
            phoneVisible = visible;
            if (phoneRoot != null) phoneRoot.SetActive(visible);
        }

        private void SetHoldUI(bool visible)
        {
            if (holdHint != null) holdHint.SetActive(visible);
            if (!visible) UpdateHoldFill(0f);
        }

        private void UpdateHoldFill(float amount)
        {
            if (holdFill != null) holdFill.fillAmount = Mathf.Clamp01(amount);
        }

        private void RefreshLayout()
        {
            if (content != null) LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            if (scrollRect == null || !scrollRect.gameObject.activeInHierarchy) return;
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
