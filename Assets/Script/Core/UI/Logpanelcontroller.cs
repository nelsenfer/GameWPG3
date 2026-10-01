using UnityEngine;

namespace Game.Dialogue.UI
{
    /// <summary>
    /// Buka/tutup panel histori percakapan (Scroll View yang isinya diisi
    /// ChatLogController). Defaultnya disembunyikan; dibuka manual lewat
    /// OpenLogDialog() — nanti dihubungkan ke tombol "Log" di UI, untuk
    /// sekarang bisa ditest lewat context menu di Inspector (titik 3 ⋮)
    /// sama seperti DialogueManager.StartDialogue().
    /// </summary>
    public class LogPanelController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("GameObject root panel log — biasanya GameObject 'Scroll View'.")]
        [SerializeField] private GameObject logPanelRoot;

        private void Awake()
        {
            if (logPanelRoot != null)
                logPanelRoot.SetActive(false);
        }

        [ContextMenu("TEST: Open Log Dialog")]
        public void OpenLogDialog()
        {
            if (logPanelRoot != null) logPanelRoot.SetActive(true);
        }

        [ContextMenu("TEST: Close Log Dialog")]
        public void CloseLogDialog()
        {
            if (logPanelRoot != null) logPanelRoot.SetActive(false);
        }

        /// <summary>Dipakai nanti untuk tombol "Log" yang sama (toggle buka/tutup).</summary>
        public void ToggleLogDialog()
        {
            if (logPanelRoot != null) logPanelRoot.SetActive(!logPanelRoot.activeSelf);
        }
    }
}