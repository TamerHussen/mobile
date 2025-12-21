using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Samples.Friends;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenuPanel;

    private void Start()
    {
        mainMenuPanel.SetActive(true);
        Time.timeScale = 1f; // ensure game is running
    }

    public void OnStartButton()
    {
        SceneManager.LoadScene("Lobby");
    }
    public void OnQuitButton()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}

