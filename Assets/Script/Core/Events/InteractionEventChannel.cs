using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Event channel untuk perubahan fokus dan interaksi pemain.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Events/Interaction Event Channel", fileName = "New Interaction Event Channel")]
    public class InteractionEventChannel : ScriptableObject
    {
        public event Action<IInteractable> OnFocusChanged;
        public event Action<IInteractable> OnInteracted;

        public void RaiseFocusChanged(IInteractable target) => OnFocusChanged?.Invoke(target);
        public void RaiseInteracted(IInteractable target) => OnInteracted?.Invoke(target);
    }
}