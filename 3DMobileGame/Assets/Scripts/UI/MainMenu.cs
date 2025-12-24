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

        // CRITICAL FIX: Ensure SaveManager exists from the start
        EnsureSaveManagerExists();
    }

    // NEW METHOD: Create SaveManager if it doesn't exist
    private void EnsureSaveManagerExists()
    {
        if (SaveManager.Instance == null)
        {
            Debug.Log("SaveManager not found, creating one...");
            GameObject saveManagerObj = new GameObject("SaveManager");
            saveManagerObj.AddComponent<SaveManager>();
            DontDestroyOnLoad(saveManagerObj);

            // Give it a frame to initialize
            Debug.Log("SaveManager created and initialized");
        }
        else
        {
            Debug.Log("SaveManager already exists");
            // Ensure data is loaded
            SaveManager.Instance.Load();
        }
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
            // CRITICAL FIX: Double-check SaveManager exists before proceeding
            loadingText.text = "Preparing player data...";
            loadingScreen.SetProgress(0.1f);
            EnsureSaveManagerExists();

            // Wait a frame to ensure SaveManager is fully initialized
            await Task.Yield();

            loadingText.text = "Initializing services...";
            loadingScreen.SetProgress(0.3f);
            await ServiceInitializer.Initialize();

            // CRITICAL FIX: Sync player names after authentication
            loadingText.text = "Syncing player data...";
            loadingScreen.SetProgress(0.4f);
            if (PlayerNameSynchronizer.Instance != null)
            {
                await PlayerNameSynchronizer.Instance.SyncPlayerName();
            }

            if (UnityLobbyManager.Instance == null)
                throw new System.Exception("UnityLobbyManager not found");

            loadingText.text = "Creating lobby...";
            loadingScreen.SetProgress(0.6f);

            if (SaveManager.Instance == null)
            {
                Debug.LogError("SaveManager is still null! Creating emergency instance.");
                EnsureSaveManagerExists();
                await Task.Yield();
            }

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