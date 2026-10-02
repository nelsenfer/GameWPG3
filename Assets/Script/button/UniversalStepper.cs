using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class UniversalStepper : MonoBehaviour
{
    [Header("PlayerPrefs Configuration")]
    [Tooltip("Key PlayerPrefs harus beda tiap Stepper (MasterVolume, MusicVolume, SfxVolume)")]
    [SerializeField] private string playerPrefsKey = "MasterVolume";
    [SerializeField] private float defaultValue = 80f;

    [Header("Stepper Settings")]
    [SerializeField] private float minValue = 0f;
    [SerializeField] private float maxValue = 100f;
    [SerializeField] private float stepSize = 10f;
    private float currentValue;

    [Header("UI Elements")]
    [SerializeField] private Button decreaseButton;
    [SerializeField] private Button increaseButton;
    [SerializeField] private TextMeshProUGUI valueText;

    [Header("Events")]
    public UnityEvent<float> OnValueChanged;

    private void OnEnable()
    {
        // Hubungkan event click tombol
        if (decreaseButton != null) decreaseButton.onClick.AddListener(Decrease);
        if (increaseButton != null) increaseButton.onClick.AddListener(Increase);

        // Load data dan sinkronkan UI/Audio setiap kali halaman diaktifkan
        LoadSavedValue();
    }

    private void OnDisable()
    {
        // Lepas listener agar tidak duplikat
        if (decreaseButton != null) decreaseButton.onClick.RemoveListener(Decrease);
        if (increaseButton != null) increaseButton.onClick.RemoveListener(Increase);
    }

    public void LoadSavedValue()
    {
        if (!string.IsNullOrEmpty(playerPrefsKey))
        {
            currentValue = PlayerPrefs.GetFloat(playerPrefsKey, defaultValue);
        }
        else
        {
            currentValue = defaultValue;
        }

        currentValue = Mathf.Clamp(currentValue, minValue, maxValue);
        UpdateUI();
        OnValueChanged?.Invoke(currentValue);
    }

    public void Increase()
    {
        if (currentValue < maxValue)
        {
            currentValue = Mathf.Min(currentValue + stepSize, maxValue);
            UpdateUI();
            SaveAndInvoke();
        }
    }

    public void Decrease()
    {
        if (currentValue > minValue)
        {
            currentValue = Mathf.Max(currentValue - stepSize, minValue);
            UpdateUI();
            SaveAndInvoke();
        }
    }

    private void SaveAndInvoke()
    {
        if (!string.IsNullOrEmpty(playerPrefsKey))
        {
            PlayerPrefs.SetFloat(playerPrefsKey, currentValue);
            PlayerPrefs.Save();
        }
        OnValueChanged?.Invoke(currentValue);
    }

    private void UpdateUI()
    {
        if (valueText != null)
        {
            valueText.text = currentValue.ToString("0");
        }

        if (decreaseButton != null) decreaseButton.interactable = currentValue > minValue;
        if (increaseButton != null) increaseButton.interactable = currentValue < maxValue;
    }
}