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
    /// <summary>
    /// Menampilkan baris Ink bertag #hp sebagai chat HP.
    /// - Layar awal: daftar kontak (pilihan Ink bertag #kontak:ID, #preview:teks, #badge:angka).
    ///   Pemain memilih kontak, percakapannya terbuka, selesai -> kembali ke daftar.
    /// - Pesan masuk (pembicara bukan sisi pemain): muncul langsung, klik / Space / Enter untuk lanjut.
    /// - Balasan pemain (pembicara isPlayerSide): pemain menahan tombol APA SAJA sebentar,
    ///   lalu teks balasan "terketik" sendiri. Kalau ada pilihan balasan, pilih dulu, baru mekanik ini jalan.
    /// Script ini hanya mengisi data; tata letak (posisi, ukuran, warna) diatur lewat Unity Editor.
    /// </summary>
    public class PhoneChatPresenter : MonoBehaviour
    {
        private enum State { Idle, WaitingHold, Typing }

        [Header("Tampilan HP")]
        [Tooltip("Root panel HP. Dimatikan saat tidak ada chat.")]
        [SerializeField] private GameObject phoneRoot;
        [Tooltip("Content di dalam Scroll View HP (diberi Vertical Layout Group + Content Size Fitter).")]
        [SerializeField] private RectTransform content;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private ChatBubbleUI incomingBubblePrefab;
        [SerializeField] private ChatBubbleUI outgoingBubblePrefab;

        [Header("Daftar kontak (layar awal HP)")]
        [Tooltip("Panel daftar kontak. Tampil saat HP menunggu pemain memilih kontak.")]
        [SerializeField] private GameObject contactListRoot;
        [Tooltip("Wadah baris kontak (beri Vertical Layout Group).")]
        [SerializeField] private RectTransform contactListContent;
        [SerializeField] private PhoneContactItemUI contactItemPrefab;
        [Tooltip("Untuk mencari foto/nama kontak dari ID di tag #kontak.")]
        [SerializeField] private CharacterDatabase characterDatabase;
        [Tooltip("Jeda sebelum daftar kontak muncul lagi setelah balasan selesai.")]
        [SerializeField] private float returnToListDelay = 0.8f;

        [Header("Layar percakapan (opsional)")]
        [Tooltip("Objek yang disembunyikan saat daftar kontak tampil. Kosong = Scroll View.")]
        [SerializeField] private GameObject threadRoot;
        [Tooltip("Header percakapan (foto + nama lawan chat). Opsional.")]
        [SerializeField] private GameObject threadHeader;
        [SerializeField] private TMP_Text threadNameText;
        [SerializeField] private Image threadAvatar;

        [Header("Petunjuk balas (opsional)")]
        [Tooltip("Objek petunjuk 'tahan tombol apa saja'. Tampil hanya saat menunggu pemain membalas.")]
        [SerializeField] private GameObject holdHint;
        [Tooltip("Image bertipe Filled untuk menunjukkan progres tahan tombol.")]
        [SerializeField] private Image holdFill;

        [Header("Mekanik balas")]
        [SerializeField] private float holdSeconds = 0.35f;
        [SerializeField] private float charsPerSecond = 35f;

        [Header("References")]
        [SerializeField] private DialogueManager dialogueManager;
        [Tooltip("Opsional: selama panel log terbuka, input chat HP diabaikan.")]
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
        private List<Choice> pendingContactChoices;

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
        }

        private void Update()
        {
            if (!phoneVisible || isChoiceActive) return;
            if (logPanel != null && logPanel.IsOpen) return;

            switch (state)
            {
                case State.Idle:
                    if (AdvancePressed())
                    {
                        // Percakapan selesai & daftar kontak menunggu: klik membukanya.
                        if (pendingContactChoices != null) ShowContactList(pendingContactChoices);
                        else dialogueManager?.ContinueStory();
                    }
                    break;

                case State.WaitingHold:
                    UpdateHold();
                    break;
            }
        }

        // ---------- Event dari DialogueManager ----------

        private void HandleLine(DialogueLinePayload payload)
        {
            isChoiceActive = false;
            pendingContactChoices = null;

            // Baris bukan chat HP -> HP disembunyikan (riwayatnya tetap sampai dialog selesai).
            if (payload.style != DialogueStyle.Phone)
            {
                StopTyping();
                SetHoldUI(false);
                state = State.Idle;
                SetPhoneVisible(false);
                return;
            }

            // Panel harus aktif SEBELUM bubble dibuat, supaya layout langsung terhitung.
            SetPhoneVisible(true);
            ShowThreadView();

            bool outgoing = payload.speaker != null && payload.speaker.isPlayerSide;
            ChatBubbleUI prefab = outgoing ? outgoingBubblePrefab : incomingBubblePrefab;
            if (prefab == null || content == null)
            {
                Debug.LogError("[PhoneChatPresenter] Prefab bubble masuk/keluar atau Content belum di-assign.", this);
                state = State.Idle;
                return;
            }

            ChatBubbleUI bubble = Instantiate(prefab, content);
            bubble.SetData(payload.speaker, payload.text);
            if (bubble.DialogueText != null) bubble.DialogueText.ForceMeshUpdate();
            RefreshLayout();
            StartCoroutine(RefreshNextFrame()); // ukuran bubble kadang baru benar setelah 1 frame

            if (outgoing) BeginReply(bubble);
            else state = State.Idle;
        }

        private IEnumerator RefreshNextFrame()
        {
            yield return null;
            RefreshLayout();
        }

        private void HandleChoices(List<Choice> choices)
        {
            Debug.Log("[PhoneChatPresenter] Pilihan masuk. Daftar kontak? " + PhoneChoiceTags.IsContactList(choices)
                + (choices.Count > 0 ? " | contoh teks: \"" + choices[0].text + "\"" : ""));

            // Pilihan kontak: ditampilkan sebagai daftar kontak HP, bukan tombol biasa.
            if (PhoneChoiceTags.IsContactList(choices))
            {
                bool nothingShownYet = content == null || content.childCount == 0;
                if (state == State.Idle && nothingShownYet) ShowContactList(choices);
                else pendingContactChoices = choices; // tunggu balasan selesai dulu
                return;
            }

            isChoiceActive = true; // pemain wajib memilih lewat tombol; klik sembarangan tidak melanjutkan
            SetHoldUI(false);
        }

        private void HandleDialogueEnded()
        {
            StopTyping();
            SetHoldUI(false);
            state = State.Idle;
            isChoiceActive = false;
            pendingContactChoices = null;
            SetPhoneVisible(false);

            ClearChildren(content);
            ClearChildren(contactListContent);
        }

        // ---------- Daftar kontak ----------

        private void ShowContactList(List<Choice> choices)
        {
            pendingContactChoices = null;
            isChoiceActive = true; // klik sembarangan tidak melanjutkan; pemain wajib pilih kontak
            SetHoldUI(false);
            SetPhoneVisible(true);

            SetThreadViewActive(false);
            if (contactListRoot != null) contactListRoot.SetActive(true);

            ClearChildren(contactListContent);
            if (contactItemPrefab == null || contactListContent == null)
            {
                Debug.LogError("[PhoneChatPresenter] Contact Item Prefab / Contact List Content belum di-assign.", this);
                return;
            }

            for (int i = 0; i < choices.Count; i++)
            {
                int index = i;
                Choice choice = choices[i];

                PhoneChoiceTags.TryParseContact(choice, out string characterId, out string preview, out int badge);

                CharacterData character = characterDatabase != null ? characterDatabase.GetByID(characterId) : null;

                PhoneContactItemUI item = Instantiate(contactItemPrefab, contactListContent);
                item.Set(character, characterId, preview, badge, () => PickContact(index, character, characterId));
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contactListContent);
        }

        private void PickContact(int choiceIndex, CharacterData character, string fallbackName)
        {
            isChoiceActive = false;
            if (contactListRoot != null) contactListRoot.SetActive(false);

            // Tiap percakapan dimulai dari layar bersih.
            ClearChildren(content);

            if (threadNameText != null) threadNameText.text = character != null ? character.displayName : fallbackName;
            if (threadAvatar != null)
            {
                bool hasPortrait = character != null && character.portrait != null;
                if (hasPortrait) threadAvatar.sprite = character.portrait;
            }

            ShowThreadView();
            dialogueManager?.ChooseOption(choiceIndex);
        }

        private void ShowThreadView()
        {
            if (contactListRoot != null) contactListRoot.SetActive(false);
            SetThreadViewActive(true);
        }

        private void SetThreadViewActive(bool active)
        {
            GameObject thread = threadRoot != null ? threadRoot : (scrollRect != null ? scrollRect.gameObject : null);
            if (thread != null) thread.SetActive(active);
            if (threadHeader != null) threadHeader.SetActive(active);
        }

        private static void ClearChildren(RectTransform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        // ---------- Mekanik balas ----------

        private void BeginReply(ChatBubbleUI bubble)
        {
            pendingBubble = bubble;
            holdTimer = 0f;
            released = !Input.anyKey; // tombol yang masih tertahan dari klik sebelumnya tidak dihitung
            state = State.WaitingHold;

            if (bubble.DialogueText != null) bubble.DialogueText.maxVisibleCharacters = 0;
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

            UpdateHoldFill(holdSeconds > 0f ? holdTimer / holdSeconds : 1f);

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

            // Balasan selesai & ada daftar kontak menunggu: kembali ke daftar setelah jeda singkat.
            if (pendingContactChoices != null)
            {
                yield return new WaitForSecondsRealtime(returnToListDelay);
                if (pendingContactChoices != null && state == State.Idle)
                    ShowContactList(pendingContactChoices);
            }
        }

        private void StopTyping()
        {
            if (typingRoutine != null) StopCoroutine(typingRoutine);
            typingRoutine = null;

            // Kalau terpotong, tampilkan teks utuh supaya tidak ada balasan yang setengah terlihat.
            if (pendingBubble != null && pendingBubble.DialogueText != null)
                pendingBubble.DialogueText.maxVisibleCharacters = int.MaxValue;
            pendingBubble = null;
        }

        // ---------- Util ----------

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