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

        [Header("Layout (opsional tapi disarankan)")]
        [Tooltip("RectTransform 'Content' di dalam Scroll View. Dipakai untuk menghitung ulang layout tiap panel dibuka.")]
        [SerializeField] private RectTransform logContent;
        [SerializeField] private UnityEngine.UI.ScrollRect scrollRect;

        /// <summary>True selama panel log sedang terbuka.</summary>
        public bool IsOpen => logPanelRoot != null && logPanelRoot.activeSelf;

        private void Awake()
        {
            if (logPanelRoot != null)
                logPanelRoot.SetActive(false);
        }

        [ContextMenu("TEST: Open Log Dialog")]
        public void OpenLogDialog()
        {
            if (logPanelRoot == null) return;
            logPanelRoot.SetActive(true);
            RefreshLayout();
        }

        [ContextMenu("TEST: Close Log Dialog")]
        public void CloseLogDialog()
        {
            if (logPanelRoot != null) logPanelRoot.SetActive(false);
        }

        /// <summary>Dipakai nanti untuk tombol "Log" yang sama (toggle buka/tutup).</summary>
        public void ToggleLogDialog()
        {
            if (logPanelRoot == null) return;

            bool open = !logPanelRoot.activeSelf;
            logPanelRoot.SetActive(open);
            if (open) RefreshLayout();
        }

        // Bubble dibuat saat panel masih nonaktif, jadi layout-nya baru bisa dihitung setelah panel aktif.
        private void RefreshLayout()
        {
            if (logContent != null)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(logContent);

            Canvas.ForceUpdateCanvases();

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f; // langsung ke baris terbaru
        }
    }
}