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
        // Show only main menu at start
        mainMenuPanel.SetActive(true);
        levelSelectPanel.SetActive(false);
        pauseMenuPanel.SetActive(false);
        optionsPanel.SetActive(false);
        Time.timeScale = 1f; // ensure game is running
        SetMenuPresence();
    }
    async void SetMenuPresence()
    {
        await FriendsService.Instance.SetPresenceAsync(
            Availability.Online,
            new Activity { Status = "In Menu" }
        );
    }

    // MAIN MENU
    public void OnStartButton()
    {
        SceneManager.LoadScene("Lobby");
    }

    public void OnOptionsButton()
    {
        OpenPanel(optionsPanel, mainMenuPanel);
    }

    public void OnQuitButton()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }

    // PAUSE MENU
    public void OnPauseButton()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f; // Pause the game
    }

    public void OnResumeButton()
    {
        ClosePanel(pauseMenuPanel, null, true); // Resume game
    }

    // GENERIC PANEL HANDLERS
    private void OpenPanel(GameObject panelToOpen, GameObject panelToClose)
    {
        if (panelToClose != null)
            panelToClose.SetActive(false);
        panelToOpen.SetActive(true);
    }

    // Close a panel and can reopen a parent panel
    public void ClosePanel(GameObject panelToClose, GameObject panelToReturnTo = null, bool resumeGame = false)
    {
        panelToClose.SetActive(false);

        if (panelToReturnTo != null)
            panelToReturnTo.SetActive(true);

        if (resumeGame)
            Time.timeScale = 1f; // resume game if paused
    }
}

