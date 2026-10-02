using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioControl : MonoBehaviour
{
    [Header("Audio Mixer Reference")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Exposed Parameter Names")]
    [SerializeField] private string masterParam = "MasterVolume";
    [SerializeField] private string musicParam = "MusicVolume";
    [SerializeField] private string sfxParam = "SfxVolume";

    private IEnumerator Start()
    {
        // Tunda 1 frame agar AudioMixer Unity sepenuhnya ter-inisialisasi
        yield return null;

        // Ambil nilai dari PlayerPrefs (default: 80)
        float masterVal = PlayerPrefs.GetFloat(masterParam, 80f);
        float musicVal = PlayerPrefs.GetFloat(musicParam, 80f);
        float sfxVal = PlayerPrefs.GetFloat(sfxParam, 80f);

        // Terapkan ke AudioMixer
        SetMasterVolume(masterVal);
        SetMusicVolume(musicVal);
        SetSfxVolume(sfxVal);
    }

    public void SetVolume(string parameterName, float volume)
    {
        if (audioMixer != null)
        {
            // Konversi nilai 0 - 100 menjadi skala linear 0.0001 - 1.0
            float normalizedVolume = Mathf.Max(0.0001f, volume / 100f);

            // Konversi skala linear ke desibel (-80 dB s/d 0 dB)
            float db = Mathf.Log10(normalizedVolume) * 20f;

            audioMixer.SetFloat(parameterName, db);
        }

        // Simpan nilai asli (0 - 100) ke PlayerPrefs
        PlayerPrefs.SetFloat(parameterName, volume);
        PlayerPrefs.Save();
    }

    // Public Method untuk disambungkan ke OnValueChanged Event di Inspector UniversalStepper
    public void SetMasterVolume(float volume) => SetVolume(masterParam, volume);
    public void SetMusicVolume(float volume) => SetVolume(musicParam, volume);
    public void SetSfxVolume(float volume) => SetVolume(sfxParam, volume);
}