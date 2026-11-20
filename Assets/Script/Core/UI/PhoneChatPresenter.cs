using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ink.Runtime;
using Game.Core;
using Game.Dialogue.Core;
using Game.Dialogue.Data;
using Game.Dialogue.Events;

namespace Game.Dialogue.UI
{
    /// <summary>
    /// Alur chat HP:
    ///  - Pesan lawan bicara : muncul otomatis setelah jeda, lalu cerita lanjut sendiri.
    ///  - Pesan pemain       : diketik lewat mekanik tahan tombol, lalu cerita lanjut sendiri.
    ///  - Pilihan Ink        : tombol pilihan / daftar kontak.
    ///  - Tombol Back        : boleh kapan saja; chat yang ditinggal bisa dilanjutkan dari daftar kontak.
    /// </summary>
    public class PhoneChatPresenter : MonoBehaviour
    {
        private enum Mode
        {
            Closed,      // HP tidak tampil
            ContactList, // menunggu pemain memilih kontak
            Thread,      // percakapan terbuka, tidak ada proses berjalan
            Busy,        // pesan masuk / pemain sedang mengetik
            Choice       // menunggu pemain memilih opsi balasan
        }

        private const string DirectThreadId = "_direct";

        [SerializeField] private PhoneChatView view;
        [SerializeField] private PhoneReplyTyper typer;
        [SerializeField] private CharacterDatabase characterDatabase;
        [Tooltip("Pembuat tombol pilihan (dipakai juga oleh dialog biasa).")]
        [SerializeField] private DialogueChoiceUIController choiceUI;

        [Header("Jeda")]
        [Tooltip("Jeda pesan masuk dari lawan bicara muncul di layar.")]
        [SerializeField] private float incomingMessageDelay = 1.5f;
        [Tooltip("Jeda setelah pesan pemain terkirim / sebelum HP ditutup saat semua chat selesai.")]
        [SerializeField] private float closePhoneDelay = 1f;

        [Header("References")]
        [SerializeField] private DialogueManager dialogueManager;
        [SerializeField] private LogPanelController logPanel;

        [Header("Events")]
        [SerializeField] private DialogueLineEventChannel lineEventChannel;
        [SerializeField] private DialogueChoicesEventChannel choicesEventChannel;
        [SerializeField] private VoidEventChannelSO dialogueEndedChannel;

        private readonly PhoneChatHistory history = new PhoneChatHistory();
        private Mode mode = Mode.Closed;
        private ChatThread current;           // != null  <=>  layar percakapan sedang tampil
        private List<Choice> contactChoices;  // daftar kontak terbaru dari Ink
        private Coroutine routine;

        // Cara mengulang proses yang sedang berjalan (dipakai saat pemain keluar di tengah chat).
        private System.Action resume;
        private ChatThread suspendedThread;
        private System.Action suspendedResume;

        // ---------- Lifecycle ----------

        private void OnEnable()
        {
            if (lineEventChannel != null) lineEventChannel.OnLineRaised += HandleLine;
            if (choicesEventChannel != null) choicesEventChannel.OnChoicesRaised += HandleChoices;
            if (dialogueEndedChannel != null) dialogueEndedChannel.OnRaised += HandleDialogueEnded;

            view.Init();
            mode = Mode.Closed;
        }

        private void OnDisable()
        {
            if (lineEventChannel != null) lineEventChannel.OnLineRaised -= HandleLine;
            if (choicesEventChannel != null) choicesEventChannel.OnChoicesRaised -= HandleChoices;
            if (dialogueEndedChannel != null) dialogueEndedChannel.OnRaised -= HandleDialogueEnded;

            StopRoutine();
        }

        // ---------- Event dari Ink ----------

        private void HandleLine(DialogueLinePayload payload)
        {
            if (mode == Mode.Busy) return;

            ClearChoices();

            if (payload.style != DialogueStyle.Phone)
            {
                ClosePhone();
                return;
            }

            EnsureThreadOpen(payload.speaker);
            PlayLine(payload);
        }

