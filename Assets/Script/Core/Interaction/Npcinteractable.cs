using UnityEngine;
using Game.Core;
using Game.Dialogue.Core;

namespace Game.Dialogue.Core
{
    /// <summary>
    /// Attach ke GameObject NPC/hantu di scene (yang punya Collider2D Is Trigger
    /// + layer Interactable, sama seperti objek interaktif lain). Saat pemain
    /// mendekat dan menekan E, ini manggil DialogueManager.StartDialogue().
    ///
    /// Beda dari CandleInteractable/GenericInteractable yang biasanya
    /// "selesai sekali pakai" — NPCInteractable defaultnya BISA diajak
    /// bicara berulang kali (CanInteract selalu true) kecuali dicentang
    /// "Disable After Interact" untuk kasus hantu yang cuma boleh diajak
    /// bicara 1 kali sampai urusan fulfillment task-nya selesai duluan.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class NPCInteractable : MonoBehaviour, IInteractable
    {
        [Header("References")]
        [SerializeField] private DialogueManager dialogueManager;

        [Header("Prompt")]
        [SerializeField] private string interactionPrompt = "[E] Bicara";

        [Header("Behaviour")]
        [Tooltip("Centang kalau NPC ini cuma boleh diajak ngobrol sekali (misal sudah selesai urusannya).")]
        [SerializeField] private bool disableAfterInteract = false;

        private bool hasInteracted = false;

        public bool CanInteract() => !(disableAfterInteract && hasInteracted);
        public string GetInteractionPrompt() => interactionPrompt;

        public void OnInteract(GameObject interactor)
        {
            if (!CanInteract()) return;
            if (dialogueManager == null)
            {
                Debug.LogError("[NPCInteractable] DialogueManager belum di-assign.", this);
                return;
            }

            hasInteracted = true;
            dialogueManager.StartDialogue();
        }
    }
}