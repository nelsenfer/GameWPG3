using Game.Dialogue.Data;

namespace Game.Dialogue.Events
{
    /// <summary>Jenis pilihan dialog, dibaca dari Ink tag (#reflective / #avoidant).</summary>
    public enum ChoiceAlignment { Neutral, Reflective, Avoidant }

    /// <summary>Gaya tampilan baris dialog, dibaca dari tag Ink.</summary>
    public enum DialogueStyle { Character, Narration, Phone }

    /// <summary>1 baris dialog siap tampil: sudah tahu karakter dan gaya tampilnya.</summary>
    public struct DialogueLinePayload
    {
        public CharacterData speaker;
        public string text;
        public DialogueStyle style;

        public DialogueLinePayload(
            CharacterData speaker,
            string text,
            DialogueStyle style = DialogueStyle.Character)
        {
            this.speaker = speaker;
            this.text = text;
            this.style = style;
        }
    }
}