using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenuPanel;

    void Start()
    {
        mainMenuPanel.SetActive(true);
        Time.timeScale = 1f;
    }

    public async void OnStartButton()
    {
        // Ensure services are ready
        await ServiceInitializer.Initialize();

        // Ensure lobby manager exists
        if (UnityLobbyManager.Instance == null)
        {
            Debug.LogError("UnityLobbyManager not found in scene!");
            return;
        }

        // Create personal lobby
        await UnityLobbyManager.Instance.CreateLobby(3);

        // Enter lobby scene
        SceneManager.LoadScene("Lobby");
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }
}
