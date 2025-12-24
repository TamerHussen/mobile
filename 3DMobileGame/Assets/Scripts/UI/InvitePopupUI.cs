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
            // 1. Use the manager to leave cleanly (willJoinAnother: true stops personal lobby creation)
            await UnityLobbyManager.Instance.LeaveLobby(willJoinAnother: true);

            // 2. Join the new lobby
            // JoinLobbyByCode now handles GetLocalPlayerData inside it, 
            // so the Host sees your name IMMEDIATELY.
            await UnityLobbyManager.Instance.JoinLobbyByCode(currentJoinCode);

            // 3. Close UI and switch scenes
            VisualPanel.SetActive(false);
            SceneManager.LoadScene("Lobby");

            Debug.Log("Joined lobby successfully via invite!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to accept invite: {e.Message}");
        }
    }

}
