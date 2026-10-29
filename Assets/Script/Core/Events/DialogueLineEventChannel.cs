using System;
using UnityEngine;

namespace Game.Dialogue.Events
{
    /// <summary>Diraise tiap DialogueManager dapat 1 baris dialog baru, lengkap dengan data karakternya.</summary>
    [CreateAssetMenu(menuName = "Game/Events/Dialogue Line Event Channel", fileName = "New Dialogue Line Event Channel")]
    public class DialogueLineEventChannel : ScriptableObject
    {
        public event Action<DialogueLinePayload> OnLineRaised;

        public void Raise(DialogueLinePayload payload) => OnLineRaised?.Invoke(payload);
    }
}
