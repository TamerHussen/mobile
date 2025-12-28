using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GooglePlayLogin : MonoBehaviour
{
    public static GooglePlayLogin Instance;

    private TextMeshProUGUI StatusText;
    private TextMeshProUGUI PlayerNameText;
    private TextMeshProUGUI PlayerIdText;
    private Image ProfilePictureImage;
    private GameObject PlayerInfoPanel;

    private bool isAuthenticated = false;
    private string cachedPlayerName = "";
    private string cachedPlayerId = "";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        FindUIReferences();
        SignIn();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindUIReferences();

        if (isAuthenticated)
        {
            UpdateUIWithCachedData();
        }
    }

    void FindUIReferences()
    {
        var statusObj = GameObject.Find("GooglePlayStatusText");
        if (statusObj != null) StatusText = statusObj.GetComponent<TextMeshProUGUI>();

        var nameObj = GameObject.Find("GooglePlayPlayerNameText");
        if (nameObj != null) PlayerNameText = nameObj.GetComponent<TextMeshProUGUI>();

        var idObj = GameObject.Find("GooglePlayPlayerIdText");
        if (idObj != null) PlayerIdText = idObj.GetComponent<TextMeshProUGUI>();

        var picObj = GameObject.Find("GooglePlayProfilePicture");
        if (picObj != null) ProfilePictureImage = picObj.GetComponent<Image>();

        var panelObj = GameObject.Find("GooglePlayPlayerInfoPanel");
        if (panelObj != null) PlayerInfoPanel = panelObj;

        Debug.Log($"GooglePlayLogin UI references found: Status={StatusText != null}, Name={PlayerNameText != null}, ID={PlayerIdText != null}");
    }

    void UpdateUIWithCachedData()
    {
        if (StatusText != null)
            StatusText.text = "Signed In Successfully!";

        if (PlayerNameText != null)
            PlayerNameText.text = "Name: " + cachedPlayerName;

        if (PlayerIdText != null)
            PlayerIdText.text = "ID: " + cachedPlayerId;

        if (PlayerInfoPanel != null)
            PlayerInfoPanel.SetActive(true);

        Debug.Log(" Google Play UI updated with cached data");
    }

    public void SignIn()
    {
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
    }

    public void ManuallySignIn()
    {
        PlayGamesPlatform.Instance.ManuallyAuthenticate(ProcessAuthentication);
    }

    void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            string name = PlayGamesPlatform.Instance.GetUserDisplayName();
            string id = PlayGamesPlatform.Instance.GetUserId();

            isAuthenticated = true;
            cachedPlayerName = name;
            cachedPlayerId = id;

            UpdateUIWithCachedData();

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.data.playerName = name;
                SaveManager.Instance.Save();
                Debug.Log($"Synced Google Play name to SaveManager: {name}");
            }
            else
            {
                Debug.LogWarning("SaveManager not found when trying to sync Google Play name");
            }
        }
        else
        {
            Debug.LogWarning($"Google Play authentication failed: {status}");

            if (StatusText != null)
                StatusText.text = "Sign-In Failed: " + status;
        }
    }
}