using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InGamePauseController : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("Panel pause yang akan dimunculkan saat ESC atau tombol Pause ditekan.")]
    [SerializeField] private GameObject pausePanel;
    [Tooltip("Panel setting yang dibuka dari dalam menu pause.")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Scene Config")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Audio (Opsional)")]
    [SerializeField] private string clickSfxId = "Click";

    private bool isPaused = false;

    private void Start()
    {
        // Pastikan panel pause dan setting tertutup saat game dimulai
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void Update()
    {
        // Cek jika tombol ESC (Escape) ditekan menggunakan Input System v6
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettingsPanel();
                return;
            }

            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused)
            ClosePausePanel();
        else
            OpenPausePanel();
    }

    // Dipanggil oleh tombol HUD "Pause" untuk masuk ke Panel Pause
    public void OpenPausePanel()
    {
        isPaused = true;

        if (pausePanel != null) pausePanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        Time.timeScale = 0f;
        PlayClickSFX();
    }

    // Dipanggil oleh tombol "Resume" di dalam Panel Pause
    public void ClosePausePanel()
    {
        isPaused = false;

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        Time.timeScale = 1f;
        PlayClickSFX();
    }

    public void OnResumeClicked()
    {
        ClosePausePanel();
    }

    // Dipanggil oleh tombol "Setting" di dalam Panel Pause
    public void OpenSettingsPanel()
    {
        PlayClickSFX();
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    // Dipanggil oleh tombol "Back" di dalam Panel Setting
    public void CloseSettingsPanel()
    {
        PlayClickSFX();
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void OnToggleSettingsPanel()
    {
        PlayClickSFX();
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    // Dipanggil oleh tombol "To Main Menu" di dalam Panel Pause
    public void OnMainMenuClicked()
    {
        isPaused = false;
        Time.timeScale = 1f;
        PlayClickSFX();

        if (NavigationManager.Instance != null)
            NavigationManager.Instance.LoadScene(mainMenuSceneName);
        else
            SceneManager.LoadScene(mainMenuSceneName);
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
