using NUnit.Framework;
using System.Collections.Generic;
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
            LobbyInfo.Instance.SetPlayers(new List<LobbyPlayer>());

            if (UnityLobbyManager.Instance.CurrentLobby != null)
            {
                await UnityLobbyManager.Instance.LeaveLobby();
            }

            await UnityLobbyManager.Instance.JoinLobbyByCode(currentJoinCode);
            LobbyInfo.Instance.SubscribeToLobby(UnityLobbyManager.Instance.CurrentLobby.Id);
            UnityLobbyManager.Instance.SyncLobbyToLocal();

            SceneManager.LoadScene("Lobby");
            Debug.Log("Joined lobby successfully!");
            gameObject.SetActive(false);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message}");
        }
    }
}
