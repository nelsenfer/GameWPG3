using System;
using System.Collections.Generic;
using Ink.Runtime;
using UnityEngine;
using Game.Dialogue.Data;

namespace Game.Dialogue.Events
{
    public enum ChoiceAlignment { Neutral, Reflective, Avoidant }

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

    [CreateAssetMenu(menuName = "Game/Events/Choice Event Channel", fileName = "New Choice Event Channel")]
    public class ChoiceEventChannel : ScriptableObject
    {
        public event Action<ChoiceAlignment> OnChoiceMade;

        public void Raise(ChoiceAlignment alignment) => OnChoiceMade?.Invoke(alignment);
    }

    [CreateAssetMenu(menuName = "Game/Events/Dialogue Line Event Channel", fileName = "New Dialogue Line Event Channel")]
    public class DialogueLineEventChannel : ScriptableObject
    {
        public event Action<DialogueLinePayload> OnLineRaised;

        public void Raise(DialogueLinePayload payload) => OnLineRaised?.Invoke(payload);
    }

    [CreateAssetMenu(menuName = "Game/Events/Dialogue Choices Event Channel", fileName = "New Dialogue Choices Event Channel")]
    public class DialogueChoicesEventChannel : ScriptableObject
    {
        public event Action<List<Choice>> OnChoicesRaised;

        public void Raise(List<Choice> choices) => OnChoicesRaised?.Invoke(choices);
    }
}