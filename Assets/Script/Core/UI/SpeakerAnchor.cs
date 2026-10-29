using System.Collections.Generic;
using UnityEngine;
using Game.Dialogue.Data;

namespace Game.Dialogue.UI
{
    /// <summary>
    /// Menentukan titik dunia tempat bubble komik muncul untuk satu karakter.
    /// </summary>
    public class SpeakerAnchor : MonoBehaviour
    {
        [SerializeField] private CharacterData character;
        [SerializeField] private Transform bubblePoint;

        private static readonly Dictionary<CharacterData, SpeakerAnchor> anchors =
            new Dictionary<CharacterData, SpeakerAnchor>();

        public Vector3 BubbleWorldPosition =>
            (bubblePoint != null ? bubblePoint : transform).position;

        private void OnEnable()
        {
            if (character != null) anchors[character] = this;
        }

        private void OnDisable()
        {
            if (character != null && anchors.TryGetValue(character, out SpeakerAnchor anchor) &&
                anchor == this)
                anchors.Remove(character);
        }

        public static bool TryGet(CharacterData data, out SpeakerAnchor anchor)
        {
            anchor = null;
            return data != null && anchors.TryGetValue(data, out anchor) && anchor != null;
        }
    }
}
