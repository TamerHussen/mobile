using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages audio settings (Master, Music, SFX volumes)
/// Attach to: AudioManager GameObject (DontDestroyOnLoad)
/// </summary>
public class AudioSettingsManager : MonoBehaviour
{
    public static AudioSettingsManager Instance;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("UI References (Optional - will be found dynamically)")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public TextMeshProUGUI masterValueText;
    public TextMeshProUGUI musicValueText;
    public TextMeshProUGUI sfxValueText;

    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        BindUIElements();
        ApplySettings();
    }

    void BindUIElements()
    {
        // Try to find sliders if not assigned
        if (masterSlider == null)
            masterSlider = GameObject.Find("MasterVolumeSlider")?.GetComponent<Slider>();

        if (musicSlider == null)
            musicSlider = GameObject.Find("MusicVolumeSlider")?.GetComponent<Slider>();

        if (sfxSlider == null)
            sfxSlider = GameObject.Find("SFXVolumeSlider")?.GetComponent<Slider>();

        // Bind slider events
        if (masterSlider != null)
        {
            masterSlider.value = masterVolume;
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.value = musicVolume;
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = sfxVolume;
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        UpdateUI();
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        SetMixerVolume("MasterVolume", volume);
        UpdateUI();
        SaveSettings();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        SetMixerVolume("MusicVolume", volume);
        UpdateUI();
        SaveSettings();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        SetMixerVolume("SFXVolume", volume);
        UpdateUI();
        SaveSettings();
    }

    void SetMixerVolume(string parameterName, float volume)
    {
        if (audioMixer == null)
        {
            Debug.LogWarning("Audio Mixer not assigned!");
            return;
        }

        // Convert 0-1 range to decibels (-80 to 0)
        float dbVolume = volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;
        audioMixer.SetFloat(parameterName, dbVolume);
    }

    void UpdateUI()
    {
        if (masterSlider != null)
            masterSlider.SetValueWithoutNotify(masterVolume);

        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(musicVolume);

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(sfxVolume);

        if (masterValueText != null)
            masterValueText.text = $"{Mathf.RoundToInt(masterVolume * 100)}%";

        if (musicValueText != null)
            musicValueText.text = $"{Mathf.RoundToInt(musicVolume * 100)}%";

        if (sfxValueText != null)
            sfxValueText.text = $"{Mathf.RoundToInt(sfxVolume * 100)}%";
    }

    void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.8f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);

        Debug.Log($"Loaded audio settings - Master: {masterVolume}, Music: {musicVolume}, SFX: {sfxVolume}");
    }

    void SaveSettings()
    {
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
        PlayerPrefs.Save();
    }

    void ApplySettings()
    {
        SetMixerVolume("MasterVolume", masterVolume);
        SetMixerVolume("MusicVolume", musicVolume);
        SetMixerVolume("SFXVolume", sfxVolume);
        UpdateUI();
    }

    // Public methods for UI buttons
    public void ResetToDefaults()
    {
        SetMasterVolume(1f);
        SetMusicVolume(0.8f);
        SetSFXVolume(1f);
        Debug.Log("Audio settings reset to defaults");
    }
}

/*
=== AUDIO MIXER SETUP INSTRUCTIONS ===

1. Create Audio Mixer:
   - Right-click in Project → Create → Audio Mixer
   - Name it "MainAudioMixer"

2. Add Exposed Parameters:
   - Click on Master group
   - In Inspector, right-click "Volume" → Expose "Volume" to script
   - Rename exposed parameter to "MasterVolume"
   
3. Create Music and SFX Groups:
   - Right-click Master → Add child group → "Music"
   - Right-click Master → Add child group → "SFX"
   - Expose their volumes as "MusicVolume" and "SFXVolume"

4. Assign Audio Sources:
   - Set music AudioSource.outputAudioMixerGroup = Music
   - Set sfx AudioSource.outputAudioMixerGroup = SFX

5. Assign Mixer to Script:
   - Drag MainAudioMixer into AudioSettingsManager.audioMixer field
*/