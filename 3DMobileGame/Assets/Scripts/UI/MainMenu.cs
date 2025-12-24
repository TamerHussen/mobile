using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;

    [Header("UI")]
    public Button startButton;
    public TextMeshProUGUI loadingText;

    [Header("Loading Screen")]
    public LoadingScreen loadingScreen;

    private bool isLoading = false;

    void Start()
    {
        Time.timeScale = 1f;

        mainMenuPanel.SetActive(true);
        loadingScreen.Hide();

        if (startButton != null)
            startButton.interactable = true;
    }

    public async void OnStartButton()
    {
        if (isLoading)
            return;

        isLoading = true;

        startButton.interactable = false;
        mainMenuPanel.SetActive(false);
        loadingScreen.Show();

        try
        {
            loadingText.text = "Initializing services...";
            loadingScreen.SetProgress(0.2f);

            await ServiceInitializer.Initialize();

            if (UnityLobbyManager.Instance == null)
                throw new System.Exception("UnityLobbyManager not found");

            loadingText.text = "Creating lobby...";
            loadingScreen.SetProgress(0.5f);

            await UnityLobbyManager.Instance.CreateLobby(3);

            loadingText.text = "Loading lobby...";
            loadingScreen.SetProgress(0.9f);

            await Task.Delay(100); 

            SceneManager.LoadScene("Lobby");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to start game: {e.Message}");

            isLoading = false;

            loadingScreen.Hide();
            mainMenuPanel.SetActive(true);
            startButton.interactable = true;

            loadingText.text = "Failed to start";
        }
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }
}
