using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
        public bool loop = false;
    }
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    
    [Header("Background Music")]
    [SerializeField] private Sound[] bgmClips;
    
    [Header("Sound Effects")]
    [SerializeField] private Sound[] sfxClips;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;
    
    // Dictionary for quick lookup
    private Dictionary<string, Sound> bgmDictionary;
    private Dictionary<string, Sound> sfxDictionary;
    
    // Singleton instance
    public static AudioManager Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
            InitializeDictionaries();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Apply initial volume settings
        UpdateVolumes();
    }
    
    private void InitializeAudioSources()
    {
        // Create BGM AudioSource if not assigned
        if (bgmSource == null)
        {
            GameObject bgmObject = new GameObject("BGM Source");
            bgmObject.transform.SetParent(transform);
            bgmSource = bgmObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        
        // Create SFX AudioSource if not assigned
        if (sfxSource == null)
        {
            GameObject sfxObject = new GameObject("SFX Source");
            sfxObject.transform.SetParent(transform);
            sfxSource = sfxObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }
    
    private void InitializeDictionaries()
    {
        // Initialize BGM dictionary
        bgmDictionary = new Dictionary<string, Sound>();
        if (bgmClips != null)
        {
            foreach (Sound sound in bgmClips)
            {
                if (sound != null && !string.IsNullOrEmpty(sound.name))
                {
                    bgmDictionary[sound.name] = sound;
                }
            }
        }
        
        // Initialize SFX dictionary
        sfxDictionary = new Dictionary<string, Sound>();
        if (sfxClips != null)
        {
            foreach (Sound sound in sfxClips)
            {
                if (sound != null && !string.IsNullOrEmpty(sound.name))
                {
                    sfxDictionary[sound.name] = sound;
                }
            }
        }
    }
    
    // Play BGM by name
    public void PlayBGM(string bgmName)
    {
        if (bgmDictionary.ContainsKey(bgmName))
        {
            PlayBGM(bgmDictionary[bgmName]);
        }
        else
        {
            Debug.LogWarning($"BGM '{bgmName}' not found in AudioManager!");
        }
    }
    
    // Play BGM by Sound object
    public void PlayBGM(Sound bgm)
    {
        if (bgm == null || bgm.clip == null)
        {
            Debug.LogWarning("BGM clip is null!");
            return;
        }
        
        bgmSource.clip = bgm.clip;
        bgmSource.volume = bgm.volume * bgmVolume * masterVolume;
        bgmSource.loop = bgm.loop;
        bgmSource.Play();
    }
    
    // Play BGM by index
    public void PlayBGM(int index)
    {
        if (bgmClips != null && index >= 0 && index < bgmClips.Length)
        {
            PlayBGM(bgmClips[index]);
        }
        else
        {
            Debug.LogWarning($"BGM index {index} is out of range!");
        }
    }
    
    // Stop BGM
    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }
    
    // Pause BGM
    public void PauseBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Pause();
        }
    }
    
    // Resume BGM
    public void ResumeBGM()
    {
        if (bgmSource != null && !bgmSource.isPlaying)
        {
            bgmSource.UnPause();
        }
    }
    
    // Play SFX by name
    public void PlaySFX(string sfxName)
    {
        if (sfxDictionary.ContainsKey(sfxName))
        {
            PlaySFX(sfxDictionary[sfxName]);
        }
        else
        {
            Debug.LogWarning($"SFX '{sfxName}' not found in AudioManager!");
        }
    }
    
    // Play SFX by Sound object
    public void PlaySFX(Sound sfx)
    {
        if (sfx == null || sfx.clip == null)
        {
            Debug.LogWarning("SFX clip is null!");
            return;
        }
        
        sfxSource.PlayOneShot(sfx.clip, sfx.volume * sfxVolume * masterVolume);
    }
    
    // Play SFX by index
    public void PlaySFX(int index)
    {
        if (sfxClips != null && index >= 0 && index < sfxClips.Length)
        {
            PlaySFX(sfxClips[index]);
        }
        else
        {
            Debug.LogWarning($"SFX index {index} is out of range!");
        }
    }
    
    // Play SFX at specific volume (override)
    public void PlaySFX(string sfxName, float volumeMultiplier)
    {
        if (sfxDictionary.ContainsKey(sfxName))
        {
            Sound sfx = sfxDictionary[sfxName];
            sfxSource.PlayOneShot(sfx.clip, sfx.volume * sfxVolume * masterVolume * volumeMultiplier);
        }
        else
        {
            Debug.LogWarning($"SFX '{sfxName}' not found in AudioManager!");
        }
    }
    
    // Volume control methods
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }
    
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }
    
    private void UpdateVolumes()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume * masterVolume;
        }
    }
    
    // Get current volume values
    public float GetMasterVolume() => masterVolume;
    public float GetBGMVolume() => bgmVolume;
    public float GetSFXVolume() => sfxVolume;
    
    // Check if BGM is playing
    public bool IsBGMPlaying() => bgmSource != null && bgmSource.isPlaying;
}
