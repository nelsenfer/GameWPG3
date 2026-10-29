using UnityEngine;
using UnityEngine.InputSystem;

public class InGamePauseController : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("Panel pause yang akan dimunculkan saat ESC ditekan.")]
    [SerializeField] private GameObject pausePanel;

    [Header("Audio (Opsional)")]
    [SerializeField] private string clickSfxId = "Click";

    private bool isPaused = false;

    private void Start()
    {
        // Pastikan panel pause tertutup saat game dimulai
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    private void Update()
    {
        // Cek jika tombol ESC (Escape) ditekan menggunakan Input System v6
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            // Munculkan panel
            if (pausePanel != null) pausePanel.SetActive(true);
            
            // Hentikan waktu game (Game membeku)
            Time.timeScale = 0f; 
            
            // Mainkan SFX klik
            PlayClickSFX();
        }
        else
        {
            // Sembunyikan panel
            if (pausePanel != null) pausePanel.SetActive(false);
            
            // Lanjutkan waktu game
            Time.timeScale = 1f;
            
            PlayClickSFX();
        }
    }

    // Fungsi ini bisa dipanggil oleh tombol "Resume" atau "Close" di dalam panel pause (via OnClick Inspector)
    public void ClosePausePanel()
    {
        if (isPaused) TogglePause();
    }

    private void PlayClickSFX()
    {
        // Menggunakan PlaySFX dari AudioManager. 
        // Karena Time.timeScale = 0, pastikan AudioManager-mu memutar SFX di UI yang tidak terpengaruh waktu game.
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(clickSfxId))
        {
            AudioManager.Instance.PlaySFX(clickSfxId);
        }
    }
}