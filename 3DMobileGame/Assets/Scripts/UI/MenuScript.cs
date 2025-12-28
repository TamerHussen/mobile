using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Samples.Friends;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject levelSelectPanel;
    public GameObject pauseMenuPanel;
    public GameObject optionsPanel;

    private void Start()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (levelSelectPanel != null)
            levelSelectPanel.SetActive(false);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        Time.timeScale = 1f;
        SetMenuPresence();
    }

    async void SetMenuPresence()
    {
        try
        {
            await FriendsService.Instance.SetPresenceAsync(
                Availability.Online,
                new Activity { Status = "In Menu" }
            );
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not set presence: {e.Message}");
        }
    }

    public void OnStartButton()
    {
        SceneManager.LoadScene("Lobby");
    }

    public void OnOptionsButton()
    {
        bool isPauseMenu = pauseMenuPanel != null && pauseMenuPanel.activeSelf;

        if (isPauseMenu)
        {
            OpenPanel(optionsPanel, pauseMenuPanel);
        }
        else
        {
            OpenPanel(optionsPanel, mainMenuPanel);
        }
    }

    public void OnQuitButton()
    {
        Debug.Log("Quitting game...");

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
        }

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
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
        ClosePanel(pauseMenuPanel, null, true);
        Debug.Log("▶️ Game resumed");
    }

    public void OnBackButton()
    {
        Debug.Log("Returning to lobby from pause menu...");

        Time.timeScale = 1f;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
        }

        if (PlayerScore.Instance != null)
        {
            int currentScore = PlayerScore.Instance.Score;
            int coinsEarned = Mathf.FloorToInt(currentScore * 0.025f);

            if (CoinsManager.Instance != null && coinsEarned > 0)
            {
                CoinsManager.Instance.AddCoins(coinsEarned);
                Debug.Log($"Awarded {coinsEarned} coins for partial progress");
            }
        }

        SceneManager.LoadScene("Lobby");
    }

    private void OpenPanel(GameObject panelToOpen, GameObject panelToClose)
    {
        if (panelToClose != null)
            panelToClose.SetActive(false);

        if (panelToOpen != null)
            panelToOpen.SetActive(true);
    }

    public void ClosePanel(GameObject panelToClose, GameObject panelToReturnTo = null, bool resumeGame = false)
    {
        if (panelToClose != null)
            panelToClose.SetActive(false);

        if (panelToReturnTo != null)
            panelToReturnTo.SetActive(true);

        if (resumeGame)
        {
            Time.timeScale = 1f;
        }
    }
}