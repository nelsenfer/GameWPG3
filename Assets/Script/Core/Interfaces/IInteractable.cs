using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Kontrak untuk semua objek yang dapat diinteraksi pemain.
    /// </summary>
    public interface IInteractable
    {
        void OnInteract(GameObject interactor);
        bool CanInteract();
        string GetInteractionPrompt();
    }
}