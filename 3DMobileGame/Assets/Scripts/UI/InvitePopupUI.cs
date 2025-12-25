using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InvitePopupUI : MonoBehaviour
{
    public static InvitePopupUI Instance;
    public TextMeshProUGUI statusText;
    public Button acceptButton;
    public Button declineButton;

    public GameObject VisualPanel;

    private string currentJoinCode;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Show(string senderId, string joinCode)
    {
        currentJoinCode = joinCode;
        statusText.text = $"Invite received to join Lobby!";
        VisualPanel.SetActive(true);

        acceptButton.onClick.RemoveAllListeners();
        acceptButton.onClick.AddListener(AcceptInvite);

        declineButton.onClick.RemoveAllListeners();
        declineButton.onClick.AddListener(() => VisualPanel.SetActive(false));
    }

    async void AcceptInvite()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager is null when accepting invite!");
            statusText.text = "Error: Game not ready";
            return;
        }

        SaveManager.Instance.Load();
        Debug.Log($"Loaded player data: Name={SaveManager.Instance.data.playerName}, Cosmetic={SaveManager.Instance.data.selectedCosmetic}");

        try
        {
            statusText.text = "Joining lobby...";
            acceptButton.interactable = false;

            if (UnityLobbyManager.Instance.CurrentLobby != null)
            {
                Debug.Log("Leaving current lobby...");

                // Unsubscribe from events
                LobbyInfo.Instance?.UnsubscribeFromLobby();

                // Remove from lobby
                await LobbyService.Instance.RemovePlayerAsync(
                    UnityLobbyManager.Instance.CurrentLobby.Id,
                    AuthenticationService.Instance.PlayerId
                );

                // Clear local state
                UnityLobbyManager.Instance.CurrentLobby = null;
                LobbyInfo.Instance?.ClearLocalLobby();

                await Task.Delay(300);
            }

            // Set flag before joining
            UnityLobbyManager.Instance.IsJoiningExternalLobby = true;

            // Join new lobby
            await UnityLobbyManager.Instance.JoinLobbyByCode(currentJoinCode);

            await Task.Delay(500);

            if (UnityLobbyManager.Instance.CurrentLobby == null)
            {
                Debug.LogError("Failed to join lobby - CurrentLobby is null!");
                statusText.text = "Failed to join lobby";
                acceptButton.interactable = true;
                UnityLobbyManager.Instance.IsJoiningExternalLobby = false;
                return;
            }

            Debug.Log($"Successfully joined lobby: {UnityLobbyManager.Instance.CurrentLobby.Id}");

            // Load lobby scene
            SceneManager.LoadScene("Lobby");

            Debug.Log("✅ Joined lobby successfully!");
            VisualPanel.SetActive(false);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message}");
            statusText.text = "Failed to join lobby";
            acceptButton.interactable = true;
            UnityLobbyManager.Instance.IsJoiningExternalLobby = false;
        }
    }
}