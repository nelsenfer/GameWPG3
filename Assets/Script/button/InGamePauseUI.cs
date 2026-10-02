using UnityEngine;

public class InGamePauseUI : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool _isPaused = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        _isPaused = !_isPaused;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(_isPaused);

        if (NavigationManager.Instance != null)
            NavigationManager.Instance.SetTimeScale(_isPaused ? 0f : 1f);
    }

    public void OnResumeButtonClicked()
    {
        TogglePause();
    }

    public void OnRetryButtonClicked()
    {
        if (NavigationManager.Instance != null)
            NavigationManager.Instance.ReloadCurrentScene();
    }

    public void OnMainMenuButtonClicked()
    {
        if (NavigationManager.Instance != null)
            NavigationManager.Instance.LoadScene(mainMenuSceneName);
    }
}