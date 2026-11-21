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

    [Header("Ambience & Loop SFX Configuration")]
    [SerializeField] private string ambienceId = "Ambience";
    [SerializeField] private string footstepSfxId = "Footstep";

    [Header("Typing SFX Configuration")]
    [SerializeField] private string typingSfxId = "Typing";
    [SerializeField] private float typingSfxDelay = 0.08f;
    [Tooltip("Berapa detik suara keyboard tetap menyala setelah huruf terakhir diketik.")]
    [SerializeField] private float typingHoldDuration = 0.28f;

    private float _lastTypingTime = -10f;
    private readonly float[] _typingPitchVariants = new float[] { 0.96f, 1.0f, 1.04f };

    private AudioSource _ambienceSource;
    private AudioSource _footstepSource;
    private AudioSource _typingSource;

    public static AudioManager Instance { get; private set; }

    private readonly Dictionary<string, AudioItem> _bgmDict = new Dictionary<string, AudioItem>();
    private readonly Dictionary<string, AudioItem> _sfxDict = new Dictionary<string, AudioItem>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDictionaries();
            SetupHelperSources();
        }
        else if (Instance != this)
        {
            // Gabungkan daftar audio baru dari scene ini ke Instance yang sudah ada
            Instance.MergeRegistries(bgmList, sfxList);
            Instance.PlayBGM("MainBgm");
            Instance.PlayAmbience(ambienceId);
            Destroy(gameObject);
            return;
        }

        PlayBGM("MainBgm");
        PlayAmbience(ambienceId);
    }

    private void Update()
    {
        // Hentikan suara ketikan keyboard secara otomatis jika pemain berhenti mengetik
        if (_typingSource != null && _typingSource.isPlaying)
        {
            if (Time.unscaledTime - _lastTypingTime > typingHoldDuration)
            {
                _typingSource.Stop();
            }
        }
    }

    private void SetupHelperSources()
    {
        AudioMixerGroup sfxGroup = sfxSource != null ? sfxSource.outputAudioMixerGroup : null;
        AudioMixerGroup musicGroup = musicSource != null ? musicSource.outputAudioMixerGroup : null;

        _ambienceSource = CreateChildSource("AmbienceSource", musicGroup != null ? musicGroup : sfxGroup, true);
        _footstepSource = CreateChildSource("FootstepSource", sfxGroup, true);
        _typingSource = CreateChildSource("TypingSource", sfxGroup, true);
    }

    private AudioSource CreateChildSource(string sourceName, AudioMixerGroup group, bool loop)
    {
        GameObject child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);
        AudioSource src = child.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f;
        if (group != null) src.outputAudioMixerGroup = group;
        return src;
    }

    private void InitializeDictionaries()
    {
        _bgmDict.Clear();
        foreach (var item in bgmList)
        {
            if (item != null && !string.IsNullOrEmpty(item.id) && !_bgmDict.ContainsKey(item.id))
                _bgmDict.Add(item.id, item);
        }

        _sfxDict.Clear();
        foreach (var item in sfxList)
        {
            if (item != null && !string.IsNullOrEmpty(item.id) && !_sfxDict.ContainsKey(item.id))
                _sfxDict.Add(item.id, item);
        }
    }

    public void MergeRegistries(List<AudioItem> newBgms, List<AudioItem> newSfxs)
    {
        if (newBgms != null)
        {
            foreach (var item in newBgms)
            {
                if (item == null || string.IsNullOrEmpty(item.id) || item.clip == null) continue;
                _bgmDict[item.id] = item;
            }
        }

        if (newSfxs != null)
        {
            foreach (var item in newSfxs)
            {
                if (item == null || string.IsNullOrEmpty(item.id) || item.clip == null) continue;
                _sfxDict[item.id] = item;
            }
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

    // --- Ambience Methods (Mode Side-Scrolling) ---
    public void PlayAmbience(string id = null, float volumeMultiplier = 1f)
    {
        string targetId = string.IsNullOrEmpty(id) ? ambienceId : id;
        if (string.IsNullOrEmpty(targetId)) return;

        if (!_bgmDict.TryGetValue(targetId, out AudioItem item) && !_sfxDict.TryGetValue(targetId, out item))
            return;

        if (item == null || item.clip == null) return;
        if (_ambienceSource == null) SetupHelperSources();

        if (_ambienceSource.clip == item.clip && _ambienceSource.isPlaying) return;

        _ambienceSource.clip = item.clip;
        _ambienceSource.volume = item.defaultVolume * volumeMultiplier;
        _ambienceSource.loop = true;
        _ambienceSource.Play();
    }

    public void StopAmbience()
    {
        if (_ambienceSource != null && _ambienceSource.isPlaying)
        {
            _ambienceSource.Stop();
        }
    }

    // --- Footstep Methods (Saat Berjalan) ---
    public void SetFootstepActive(bool isWalking)
    {
        if (_footstepSource == null) SetupHelperSources();

        if (!isWalking)
        {
            if (_footstepSource != null && _footstepSource.isPlaying)
            {
                _footstepSource.Stop();
            }
            return;
        }

        if (_footstepSource.isPlaying) return;

        if (_sfxDict.TryGetValue(footstepSfxId, out AudioItem item) && item.clip != null)
        {
            _footstepSource.clip = item.clip;
            _footstepSource.volume = item.defaultVolume;
            _footstepSource.loop = true;
            _footstepSource.Play();
        }
    }

    // --- SFX Methods ---
    public void PlaySFX(string id, float volumeMultiplier = 1f, bool randomPitch = false)
    {
        if (_sfxDict.TryGetValue(id, out AudioItem item))
        {
            if (item.clip != null && sfxSource != null)
            {
                sfxSource.pitch = randomPitch ? UnityEngine.Random.Range(0.95f, 1.05f) : 1f;
                sfxSource.PlayOneShot(item.clip, item.defaultVolume * volumeMultiplier);
            }
        }
    }

    // --- Keyboard Typing SFX Methods ---
    public void PlayTypingSFX()
    {
        if (!_sfxDict.TryGetValue(typingSfxId, out AudioItem item) && !_sfxDict.TryGetValue("Keyboard", out item))
            return;

        if (item == null || item.clip == null) return;
        if (_typingSource == null) SetupHelperSources();

        _lastTypingTime = Time.unscaledTime;

        if (!_typingSource.isPlaying)
        {
            _typingSource.clip = item.clip;
            _typingSource.volume = item.defaultVolume;
            int randomIndex = UnityEngine.Random.Range(0, _typingPitchVariants.Length);
            _typingSource.pitch = _typingPitchVariants[randomIndex];
            _typingSource.loop = true;
            _typingSource.Play();
        }
    }

    public void StopTypingSFX()
    {
        _lastTypingTime = -10f;
        if (_typingSource != null && _typingSource.isPlaying)
        {
            _typingSource.Stop();
        }
    }

    public void StopSFX()
    {
        if (sfxSource != null && sfxSource.isPlaying)
        {
            sfxSource.Stop();
        }
        SetFootstepActive(false);
        StopTypingSFX();
    }
}
