using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class AudioItem
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)] public float defaultVolume = 1f;
    }

    [Header("Audio Mixer & Sources")]
    public AudioMixer audioMixer;
    public AudioSource masterSource;
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Audio Registries (Add via Inspector)")]
    [SerializeField] private List<AudioItem> bgmList = new List<AudioItem>();
    [SerializeField] private List<AudioItem> sfxList = new List<AudioItem>();

    [Header("Typing SFX Configuration")]
    [SerializeField] private string typingSfxId = "Typing";
    [SerializeField] private float typingSfxDelay = 0.08f;
    private float _lastTypingTime = 0f;
    private readonly float[] _typingPitchVariants = new float[] { 0.944f, 1.0f, 1.059f };

    public static AudioManager Instance { get; private set; }

    private Dictionary<string, AudioItem> _bgmDict = new Dictionary<string, AudioItem>();
    private Dictionary<string, AudioItem> _sfxDict = new Dictionary<string, AudioItem>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDictionaries();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDictionaries()
    {
        _bgmDict.Clear();
        foreach (var item in bgmList)
        {
            if (!string.IsNullOrEmpty(item.id) && !_bgmDict.ContainsKey(item.id))
                _bgmDict.Add(item.id, item);
        }

        _sfxDict.Clear();
        foreach (var item in sfxList)
        {
            if (!string.IsNullOrEmpty(item.id) && !_sfxDict.ContainsKey(item.id))
                _sfxDict.Add(item.id, item);
        }
    }

    // --- BGM Methods ---
    public void PlayBGM(string id, float volumeMultiplier = 1f)
    {
        if (_bgmDict.TryGetValue(id, out AudioItem item))
        {
            if (item.clip == null || musicSource == null) return;
            if (musicSource.clip == item.clip && musicSource.isPlaying) return;

            musicSource.clip = item.clip;
            musicSource.volume = item.defaultVolume * volumeMultiplier;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void StopBGM()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }
    }

    // --- SFX Methods ---
    public void PlaySFX(string id, float volumeMultiplier = 1f, bool randomPitch = false)
    {
        if (_sfxDict.TryGetValue(id, out AudioItem item))
        {
            if (item.clip != null && sfxSource != null)
            {
                sfxSource.pitch = randomPitch ? UnityEngine.Random.Range(0.9f, 1.1f) : 1f;
                sfxSource.PlayOneShot(item.clip, item.defaultVolume * volumeMultiplier);
            }
        }
    }

    public void PlayTypingSFX()
    {
        if (Time.unscaledTime - _lastTypingTime < typingSfxDelay) return;

        if (_sfxDict.TryGetValue(typingSfxId, out AudioItem item) && sfxSource != null && item.clip != null)
        {
            _lastTypingTime = Time.unscaledTime;
            int randomIndex = UnityEngine.Random.Range(0, _typingPitchVariants.Length);
            sfxSource.pitch = _typingPitchVariants[randomIndex];
            sfxSource.PlayOneShot(item.clip, item.defaultVolume);
        }
    }

    public void StopSFX()
    {
        if (sfxSource != null && sfxSource.isPlaying)
        {
            sfxSource.Stop();
        }
    }
}