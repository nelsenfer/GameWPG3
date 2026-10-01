using System;
using System.Collections.Generic;
using Ink.Runtime;
using UnityEngine;
using Game.Dialogue.Data;

namespace Game.Dialogue.Events
{
    /// <summary>Jenis pilihan dialog, dibaca dari Ink tag (#reflective / #avoidant).</summary>
    public enum ChoiceAlignment { Neutral, Reflective, Avoidant }

    /// <summary>1 baris dialog siap tampil: sudah tahu karakternya siapa, bukan cuma teks mentah.</summary>
    public struct DialogueLinePayload
    {
        public CharacterData speaker;
        public string text;

        public DialogueLinePayload(CharacterData speaker, string text)
        {
            this.speaker = speaker;
            this.text = text;
        }
    }

    /// <summary>Diraise tiap pemain memilih 1 choice reflektif/avoidant.</summary>
    [CreateAssetMenu(menuName = "Game/Events/Choice Event Channel", fileName = "New Choice Event Channel")]
    public class ChoiceEventChannel : ScriptableObject
    {
        public event Action<ChoiceAlignment> OnChoiceMade;

        public void Raise(ChoiceAlignment alignment) => OnChoiceMade?.Invoke(alignment);
    }

    /// <summary>Diraise tiap DialogueManager dapat 1 baris dialog baru, lengkap dengan data karakternya.</summary>
    [CreateAssetMenu(menuName = "Game/Events/Dialogue Line Event Channel", fileName = "New Dialogue Line Event Channel")]
    public class DialogueLineEventChannel : ScriptableObject
    {
        public event Action<DialogueLinePayload> OnLineRaised;

        public void Raise(DialogueLinePayload payload) => OnLineRaised?.Invoke(payload);
    }

    /// <summary>Diraise saat dialog sampai ke titik percabangan (choices).</summary>
    [CreateAssetMenu(menuName = "Game/Events/Dialogue Choices Event Channel", fileName = "New Dialogue Choices Event Channel")]
    public class DialogueChoicesEventChannel : ScriptableObject
    {
        public event Action<List<Choice>> OnChoicesRaised;

        public void Raise(List<Choice> choices) => OnChoicesRaised?.Invoke(choices);
    }
}