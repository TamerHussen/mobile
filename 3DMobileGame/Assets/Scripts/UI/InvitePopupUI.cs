using NUnit.Framework;
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

                UnityLobbyManager.Instance.CurrentLobby = null;
                LobbyInfo.Instance?.ClearLocalLobby();
            }

            await UnityLobbyManager.Instance.JoinLobbyByCode(currentJoinCode);

            await Task.Delay(500);

            LobbyInfo.Instance.SubscribeToLobby(UnityLobbyManager.Instance.CurrentLobby.Id);

            SceneManager.LoadScene("Lobby");
            Debug.Log("Joined lobby successfully!");
            VisualPanel.SetActive(false);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message}");
        }
    }
}
