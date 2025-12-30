using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

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

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        BindUIElements();
        ApplySettings();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Audio manager detecting scene change to: {scene.name}");
        Invoke(nameof(RebindUI), 0.1f);
    }

    void RebindUI()
    {
        masterSlider = null;
        musicSlider = null;
        sfxSlider = null;
        masterValueText = null;
        musicValueText = null;
        sfxValueText = null;

        BindUIElements();
        ApplySettings();

        Debug.Log("Audio UI rebound for new scene");
    }

    void BindUIElements()
    {
        if (masterSlider == null)
            masterSlider = FindObjectOfTypeIncludingInactive<Slider>("MasterVolumeSlider");

        if (musicSlider == null)
            musicSlider = FindObjectOfTypeIncludingInactive<Slider>("MusicVolumeSlider");

        if (sfxSlider == null)
            sfxSlider = FindObjectOfTypeIncludingInactive<Slider>("SFXVolumeSlider");

        if (masterValueText == null)
            masterValueText = FindObjectOfTypeIncludingInactive<TextMeshProUGUI>("MasterValueText");

        if (musicValueText == null)
            musicValueText = FindObjectOfTypeIncludingInactive<TextMeshProUGUI>("MusicValueText");

        if (sfxValueText == null)
            sfxValueText = FindObjectOfTypeIncludingInactive<TextMeshProUGUI>("SFXValueText");

        if (masterSlider != null)
        {
            masterSlider.value = masterVolume;
            masterSlider.onValueChanged.RemoveAllListeners();
            masterSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.value = musicVolume;
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = sfxVolume;
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        UpdateUI();
    }

    T FindObjectOfTypeIncludingInactive<T>(string name) where T : Component
    {
        T[] allObjects = Resources.FindObjectsOfTypeAll<T>();
        foreach (T obj in allObjects)
        {
            if (obj.gameObject.scene.isLoaded && obj.gameObject.name == name)
            {
                return obj;
            }
        }
        return null;
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
            masterValueText.text = $"Master: {Mathf.RoundToInt(masterVolume * 100)}%";

        if (musicValueText != null)
            musicValueText.text = $"Music: {Mathf.RoundToInt(musicVolume * 100)}%";

        if (sfxValueText != null)
            sfxValueText.text = $"SFX: {Mathf.RoundToInt(sfxVolume * 100)}%";
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

    public void ResetToDefaults()
    {
        SetMasterVolume(1f);
        SetMusicVolume(0.8f);
        SetSFXVolume(1f);
        Debug.Log("Audio settings reset to defaults");
    }
}