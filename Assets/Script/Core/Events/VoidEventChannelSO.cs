using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Event channel tanpa payload untuk melaporkan task yang selesai.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Events/Void Event Channel", fileName = "New Void Event Channel")]
    public class VoidEventChannelSO : ScriptableObject
    {
        public event Action OnRaised;

        public void Raise() => OnRaised?.Invoke();
    }
}