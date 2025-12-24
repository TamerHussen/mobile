using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using TMPro;
using UnityEngine.UI; // Required for Image components

public class GooglePlayLogin : MonoBehaviour
{
    public static GooglePlayLogin Instance;

    // Assign these TextMeshProUGUI elements in the Unity Inspector
    public TextMeshProUGUI StatusText;
    public TextMeshProUGUI PlayerNameText;
    public TextMeshProUGUI PlayerIdText;
    public Image ProfilePictureImage; 

    // Reference to the panel you want to show/hide
    public GameObject PlayerInfoPanel;

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

    void Start()
    {
        SignIn();
    }

    public void SignIn()
    {
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
    }

    public void ManuallySignIn()
    {
        PlayGamesPlatform.Instance.ManuallyAuthenticate(ProcessAuthentication);
    }

    // After successful Google Play login, sync with SaveManager
    void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            string name = PlayGamesPlatform.Instance.GetUserDisplayName();
            string id = PlayGamesPlatform.Instance.GetUserId();

            // Update UI
            StatusText.text = "Signed In Successfully!";
            PlayerNameText.text = "Name: " + name;
            PlayerIdText.text = "ID: " + id;

            // SYNC WITH SAVEMANAGER
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.data.playerName = name;
                SaveManager.Instance.Save();
                Debug.Log($"Synced Google Play name to SaveManager: {name}");
            }

            if (PlayerInfoPanel != null)
                PlayerInfoPanel.SetActive(true);
        }
    }
}
