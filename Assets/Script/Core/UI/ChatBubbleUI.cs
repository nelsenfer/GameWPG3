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

        /// <summary>Isi kotak ini dengan data karakter & teks. Tidak mengubah posisi apapun.</summary>
        public void SetData(CharacterData speaker, string text)
        {
            if (dialogueText != null) dialogueText.text = text;

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