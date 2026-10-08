using System;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// Kontrak umum untuk minigame yang dapat dipanggil dari tahap interaksi.
    /// </summary>
    public abstract class MinigameLauncher : MonoBehaviour
    {
        public abstract bool IsOpen { get; }

        /// <summary>Memulai minigame. Callback dipanggil hanya saat pemain lulus.</summary>
        public abstract void Launch(Action onSuccess);
    }
}
