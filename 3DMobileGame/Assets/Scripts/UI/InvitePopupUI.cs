using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.UI;

public class InvitePopupUI : MonoBehaviour
{
    public static InvitePopupUI Instance;
    public TextMeshProUGUI statusText;
    public Button acceptButton;
    public Button declineButton;

    public GameObject VisualPanel;

    private string currentJoinCode;

    void Awake() => Instance = this;

    public void Show(string senderId, string joinCode)
    {
        currentJoinCode = joinCode;
        statusText.text = $"Invite received to join Lobby!";
        VisualPanel.SetActive(true);

        acceptButton.onClick.RemoveAllListeners();
        acceptButton.onClick.AddListener(AcceptInvite);

        declineButton.onClick.RemoveAllListeners();
        declineButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    async void AcceptInvite()
    {
        try
        {
            if (UnityLobbyManager.Instance.CurrentLobby != null)
            {
                await LobbyService.Instance.RemovePlayerAsync(
                    UnityLobbyManager.Instance.CurrentLobby.Id,
                    AuthenticationService.Instance.PlayerId);
            }

            // Join the lobby using the code received from the friend
            await LobbyService.Instance.JoinLobbyByCodeAsync(currentJoinCode);
            Debug.Log("Joined lobby successfully!");
            gameObject.SetActive(false);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e.Reason} - {e.Message}");
        }
    }
}
