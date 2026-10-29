using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Ink.Runtime;
using Game.Core;
using Game.Dialogue.Core;
using Game.Dialogue.Data;
using Game.Dialogue.Events;

namespace Game.Dialogue.UI
{
    public class PhoneChatPresenter : MonoBehaviour
    {
        private enum State { Idle, WaitingHold, Typing }

        private class ChatMessage
        {
            public CharacterData speaker;
            public string text;
            public bool outgoing;
        }

        private class ChatThread
        {
            public string id;
            public CharacterData character;
            public bool isCompleted; 
            public readonly List<ChatMessage> messages = new List<ChatMessage>();
        }

        private class UnreadContact
        {
            public int index;
            public string preview;
            public int badge;
        }

        [Header("Tampilan HP")]
        [SerializeField] private GameObject phoneRoot;
        [SerializeField] private RectTransform content;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private ChatBubbleUI incomingBubblePrefab;
        [SerializeField] private ChatBubbleUI outgoingBubblePrefab;

        [Header("Daftar kontak (layar awal HP)")]
        [SerializeField] private GameObject contactListRoot;
        [SerializeField] private RectTransform contactListContent;
        [SerializeField] private PhoneContactItemUI contactItemPrefab;
        [SerializeField] private CharacterDatabase characterDatabase;

        [Header("Layar percakapan")]
        [SerializeField] private GameObject threadRoot;
        [SerializeField] private GameObject threadHeader;
        [SerializeField] private TMP_Text threadNameText;
        [SerializeField] private Image threadAvatar;
        [SerializeField] private GameObject backButton;

        [Header("Wadah UI Bawah")]
        [SerializeField] private GameObject holdHint;
        [SerializeField] private Image holdFill;
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private bool choicesReplaceInput = false;

        [Header("Mekanik balas")]
        [SerializeField] private bool requireHoldToType = true;
        [SerializeField] private float holdSeconds = 0.35f;
        [SerializeField] private float charsPerSecond = 35f;
        [SerializeField] private float autoTypeDelay = 0.4f;
        [SerializeField] private float firstCharPause = 0f;
        [SerializeField] private float sendDelay = 0.17f;
        [Tooltip("Jeda sebelum HP tertutup saat semua chat selesai.")]
        [SerializeField] private float closePhoneDelay = 1.5f; // <-- FITUR BARU

        [Header("References")]
        [SerializeField] private DialogueManager dialogueManager;
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
        private Coroutine typingRoutine;

        private bool hasPendingMessage;
        private CharacterData pendingSpeaker;
        private string pendingText;
        private bool isProcessingReply = false; 

        private readonly Dictionary<string, ChatThread> threads = new Dictionary<string, ChatThread>();
        private readonly List<string> contactOrder = new List<string>();
        private string currentChatId;
        private bool viewingHistoryOnly;

        private List<Choice> contactChoices;
        private List<Choice> lastKnownContactChoices; 
        private List<Button> spawnedChoiceButtons = new List<Button>();

        private void OnEnable()
        {
            if (lineEventChannel != null) lineEventChannel.OnLineRaised += HandleLine;
            if (choicesEventChannel != null) choicesEventChannel.OnChoicesRaised += HandleChoices;
            if (dialogueEndedChannel != null) dialogueEndedChannel.OnRaised += HandleDialogueEnded;

            SetPhoneVisible(false);
            SetHoldUI(false);
            PrepareInputField();
        }

        private void OnDisable()
        {
            if (lineEventChannel != null) lineEventChannel.OnLineRaised -= HandleLine;
            if (choicesEventChannel != null) choicesEventChannel.OnChoicesRaised -= HandleChoices;
            if (dialogueEndedChannel != null) dialogueEndedChannel.OnRaised -= HandleDialogueEnded;
        }

        private void Update()
        {
            if (!phoneVisible) return;
            if (contactListRoot != null && contactListRoot.activeSelf) return;
            if (logPanel != null && logPanel.IsOpen) return;

            switch (state)
            {
                case State.Idle:
                    if (viewingHistoryOnly || isChoiceActive || contactChoices != null) break;
                    if (isProcessingReply) break;

                    if (AdvancePressed())
                        dialogueManager?.ContinueStory();
                    break;

                case State.WaitingHold:
                    UpdateHold();
                    break;
            }
        }

