using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// Dipasang otomatis oleh StoryDirector pada objek area beat EnterArea.
    /// </summary>
    public class StoryZone : MonoBehaviour
    {
        [HideInInspector] public StoryDirector director;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (director != null && other.CompareTag("Player"))
                director.ZoneEntered(this);
        }
    }
}