        private void HandleChoices(List<Choice> choices)
        {
            // DialogueManager me-raise baris + pilihan sekaligus. Saat pesan masih diproses,
            // abaikan dulu: setelah Advance() -> ContinueStory(), pilihan di-raise ulang.
            if (mode == Mode.Busy) return;

            if (PhoneChoiceTags.IsContactList(choices)) HandleContactChoices(choices);
            else ShowReplyChoices(choices);
        }

        private void HandleDialogueEnded()
        {
            StopRoutine();
            contactChoices = null;
            resume = null;
            suspendedThread = null;
            suspendedResume = null;
            ClosePhone();
        }

        // ---------- Pesan ----------

        private void PlayLine(DialogueLinePayload payload)
        {
            mode = Mode.Busy;
            resume = () => PlayLine(payload);

            bool outgoing = payload.speaker != null && payload.speaker.isPlayerSide;
            routine = StartCoroutine(outgoing ? PlayerReplyRoutine(payload) : IncomingRoutine(payload));
        }

        private IEnumerator IncomingRoutine(DialogueLinePayload payload)
        {
            yield return new WaitForSecondsRealtime(incomingMessageDelay);
            AddMessage(payload.speaker, payload.text, false);
            Advance(); // otomatis, tanpa input pemain
        }

        private IEnumerator PlayerReplyRoutine(DialogueLinePayload payload)
        {
            string text = payload.text ?? string.Empty;

            yield return typer.Run(text, view, IsLogOpen);

            view.SetInputText(string.Empty);
            AddMessage(payload.speaker, text, true);

            if (closePhoneDelay > 0f) yield return new WaitForSecondsRealtime(closePhoneDelay);
            Advance();
        }

        private void AddMessage(CharacterData speaker, string text, bool outgoing)
        {
            current?.Messages.Add(new ChatMessage { Speaker = speaker, Text = text, Outgoing = outgoing });
            view.AddBubble(speaker, text, outgoing);
        }

        /// <summary>Selesai satu pesan: kembali idle lalu lanjut cerita.</summary>
        private void Advance()
        {
            routine = null;
            resume = null;
            mode = Mode.Thread;
            if (dialogueManager != null) dialogueManager.ContinueStory();
        }

        // ---------- Pilihan ----------

        private void HandleContactChoices(List<Choice> choices)
        {
            contactChoices = choices;

            // Cerita kembali ke daftar kontak = chat yang tadi terbuka sudah selesai.
            if (current != null) current.IsCompleted = true;

            if (AllContactsCompleted())
            {
                routine = StartCoroutine(ClosePhoneRoutine());
                return;
            }


            ShowContactList();

        }

        private void ShowReplyChoices(List<Choice> choices)
        {
            mode = Mode.Choice;
            resume = () => ShowReplyChoices(choices);
            view.SetHoldHint(false);

            if (choiceUI == null)
            {
                Debug.LogError("[PhoneChatPresenter] Choice UI belum di-assign.", this);
                return;
            }

            choiceUI.ShowChoices(choices, index =>
            {
                resume = null;
                mode = Mode.Thread;
                if (dialogueManager != null) dialogueManager.ChooseOption(index);
            });
        }

        private IEnumerator ClosePhoneRoutine()
        {
            mode = Mode.Busy;
            resume = null;
            yield return new WaitForSecondsRealtime(closePhoneDelay);
            Advance();
        }

        private bool AllContactsCompleted()
        {
            if (contactChoices == null) return false;

            foreach (Choice choice in contactChoices)
            {
                if (PhoneChoiceTags.TryParseContact(choice, out string id, out _, out _)
                    && !history.IsCompleted(id))
                    return false;
            }
            return true;
        }

        // ---------- Back / lanjut chat ----------

        public void OnBackButtonClicked()
        {
            if (current == null) return; // sudah di daftar kontak

            if (mode == Mode.Busy || mode == Mode.Choice)
            {
                if (resume == null) return; // sedang menutup HP
                SuspendCurrentChat();
            }

            ShowContactList();
        }

