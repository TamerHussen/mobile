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

    void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            // Get data from Google Play
            string name = PlayGamesPlatform.Instance.GetUserDisplayName();
            string id = PlayGamesPlatform.Instance.GetUserId();

            // Update UI elements with retrieved information
            StatusText.text = "Signed In Successfully!";
            PlayerNameText.text = "Name: " + name;
            PlayerIdText.text = "ID: " + id;

            // Show the panel that displays this info
            if (PlayerInfoPanel != null)
            {
                PlayerInfoPanel.SetActive(true);
            }

            // Optional: Fetch Profile Picture
            // This requires using Unity's Image component and a separate async method.
            // StartCoroutine(FetchProfilePicture(id)); 
        }
        else
        {
            StatusText.text = "Sign in Failed: " + status;
            PlayerNameText.text = "";
            PlayerIdText.text = "";

            // Hide the panel if login failed
            if (PlayerInfoPanel != null)
            {
                PlayerInfoPanel.SetActive(false);
            }
        }
    }

    // ... (You would need a coroutine here to fetch the profile picture from a URL)
}
