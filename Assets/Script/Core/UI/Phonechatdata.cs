using System.Collections.Generic;
using Game.Dialogue.Data;

namespace Game.Dialogue.UI
{
    public class ChatMessage
    {
        public CharacterData Speaker;
        public string Text;
        public bool Outgoing;
    }

    public class ChatThread
    {
        public string Id;
        public CharacterData Character;
        public bool IsCompleted;
        public readonly List<ChatMessage> Messages = new List<ChatMessage>();

        public string LastPreview
        {
            get
            {
                if (Messages.Count == 0) return string.Empty;
                string text = Messages[Messages.Count - 1].Text ?? string.Empty;
                return text.Replace('\n', ' ');
            }
        }
    }

    /// <summary>Menyimpan semua thread chat dan urutan kontak. Tidak tahu apa pun soal UI.</summary>
    public class PhoneChatHistory
    {
        private readonly Dictionary<string, ChatThread> threads = new Dictionary<string, ChatThread>();
        private readonly List<string> order = new List<string>();

        public IReadOnlyList<string> Order => order;

        public ChatThread Get(string id)
        {
            return threads.TryGetValue(id, out ChatThread thread) ? thread : null;
        }

        public ChatThread GetOrCreate(string id, CharacterData character)
        {
            if (!threads.TryGetValue(id, out ChatThread thread))
            {
                thread = new ChatThread { Id = id, Character = character };
                threads.Add(id, thread);
            }
            else if (thread.Character == null)
            {
                thread.Character = character;
            }

            Register(id);
            return thread;
        }

        public void Register(string id)
        {
            if (!order.Contains(id)) order.Add(id);
        }

        public bool IsCompleted(string id)
        {
            ChatThread thread = Get(id);
            return thread != null && thread.IsCompleted;
        }
    }
}