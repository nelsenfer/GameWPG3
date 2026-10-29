using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Dialogue.Data;

namespace Game.Dialogue.UI
{
    /// <summary>
    /// Satu baris kontak di layar awal HP (foto, nama, cuplikan chat, lencana notifikasi).
    /// Hanya mengisi data; tata letak diatur lewat Unity Editor.
    /// Pasang di root prefab kontak (root harus punya Button).
    /// </summary>
    public class PhoneContactItemUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image avatarImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text previewText;
        [Tooltip("Lingkaran notifikasi. Disembunyikan kalau jumlahnya 0.")]
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private TMP_Text badgeText;

        public void Set(CharacterData character, string displayName, string preview, int badge, Action onClick)
        {
            if (nameText != null)
            {
                bool hasName = character != null && !string.IsNullOrWhiteSpace(character.displayName);
                nameText.text = hasName ? character.displayName : displayName;
            }
            if (previewText != null) previewText.text = preview;

            if (avatarImage != null)
            {
                bool hasPortrait = character != null && character.portrait != null;
                if (hasPortrait) avatarImage.sprite = character.portrait;
                // Tanpa potret: gambar tetap tampil sebagai placeholder (warna bawaan).
            }

            if (badgeRoot != null) badgeRoot.SetActive(badge > 0);
            if (badgeText != null) badgeText.text = badge.ToString();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke());
            }
        }
    }
}