using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.Progression
{
    /// <summary>
    /// Menyimpan flag progres cerita dan misi yang sedang aktif.
    /// </summary>
    public class StoryProgress : MonoBehaviour
    {
        [Header("HUD Misi (opsional)")]
        [SerializeField] private GameObject objectiveRoot;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private string objectivePrefix = "Misi: ";

        [Header("Testing")]
        [Tooltip("Flag yang sudah aktif sejak awal.")]
        [SerializeField] private List<string> startingFlags = new List<string>();
        [Tooltip("Tampilan saja: flag yang sedang aktif.")]
        [SerializeField] private List<string> activeFlagsView = new List<string>();

        private readonly HashSet<string> flags = new HashSet<string>();

        public event Action<string> OnFlagSet;
        public event Action<string> OnObjectiveChanged;
        public string CurrentObjective { get; private set; } = "";

        private void Awake()
        {
            foreach (string flag in startingFlags)
            {
                if (!string.IsNullOrWhiteSpace(flag))
                    flags.Add(flag.Trim());
            }

            RefreshView();
            ApplyObjectiveToUI();
        }

        public bool HasFlag(string flag)
        {
            return string.IsNullOrWhiteSpace(flag) || flags.Contains(flag.Trim());
        }

        public void SetFlag(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag)) return;

            flag = flag.Trim();
            if (!flags.Add(flag)) return;

            RefreshView();
            OnFlagSet?.Invoke(flag);
        }

        public void SetObjective(string text)
        {
            CurrentObjective = text ?? "";
            ApplyObjectiveToUI();
            OnObjectiveChanged?.Invoke(CurrentObjective);
        }

        public void ClearObjective()
        {
            SetObjective("");
        }

        private void ApplyObjectiveToUI()
        {
            bool hasObjective = !string.IsNullOrEmpty(CurrentObjective);
            if (objectiveRoot != null) objectiveRoot.SetActive(hasObjective);
            if (objectiveText != null)
                objectiveText.text = hasObjective ? objectivePrefix + CurrentObjective : "";
        }

        private void RefreshView()
        {
            activeFlagsView.Clear();
            activeFlagsView.AddRange(flags);
        }

        [ContextMenu("TEST: Reset Semua Flag")]
        private void ResetAll()
        {
            flags.Clear();
            foreach (string flag in startingFlags)
            {
                if (!string.IsNullOrWhiteSpace(flag))
                    flags.Add(flag.Trim());
            }

            RefreshView();
            ClearObjective();
        }
    }
}
