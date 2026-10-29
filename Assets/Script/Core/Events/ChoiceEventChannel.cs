using System;
using UnityEngine;

namespace Game.Dialogue.Events
{
    /// <summary>Diraise tiap pemain memilih 1 choice reflektif/avoidant.</summary>
    [CreateAssetMenu(menuName = "Game/Events/Choice Event Channel", fileName = "New Choice Event Channel")]
    public class ChoiceEventChannel : ScriptableObject
    {
        public event Action<ChoiceAlignment> OnChoiceMade;

        public void Raise(ChoiceAlignment alignment) => OnChoiceMade?.Invoke(alignment);
    }
}
