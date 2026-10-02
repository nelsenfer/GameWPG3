using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    
    [Header("Scene Config")]
    [SerializeField] private string gameplaySceneName = "Gameplay";
    [SerializeField] private string creditSceneName = "Credit";

    public void OnPlayButtonClicked()
    {
        PlayClickSFX();
        if (NavigationManager.Instance != null)
            NavigationManager.Instance.LoadScene(gameplaySceneName);
    }

    public void OnCreditButtonClicked()
    {
        PlayClickSFX();
        if (NavigationManager.Instance != null)
            NavigationManager.Instance.LoadScene(creditSceneName);
    }

    public void OnToggleSettingsPanel()
    {
        PlayClickSFX();
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void OnQuitButtonClicked()
    {
        PlayClickSFX();
        if (NavigationManager.Instance != null)
            NavigationManager.Instance.QuitGame();
    }

    private void PlayClickSFX()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("Click");
    }
}