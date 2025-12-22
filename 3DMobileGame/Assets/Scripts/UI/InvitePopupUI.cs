using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies;

public class InvitePopupUI : MonoBehaviour
{
    public static InvitePopupUI Instance;
    public TextMeshProUGUI statusText;
    public Button acceptButton;
    public Button declineButton;

    private string currentJoinCode;

    void Awake() => Instance = this;

    public void Show(string senderId, string joinCode)
    {
        currentJoinCode = joinCode;
        statusText.text = $"Invite received to join Lobby!";
        gameObject.SetActive(true);

        acceptButton.onClick.RemoveAllListeners();
        acceptButton.onClick.AddListener(AcceptInvite);

        declineButton.onClick.RemoveAllListeners();
        declineButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    async void AcceptInvite()
    {
        try
        {
            // Join the lobby using the code received from the friend
            await LobbyService.Instance.JoinLobbyByCodeAsync(currentJoinCode);
            Debug.Log("Joined lobby successfully!");
            gameObject.SetActive(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message}");
        }
    }
}
