using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Friends.Notifications;
using Unity.Services.Samples.Friends;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyPresenceListener : MonoBehaviour
{
    private bool isInitialized= false;

    void Awake()
    {

    }
    public void Initialize()
    {
        if (isInitialized) return;

        FriendsService.Instance.PresenceUpdated += OnPresenceUpdated;
        isInitialized = true;
        Debug.Log("LobbyPresenceListener initialized.");
    }
    async void OnPresenceUpdated(IPresenceUpdatedEvent e)
    {
        var presence = e.Presence;
        if (presence == null)
            return;

        var activity = presence.GetActivity<Activity>();
        if (activity == null || activity.Properties == null)
            return;

        if (!activity.Properties.TryGetValue("join_lobby", out var lobbyId))
            return;

        Debug.Log("Auto-joining friend's lobby: " + lobbyId);

        await UnityLobbyManager.Instance.JoinLobbyById(lobbyId);
        LobbyInfo.Instance.SubscribeToLobby(lobbyId);
        SceneManager.LoadScene("Lobby");
    }

    void OnDestroy()
    {
        if (isInitialized && FriendsService.Instance != null)
            FriendsService.Instance.PresenceUpdated -= OnPresenceUpdated;
    }
}