        private void HandleLine(DialogueLinePayload payload)
        {
            if (isProcessingReply) return;

            StopTyping();
            contactChoices = null; 
            viewingHistoryOnly = false;
            ClearChoiceButtons(); 

            if (payload.style != DialogueStyle.Phone)
            {
                SetHoldUI(false);
                state = State.Idle;
                // SetPhoneVisible(false); <-- JANGAN MATIKAN HP DISINI, TUNGGU END
                return;
            }

            SetPhoneVisible(true);
            ShowThreadView();

            bool outgoing = payload.speaker != null && payload.speaker.isPlayerSide;

            if (outgoing)
            {
                BeginReply(payload);
            }
            else
            {
                AddMessage(payload.speaker, payload.text, false);
                state = State.Idle;
            }
        }

        private void HandleChoices(List<Choice> choices)
        {
            if (PhoneChoiceTags.IsContactList(choices))
            {
                contactChoices = choices;
                lastKnownContactChoices = choices; 

                if (AreAllChatsCompleted()) 
                {
                    // JIKA SEMUA SELESAI, TUNGGU JEDA LALU TUTUP
                    StartCoroutine(ClosePhoneRoutine());
                    return;
                }

                if (state == State.Idle && (content == null || content.childCount == 0))
                {
                    ShowContactList();
                }
                return;
            }

            isChoiceActive = true;
            SetHoldUI(false);
            state = State.Idle;

            ClearChoiceButtons();
            if (choiceContainer != null && choices.Count > 0)
            {
                choiceContainer.gameObject.SetActive(true);
                SetInputFieldVisible(false);

                for (int i = 0; i < choices.Count; i++)
                {
                    int index = i;
                    Button btn = CreateChoiceButton(choices[i].text, () => {
                        isChoiceActive = false;
                        ClearChoiceButtons();
                        isProcessingReply = true; 
                        dialogueManager?.ChooseOption(index);
                    });
                    spawnedChoiceButtons.Add(btn);
                }
            }
        }

        private IEnumerator ClosePhoneRoutine()
        {
            // Beri jeda agar pemain bisa melihat chat terakhir
            yield return new WaitForSecondsRealtime(closePhoneDelay);
            dialogueManager?.ContinueStory(); 
        }

        private void HandleDialogueEnded()
        {
            StopTyping(); 
            SetHoldUI(false);
            state = State.Idle;
            isChoiceActive = false;
            contactChoices = null;
            viewingHistoryOnly = false;
            SetPhoneVisible(false);
            ClearChoiceButtons();
            isProcessingReply = false; 
        }

        public void OnBackButtonClicked()
        {
            StopTyping(); 
            ClearChildren(content);
            ShowContactList();
            isProcessingReply = false; 
        }

        private bool AreAllChatsCompleted()
        {
            if (contactChoices == null) return false;

            foreach (var choice in contactChoices)
            {
                if (PhoneChoiceTags.TryParseContact(choice, out string id, out _, out _))
                {
                    if (!threads.TryGetValue(id, out ChatThread thread) || !thread.isCompleted)
                        return false;
                }
            }
            return true; 
        }

