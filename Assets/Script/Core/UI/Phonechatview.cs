using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Core;
using Game.Dialogue.Core;
using Game.Dialogue.Data;

namespace Game.Dialogue.UI
{
    public class ContactEntry
    {
        public string Id;
        public CharacterData Character;
        public string Preview;
        public int Badge;
        public Action OnClick;
    }

    /// <summary>Semua urusan UI HP. Tidak berisi logika cerita.</summary>
    [Serializable]
    public class PhoneChatView
    {
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

        [Header("Layar percakapan")]
        [SerializeField] private GameObject threadRoot;
        [SerializeField] private GameObject threadHeader;
        [SerializeField] private TMP_Text threadNameText;
        [SerializeField] private Image threadAvatar;
        [SerializeField] private GameObject backButton;

        [Header("Wadah UI Bawah")]
        [SerializeField] private GameObject holdHint;
        [SerializeField] private Image holdFill;
        [SerializeField] private TMP_InputField inputField;

        /// <summary>Wadah bubble. Utamakan Content milik ScrollRect agar tidak salah isi di Inspector.</summary>
        private RectTransform ContentRoot =>
            scrollRect != null && scrollRect.content != null ? scrollRect.content : content;

        // ---------- Umum ----------

        public void Init()
        {
            SetPhoneVisible(false);
            SetHoldHint(false);
            if (inputField != null)
            {
                inputField.readOnly = true;
                inputField.text = string.Empty;
            }
        }

        public void SetPhoneVisible(bool visible)
        {
            if (phoneRoot != null) phoneRoot.SetActive(visible);
        }

        // ---------- Layar ----------

        public void ShowContactList(List<ContactEntry> entries)
        {
            SetHoldHint(false);
            SetThreadScreen(false);
            if (contactListRoot != null) contactListRoot.SetActive(true);

            ClearChildren<PhoneContactItemUI>(contactListContent);
            if (contactItemPrefab == null || contactListContent == null) return;

            foreach (ContactEntry e in entries)
            {
                PhoneContactItemUI item = UnityEngine.Object.Instantiate(contactItemPrefab, contactListContent);
                item.Set(e.Character, e.Id, e.Preview, e.Badge, e.OnClick);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(contactListContent);
        }

        public void ShowThread(ChatThread thread)
        {
            if (contactListRoot != null) contactListRoot.SetActive(false);
            SetThreadScreen(true);
            SetHoldHint(false);

            CharacterData character = thread.Character;
            if (threadNameText != null)
                threadNameText.text = character != null ? character.displayName : thread.Id;
            if (threadAvatar != null && character != null && character.portrait != null)
                threadAvatar.sprite = character.portrait;

            ClearChildren<ChatBubbleUI>(ContentRoot);
            foreach (ChatMessage m in thread.Messages)
                SpawnBubble(m.Speaker, m.Text, m.Outgoing);
            Refresh();
        }

        private void SetThreadScreen(bool active)
        {
            GameObject thread = threadRoot != null ? threadRoot : (scrollRect != null ? scrollRect.gameObject : null);
            if (thread != null) thread.SetActive(active);
            if (threadHeader != null) threadHeader.SetActive(active);
            if (backButton != null) backButton.SetActive(active);
        }

        // ---------- Chat ----------

        public void AddBubble(CharacterData speaker, string text, bool outgoing)
        {
            SpawnBubble(speaker, text, outgoing);
            Refresh();
        }

        private void SpawnBubble(CharacterData speaker, string text, bool outgoing)
        {
            ChatBubbleUI prefab = outgoing ? outgoingBubblePrefab : incomingBubblePrefab;
            RectTransform root = ContentRoot;
            if (prefab == null || root == null) return;

            ChatBubbleUI bubble = UnityEngine.Object.Instantiate(prefab, root);
            bubble.SetData(speaker, text);
            if (bubble.DialogueText != null) bubble.DialogueText.ForceMeshUpdate();
        }

        private void Refresh()
        {
            RectTransform root = ContentRoot;
            if (root != null) LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            if (scrollRect == null || !scrollRect.gameObject.activeInHierarchy) return;
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        // ---------- Input / hold ----------

        public void SetInputText(string value)
        {
            if (inputField != null) inputField.text = value;
        }

        public void SetHoldHint(bool visible)
        {
            if (holdHint != null) holdHint.SetActive(visible);
            SetPlaceholderText(visible ? "Tahan tombol apa saja untuk mengetik..." : string.Empty);
        }

        public void SetIncomingTyping(bool isTyping, CharacterData speaker = null)
        {
            SetPlaceholderText(isTyping ? "Lawan bicara sedang mengetik..." : string.Empty);
            if (threadNameText != null && speaker != null && !speaker.isPlayerSide)
            {
                threadNameText.text = isTyping
                    ? $"{speaker.displayName} <size=72%><color=#88D4AB>• mengetik...</color></size>"
                    : speaker.displayName;
            }
        }

        public void SetPlaceholderText(string text)
        {
            if (inputField != null && inputField.placeholder is TMP_Text placeholderTmp)
            {
                placeholderTmp.text = text;
            }
        }

        public void SetHoldFill(float amount)
        {
            if (holdFill != null) holdFill.fillAmount = Mathf.Clamp01(amount);
        }

        /// <summary>Hanya menghapus anak bertipe T (bubble / item kontak), objek UI lain aman.</summary>
        private static void ClearChildren<T>(RectTransform parent) where T : Component
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.GetComponent<T>() == null) continue;
                child.SetParent(null, false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }
}