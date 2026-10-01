using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Dialogue.Data;

namespace Game.Dialogue.UI
{
    public class ChatBubbleUI : MonoBehaviour
    {
        [SerializeField] private HorizontalLayoutGroup rowLayoutGroup;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Image bubbleBackground;
        [SerializeField] private Color npcBubbleColor = Color.white;
        [SerializeField] private Color playerBubbleColor = new Color(0.7f, 0.85f, 1f);

        public void SetData(CharacterData speaker, string text)
        {
            bool isPlayerSide = speaker != null && speaker.isPlayerSide;
            if (dialogueText != null) dialogueText.text = text;

            if (speaker != null)
            {
                if (nameText != null) nameText.text = speaker.displayName;
                if (portraitImage != null)
                {
                    portraitImage.sprite = speaker.portrait;
                    portraitImage.gameObject.SetActive(speaker.portrait != null);
                }
            }
            else
            {
                if (nameText != null) nameText.text = string.Empty;
                if (portraitImage != null) portraitImage.gameObject.SetActive(false);
            }

            ApplySide(isPlayerSide);
        }

        private void ApplySide(bool isPlayerSide)
        {
            if (rowLayoutGroup != null)
            {
                rowLayoutGroup.reverseArrangement = isPlayerSide;
                rowLayoutGroup.childAlignment = isPlayerSide ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            }

            if (nameText != null) nameText.alignment = isPlayerSide ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            if (dialogueText != null) dialogueText.alignment = isPlayerSide ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            if (bubbleBackground != null) bubbleBackground.color = isPlayerSide ? playerBubbleColor : npcBubbleColor;
        }
    }
}