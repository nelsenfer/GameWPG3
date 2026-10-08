using System;
using UnityEngine;
using Game.Progression;

namespace Game.Typing
{
    /// <summary>
    /// Adapter yang menghubungkan TypingMinigameController ke sistem tahap cerita.
    /// </summary>
    public class TypingMinigameLauncher : MinigameLauncher
    {
        [SerializeField] private TypingMinigameController controller;
        [SerializeField] private TypingTaskData task;

        public override bool IsOpen => controller != null && controller.IsOpen;

        public override void Launch(Action onSuccess)
        {
            if (controller == null || task == null)
            {
                Debug.LogError("[TypingMinigameLauncher] Controller / Task belum di-assign.", this);
                return;
            }

            controller.Open(task, onSuccess);
        }
    }
}
