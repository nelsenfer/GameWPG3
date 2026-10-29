using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Dialogue.Data;

namespace Game.Dialogue.UI
{
    /// <summary>
    /// Mengisi 1 kotak dialog dengan data (nama, gambar, teks) SAJA.
    /// Tidak mengatur posisi/layout apapun — urusan tata letak sepenuhnya
    /// di tangan GD lewat Unity Editor (drag-drop manual, anchor, dst),
    /// biar bebas direvisi kapan saja tanpa perlu ubah kode.
    /// </summary>
    public class ChatBubbleUI : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Image bubbleBackground;

        [Header("Bubble Style (opsional)")]
        [SerializeField] private Color npcBubbleColor = Color.white;
        [SerializeField] private Color playerBubbleColor = new Color(0.7f, 0.85f, 1f);

        [Header("Lebar otomatis (opsional, untuk bubble chat HP)")]
        [Tooltip("Layout Element di objek Bubble. Kosong = fitur ini mati.")]
        [SerializeField] private LayoutElement bubbleLayout;
        [Tooltip("Lebar maksimal bubble. Teks yang lebih panjang turun ke baris berikutnya.")]
        [SerializeField] private float maxBubbleWidth = 450f;
        [Tooltip("Total padding kiri + kanan pada Bubble (samakan dengan Vertical Layout Group-nya).")]
        [SerializeField] private float horizontalPadding = 30f;

        /// <summary>Teks isi dialog (dipakai chat HP untuk efek mengetik).</summary>
        public TMP_Text DialogueText => dialogueText;

        /// <summary>Warna latar bubble saat ini, dipakai ekor bubble komik.</summary>
        public Color BackgroundColor => bubbleBackground != null ? bubbleBackground.color : Color.white;

        /// <summary>Isi kotak ini dengan data karakter dan teks.</summary>
        public void SetData(CharacterData speaker, string text)
        {
            if (dialogueText != null) dialogueText.text = text;

            if (bubbleLayout != null && dialogueText != null)
            {
                float wanted = dialogueText.preferredWidth + horizontalPadding;
                bubbleLayout.preferredWidth = Mathf.Min(wanted, maxBubbleWidth);
            }

            if (portraitImage != null)
                portraitImage.gameObject.SetActive(speaker != null && speaker.portrait != null);

            if (speaker != null)
            {
                if (nameText != null) nameText.text = speaker.displayName;

                if (portraitImage != null && speaker.portrait != null)
                    portraitImage.sprite = speaker.portrait;

                if (bubbleBackground != null)
                    bubbleBackground.color = speaker.isPlayerSide ? playerBubbleColor : npcBubbleColor;
            }
            else
            {
                if (nameText != null) nameText.text = string.Empty;
            }
        }
    }
}