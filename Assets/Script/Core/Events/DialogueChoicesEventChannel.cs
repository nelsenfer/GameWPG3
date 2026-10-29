using System;
using System.Collections.Generic;
using Ink.Runtime;
using UnityEngine;

namespace Game.Dialogue.Events
{
    /// <summary>Diraise saat dialog sampai ke titik percabangan (choices).</summary>
    [CreateAssetMenu(menuName = "Game/Events/Dialogue Choices Event Channel", fileName = "New Dialogue Choices Event Channel")]
    public class DialogueChoicesEventChannel : ScriptableObject
    {
        public event Action<List<Choice>> OnChoicesRaised;

        public void Raise(List<Choice> choices) => OnChoicesRaised?.Invoke(choices);
    }
}
