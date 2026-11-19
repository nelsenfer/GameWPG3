using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Core;
using Game.Dialogue.Core;

namespace Game.Progression
{
    /// <summary>
    /// Mengontrol fitur keluar-masuk antara Dunia Nyata (Kantor) dan Dunia Lain
    /// menggunakan tombol UI Lonceng atau shortcut keyboard [L].
    /// Fitur ini awalnya terkunci dan baru aktif setelah minigame pertama selesai.
    /// </summary>
    public class DimensionTravelController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpaceDiveTransition spaceDive;
        [SerializeField] private GameObject bellButtonRoot;
        [SerializeField] private Button bellButton;
        [SerializeField] private TextMeshProUGUI bellButtonText;
        [SerializeField] private GameObject ghostObject;

        [Header("State")]
        [SerializeField] private bool isUnlocked = false;
        [SerializeField] private bool isInOtherWorld = false;

        [Header("Shortcut Input")]
        [SerializeField] private KeyCode travelHotkey = KeyCode.L;

        public bool IsUnlocked => isUnlocked;
        public bool IsInOtherWorld => isInOtherWorld;

        private void Awake()
        {
            if (spaceDive == null)
            {
                spaceDive = FindFirstObjectByType<SpaceDiveTransition>(FindObjectsInactive.Include);
            }

            if (ghostObject == null)
            {
                ghostObject = GameObject.Find("Hantu_HyperResponsibility");
            }

            if (bellButton != null)
            {
                bellButton.onClick.AddListener(OnBellButtonClicked);
            }

            // Sembunyikan tombol UI Lonceng sebelum fitur di-unlock
            if (!isUnlocked && bellButtonRoot != null)
            {
                bellButtonRoot.SetActive(false);
            }
        }

        private void Update()
        {
            if (!isUnlocked) return;

            // Shortcut keyboard [L]
            if (Input.GetKeyDown(travelHotkey))
            {
                OnBellButtonClicked();
            }
        }

        /// <summary>
        /// Dipanggil setelah minigame pertama selesai untuk membuka fitur Lonceng.
        /// </summary>
        public void UnlockBell()
        {
            isUnlocked = true;
            if (bellButtonRoot != null)
            {
                bellButtonRoot.SetActive(true);
            }
            UpdateButtonVisuals();
        }

        public void SetInOtherWorldState(bool inOtherWorld)
        {
            isInOtherWorld = inOtherWorld;
            UpdateButtonVisuals();
        }

        public void OnBellButtonClicked()
        {
            if (!isUnlocked || spaceDive == null || spaceDive.IsOpen) return;

            // Cegah ganti dimensi saat dialog sedang berlangsung
            var dm = FindFirstObjectByType<DialogueManager>();
            if (dm != null && dm.IsDialogueActive) return;

            if (isInOtherWorld)
            {
                TravelToRealWorld();
            }
            else
            {
                TravelToOtherWorld();
            }
        }

        public void TravelToRealWorld(Action onComplete = null)
        {
            if (spaceDive == null || spaceDive.IsOpen) return;

            spaceDive.EmergeToRealWorld(() =>
            {
                isInOtherWorld = false;
                if (ghostObject != null) ghostObject.SetActive(false);
                UpdateButtonVisuals();
                onComplete?.Invoke();
            });
        }

        public void TravelToOtherWorld(Action onComplete = null)
        {
            if (spaceDive == null || spaceDive.IsOpen) return;

            spaceDive.DiveToOtherWorld(() =>
            {
                isInOtherWorld = true;
                UpdateButtonVisuals();
                onComplete?.Invoke();
            });
        }

        private void UpdateButtonVisuals()
        {
            if (bellButtonText == null) return;
            bellButtonText.text = isInOtherWorld ? "Kembali [L]" : "Lonceng [L]";
        }
    }
}
