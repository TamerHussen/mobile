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

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (loadingScreen != null)
            loadingScreen.Hide();

        if (startButton != null)
            startButton.interactable = true;

        EnsureSaveManagerExists();

        DontDestroyOnLoad(loadingScreen.gameObject);

    }

    private void EnsureSaveManagerExists()
    {
        if (SaveManager.Instance == null)
        {
            Debug.Log("SaveManager not found, creating one...");
            GameObject saveManagerObj = new GameObject("SaveManager");
            saveManagerObj.AddComponent<SaveManager>();
            DontDestroyOnLoad(saveManagerObj);

            Debug.Log("SaveManager created and initialized");
        }
        else
        {
            Debug.Log("SaveManager already exists");
            SaveManager.Instance.Load();
        }
    }

    public async void OnStartButton()
    {
        if (isLoading)
            return;

        isLoading = true;

        if (startButton != null)
            startButton.interactable = false;

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        if (loadingScreen != null)
        {
            loadingScreen.Show();
            Debug.Log("Loading screen shown");
        }

        try
        {
            if (loadingText != null)
                loadingText.text = "Preparing player data...";
            if (loadingScreen != null)
                loadingScreen.SetProgress(0.1f);

            EnsureSaveManagerExists();
            await Task.Delay(100);

            if (loadingText != null)
                loadingText.text = "Initializing services...";
            if (loadingScreen != null)
                loadingScreen.SetProgress(0.3f);

            await ServiceInitializer.Initialize();
            await Task.Delay(100);

            if (loadingText != null)
                loadingText.text = "Syncing player data...";
            if (loadingScreen != null)
                loadingScreen.SetProgress(0.4f);

            if (PlayerNameSynchronizer.Instance != null)
            {
                await PlayerNameSynchronizer.Instance.SyncPlayerName();
            }
            await Task.Delay(100);

            if (UnityLobbyManager.Instance == null)
                throw new System.Exception("UnityLobbyManager not found");

            if (loadingText != null)
                loadingText.text = "Creating lobby...";
            if (loadingScreen != null)
                loadingScreen.SetProgress(0.6f);

            if (SaveManager.Instance == null)
            {
                Debug.LogError("SaveManager is still null! Creating emergency instance.");
                EnsureSaveManagerExists();
                await Task.Delay(100);
            }

            await UnityLobbyManager.Instance.CreateLobby(3);
            await Task.Delay(100);

            if (loadingText != null)
                loadingText.text = "Loading lobby...";
            if (loadingScreen != null)
                loadingScreen.SetProgress(0.9f);

            await Task.Delay(200);

            if (loadingScreen != null)
                loadingScreen.SetProgress(1f);

            Debug.Log("All initialization complete, loading Lobby scene");

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Lobby");
            asyncLoad.allowSceneActivation = false;

            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                if (loadingScreen != null)
                    loadingScreen.SetProgress(progress);

                if (asyncLoad.progress >= 0.9f)
                    asyncLoad.allowSceneActivation = true;

                await Task.Yield();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to start game: {e.Message}");
            isLoading = false;

            if (loadingScreen != null)
                loadingScreen.Hide();

            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);

            if (startButton != null)
                startButton.interactable = true;

            if (loadingText != null)
                loadingText.text = "Failed to start";
        }
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }
}