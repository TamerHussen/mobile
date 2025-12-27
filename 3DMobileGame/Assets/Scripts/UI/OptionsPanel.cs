using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the Options/Settings panel UI
/// Attach to: OptionsPanel GameObject
/// Works in both Pause Menu (Level) and Main Menu scenes
/// </summary>
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
    public bool pauseGameWhenOpen = true; // True for in-game options, false for main menu

    private bool wasGamePaused = false;

    void Awake()
    {
        // Auto-find UI elements if not assigned
        AutoFindUIElements();

        // Bind button events
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);
    }

    void AutoFindUIElements()
    {
        // Find sliders
        if (masterVolumeSlider == null)
            masterVolumeSlider = GameObject.Find("MasterVolumeSlider")?.GetComponent<Slider>();

        if (musicVolumeSlider == null)
            musicVolumeSlider = GameObject.Find("MusicVolumeSlider")?.GetComponent<Slider>();

        if (sfxVolumeSlider == null)
            sfxVolumeSlider = GameObject.Find("SFXVolumeSlider")?.GetComponent<Slider>();

        // Find value texts
        if (masterValueText == null)
            masterValueText = GameObject.Find("MasterValueText")?.GetComponent<TextMeshProUGUI>();

        if (musicValueText == null)
            musicValueText = GameObject.Find("MusicValueText")?.GetComponent<TextMeshProUGUI>();

        if (sfxValueText == null)
            sfxValueText = GameObject.Find("SFXValueText")?.GetComponent<TextMeshProUGUI>();

        // Find buttons by name if not assigned
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
        // Detect if we're in a gameplay scene (pause the game)
        if (pauseGameWhenOpen)
        {
            wasGamePaused = Time.timeScale == 0f;
            Time.timeScale = 0f;
            Debug.Log("⏸️ Game paused - Options panel opened");
        }

        // Sync with AudioSettingsManager
        SyncWithAudioManager();
    }

    void SyncWithAudioManager()
    {
        if (AudioSettingsManager.Instance == null)
        {
            Debug.LogWarning("AudioSettingsManager not found! Options panel cannot control audio.");
            return;
        }

        // Assign sliders to AudioSettingsManager
        AudioSettingsManager.Instance.masterSlider = masterVolumeSlider;
        AudioSettingsManager.Instance.musicSlider = musicVolumeSlider;
        AudioSettingsManager.Instance.sfxSlider = sfxVolumeSlider;

        AudioSettingsManager.Instance.masterValueText = masterValueText;
        AudioSettingsManager.Instance.musicValueText = musicValueText;
        AudioSettingsManager.Instance.sfxValueText = sfxValueText;

        // Re-bind UI elements to AudioSettingsManager
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

        Debug.Log("✅ Options panel synced with AudioSettingsManager");
    }

    void OnBackClicked()
    {
        Debug.Log("⬅️ Back button clicked");

        // Close options panel
        gameObject.SetActive(false);

        // Resume game if it wasn't paused before
        if (pauseGameWhenOpen && !wasGamePaused)
        {
            Time.timeScale = 1f;
            Debug.Log("▶️ Game resumed");
        }

        // If we're in pause menu, re-show pause panel
        var pausePanel = GameObject.Find("PausePanel");
        if (pausePanel != null && pauseGameWhenOpen)
        {
            pausePanel.SetActive(true);
        }
    }

    void OnResetClicked()
    {
        Debug.Log("🔄 Reset audio settings to defaults");

        if (AudioSettingsManager.Instance != null)
        {
            AudioSettingsManager.Instance.ResetToDefaults();

            // Update UI to reflect reset values
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
        // Safety: ensure game is unpaused when panel closes (if appropriate)
        if (pauseGameWhenOpen && !wasGamePaused)
        {
            // Check if another pause panel is active
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