        private void ShowContactList()
        {
            SetHoldUI(false);
            SetPhoneVisible(true);
            viewingHistoryOnly = false;
            currentChatId = null;

            SetThreadViewActive(false);
            if (contactListRoot != null) contactListRoot.SetActive(true);
            if (backButton != null) backButton.SetActive(false);

            ClearChildren(contactListContent);
            if (contactItemPrefab == null || contactListContent == null) return;

            List<Choice> choicesToRender = contactChoices ?? lastKnownContactChoices;

            Dictionary<string, UnreadContact> unread = new Dictionary<string, UnreadContact>();
            if (choicesToRender != null)
            {
                for (int i = 0; i < choicesToRender.Count; i++)
                {
                    PhoneChoiceTags.TryParseContact(choicesToRender[i], out string contactId, out string preview, out int badge);
                    if (string.IsNullOrEmpty(contactId) || unread.ContainsKey(contactId)) continue;

                    unread[contactId] = new UnreadContact { index = i, preview = preview, badge = badge };
                    if (!contactOrder.Contains(contactId)) contactOrder.Add(contactId);
                }
            }

            foreach (string id in contactOrder)
            {
                UnreadContact unreadInfo = null;
                ChatThread readThread = null;
                bool isUnread = unread.TryGetValue(id, out unreadInfo);
                bool isRead = !isUnread && threads.TryGetValue(id, out readThread) && readThread.messages.Count > 0;
                
                if (!isUnread && !isRead) continue;

                CharacterData character = characterDatabase != null ? characterDatabase.GetByID(id) : null;
                PhoneContactItemUI item = Instantiate(contactItemPrefab, contactListContent);

                if (isUnread)
                {
                    int index = unreadInfo.index;
                    item.Set(character, id, unreadInfo.preview, unreadInfo.badge, () => PickUnreadContact(index, character, id));
                }
                else
                {
                    ChatThread thread = readThread;
                    item.Set(character, id, LastPreview(thread), 0, () => OpenReadContact(thread));
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(contactListContent);
        }

        private void PickUnreadContact(int choiceIndex, CharacterData character, string id)
        {
            isChoiceActive = false;
            contactChoices = null; 
            viewingHistoryOnly = false;
            if (contactListRoot != null) contactListRoot.SetActive(false);
            ClearChoiceButtons();

            currentChatId = id;
            ChatThread thread = GetOrCreateThread(id, character);
            ApplyThreadHeader(thread);
            ClearChildren(content);

            ShowThreadView();
            RebuildHistory(thread); 
            dialogueManager?.ChooseOption(choiceIndex);
        }

        private void OpenReadContact(ChatThread thread)
        {
            viewingHistoryOnly = true;
            currentChatId = thread.id;
            if (contactListRoot != null) contactListRoot.SetActive(false);

            ApplyThreadHeader(thread);
            ClearChildren(content);
            ShowThreadView();
            RebuildHistory(thread);
        }

        private ChatThread GetOrCreateThread(string id, CharacterData character)
        {
            if (!threads.TryGetValue(id, out ChatThread thread))
            {
                thread = new ChatThread { id = id, character = character, isCompleted = false };
                threads.Add(id, thread);
            }
            else if (thread.character == null)
            {
                thread.character = character;
            }

            if (!contactOrder.Contains(id)) contactOrder.Add(id);
            return thread;
        }

        private void ApplyThreadHeader(ChatThread thread)
        {
            CharacterData character = thread.character;
            if (threadNameText != null) threadNameText.text = character != null ? character.displayName : thread.id;
            if (threadAvatar != null)
            {
                bool hasPortrait = character != null && character.portrait != null;
                if (hasPortrait) threadAvatar.sprite = character.portrait;
            }
        }

        private void RebuildHistory(ChatThread thread)
        {
            foreach (ChatMessage message in thread.messages)
                SpawnBubble(message.speaker, message.text, message.outgoing, false);
            RefreshLayout();
        }

        private void AddMessage(CharacterData speaker, string text, bool outgoing)
        {
            if (currentChatId != null && threads.TryGetValue(currentChatId, out ChatThread thread))
                thread.messages.Add(new ChatMessage { speaker = speaker, text = text, outgoing = outgoing });

            SpawnBubble(speaker, text, outgoing, true);
        }

        private static string LastPreview(ChatThread thread)
        {
            if (thread.messages.Count == 0) return string.Empty;
            string text = thread.messages[thread.messages.Count - 1].text ?? string.Empty;
            return text.Replace('\n', ' ');
        }

        private void ShowThreadView()
        {
            if (contactListRoot != null) contactListRoot.SetActive(false);
            SetThreadViewActive(true);
            if (backButton != null) backButton.SetActive(true); 
            // Pastikan Input Field selalu muncul saat masuk ke thread chat
            SetInputFieldVisible(true);
        }

        private void SetThreadViewActive(bool active)
        {
            GameObject thread = threadRoot != null ? threadRoot : (scrollRect != null ? scrollRect.gameObject : null);
            if (thread != null) thread.SetActive(active);
            if (threadHeader != null) threadHeader.SetActive(active);
            if (backButton != null) backButton.SetActive(active);
        }

        private Button CreateChoiceButton(string text, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject("ChoiceButton");
            btnObj.transform.SetParent(choiceContainer, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.9f, 0.9f, 0.9f); 

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            TMP_Text txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = text;
            txt.color = Color.black;
            txt.alignment = TextAlignmentOptions.Center;

            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            LayoutElement layout = btnObj.AddComponent<LayoutElement>();
            layout.minHeight = 40f;

            return btn;
        }

        private void ClearChoiceButtons()
        {
            foreach (var btn in spawnedChoiceButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            spawnedChoiceButtons.Clear();
            if (choiceContainer != null) choiceContainer.gameObject.SetActive(false);
            SetInputFieldVisible(true);
        }

        private void ClearChildren(RectTransform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                child.SetParent(null, false); 
                Destroy(child.gameObject);
            }
        }

        private void SpawnBubble(CharacterData speaker, string text, bool outgoing, bool refresh)
        {
            ChatBubbleUI prefab = outgoing ? outgoingBubblePrefab : incomingBubblePrefab;
            if (prefab == null || content == null) return;

            ChatBubbleUI bubble = Instantiate(prefab, content);
            bubble.SetData(speaker, text);
            if (bubble.DialogueText != null) bubble.DialogueText.ForceMeshUpdate();
            if (refresh) RefreshLayout();
        }

        private void BeginReply(DialogueLinePayload payload)
        {
            hasPendingMessage = true;
            pendingSpeaker = payload.speaker;
            pendingText = payload.text ?? string.Empty;
            SetInputText(string.Empty); 

            if (requireHoldToType)
            {
                holdTimer = 0f;
                released = !Input.anyKey;
                state = State.WaitingHold;
                SetHoldUI(true);
                UpdateHoldFill(0f);
            }
            else
            {
                StartTyping(autoTypeDelay);
            }
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

            UpdateHoldFill(holdSeconds > 0f ? holdTimer / holdSeconds : 1f);

            if (holdTimer >= holdSeconds) StartTyping(0f);
        }

        private void StartTyping(float startDelay)
        {
            state = State.Typing;
            SetHoldUI(false);
            typingRoutine = StartCoroutine(TypeRoutine(startDelay));
        }

        private IEnumerator TypeRoutine(float startDelay)
        {
            if (startDelay > 0f) yield return new WaitForSecondsRealtime(startDelay);

            string full = pendingText ?? string.Empty;
            int total = full.Length;
            float cps = Mathf.Max(1f, charsPerSecond);
            float shown = 0f;
            int lastCount = -1;

            if (firstCharPause > 0f && total > 1)
            {
                SetInputText(full.Substring(0, SafeLength(full, 1)));
                lastCount = 1;
                shown = 1f;
                yield return new WaitForSecondsRealtime(firstCharPause);
            }

            while (shown < total)
            {
                shown += cps * Time.unscaledDeltaTime;
                int count = Mathf.Min(total, Mathf.FloorToInt(shown));

                if (count != lastCount)
                {
                    lastCount = count;
                    SetInputText(full.Substring(0, SafeLength(full, count)));
                }
                yield return null;
            }

            SetInputText(full);
            if (sendDelay > 0f) yield return new WaitForSecondsRealtime(sendDelay);

            typingRoutine = null;
            CommitPendingMessage();
            state = State.Idle;
            ScrollToBottom();

            isProcessingReply = false; 
            dialogueManager?.ContinueStory();
        }

        private void CommitPendingMessage()
        {
            if (!hasPendingMessage) return;
            hasPendingMessage = false;

            SetInputText(string.Empty);
            AddMessage(pendingSpeaker, pendingText, true);

            pendingSpeaker = null;
            pendingText = null;
        }

        private void StopTyping()
        {
            if (typingRoutine != null) StopCoroutine(typingRoutine);
            typingRoutine = null;
            CommitPendingMessage();
            
            if (currentChatId != null && threads.TryGetValue(currentChatId, out ChatThread thread))
            {
                thread.isCompleted = true;
            }
        }

        private void PrepareInputField()
        {
            if (inputField == null) return;
            inputField.readOnly = true; 
            inputField.text = string.Empty;
        }

        private void SetInputFieldVisible(bool visible)
        {
            if (inputField != null && choicesReplaceInput) inputField.gameObject.SetActive(visible);
        }

        private void SetInputText(string value)
        {
            if (inputField != null) inputField.text = value;
        }

        private static int SafeLength(string s, int count)
        {
            if (count <= 0) return 0;
            if (count >= s.Length) return s.Length;

            int open = s.LastIndexOf('<', count - 1);
            if (open < 0 || s.LastIndexOf('>', count - 1) > open) return count;

            int close = s.IndexOf('>', open);
            return close >= 0 ? close + 1 : count;
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