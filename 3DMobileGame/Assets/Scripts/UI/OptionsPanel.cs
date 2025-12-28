using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsPanel : MonoBehaviour
{
    [Header("Audio Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Volume Value Text")]
    public TextMeshProUGUI masterValueText;
    public TextMeshProUGUI musicValueText;
    public TextMeshProUGUI sfxValueText;

    [Header("Buttons")]
    public Button backButton;
    public Button resetButton;

    [Header("Settings")]
    public bool pauseGameWhenOpen = true;

    private bool wasGamePaused = false;

    void Awake()
    {
        AutoFindUIElements();

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);
    }

    void AutoFindUIElements()
    {
        if (masterVolumeSlider == null)
            masterVolumeSlider = GameObject.Find("MasterVolumeSlider")?.GetComponent<Slider>();

        if (musicVolumeSlider == null)
            musicVolumeSlider = GameObject.Find("MusicVolumeSlider")?.GetComponent<Slider>();

        if (sfxVolumeSlider == null)
            sfxVolumeSlider = GameObject.Find("SFXVolumeSlider")?.GetComponent<Slider>();

        if (masterValueText == null)
            masterValueText = GameObject.Find("MasterValueText")?.GetComponent<TextMeshProUGUI>();

        if (musicValueText == null)
            musicValueText = GameObject.Find("MusicValueText")?.GetComponent<TextMeshProUGUI>();

        if (sfxValueText == null)
            sfxValueText = GameObject.Find("SFXValueText")?.GetComponent<TextMeshProUGUI>();

        if (backButton == null)
        {
            var backObj = GameObject.Find("BackButton");
            if (backObj != null)
                backButton = backObj.GetComponent<Button>();
        }

        if (resetButton == null)
        {
            var resetObj = GameObject.Find("ResetButton");
            if (resetObj != null)
                resetButton = resetObj.GetComponent<Button>();
        }
    }

    void OnEnable()
    {
        if (pauseGameWhenOpen)
        {
            wasGamePaused = Time.timeScale == 0f;
            Time.timeScale = 0f;
            Debug.Log("⏸️ Game paused - Options panel opened");
        }

        SyncWithAudioManager();
    }

    void SyncWithAudioManager()
    {
        if (AudioSettingsManager.Instance == null)
        {
            Debug.LogWarning("AudioSettingsManager not found! Options panel cannot control audio.");
            return;
        }

        AudioSettingsManager.Instance.masterSlider = masterVolumeSlider;
        AudioSettingsManager.Instance.musicSlider = musicVolumeSlider;
        AudioSettingsManager.Instance.sfxSlider = sfxVolumeSlider;

        AudioSettingsManager.Instance.masterValueText = masterValueText;
        AudioSettingsManager.Instance.musicValueText = musicValueText;
        AudioSettingsManager.Instance.sfxValueText = sfxValueText;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = AudioSettingsManager.Instance.masterVolume;
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(AudioSettingsManager.Instance.SetMasterVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = AudioSettingsManager.Instance.musicVolume;
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.onValueChanged.AddListener(AudioSettingsManager.Instance.SetMusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = AudioSettingsManager.Instance.sfxVolume;
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(AudioSettingsManager.Instance.SetSFXVolume);
        }

        Debug.Log("Options panel synced with AudioSettingsManager");
    }

    void OnBackClicked()
    {
        Debug.Log("Back button clicked");

        gameObject.SetActive(false);

        if (pauseGameWhenOpen && !wasGamePaused)
        {
            Time.timeScale = 1f;
            Debug.Log("Game resumed");
        }

        var pausePanel = GameObject.Find("PausePanel");
        if (pausePanel != null && pauseGameWhenOpen)
        {
            pausePanel.SetActive(true);
        }
    }

    void OnResetClicked()
    {
        Debug.Log(" Reset audio settings to defaults");

        if (AudioSettingsManager.Instance != null)
        {
            AudioSettingsManager.Instance.ResetToDefaults();

            if (masterVolumeSlider != null)
                masterVolumeSlider.value = AudioSettingsManager.Instance.masterVolume;

            if (musicVolumeSlider != null)
                musicVolumeSlider.value = AudioSettingsManager.Instance.musicVolume;

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = AudioSettingsManager.Instance.sfxVolume;
        }
    }

    void OnDisable()
    {
        if (pauseGameWhenOpen && !wasGamePaused)
        {
            var pausePanel = GameObject.Find("PausePanel");
            var gameOverPanel = GameObject.Find("GameOverPanel");
            var revivePanel = GameObject.Find("ReviveAdPanel");

            bool anotherPanelActive = (pausePanel != null && pausePanel.activeSelf) ||
                                     (gameOverPanel != null && gameOverPanel.activeSelf) ||
                                     (revivePanel != null && revivePanel.activeSelf);

            if (!anotherPanelActive)
            {
                Time.timeScale = 1f;
            }
        }
    }
}