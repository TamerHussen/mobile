using UnityEngine;
using UnityEngine.UI;

public class LevelSelector : MonoBehaviour
{
    [Header("Level Settings")]
    public string levelName = "Level1";

    [Header("Visual Feedback")]
    public Color selectedColor = Color.green;
    public Color normalColor = Color.white;
    public Color disabledColor = Color.gray;

    private Button button;
    private Image buttonImage;

    void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
    }

    void Start()
    {
        button.onClick.AddListener(() => SelectLevel(levelName));
        UpdateVisual();
    }

    void OnEnable()
    {
        UpdateVisual();
    }

    public void SelectLevel(string level)
    {
        if (UnityLobbyManager.Instance?.CurrentLobby != null)
        {
            string localPlayerId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
            bool isHost = UnityLobbyManager.Instance.CurrentLobby.HostId == localPlayerId;

            if (!isHost)
            {
                Debug.LogWarning("Only the host can change the level");
                return;
            }
        }

        if (LobbyInfo.Instance != null)
        {
            LobbyInfo.Instance.SetSelectedLevel(level);
            Debug.Log($"Selected level: {level}");

            UpdateAllLevelButtons();
        }
    }

    public void UpdateVisual()
    {
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        if (LobbyInfo.Instance == null) return;

        bool isSelected = LobbyInfo.Instance.GetSelectedLevel() == levelName;

        bool isHost = true;
        if (UnityLobbyManager.Instance?.CurrentLobby != null)
        {
            string localPlayerId = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId;
            isHost = UnityLobbyManager.Instance.CurrentLobby.HostId == localPlayerId;
        }

        if (isSelected)
        {
            buttonImage.color = selectedColor;
        }
        else if (!isHost)
        {
            buttonImage.color = disabledColor;
            button.interactable = false;
        }
        else
        {
            buttonImage.color = normalColor;
            button.interactable = true;
        }
    }

    void UpdateAllLevelButtons()
    {
        var allButtons = FindObjectsByType<LevelSelector>(FindObjectsSortMode.None);
        foreach (var btn in allButtons)
        {
            btn.UpdateVisual();
        }
    }
}