using Unity.Services.Friends;
using Unity.Services.Friends.Models;
using Unity.Services.Friends.Notifications;
using Unity.Services.Samples.Friends;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyPresenceListener : MonoBehaviour
{
    void Awake()
    {
        FriendsService.Instance.PresenceUpdated += OnPresenceUpdated;
    }

    async void OnPresenceUpdated(IPresenceUpdatedEvent e)
    {
        var presence = e.Presence;
        if (presence == null)
            return;

        var activity = presence.GetActivity<LobbyActivity>();
        if (activity == null || string.IsNullOrEmpty(activity.join_lobby))
            return;

        string lobbyId = activity.join_lobby;

        Debug.Log("Auto-joining friend's lobby: " + lobbyId);

        await UnityLobbyManager.Instance.JoinLobbyById(lobbyId);
        SceneManager.LoadScene("Lobby");
    }

    void OnDestroy()
    {
        if (FriendsService.Instance != null)
            FriendsService.Instance.PresenceUpdated -= OnPresenceUpdated;
    }
}
