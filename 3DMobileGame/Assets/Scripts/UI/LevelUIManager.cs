using TMPro;
using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Samples.Friends;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelUIManager : MonoBehaviour
{
    public static LevelUIManager Instance;

    [Header("Main Panels")]
    public GameObject HudPanel;
    public GameObject pauseMenuPanel;
    public GameObject optionsPanel;

    [Header("Critical Panels")]
    public GameOverPanel gameOverPanel;
    public ReviveAdPanel reviveAdPanel;

    [Header("Effects")]
    public Image jumpscareImage;
    public TextMeshProUGUI livesText;
    public Image[] lifeIcons;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (HudPanel != null)
            HudPanel.SetActive(true);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.gameObject.SetActive(false);

        if (reviveAdPanel != null)
            reviveAdPanel.gameObject.SetActive(false);

        if (jumpscareImage != null)
            jumpscareImage.gameObject.SetActive(false);

        Time.timeScale = 1f;
        SetMenuPresence();
    }

    async void SetMenuPresence()
    {
        try
        {
            await FriendsService.Instance.SetPresenceAsync(
                Availability.Online,
                new Activity { Status = "In Game" }
            );
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not set presence: {e.Message}");
        }
    }

    public void UpdateLivesDisplay(int currentLives, int maxLives)
    {
        if (livesText != null)
            livesText.text = $"Lives: {currentLives}";

        if (lifeIcons != null)
        {
            for (int i = 0; i < lifeIcons.Length; i++)
            {
                if (lifeIcons[i] != null)
                    lifeIcons[i].enabled = i < currentLives;
            }
        }
    }

    public bool IsCriticalPanelActive()
    {
        return (gameOverPanel != null && gameOverPanel.gameObject.activeSelf) ||
               (reviveAdPanel != null && reviveAdPanel.gameObject.activeSelf);
    }

    public void OnPauseButton()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            Time.timeScale = 0f;
            Debug.Log("Game paused");
        }
    }

    public void OnResumeButton()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (!IsCriticalPanelActive())
        {
            Time.timeScale = 1f;
            Debug.Log("Game resumed");
        }
    }

    public void OpenPanel(GameObject panelToOpen, GameObject panelToClose)
    {
        if (panelToClose != null)
            panelToClose.SetActive(false);

        if (panelToOpen != null)
            panelToOpen.SetActive(true);
    }

    public void ClosePanel(GameObject panel, bool resumeGame)
    {
        if (panel != null)
            panel.SetActive(false);

        if (resumeGame && !IsCriticalPanelActive())
            Time.timeScale = 1f;
    }

    public void OnQuitButton()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.Save();

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void ShowGameOver(int score, int coins)
    {
        if (gameOverPanel == null)
        {
            Debug.LogError("GameOverPanel missing!");
            return;
        }

        gameOverPanel.gameObject.SetActive(true);
        gameOverPanel.ShowDefeat(score, coins);
    }

    public void OnOptionsButton()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }
    public void OnReturnButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Lobby");
    }
    public void ShowRevive()
    {
        if (reviveAdPanel == null)
        {
            Debug.LogError("ReviveAdPanel missing!");
            return;
        }

        reviveAdPanel.gameObject.SetActive(true);
    }

}
