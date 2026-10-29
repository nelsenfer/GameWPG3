using UnityEngine;
using Game.Core;

namespace Game.Progression
{
    /// <summary>
    /// Dipasang otomatis oleh StoryDirector pada objek yang bisa diinteraksi.
    /// </summary>
    public class StoryTarget : MonoBehaviour, IInteractable
    {
        [HideInInspector] public StoryDirector director;

        public bool CanInteract() => director != null && director.CanInteractWith(this);

        public string GetInteractionPrompt() => director != null ? director.PromptFor(this) : "";

        public void OnInteract(GameObject interactor)
        {
            if (director != null) director.Interacted(this);
        }
    }
}
