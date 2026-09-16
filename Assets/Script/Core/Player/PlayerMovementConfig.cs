using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Data-driven config untuk gerak pemain dan deteksi interaksi.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Config/Player Movement Config", fileName = "New Movement Config")]
    public class PlayerMovementConfig : ScriptableObject
    {
        [Header("Horizontal Movement")]
        public float moveSpeed = 4f;
        public float acceleration = 40f;
        public float deceleration = 50f;

        [Header("Interaction")]
        [Tooltip("Radius deteksi objek interaktif di sekitar pemain.")]
        public float interactionRadius = 1.2f;
        public LayerMask interactableLayer;
    }
}