        /// <summary>Tinggalkan chat di tengah jalan; cerita Ink berhenti di titik yang sama.</summary>
        private void SuspendCurrentChat()
        {
            StopRoutine();
            view.SetHoldHint(false);
            view.SetInputText(string.Empty);
            ClearChoices();

            suspendedThread = current;
            suspendedResume = resume;
            resume = null;
        }

        private void ResumeSuspendedChat()
        {
            ChatThread thread = suspendedThread;
            System.Action action = suspendedResume;
            suspendedThread = null;
            suspendedResume = null;

            OpenThread(thread);
            action?.Invoke();
        }

        // ---------- Layar ----------

        private void EnsureThreadOpen(CharacterData speaker)
        {
            view.SetPhoneVisible(true);
            if (current != null) return;

            CharacterData character = speaker != null && !speaker.isPlayerSide ? speaker : null;
            OpenThread(history.GetOrCreate(DirectThreadId, character));
        }

        private void OpenThread(ChatThread thread)
        {
            current = thread;
            mode = Mode.Thread;
            view.SetPhoneVisible(true);
            view.ShowThread(thread);
        }

        private void ShowContactList()
        {
            current = null;
            mode = Mode.ContactList;
            view.SetPhoneVisible(true);
            view.ShowContactList(BuildContactEntries());
        }

        private void ClosePhone()
        {
            current = null;
            mode = Mode.Closed;
            view.SetHoldHint(false);
            ClearChoices();
            view.SetPhoneVisible(false);
        }

        private List<ContactEntry> BuildContactEntries()
        {
            string activeId = suspendedThread != null ? suspendedThread.Id : null;
            var unread = new Dictionary<string, (int index, string preview, int badge)>();

            if (contactChoices != null)
            {
                for (int i = 0; i < contactChoices.Count; i++)
                {
                    if (!PhoneChoiceTags.TryParseContact(contactChoices[i], out string id, out string preview, out int badge)) continue;
                    if (id == activeId || unread.ContainsKey(id)) continue;

                    unread[id] = (i, preview, badge);
                    history.Register(id);
                }
            }

            var entries = new List<ContactEntry>();
            foreach (string id in history.Order)
            {
                if (id == DirectThreadId) continue;

                string contactId = id;
                CharacterData character = characterDatabase != null ? characterDatabase.GetByID(contactId) : null;

                // Chat yang ditinggal di tengah jalan: bisa dilanjutkan.
                if (contactId == activeId)
                {
                    entries.Add(new ContactEntry
                    {
                        Id = contactId,
                        Character = character,
                        Preview = suspendedThread.LastPreview,
                        Badge = 0,
                        OnClick = ResumeSuspendedChat
                    });
                    continue;
                }

                if (unread.TryGetValue(contactId, out var info))
                {
                    // Selama ada chat yang belum selesai, kontak baru belum bisa dibuka
                    // (cerita Ink masih berada di dalam chat tersebut).
                    System.Action onClick = null;
                    if (suspendedThread == null)
                    {
                        int choiceIndex = info.index;
                        onClick = () => PickUnreadContact(choiceIndex, contactId, character);
                    }

                    entries.Add(new ContactEntry
                    {
                        Id = contactId,
                        Character = character,
                        Preview = info.preview,
                        Badge = info.badge,
                        OnClick = onClick
                    });
                }
                else
                {
                    ChatThread thread = history.Get(contactId);
                    if (thread == null || thread.Messages.Count == 0) continue;

                    entries.Add(new ContactEntry
                    {
                        Id = contactId,
                        Character = character,
                        Preview = thread.LastPreview,
                        Badge = 0,
                        OnClick = () => OpenThread(thread)
                    });
                }
            }
            return entries;
        }

        private void PickUnreadContact(int choiceIndex, string id, CharacterData character)
        {
            OpenThread(history.GetOrCreate(id, character));
            if (dialogueManager != null) dialogueManager.ChooseOption(choiceIndex);
        }

        // ---------- Util ----------

        private void ClearChoices()
        {
            if (choiceUI != null) choiceUI.ClearChoices();
        }

        private bool IsLogOpen() => logPanel != null && logPanel.IsOpen;

        private void StopRoutine()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
        }
    }
}